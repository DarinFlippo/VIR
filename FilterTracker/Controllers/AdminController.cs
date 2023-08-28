using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;
using System.Web.Mvc;

using FilterTracker.Models;

//using static FilterTracker.XLSImporter;

namespace FilterTracker.Controllers
{


	public partial class AdminController : ControllerBase
	{
		[HttpGet]
		[Authorize(Roles = ("OrganizationAdmins,SuperUsers,Users,Physician"))]
		public ActionResult CreateRetrievalDateOverdueTasks()
		{
			DateTime now = DateTime.UtcNow;
			string model = "";

			try
			{
				if (db.Database.Connection.State != ConnectionState.Open)
				{
					db.Database.Connection.Open();
				}

				using (var cmd = db.Database.Connection.CreateCommand())
				{

					cmd.CommandText = "EvaluateAndCreateTasks";
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.ExecuteNonQuery();
				}
				model = "OK";

				db.Database.Connection.Close();
			}
			catch (Exception ex)
			{
				Logger.LogException(ex);
				model = ex.Message;
			}

			return View("CreateRetrievalDateOverdueTasks", (object)model);
		}

		[HttpGet]
		[Authorize(Roles = "OrganizationAdmins,SuperUsers,Users,Physician")]
		public ActionResult GetMIPSNumbers()
		{
			var model = new GetMIPSNumbersModel();
			model.IsCalculated = false;

			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = "OrganizationAdmins,SuperUsers,Users,Physician")]
		public ActionResult GetMIPSNumbers(DateTime? start, DateTime? end, int SelectedOrganizationId)
		{
			var model = new GetMIPSNumbersModel();
			model.IsCalculated = true;
			/*
				 -(d in the chart): normally would be filters with intent to removed placed from 1/1 to 9/30 of a given year. 
				 My current clinic data will be 10/1/20-12/31/20 or just all dates(up to you).  
				 Then subtract if patients deceased less than 3 months after filter placement. This will give you the “d” number.

				-a1=Out of the patients selected above( d), calculate number of filters removed within 3 months of placement.

				-a2= out of patients that did not have filter removed from a1, calculate number of filters that were 
				reassessed(have a completed review case) in <3 months after filter placement. 

				-a3= (this one will be a little weird for us since all filters will have been reassessed). 
				out of the patients that were not assessed(did not have a  completed review case), how many 
				were contacted at least twice? This will likely always be zero, 
				because all patients will be in in a2 value.

				-c= out of a3, how many did not get contacted twice or have a complete review case?
			 */

			var query = db.PatientFilters
				.Where(w => w.IsTemporary == true)
				.Where(w => w.ProcedureDate.HasValue)
				.Where(w => w.OrganizationId == SelectedOrganizationId);

			if (start.HasValue)
			{
				start = start.Value.Date;
				query = query.Where(w => w.ProcedureDate.Value >= start.Value);
			}

			if (end.HasValue)
			{
				end = end.Value.Date;
				query = query.Where(w => w.ProcedureDate.Value <= end.Value);
			}

			foreach (PatientFilter pf in query)
			{
				// We will need this date a lot.
				var cutoffDate = pf.ProcedureDate.Value.AddMonths(3);

				if (pf.Patient.DeceasedDate.HasValue && pf.Patient.DeceasedDate.Value < cutoffDate)
				{
					continue;
				}
				model.Denominator++;
				model.DPatients.Add(pf.Patient);

				if (pf.ActualRemovalDate.HasValue && pf.ActualRemovalDate.Value < cutoffDate)
				{
					// N1
					// Out of the patients selected above( d), calculate 
					// number of filters removed within 3 months of placement.
					model.N1++;
					model.N1Patients.Add(pf.Patient);
					continue;
				}

				// N2
				var reviewcases = pf.Tasks.Where(w => w.TaskTypeId == (int)TaskTypes.ReviewCase);
				if (reviewcases.Count(a => a.ClosedDate < cutoffDate) > 0)
				{
					model.N2++;
					model.N2Patients.Add(pf.Patient);
					continue;
				}

				// N3
				if (pf.PatientContactAttempts.Count(c => c.Timestamp < cutoffDate) >= 2)
				{
					model.N3++;
					model.N3Patients.Add(pf.Patient);
					continue;
				}

				// N4
				model.N4++;
				model.N4Patients.Add(pf.Patient);
			}

			return View(model);
		}

		[HttpGet]
		[Authorize(Roles = ("SuperUsers,OrganizationAdmins,Physician"))]
		public ActionResult Index()
		{
			Dictionary<string, string> model = new Dictionary<string, string>();

			model.Add("Organization List", "OrganizationList");
			model.Add("Filter List", "FilterList");
			model.Add("User List", "UserList");
			model.Add("Export Organization Data", "ExportOrgData");
			model.Add("Import Patient Data", "ImportPatientData");
			model.Add("Get MIPS Numbers", "GetMIPSNumbers");
			model.Add("Interval Report", "IntervalData");

			return View(model);
		}

		[HttpGet]
		[Authorize(Roles = ("SuperUsers"))]
		public async Task<ActionResult> OrganizationList()
		{
			List<Organization> model = null;

			try
			{
				int offset = CurrentUser().Organization.TimezoneOffset;
				var items = await db.Organizations.AsNoTracking().OrderBy(o => o.Name).ToListAsync();
				items.ForEach(f =>
				{
					f.CreateTimestamp = f.CreateTimestamp.AddHours(offset);
					f.UpdateTimestamp = f.UpdateTimestamp.AddHours(offset);
				});

				model = items;
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
			}

			return View(model);
		}


		[HttpGet]
		[Authorize(Roles = ("SuperUsers,OrganizationAdmins,Physician"))]
		public ActionResult ResetPassword()
		{
			ResetPasswordViewModel model = null;
			try
			{
				model = new ResetPasswordViewModel(User.Identity.Name, User.IsInRole("SuperUsers") ? "SuperUsers" : "OrganizationAdmins");
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
			}

			return View(model);

		}

		[HttpPost]
		[Authorize(Roles = ("SuperUsers,OrganizationAdmins,Physician"))]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
		{
			try
			{
				if (!ModelState.IsValid)
				{
					var errors = GetModelsStateErrors(ModelState);
					model.Message = BuildErrorListElement(errors);
					return View(model);
				}

				model.Message = "";
				int user_id = model.SelectedUserId;
				int organization_id = model.SelectedOrganizationId;
				string new_password = model.Password;

				var user = await db.Users.SingleOrDefaultAsync(sd => sd.Id == user_id);

				if (user != null && user.Id == model.SelectedUserId)
				{
					PasswordHash hash = new PasswordHash(model.Password);
					byte[] hashBytes = hash.ToArray();
					user.PasswordHash = hashBytes;

					await db.SaveChangesAsync();

					model = new ResetPasswordViewModel(User.Identity.Name, User.IsInRole("SuperUsers") ? "SuperUsers" : "OrganizationAdmins");
					model.Message = "Password was successfully reset.";
				}
				else
				{
					model = new ResetPasswordViewModel(User.Identity.Name, User.IsInRole("SuperUsers") ? "SuperUsers" : "OrganizationAdmins");
					model.Message = "Unknown user.";
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return View(model);
		}

		[HttpGet]
		[Authorize(Roles = ("SuperUsers, OrganizationAdmins,Physician"))]
		public async Task<ActionResult> PhysicianList()
		{
			List<Physician> model = null;

			try
			{
				User current = CurrentUser(); // inlining this directly below throws async exception, leave this here.

				model = await db.Physicians
					.AsNoTracking()
					.Where(w => w.OrganizationId == current.OrganizationId)
					.OrderBy(o => o.Name)
					.ToListAsync();
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
			}

			return View(model);
		}

		[Authorize(Roles = ("SuperUsers,OrganizationAdmins,Physician"))]
		[HttpGet]
		public async Task<ActionResult> CreatePhysician()
		{
			PhysicianEditorModel model = null;

			try
			{
				model = new PhysicianEditorModel();

				model.Active = true;
				var user = CurrentUser();
				model.OrganizationId = user.OrganizationId;
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = ("SuperUsers,OrganizationAdmins,Physician"))]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> CreatePhysician(PhysicianEditorModel model)
		{
			DateTime now = DateTime.UtcNow;
			var cu = CurrentUser();

			bool isSuper = false;
			var roles = cu.UserRoles.Select(s => s.Role.Name).ToList();
			isSuper = roles.Contains(Roles.SuperUsers);

			try
			{
				if (!ModelState.IsValid)
				{
					var errors = GetModelsStateErrors(ModelState);
					model.ErrorMessage = BuildErrorListElement(errors);
					return View(model);
				}
				else
				{
					Physician entity = new Physician();

					entity.Name = model.Name.Trim();
					if (isSuper)
						entity.OrganizationId = model.OrganizationId;
					else
						entity.OrganizationId = cu.OrganizationId;

					entity.Email = model.Email.Trim();
					entity.Phone = model.Phone.Trim();
					entity.Fax = string.IsNullOrEmpty(model.Fax) ? null : model.Fax.Trim();
					entity.Active = true;
					entity.RequiresRemovalApproval = model.RequiresRemovalApproval;

					entity.CreateTimestamp = now;
					entity.UpdateTimestamp = now;
					entity.CreateUserId = cu.Id;
					entity.UpdateUserId = entity.CreateUserId;

					db.Physicians.Add(entity);
					await db.SaveChangesAsync();
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				model.ErrorMessage = GetErrorMessage(now);
			}

			return RedirectToAction("PhysicianList");

		}


		[Authorize(Roles = ("SuperUsers,OrganizationAdmins,Physician"))]
		[HttpGet]
		public ActionResult EditPhysician(int id)
		{
			var model = new PhysicianEditorModel();

			var cu = CurrentUser();

			try
			{
				Physician p = db.Physicians.SingleOrDefault(sd => sd.Id == id && sd.OrganizationId == cu.OrganizationId);
				if (p != null)
				{
					model.Id = p.Id;
					model.OrganizationId = p.OrganizationId;
					model.Name = p.Name;
					model.Email = p.Email;
					model.Fax = p.Fax;
					model.Phone = p.Phone;
					model.Active = p.Active;
					model.RequiresRemovalApproval = p.RequiresRemovalApproval;
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}
			return View(model);
		}

		[Authorize(Roles = ("SuperUsers,OrganizationAdmins,Physician"))]
		[HttpGet]
		public ActionResult DeactivatePhysician(int id)
		{

			var cu = CurrentUser();
			bool isSuper = false;
			var roles = cu.UserRoles.Select(s => s.Role.Name).ToList();
			isSuper = roles.Contains(Roles.SuperUsers);

			try
			{
				Physician physician;

				if (!isSuper)
					physician = db.Physicians.SingleOrDefault(sd => sd.Id == id && sd.OrganizationId == cu.OrganizationId);
				else
					physician = db.Physicians.SingleOrDefault(sd => sd.Id == id);


				if (physician != null && physician.Id == id)
				{
					physician.Active = false;
					physician.UpdateTimestamp = DateTime.UtcNow;
					physician.UpdateUserId = cu.Id;

					db.SaveChanges();
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
			}

			return RedirectToAction("PhysicianList");
		}


		[HttpPost]
		[Authorize(Roles = ("SuperUsers,OrganizationAdmins,Physician"))]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> EditPhysician(PhysicianEditorModel model)
		{

			var cu = CurrentUser();
			bool isSuper = false;
			var roles = cu.UserRoles.Select(s => s.Role.Name).ToList();
			isSuper = roles.Contains(Roles.SuperUsers);
			try
			{
				if (!ModelState.IsValid)
				{
					var errors = GetModelsStateErrors(ModelState);
					model.ErrorMessage = BuildErrorListElement(errors);
					return View(model);
				}
				else
				{
					Physician entity;

					if (isSuper)
						entity = db.Physicians.SingleOrDefault(sd => sd.Id == model.Id);
					else
						entity = db.Physicians.SingleOrDefault(sd => sd.Id == model.Id && cu.OrganizationId == cu.OrganizationId);

					if (entity != null)
					{

						string temp = model.Name.Trim();
						if (entity.Name != temp)
						{
							entity.Name = temp;
						}

						temp = model.Email.Trim();
						if (entity.Email != temp)
						{
							entity.Email = temp;
						}

						temp = model.Phone.Trim();
						if (entity.Phone != temp)
						{
							entity.Phone = temp;
						}

						if (string.IsNullOrEmpty(model.Fax))
						{
							entity.Fax = null;
						}
						else
						{
							temp = model.Fax.Trim();
							if (entity.Fax != temp)
							{
								entity.Fax = temp;
							}
						}

						if (entity.Active != model.Active)
						{
							entity.Active = model.Active;
						}

						entity.RequiresRemovalApproval = model.RequiresRemovalApproval;

						entity.UpdateUserId = cu.Id;
						entity.UpdateTimestamp = DateTime.UtcNow;

						await db.SaveChangesAsync();
					}
					else
					{
						model.ErrorMessage = "Not found.";
					}
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return RedirectToAction("PhysicianList");

		}


		[HttpGet]
		[Authorize(Roles = ("SuperUsers"))]
		public ActionResult CreateOrganization()
		{
			OrganizationEditorViewModel model = null;

			try
			{
				model = new OrganizationEditorViewModel();
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = ("SuperUsers"))]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> CreateOrganization(OrganizationEditorViewModel model)
		{
			DateTime now = DateTime.UtcNow;
			var cu = CurrentUser();
			if (!ModelState.IsValid)
			{
				var errors = GetModelsStateErrors(ModelState);
				model.ErrorMessage = BuildErrorListElement(errors);
				return View(model);
			}
			else
			{
				try
				{
					Organization organization_entity = new Organization();

					organization_entity.HydrateFrom(model);
					organization_entity.CreateUserId = cu.Id;
					organization_entity.UpdateUserId = cu.Id;

					db.Organizations.Add(organization_entity);

					OrganizationDefault od = new OrganizationDefault();
					od.OrganizationId = model.OrganizationId;

					int i;

					if (int.TryParse(model.ContactAttemptsBeforeRegisteredLetter, out i))
						od.ContactAttemptsBeforeRegisteredLetter = i;

					if (int.TryParse(model.FilterAge, out i))
						od.FilterAge = i;

					if (int.TryParse(model.PatientContactDays, out i))
						od.PatientContactDays = i;

					if (int.TryParse(model.ReassessDays, out i))
						od.ReassessDays = i;

					od.CreateTimestamp = now;
					od.UpdateTimestamp = now;
					od.CreateUserId = cu.Id;
					od.UpdateUserId = cu.Id;

					db.OrganizationDefaults.Add(od);

					await db.SaveChangesAsync();
				}
				catch (Exception ex)
				{
					LogException(ex, $"UserId: {cu.Id}");
				}
			}

			return RedirectToAction("OrganizationList");

		}

		[HttpGet]
		[Authorize(Roles = ("SuperUsers"))]
		public ActionResult EditOrganization(int id)
		{
			OrganizationEditorViewModel model = null;

			try
			{
				model = new OrganizationEditorViewModel();

				Organization entity = db.Organizations.SingleOrDefault(sd => sd.Id == id);
				if (entity != null && entity.Id == id)
				{
					model.Active = entity.Active;
					model.AddressLine1 = entity.AddressLine1;
					model.AddressLine2 = entity.AddressLine2;
					model.City = entity.City;
					model.ContactEmail = entity.ContactEmail;
					model.ContactName = entity.ContactName;
					model.ContactPhone = entity.ContactPhone;
					model.Description = entity.Description;
					model.MaxUsers = entity.MaxUsers;
					model.Name = entity.Name;
					model.OrganizationId = entity.Id;
					model.State = entity.State;
					model.Zipcode = entity.Zipcode;

					OrganizationDefault od = entity.OrganizationDefaults.SingleOrDefault();
					if (od != null && od.Id != 0)
					{
						model.ContactAttemptsBeforeRegisteredLetter = od.ContactAttemptsBeforeRegisteredLetter.HasValue ? od.ContactAttemptsBeforeRegisteredLetter.Value.ToString() : "";
						model.FilterAge = od.FilterAge.HasValue ? od.FilterAge.Value.ToString() : "";
						model.ReassessDays = od.ReassessDays.HasValue ? od.ReassessDays.Value.ToString() : "";
						model.PatientContactDays = od.PatientContactDays.HasValue ? od.PatientContactDays.Value.ToString() : "";
					}

				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = ("SuperUsers"))]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> EditOrganization(OrganizationEditorViewModel model)
		{
			DateTime now = DateTime.UtcNow;

			if (!ModelState.IsValid)
			{
				var errors = GetModelsStateErrors(ModelState);
				model.ErrorMessage = BuildErrorListElement(errors);
				return View(model);
			}

			try
			{
				if (model != null)
				{
					if (model.OrganizationId > 0)
					{
						Organization organization_entity = await db.Organizations.SingleOrDefaultAsync(sda => sda.Id == model.OrganizationId);
						if (organization_entity != null && organization_entity.Id > 0)
						{
							organization_entity.UpdateFrom(model);
							organization_entity.UpdateUserId = CurrentUser().Id;

							bool updating = false;
							OrganizationDefault od;
							if (organization_entity.OrganizationDefaults != null && organization_entity.OrganizationDefaults.Any())
							{
								od = organization_entity.OrganizationDefaults.SingleOrDefault();
								updating = true;
							}
							else
							{
								od = new OrganizationDefault();
								od.CreateTimestamp = now;
								od.CreateUserId = CurrentUser().Id;
								od.OrganizationId = organization_entity.Id;
							}
							od.UpdateTimestamp = now;
							od.UpdateUserId = CurrentUser().Id;

							int i;
							if (int.TryParse(model.ContactAttemptsBeforeRegisteredLetter, out i))
								od.ContactAttemptsBeforeRegisteredLetter = i;

							if (int.TryParse(model.FilterAge, out i))
								od.FilterAge = i;

							if (int.TryParse(model.PatientContactDays, out i))
								od.PatientContactDays = i;

							if (int.TryParse(model.ReassessDays, out i))
								od.ReassessDays = i;

							if (!updating)
								db.OrganizationDefaults.Add(od);

							await db.SaveChangesAsync();
						}
					}
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return RedirectToAction("OrganizationList");
		}

		[HttpPost]
		[Authorize(Roles = ("SuperUsers"))]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> DeleteOrganization(int? id)
		{
			try
			{
				if (id.HasValue)
				{
					if (id.Value > 0)
					{
						Organization organization_entity = await db.Organizations.SingleOrDefaultAsync(sda => sda.Id == id.Value);
						if (organization_entity != null && organization_entity.Id > 0)
						{
							db.Organizations.Remove(organization_entity);

							await db.SaveChangesAsync();
						}
					}

				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
			}

			return View();
		}

		[HttpGet]
		[Authorize(Roles = ("SuperUsers,OrganizationAdmins,Physician"))]
		public async Task<ActionResult> UserList()
		{
			List<User> model = null;

			var cu = CurrentUser();
			try
			{


				if (User.IsInRole("OrganizationAdmins"))
				{
					model = await db.Users.Where(w => w.OrganizationId == cu.OrganizationId).ToListAsync();
				}
				else
				{
					model = await db.Users.OrderBy(o => o.OrganizationId).ThenBy(o => o.Email).ToListAsync();
				}

				// Remove the logged on users User from the model to disallow self editing.
				model.RemoveAll(ra => ra.Email == cu.Email);
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
			}

			return View(model);
		}

		[HttpGet]
		[Authorize(Roles = ("SuperUsers,OrganizationAdmins,Physician"))]
		public async Task<JsonResult> UsersByOrganization(int id)
		{
			Organization org;
			var cu = CurrentUser();
			if (User.IsInRole("OrganizationAdmins"))
			{
				org = await db.Organizations.AsNoTracking().Where(w => w.Id == cu.OrganizationId).SingleOrDefaultAsync();
			}
			else
			{
				org = await db.Organizations.AsNoTracking().Where(w => w.Id == id).SingleOrDefaultAsync();
			}

			var returned = org.Users.Select(s => new { Key = s.Email, Value = s.Id.ToString() }).ToArray();

			return new JsonResult() { Data = returned, ContentType = "application/json", JsonRequestBehavior = JsonRequestBehavior.AllowGet };
		}


		[HttpGet]
		[Authorize(Roles = ("OrganizationAdmins,SuperUsers,Physician"))]
		public async Task<ActionResult> CreateUser()
		{
			var model = new CreateUserModel();
			var user = CurrentUser();

			var isSuper = User.IsInRole(Roles.SuperUsers);

			try
			{


				//var user = await db.Users.SingleOrDefaultAsync(sd => sd.Email == User.Identity.Name);
				//var id = user.OrganizationId;
				if (!isSuper)
				{
					string orgid = user.OrganizationId.ToString();
					model.Organizations.RemoveAll(ra => ra.Value != orgid);
				}

				model.Roles.Clear();
				var item = new SelectListItem() { Text = "Users", Value = "1" };
				if (model.SelectedRoles.Contains("1"))
					item.Selected = true;

				model.Roles.Add(item);

				item = new SelectListItem() { Text = "Physician", Value = "4" };
				if (model.SelectedRoles.Contains("4"))
					item.Selected = true;

				model.Roles.Add(item);

				if (isSuper)
				{
					item = new SelectListItem() { Text = "Organization Admin", Value = "2" };
					model.Roles.Add(item);
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = ("OrganizationAdmins,SuperUsers,Physician"))]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> CreateUser(CreateUserModel model)
		{
			DateTime now = DateTime.UtcNow;

			var cu = CurrentUser();

			if (!CurrentUserIsInRole(Roles.SuperUsers) && cu.OrganizationId != model.OrganizationId)
			{
				model.ErrorMessage = "Not authorized.";
				return View(model);
			}


			try
			{
				if (!ModelState.IsValid)
				{
					var errors = GetModelsStateErrors(ModelState);
					model.Message = BuildErrorListElement(errors);
					return View(model);
				}

				if (model.OrganizationId == cu.OrganizationId || User.IsInRole("SuperUsers"))
				{
					if (db.Users.Count(c => c.Email == model.Email) == 0)
					{
						User user = new User();

						user.AccessFailedCount = 0;
						user.Active = model.Active;
						user.EmailConfirmed = false;
						user.FirstName = model?.FirstName.Trim();
						user.LastName = model?.LastName.Trim();
						user.Email = model.Email.Trim().ToLower();
						user.CreateTimestamp = now;
						user.UpdateTimestamp = user.CreateTimestamp;
						user.CreateUserId = cu.Id;
						user.UpdateUserId = cu.Id;
						user.DefaultReviewer = model.DefaultReviewer;
						user.OrganizationId = model.OrganizationId;

						PasswordHash hash = new PasswordHash(model.Password);
						byte[] hashBytes = hash.ToArray();
						user.PasswordHash = hashBytes;


						db.Users.Add(user);

						await db.SaveChangesAsync();

						if (user.Id > 0)
						{

							if (user.DefaultReviewer)
							{
								var prior_default = db.Users.Where(w => w.OrganizationId == user.OrganizationId)
								.Where(w => w.DefaultReviewer == true)
								.Where(w => w.Id != user.Id);

								if (prior_default != null)
								{
									foreach (var item in prior_default)
									{
										item.DefaultReviewer = false;
									}
								}

								// should only ever be 1 user max.
								await db.SaveChangesAsync();
							}

							var allroles = db.Roles.AsNoTracking().ToList();
							List<int> roles = allroles
										.Select(s => s.Id)
										.ToList();

							if (!CurrentUserIsInRole(Roles.SuperUsers))
							{
								var orgadmin = allroles.Single(s => s.Name == Roles.OrgAdmins);
								roles.Remove(orgadmin.Id);
							}


							foreach (string role in model.SelectedRoles)
							{

								if (int.TryParse(role, out int role_id))
								{
									if (roles.Contains(role_id))
									{
										UserRole added = new UserRole()
										{
											RoleId = role_id,
											UserId = user.Id,
											CreateUserId = cu.Id,
											UpdateUserId = cu.Id,
											CreateTimestamp = now,
											UpdateTimestamp = now
										};

										db.UserRoles.Add(added);
									}
								}
							}

							await db.SaveChangesAsync();
						}
					}
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);

				return View(model);
			}

			return RedirectToAction("UserList");
		}

		[HttpGet]
		[Authorize(Roles = ("OrganizationAdmins,SuperUsers,Physician"))]
		public async Task<ActionResult> EditUser(int id)
		{
			var cu = CurrentUser();

			try
			{
				User edited_user = await db.Users.SingleOrDefaultAsync(sd => sd.Id == id && sd.OrganizationId == cu.OrganizationId);
				var model = new UserEditorModel();

				if (edited_user != null && edited_user.Id == id)
				{
					model.SelectedRoles = edited_user.UserRoles.Select(s => s.Role.Id.ToString()).ToArray();

					if (User.IsInRole("OrganizationAdmins"))
					{
						model.Roles.Clear();
						var item = new SelectListItem() { Text = "Users", Value = "1" };
						if (model.SelectedRoles.Contains("1"))
							item.Selected = true;

						model.Roles.Add(item);

						item = new SelectListItem() { Text = "Physician", Value = "4" };
						if (model.SelectedRoles.Contains("4"))
							item.Selected = true;

						model.Roles.Add(item);

						model.Organizations.Clear();

						var u = CurrentUser();
						model.Organizations.Add(new SelectListItem() { Value = u.OrganizationId.ToString(), Text = u.Organization.Name, Selected = true });
					}

					model.Id = edited_user.Id;
					model.Active = edited_user.Active;
					model.Email = edited_user.Email;
					model.OrganizationId = edited_user.OrganizationId;
					model.FirstName = edited_user.FirstName;
					model.LastName = edited_user.LastName;
					model.DefaultReviewer = edited_user.DefaultReviewer;
					return View(model);
				}
				else
				{
					model.ErrorMessage = "Not found.";
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
				//model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return RedirectToAction("UserList");
		}

		[HttpPost]
		[Authorize(Roles = ("OrganizationAdmins,SuperUsers,Physician"))]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> EditUser(UserEditorModel model)
		{
			var cu = CurrentUser();

			if (!CurrentUserIsInRole(Roles.SuperUsers) && cu.OrganizationId != model.OrganizationId)
			{
				model.ErrorMessage = "Not authorized.";
				return View(model);
			}

			try
			{
				if (!ModelState.IsValid)
				{
					if (!ModelState.IsValid)
					{
						var errors = GetModelsStateErrors(ModelState);
						model.Message = BuildErrorListElement(errors);
						return View(model);
					}
				}


				bool modified = false;
				User entity = await db.Users.SingleOrDefaultAsync(sd => sd.Id == model.Id && sd.OrganizationId == cu.OrganizationId);

				if (entity != null && entity.Id == model.Id)
				{

					if (model.Active != entity.Active)
					{
						entity.Active = model.Active;
						modified = true;
					}

					model.Email = model.Email.Trim().ToLower();
					if (model.Email != entity.Email)
					{
						entity.Email = model.Email;
						modified = true;
					}

					model.FirstName = model?.FirstName.Trim();
					model.LastName = model?.LastName.Trim();

					if (model.FirstName != entity.FirstName)
					{
						entity.FirstName = model.FirstName;
						modified = true;
					}

					if (model.LastName != entity.LastName)
					{
						entity.LastName = model.LastName;
						modified = true;
					}

					if (model.DefaultReviewer != entity.DefaultReviewer)
					{
						entity.DefaultReviewer = model.DefaultReviewer;
						modified = true;

						if (model.DefaultReviewer)
						{
							var prior_default = db.Users.Where(w => w.OrganizationId == model.OrganizationId)
										.Where(w => w.DefaultReviewer == true)
										.Where(w => w.Id != model.Id);

							if (prior_default != null)
							{
								foreach (var item in prior_default)
								{
									item.DefaultReviewer = false;
								}
							}

							// should only ever be 1 user max.
							await db.SaveChangesAsync();
						}
					}

					if (model.SelectedRoles.Length == 0)
					{
						// Remove all
						foreach (var ur in entity.UserRoles)
						{
							modified = true;
							db.UserRoles.Remove(ur);
						}
					}
					else
					{
						// Removals
						List<UserRole> currentRoles = entity.UserRoles.ToList();
						foreach (var ur in currentRoles)
						{
							if (!model.SelectedRoles.Contains(ur.Role.Id.ToString()))
							{
								modified = true;
								db.UserRoles.Remove(ur);
							}
						}

						// Additions
						var allroles = db.Roles.AsNoTracking().ToList();
						List<int> roles = allroles
									.Select(s => s.Id)
									.ToList();

						if (!CurrentUserIsInRole(Roles.SuperUsers))
						{
							var orgadmin = allroles.Single(s => s.Name == Roles.OrgAdmins);
							roles.Remove(orgadmin.Id);
						}

						var selectedRoles = model.SelectedRoles.ToList();
						foreach (string role in selectedRoles)
						{
							if (int.TryParse(role, out int role_id))
							{
								if (roles.Contains(role_id))
								{
									if (!entity.UserRoles.Any(a => a.RoleId == role_id))
									{
										UserRole added = new UserRole()
										{
											RoleId = role_id,
											UserId = entity.Id,
											CreateUserId = CurrentUser().Id,
											UpdateUserId = CurrentUser().Id,
											CreateTimestamp = entity.UpdateTimestamp,
											UpdateTimestamp = entity.UpdateTimestamp
										};

										db.UserRoles.Add(added);
										modified = true;
									}
								}
							}


						}
					}

					if (modified)
					{
						entity.UpdateTimestamp = DateTime.UtcNow;
						entity.UpdateUserId = CurrentUser().Id;
						await db.SaveChangesAsync();
					}
				}
				else
				{
					model.ErrorMessage = "Not found.";
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return RedirectToAction("UserList");
		}


		[HttpGet]
		[Authorize(Roles = ("OrganizationAdmins,SuperUsers,Physician"))]
		public ActionResult FilterList()
		{
			FilterListModel model = null;

			try
			{
				model = new FilterListModel();
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return View(model);
		}

		[HttpGet]
		[Authorize(Roles = ("OrganizationAdmins,SuperUsers,Physician"))]
		public async Task<ActionResult> CreateFilter()
		{
			FilterEditorModel model = null;
			var cu = CurrentUser();
			try
			{
				model = new FilterEditorModel();
				model.OrganizationId = cu.OrganizationId;
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = ("OrganizationAdmins,SuperUsers,Physician"))]
		public async Task<ActionResult> CreateFilter(FilterEditorModel model)
		{
			var cu = CurrentUser();

			if (!ModelState.IsValid)
			{
				var errors = GetModelsStateErrors(ModelState);
				model.ErrorMessage = BuildErrorListElement(errors);
				return View(model);
			}
			else
			{
				if (model.OrganizationId != cu.OrganizationId)
				{
					model.ErrorMessage = "Not found.";
					return View(model);
				}
			}

			try
			{
				Models.Filter filter = new Models.Filter();


				filter.Active = model.Active;
				filter.Manufacturer = model.Manufacturer.Trim();
				filter.Name = model.Name.Trim();
				filter.Permanent = model.Permanent;
				filter.OrganizationId = cu.OrganizationId;
				filter.CreateTimestamp = DateTime.UtcNow;
				filter.UpdateTimestamp = filter.CreateTimestamp;
				filter.CreateUserId = cu.Id;
				filter.UpdateUserId = filter.CreateUserId;

				db.Filters.Add(filter);
				await db.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return RedirectToAction("FilterList");
		}

		[HttpGet]
		[Authorize(Roles = ("OrganizationAdmins,SuperUsers,Physician"))]
		public ActionResult EditFilter(int id)
		{
			if (id <= 0)
				return RedirectToAction("FilterList");

			var cu = CurrentUser();

			FilterEditorModel model = null;

			try
			{
				var filter = db.Filters.AsNoTracking().SingleOrDefault(sd => sd.Id == id && sd.OrganizationId == cu.OrganizationId);

				model = new FilterEditorModel()
				{
					Active = filter.Active,
					Id = filter.Id,
					Manufacturer = filter.Manufacturer,
					Name = filter.Name,
					OrganizationId = filter.OrganizationId,
					Permanent = filter.Permanent
				};
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = ("OrganizationAdmins,SuperUsers,Physician"))]
		public async Task<ActionResult> EditFilter(Models.FilterEditorModel model)
		{
			var cu = CurrentUser();

			if (!ModelState.IsValid)
			{
				var errors = GetModelsStateErrors(ModelState);
				model.ErrorMessage = BuildErrorListElement(errors);
				return View(model);
			}
			else
			{
				if (model.OrganizationId != cu.OrganizationId)
				{
					model.ErrorMessage = "Not found.";
					return View(model);
				}
			}

			try
			{

				var filter = db.Filters.SingleOrDefault(sd => sd.Id == model.Id && sd.OrganizationId == cu.OrganizationId);
				if (filter != null && filter.Id == model.Id)
				{

					bool modified = false;
					if (filter.Name != model.Name)
					{
						filter.Name = model?.Name.Trim();
						modified = true;
					}

					if (filter.Active != model.Active)
					{
						filter.Active = model.Active;
						modified = true;
					}

					if (filter.Permanent != model.Permanent)
					{
						filter.Permanent = model.Permanent;
						modified = true;
					}

					if (filter.Manufacturer != model.Manufacturer)
					{
						filter.Manufacturer = model?.Manufacturer.Trim();
						modified = true;
					}

					if (modified)
					{
						filter.UpdateTimestamp = DateTime.UtcNow;
						filter.UpdateUserId = cu.Id;

						await db.SaveChangesAsync();
					}
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {CurrentUser().Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return RedirectToAction("FilterList");
		}

		[HttpGet]
		[Authorize(Roles = ("SuperUsers"))]
		public async Task<ActionResult> ExportOrgData()
		{
			var model = new ExportOrgDataModel();

			var orgs = await db.Organizations.AsNoTracking()
				.Where(w => w.Id > 1)
				.OrderBy(o => o.Id)
				.Select(s => new { s.Id, s.Name })
				.ToListAsync();

			orgs.ForEach(f =>
			{
				model.Organizations.Add(new SelectListItem { Text = f.Name, Value = f.Id.ToString() });
			});

			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = ("SuperUsers"))]
		public async Task<ActionResult> ExportOrgData(int SelectedOrganizationId)
		{
			ExportOrgDataModel returned_on_error;

			try
			{
				DataSet results = new DataSet();

				var ps = new ObjectShredder<Patient>();
				var psdt = ps.Shred(db.Patients.AsNoTracking().Where(w => w.OrganizationId == SelectedOrganizationId), null, LoadOption.PreserveChanges);
				psdt.Columns.Remove("Tasks");
				psdt.Columns.Remove("Organization");
				psdt.Columns.Remove("PatientFilters");
				if (psdt.Columns.Contains("_entityWrapper"))
					psdt.Columns.Remove("_entityWrapper");
				results.Tables.Add(psdt);

				var pfs = new ObjectShredder<PatientFilter>();
				var pfsdt = pfs.Shred(db.PatientFilters.AsNoTracking().Where(w => w.OrganizationId == SelectedOrganizationId), null, LoadOption.PreserveChanges);
				if (pfsdt.Columns.Contains("_entityWrapper"))
					pfsdt.Columns.Remove("ComplicatingFactor");
				pfsdt.Columns.Remove("Filter");
				pfsdt.Columns.Remove("Indication");
				pfsdt.Columns.Remove("Organization");
				pfsdt.Columns.Remove("OrderingPhysician");
				pfsdt.Columns.Remove("PrimaryCarePhysician");
				pfsdt.Columns.Remove("PatientContactAttempts");
				pfsdt.Columns.Remove("PhysicianContactAttempts");
				pfsdt.Columns.Remove("ProcedurePhysician");
				pfsdt.Columns.Remove("Tasks");
				pfsdt.Columns.Remove("Patient");
				if (pfsdt.Columns.Contains("_entityWrapper"))
					pfsdt.Columns.Remove("_entityWrapper");
				results.Tables.Add(pfsdt);

				var pca = new ObjectShredder<PatientContactAttempt>();
				var pcadt = pca.Shred(db.PatientContactAttempts.AsNoTracking().Where(w => w.OrganizationId == SelectedOrganizationId), null, LoadOption.PreserveChanges);
				if (pcadt.Columns.Contains("_entityWrapper"))
					pcadt.Columns.Remove("_entityWrapper");
				pcadt.Columns.Remove("ContactResultCode");
				pcadt.Columns.Remove("ContactType");
				pcadt.Columns.Remove("Organization");
				pcadt.Columns.Remove("PatientFilter");
				pcadt.Columns.Remove("User");
				pcadt.Columns.Remove("User1");
				pcadt.Columns.Remove("Task");
				results.Tables.Add(pcadt);

				var crc = new ObjectShredder<ContactResultCode>();
				var crcdt = crc.Shred(db.ContactResultCodes.AsNoTracking().Where(w => w.OrganizationId == 1 || w.OrganizationId == SelectedOrganizationId), null, LoadOption.PreserveChanges);
				if (crcdt.Columns.Contains("_entityWrapper"))
					crcdt.Columns.Remove("_entityWrapper");
				crcdt.Columns.Remove("Organization");
				crcdt.Columns.Remove("PatientContactAttempts");
				crcdt.Columns.Remove("PhysicianContactAttempts");
				crcdt.Columns.Remove("User");
				crcdt.Columns.Remove("User1");
				results.Tables.Add(crcdt);

				var pq = new ObjectShredder<PatientQuestion>();
				var pqdt = pq.Shred(db.PatientQuestions.AsNoTracking().Where(w => w.OrganizationId == 1 || w.OrganizationId == SelectedOrganizationId), null, LoadOption.PreserveChanges);
				if (pqdt.Columns.Contains("_entityWrapper"))
					pqdt.Columns.Remove("_entityWrapper");
				pqdt.Columns.Remove("PatientQuestionResponses");
				pqdt.Columns.Remove("Organization");
				results.Tables.Add(pqdt);

				var pqr = new ObjectShredder<PatientQuestionResponses>();
				var pqrdt = pqr.Shred(db.PatientQuestionResponses.AsNoTracking().Where(w => w.OrganizationId == SelectedOrganizationId), null, LoadOption.PreserveChanges);
				if (pqrdt.Columns.Contains("_entityWrapper"))
					pqrdt.Columns.Remove("_entityWrapper");
				pqrdt.Columns.Remove("Organization");
				pqrdt.Columns.Remove("Task");
				pqrdt.Columns.Remove("PatientQuestion");
				results.Tables.Add(pqrdt);

				var phq = new ObjectShredder<PhysicianQuestion>();
				var phqdt = phq.Shred(db.PhysicianQuestions.AsNoTracking().Where(w => w.OrganizationId == 1 || w.OrganizationId == SelectedOrganizationId), null, LoadOption.PreserveChanges);
				if (phqdt.Columns.Contains("_entityWrapper"))
					phqdt.Columns.Remove("_entityWrapper");
				phqdt.Columns.Remove("PhysicianQuestionResponses");
				results.Tables.Add(phqdt);

				var phqr = new ObjectShredder<PhysicianQuestionResponses>();
				var phqrdt = phqr.Shred(db.PhysicianQuestionResponses.AsNoTracking().Where(w => w.OrganizationId == SelectedOrganizationId), null, LoadOption.PreserveChanges);
				if (phqrdt.Columns.Contains("_entityWrapper"))
					phqrdt.Columns.Remove("_entityWrapper");
				phqrdt.Columns.Remove("Organization");
				phqrdt.Columns.Remove("PhysicianQuestion");
				phqrdt.Columns.Remove("Task");
				results.Tables.Add(phqrdt);

				var phca = new ObjectShredder<PhysicianContactAttempt>();
				var phcadt = phca.Shred(db.PhysicianContactAttempts.AsNoTracking().Where(w => w.OrganizationId == SelectedOrganizationId), null, LoadOption.PreserveChanges);
				if (phcadt.Columns.Contains("_entityWrapper"))
					phcadt.Columns.Remove("_entityWrapper");
				phcadt.Columns.Remove("ContactResultCode");
				phcadt.Columns.Remove("ContactType");
				phcadt.Columns.Remove("Organization");
				phcadt.Columns.Remove("PatientFilter");
				phcadt.Columns.Remove("User");
				phcadt.Columns.Remove("User1");
				phcadt.Columns.Remove("Task");
				results.Tables.Add(phcadt);

				var ts = new ObjectShredder<Models.Task>();
				var tsdt = ts.Shred(db.Tasks.AsNoTracking().Where(w => w.OrganizationId == SelectedOrganizationId), null, LoadOption.PreserveChanges);
				if (tsdt.Columns.Contains("_entityWrapper"))
					tsdt.Columns.Remove("_entityWrapper");
				tsdt.Columns.Remove("Organization");
				tsdt.Columns.Remove("PatientContactAttempts");
				tsdt.Columns.Remove("PatientFilter");
				tsdt.Columns.Remove("PatientQuestionResponses");
				tsdt.Columns.Remove("PhysicianContactAttempts");
				tsdt.Columns.Remove("PhysicianQuestionResponses");
				tsdt.Columns.Remove("TaskAttachments");
				tsdt.Columns.Remove("TaskType");
				tsdt.Columns.Remove("AssignedUser");
				tsdt.Columns.Remove("CreateUser");
				tsdt.Columns.Remove("UpdateUser");
				tsdt.Columns.Remove("Patient");
				results.Tables.Add(tsdt);

				var us = new ObjectShredder<Models.User>();
				var usdt = us.Shred(db.Users.AsNoTracking().Where(w => w.OrganizationId == SelectedOrganizationId), null, LoadOption.PreserveChanges);
				var t = typeof(Models.User);
				PropertyInfo[] properties = t.GetProperties()
					.Where(p => p.GetGetMethod().IsVirtual).ToArray();

				if (usdt.Columns.Contains("_entityWrapper"))
					usdt.Columns.Remove("_entityWrapper");

				usdt.Columns.Remove("ComplicatingFactors");
				usdt.Columns.Remove("ComplicatingFactors1");
				usdt.Columns.Remove("ContactResultCodes");
				usdt.Columns.Remove("ContactResultCodes1");
				usdt.Columns.Remove("Filters");
				usdt.Columns.Remove("Filters1");
				usdt.Columns.Remove("Indications");
				usdt.Columns.Remove("Indications1");
				usdt.Columns.Remove("OrganizationDefaults");
				usdt.Columns.Remove("OrganizationDefaults1");
				usdt.Columns.Remove("OrganizationFilters");
				usdt.Columns.Remove("OrganizationFilters1");
				usdt.Columns.Remove("Organizations");
				usdt.Columns.Remove("Organizations1");
				usdt.Columns.Remove("PatientContactAttempts");
				usdt.Columns.Remove("PatientContactAttempts1");
				usdt.Columns.Remove("PatientFilters");
				usdt.Columns.Remove("PhysicianContactAttempts");
				usdt.Columns.Remove("PhysicianContactAttempts1");
				usdt.Columns.Remove("Physicians");
				usdt.Columns.Remove("Physicians1");
				usdt.Columns.Remove("TaskAttachments");
				usdt.Columns.Remove("TaskAttachments1");
				usdt.Columns.Remove("TaskTypes");
				usdt.Columns.Remove("TaskTypes1");
				usdt.Columns.Remove("UserRoles");
				usdt.Columns.Remove("Tasks");
				usdt.Columns.Remove("Tasks1");
				usdt.Columns.Remove("Tasks2");
				usdt.Columns.Remove("QuickNotes");

				this.Response.ClearContent();
				MemoryStream streamFromDataSet = ExcelUtility.GetStreamFromDataSet(results);
				streamFromDataSet.Seek(0L, SeekOrigin.Begin);
				string str = "FilterTrackerExport" + DateTime.Now.ToShortDateString().Replace("/", ".") + ".xlsx";
				FileStreamResult fileStreamResult = new FileStreamResult((Stream)streamFromDataSet, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
				fileStreamResult.FileDownloadName = str;
				return (ActionResult)fileStreamResult;
			}
			catch (Exception ex)
			{
				LogException(ex);
				returned_on_error = new ExportOrgDataModel();
				returned_on_error.ErrorMessage = ex.Message;
			}

			return View(returned_on_error);
		}

		[HttpGet]
		[Authorize(Roles = ("OrganizationAdmins,SuperUsers,Physician"))]
		public ActionResult ImportPatientData()
		{
			ImportPatientDataModel model = new ImportPatientDataModel(db);
			var cu = CurrentUser();

			model.AmSuperUser = false;
			if (cu.UserRoles.Any(a => a.Role.Name == Roles.SuperUsers))
			{
				model.AmSuperUser = true;
			}

			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = ("OrganizationAdmins,SuperUsers,Physician"))]
		public async Task<ActionResult> ImportPatientData(HttpPostedFileBase uploaded)
		{

			var model = new ImportPatientDataModel(db);
			DateTime now = DateTime.UtcNow;
			var cu = CurrentUser();
			int selectedOrganizationId = cu.OrganizationId;

			model.AmSuperUser = false;
			if (cu.UserRoles.Any(a => a.Role.Name == Roles.SuperUsers))
			{
				model.AmSuperUser = true;
				string tmp = Request.Form["SelectedOrganizationId"];
				if (!string.IsNullOrEmpty(tmp))
				{
					int itmp;
					if (int.TryParse(tmp, out itmp))
					{
						selectedOrganizationId = itmp;
					}
				}
			}

			bool filetype_allowed = false;
			string ext;

			try
			{
				if (Request.Files.Count > 0)
				{
					if (uploaded == null)
						uploaded = Request.Files[0];

					ext = uploaded.FileName.ToLower();
					if (!string.IsNullOrEmpty(ext))
					{
						int needle = ext.LastIndexOf('.');
						if (needle > 0)
						{
							ext = ext.Substring(needle);

							if (ext == ".csv")
							{
								filetype_allowed = true;
							}
						}
					}

					if (!filetype_allowed)
					{
						model.Errors.Add("Invalid file type.  Only .csv files are allowed.");
					}
					else
					{

						// if we get this far, display the counts.
						model.ImportAttemptComplete = true;
						StreamReader reader = new StreamReader(uploaded.InputStream);
						var buffer = reader.ReadToEnd();
						buffer = buffer.Replace("\r\r\n", "\r\n");
						char[] splitter = Environment.NewLine.ToCharArray();
						var rows = buffer.Split(splitter, StringSplitOptions.RemoveEmptyEntries);

						Regex regx = new Regex("," + "(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

						Logger.Log.Info("Beginning import file duplication check.");
						var duplicate_scan_result = PrescanFileForDuplicates(rows, regx);

						if (duplicate_scan_result != null && duplicate_scan_result.Count > 0)
						{
							foreach (var item in duplicate_scan_result)
							{
								model.Errors.Add($"Rows {item.Item1} and {item.Item2} are duplicates.");
							}

							return View(model);
						}

						// Process uploaded file.
						foreach (string row in rows)
						{
							// skip column headers row
							if (model.DataRowCount++ == 0)
								continue;

							string[] cols = regx.Split(row);

							if (cols.Length != 11)
							{
								model.Errors.Add("Row does not contain 11 columns.");
								model.ImportFailures.Add(row);
								continue;
							}

							try
							{
								string mrn = cols[3].Replace("\"", "");
								string fullname = cols[4].Trim().Replace("\"", "");


								var name = ParseName(fullname);

								string dob_str = cols[5];
								dob_str = dob_str.Trim().Replace("\"", "");
								DateTime dob;

								if (!DateTime.TryParse(dob_str, out dob))
								{
									model.ImportFailures.Add(row);
									model.Errors.Add($"Unable to parse DOB for patient: MRN-{mrn}.");
									continue;
								}

								DateTime placementDate;
								string placement_date = cols[1];
								if (!string.IsNullOrEmpty(placement_date))
								{
									placement_date = placement_date.Trim().Replace("\"", "");
									if (!DateTime.TryParse(placement_date, out placementDate))
									{
										model.ImportFailures.Add(row);
										model.Errors.Add($"Unable to parse Placement date for patient: MRN-{mrn}.");
										continue;
									}
								}
								else
								{
									model.ImportFailures.Add(row);
									model.Errors.Add($"Unable to parse Placement date for patient: MRN-{mrn}.");
									continue;
								}

								string phone = cols[6];
								phone = phone.Trim().Replace("-", "").Replace("\"", "");


								mrn = mrn.Trim().TrimStart(new char[] { '0' }).Replace("\"", "");


								var patient = db.Patients
									.Where(w => w.OrganizationId == selectedOrganizationId)
									.SingleOrDefault(sd => sd.MRN == mrn || (sd.FirstName == name.First && sd.LastName == name.Last && sd.DateOfBirth == dob.Date));


								if (patient == null || patient.Id == 0)
								{
									// New Patient
									patient = new Patient();

									patient.Active = true;
									patient.CreateTimestamp = now;
									patient.CreateUserId = cu.Id;
									patient.DateOfBirth = dob.Date;
									patient.FirstName = name.First;
									patient.LastName = name.Last;
									patient.MiddleName = string.IsNullOrEmpty(name.Middle) ? null : name.Middle;
									patient.MRN = string.IsNullOrEmpty(mrn) ? null : mrn;
									patient.OrganizationId = selectedOrganizationId;
									patient.PrimaryPhoneNumber = string.IsNullOrEmpty(phone) ? null : phone;

									patient.UpdateTimestamp = now;
									patient.UpdateUserId = cu.Id;

									db.Patients.Add(patient);
									await db.SaveChangesAsync();

									model.PatientsSuccessfullyImported++;
								}
								else
								{
									model.DuplicatePatients++;
								}

								if (patient.PatientFilters.Count(c => c.ProcedureDate.Value.Date == placementDate.Date) == 0)
								{
									// New filter
									if (placementDate > new DateTime(1950, 1, 1))
									{
										PatientFilter pf = new PatientFilter();
										pf.ActualRemovalDate = null;
										pf.CreateTimestamp = now;
										pf.CreateUserId = cu.Id;
										pf.OrganizationId = selectedOrganizationId;
										pf.PatientId = patient.Id;
										pf.ProcedureDate = placementDate;
										pf.UpdateTimestamp = now;
										pf.UpdateUserId = cu.Id;

										db.PatientFilters.Add(pf);

										model.PatientFiltersSuccessfullyImported++;

										// Build case task hidden until right time
										var filter_age = db.OrganizationDefaults.FirstOrDefault(f => f.OrganizationId == cu.OrganizationId).FilterAge;
										var default_filter_age = db.OrganizationDefaults.Single(s => s.Id == 1).FilterAge;

										var task = new Models.Task();

										DateTime marker = pf.ProcedureDate.HasValue ? pf.ProcedureDate.Value : now;

										task.HideUntil = marker.AddDays(filter_age.HasValue && filter_age.Value > 0 ? filter_age.Value : default_filter_age.Value);
										task.OrganizationId = selectedOrganizationId;
										task.CreateTimestamp = now;
										task.CreateUserId = cu.Id;
										task.PatientFilterId = pf.Id;
										task.PatientId = patient.Id; ;
										task.TaskTypeId = (int)Models.TaskTypes.BuildCase;
										task.UpdateTimestamp = now;
										task.UpdateUserId = cu.Id;

										db.Tasks.Add(task);
									}
								}


								await db.SaveChangesAsync();
							}
							catch (Exception ex)
							{
								model.Errors.Add("Failed to import row.");
								model.ImportFailures.Add(row);
								Logger.LogException(ex);
							}
						}


					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogException(ex);
				if (uploaded != null)
					model.Errors.Add($"Error uploading file: {uploaded.FileName}.");
			}

			// don't count the header row.
			if (model.DataRowCount > 0)
				model.DataRowCount -= 1;

			return View(model);

		}

		const int MRN_INDEX = 3;
		const int NAME_INDEX = 4;
		const int DOB_INDEX = 5;
		const int PD_INDEX = 1;

		private List<Tuple<int, int>> PrescanFileForDuplicates(string[] rows, Regex splitter)
		{
			List<Tuple<int, int>> returned = new List<Tuple<int, int>>();

			var clone = (string[])rows.Clone();

			int number_of_rows = rows.Count();
			int row_index, clone_row_index;
			for (row_index = 0; row_index < number_of_rows; row_index++)
			{
				string[] row_cols = splitter.Split(rows[row_index]);

				for (clone_row_index = 0; clone_row_index < number_of_rows; clone_row_index++)
				{
					if (row_index == clone_row_index)
						continue;


					string[] clone_cols = splitter.Split(clone[clone_row_index]);

					//
					// MRN duplicate check
					//
					if (!string.IsNullOrEmpty(row_cols[MRN_INDEX]) && !string.IsNullOrEmpty(clone_cols[MRN_INDEX]))
					{
						if (row_cols[MRN_INDEX].TrimAndLower() == clone_cols[MRN_INDEX].TrimAndLower())
						{
							returned.Add(new Tuple<int, int>(row_index, clone_row_index));
							continue;
						}
					}

					//
					// NAME and DOB duplicate check
					//
					if (!string.IsNullOrEmpty(row_cols[NAME_INDEX]) && !string.IsNullOrEmpty(clone_cols[NAME_INDEX]))
					{
						if (!string.IsNullOrEmpty(row_cols[DOB_INDEX]) && !string.IsNullOrEmpty(clone_cols[DOB_INDEX]))
						{
							if (row_cols[NAME_INDEX].TrimAndLower() == clone_cols[NAME_INDEX].TrimAndLower())
							{
								DateTime dob1, dob2;
								if (DateTime.TryParse(row_cols[DOB_INDEX], out dob1))
								{
									if (DateTime.TryParse(clone_cols[DOB_INDEX], out dob2))
									{
										if (dob1.Date == dob2.Date)
										{
											returned.Add(new Tuple<int, int>(row_index, clone_row_index));
											continue;
										}
									}
								}
							}
						}
					}
				}
			}

			return returned;
		}

		[HttpGet]
		[Authorize(Roles = ("OrganizationAdmins,SuperUsers,Physician"))]
		public async Task<ActionResult> IntervalData()
		{
			var model = new IntervalDataViewModel();
			model.IsCalculated = false;
			return View(model);
		}

		[HttpPost]
		[Authorize(Roles = ("OrganizationAdmins,SuperUsers,Physician"))]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> IntervalData(DateTime start, DateTime end, int SelectedOrganizationId)
		{
			// patients with filter placed between start and end dates supplied
			// and either is alive or was alive 3 months post placement date
			var query = db.PatientFilters
				.Include(i => i.Patient)
				.Include(i => i.Tasks)
				.Include(i => i.PatientContactAttempts)
				.Where(w => w.OrganizationId == SelectedOrganizationId)
				.Where(w => w.ProcedureDate.HasValue && w.ProcedureDate >= start && w.ProcedureDate <= end)
				.Where(w => w.Patient.DeceasedDate.HasValue == false || (w.Patient.DeceasedDate.HasValue && w.Patient.DeceasedDate.Value >= DbFunctions.AddMonths(w.ProcedureDate.Value, 3)))
				.ToArray();

			var model = new IntervalDataViewModel();
			model.start = start;
			model.end = end;
			model.FiltersPlaced = query.Count();
			model.A1 = 0;
			model.A2 = 0;
			model.A3 = 0;
			model.A4 = 0;
			
			// filters removed within 90 days of placement
			foreach (var item in query)
			{
				if (item.ActualRemovalDate.HasValue && item.ActualRemovalDate <= item.ProcedureDate.Value.AddMonths(3))
				{
					model.A1++;
					model.A1Popluation.Add(item);
				}
				else
				{
					if (item.Tasks.Count(c => c.TaskTypeId == 5 && c.ClosedDate.HasValue && c.ClosedDate <= item.ProcedureDate.Value.AddMonths(3)) > 0)
					{
						model.A2++;
						model.A2Popluation.Add(item);
					}
					else
					{
						if (item.PatientContactAttempts.Count(c => c.Timestamp <= item.ProcedureDate.Value.AddMonths(3)) >= 2)
						{
							model.A3++;
							model.A3Popluation.Add(item);
						}
						else
						{
							model.A4++;
							model.A4Population.Add(item);
						}
					}

				}
			}

			model.IsCalculated = true;
			return View(model);
		}

		private Name ParseName(string fullname)
		{
			Name returned = new Name();

			if (!fullname.Contains(", "))
			{
				return returned;
			}

			int index = fullname.IndexOf(", ");

			returned.Last = fullname.Substring(0, index);

			string remaining = fullname.Substring(index + 2);

			index = remaining.IndexOf(" ");
			if (index > 0)
			{
				returned.First = remaining.Substring(0, index);

				returned.Middle = remaining.Substring(index + 1);
			}
			else
			{
				returned.First = remaining;
			}


			return returned;

		}

		private class Name
		{
			public string Last { get; set; }
			public string First { get; set; }
			public string Middle { get; set; }
		}

		private PasswordComplexityValidationResult ValidatePasswordComplexity(string input)
		{
			var returned = new PasswordComplexityValidationResult();

			var hasNumber = new Regex(@"[0-9]+");
			var hasUpperChar = new Regex(@"[A-Z]+");
			var hasMinimum8Chars = new Regex(@".{8,}");

			returned.MeetsLengthRequirement = hasMinimum8Chars.IsMatch(input);
			returned.MeetsUppercaseCharacterRequirement = hasUpperChar.IsMatch(input);
			returned.MeetsNumericCharacterRequirement = hasNumber.IsMatch(input);

			returned.IsValid = returned.MeetsLengthRequirement && returned.MeetsUppercaseCharacterRequirement && returned.MeetsNumericCharacterRequirement;

			return returned;

		}

		public class PasswordComplexityValidationResult
		{
			public bool MeetsNumericCharacterRequirement { get; set; }
			public bool MeetsUppercaseCharacterRequirement { get; set; }
			public bool MeetsLengthRequirement { get; set; }

			public bool IsValid { get; set; }

		}
	}

	public class ImportPatientDataModel
	{
		public ImportPatientDataModel(FilterTrackerEntities db)
		{
			PatientFiltersSuccessfullyImported = 0;
			PatientsSuccessfullyImported = 0;
			DuplicateFiltersSkipped = 0;
			ImportFailures = new List<string>();
			ImportAttemptComplete = false;
			DataRowCount = 0;

			Organizations = db.Organizations.Where(w => w.Active && w.Id > 1)
			.OrderBy(ob => ob.Name)
			.Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() })
			.ToList();

			Organizations.Insert(0, new SelectListItem { Text = "--Please select an organization--", Value = "-1" });

			SelectedOrganizationId = 0;
		}

		public int DataRowCount { get; set; }
		public bool ImportAttemptComplete { get; set; }
		public int PatientsSuccessfullyImported { get; set; }
		public int PatientFiltersSuccessfullyImported { get; set; }

		public int DuplicateFiltersSkipped { get; set; }

		public int DuplicatePatients { get; set; }

		public List<string> ImportFailures { get; set; } = new List<string>();

		public List<string> Errors { get; set; } = new List<string>();

		public bool AmSuperUser { get; set; }

		public List<SelectListItem> Organizations { get; set; }

		[Required()]
		[Range(2, 99999, ErrorMessage = "Please select an organization.")]
		public int SelectedOrganizationId { get; set; }
	}
}
