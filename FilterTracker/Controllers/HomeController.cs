using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Windows.Interop;
using MathNet.Numerics;

using FilterTracker.Models;

using Microsoft.Ajax.Utilities;
using System.ComponentModel.DataAnnotations;

namespace FilterTracker.Controllers
{
	
	public class HomeController : ControllerBase
	{
		[HttpPost]
		[Authorize(Roles = "OrganizationAdmins,SuperUsers,Users,Physician")]
		public JsonResult ForceToTask(int patientFilterId, int taskTypeId, bool resetPermanency = false)
		{
			int pfid = patientFilterId;
			int tasktypeid = taskTypeId;
			DateTime now = DateTime.UtcNow;
			bool returned = true;
			string message = "";

			var cu = CurrentUser();

			try
			{
				Logger.Log.Warn($"ForceToTask Called by user: {cu.Email} for pf: {patientFilterId} to task type: {taskTypeId}.");

				var pf = db.PatientFilters
					.Where(w => w.OrganizationId == cu.OrganizationId)
					.Where(w => w.Id == patientFilterId)
					.SingleOrDefault();

				if (pf != null && pf.Id == patientFilterId)
				{
					if (!resetPermanency && pf.MadePermanent.HasValue)
					{
						returned = false;
						message = "This filter has been made permanent.  In order to continue, it must be reset.";
					}
					else
					{
						if (resetPermanency)
						{
							pf.MadePermanent = null;
							pf.MadePermanentBy = string.Empty;
						}

						var tasks = db.Tasks
							.Where(w => w.OrganizationId == cu.OrganizationId)
							.Where(w => w.PatientId == pf.PatientId)
							.Where(w => w.ClosedDate.HasValue == false);

						foreach (var t in tasks)
						{
							t.ClosedDate = now;
						}

						db.SaveChanges();

						var zombie = pf.Tasks
							.Where(w => w.TaskTypeId == tasktypeid)
							.OrderByDescending(o => o.CreateTimestamp)
							.FirstOrDefault();

						if (zombie != null && zombie.Id > 0)
						{
							// Reopen previously closed task.
							zombie.ClosedDate = null;
							zombie.HideUntil = null;
							zombie.UpdateTimestamp = now;
							zombie.UpdateUserId = cu.Id;
						}
						else
						{
							// No previous tasks of this type exist.
							// Create one.
							zombie = new Models.Task();

							zombie.TaskTypeId = tasktypeid;
							zombie.PatientFilterId = patientFilterId;
							zombie.OrganizationId = cu.OrganizationId;
							zombie.PatientId = pf.PatientId;
							zombie.ConsecutiveContacts = 0;
							zombie.Notes += $"\r\nTask reopened on {DateTime.UtcNow} (UTC) by {cu.FirstName} {cu.LastName}.";
							zombie.CreateTimestamp = now;
							zombie.CreateUserId = cu.Id;
							zombie.UpdateTimestamp = now;
							zombie.UpdateUserId = cu.Id;


							// Leave task in "Claimable" state, unless it's Review Case.  Those aren't claimable, and
							// so need to be assigned.
							if ((int)TaskTypes.ReviewCase == tasktypeid)
							{
								var physicians = db.UserRoles.Where(w => w.User.OrganizationId == cu.OrganizationId
									&& w.Role.Name == Models.Roles.Physician)
									.Select(s => s.User);

								if (physicians != null)
								{
									if (physicians.Any(a => a.DefaultReviewer && a.Active))
									{
										zombie.AssignedUserId = physicians.First(f => f.DefaultReviewer && f.Active).Id;
									}
									else
									{
										zombie.AssignedUserId = physicians.First(f => f.Active).Id;
									}
								}
								else
								{
									returned = false;
									message = "No physicians are in the system to assign the Review Case task to.";
								}

								var bc_tasks = pf.Tasks.Where(w => w.TaskTypeId == (int)TaskTypes.BuildCase).OrderBy(ob => ob.CreateTimestamp);
								if (bc_tasks != null && bc_tasks.Any())
								{
									var bc = bc_tasks.Last();
									if (bc.TaskAttachments.Any())
									{
										foreach (var ta in bc.TaskAttachments)
										{
											TaskAttachment added = new TaskAttachment();

											added.AttachmentId = ta.AttachmentId;
											added.CreateTimestamp = now;
											added.CreateUserId = cu.Id;
											added.UpdateTimestamp = now;
											added.UpdateUserId = cu.Id;

											zombie.TaskAttachments.Add(ta);
										}
									}
								}
							}

							db.Tasks.Add(zombie);
						}

						db.SaveChanges();
					}
				}
			}
			catch (Exception ex)
			{
				LogException(ex);
				returned = false;
				message = "Unable to reassign to task.";
			}

			JsonResult result = new JsonResult();
			result.Data = new
			{
				Success = returned,
				Message = returned ? null : message
			};
			return result;
		}

		[HttpGet]
		[Authorize(Roles = "OrganizationAdmins,SuperUsers,Physician")]
		public async Task<ActionResult> QualityControlReport()
		{
			DateTime now = DateTime.UtcNow;
			QualityControlReportModel model = null;

			try
			{
				var cu = CurrentUser();
				model = new QualityControlReportModel(cu.OrganizationId, db);
			}
			catch (Exception ex)
			{
				LogException(ex);
				model = new QualityControlReportModel();
				model.ErrorMessage = GetErrorMessage(now);
			}

			return View("QualityControlReport", "~/Views/Shared/_Layout2.cshtml", model);
		}


		[HttpGet]
		[Authorize(Roles = "OrganizationAdmins,SuperUsers,Physician")]
		public ActionResult PatientReconciliationReport()
		{
			DateTime now = DateTime.UtcNow;
			PatientReconciliationReportModel model = null;

			try
			{
				var cu = CurrentUser();
				model = new PatientReconciliationReportModel(cu.OrganizationId, db);
			}
			catch (Exception ex)
			{
				LogException(ex);
				model = new PatientReconciliationReportModel();
				model.ErrorMessage = GetErrorMessage(now);
			}

			return View("PatientReconciliationReport", "~/Views/Shared/_Layout2.cshtml", model);
		}


		[HttpGet]
		[Authorize(Roles = "OrganizationAdmins,Physician,SuperUsers,Users")]
		public async Task<ActionResult> Dashboard()
		{
			DateTime now = DateTime.UtcNow;
			var cu = CurrentUser();

			DashboardModel model = new DashboardModel(cu);



			try
			{
				//if (cu.UserRoles.Any(a => a.Role.Name == Models.Roles.Physician))
				//{
				//	model.MIPSHistory = db.MIPSHistories.Where(w => w.UserId == cu.Id && (w.Date.Year == now.Year || w.Date.Year == now.Year - 1)).ToList();
				//}

				if (cu.UserRoles.Any(a => a.Role.Name == Roles.Physician))
				{
					model.ReviewCaseTaskList.Tasks.AddRange(db.Tasks.Where(w => (w.AssignedUserId == cu.Id || (w.OrganizationId == cu.OrganizationId && !w.AssignedUserId.HasValue)) && w.TaskTypeId == (int)TaskTypes.ReviewCase && w.ClosedDate == null));
					//model.ReviewCaseTaskList.Tasks.AddRange(db.Tasks.Where(w => w.OrganizationId == cu.OrganizationId && w.TaskTypeId == (int)TaskTypes.ReviewCase && w.ClosedDate == null));
					model.ReviewCaseTaskList.TaskTypeEnum = TaskTypes.ReviewCase;
				}
				else
				{

					model.RetrievalDatePassedTaskList.Tasks.AddRange(db.Tasks
					//.Where(w => (w.AssignedUserId == cu.Id || (w.OrganizationId == cu.OrganizationId && !w.AssignedUserId.HasValue)) && w.TaskTypeId == (int)TaskTypes.RetrievalDatePassed && w.ClosedDate == null));
					.Where(w => w.OrganizationId == cu.OrganizationId && w.TaskTypeId == (int)TaskTypes.RetrievalDatePassed && w.ClosedDate == null));
					model.RetrievalDatePassedTaskList.TaskTypeEnum = TaskTypes.RetrievalDatePassed;

					model.ReviewPCPPreferencesTaskList.Tasks.AddRange(db.Tasks
						.Where(w => w.OrganizationId == cu.OrganizationId && w.TaskTypeId == (int)TaskTypes.ReviewPCPPreferences && w.ClosedDate == null));
					model.ReviewPCPPreferencesTaskList.TaskTypeEnum = TaskTypes.ReviewPCPPreferences;

					model.BuildCaseTaskList.Tasks.AddRange(db.Tasks.Where(
						w => w.OrganizationId == cu.OrganizationId
							&& w.TaskTypeId == (int)TaskTypes.BuildCase
							&& w.ClosedDate == null
							&& (w.HideUntil == null || w.HideUntil.Value <= now)
					));

					model.BuildCaseTaskList.TaskTypeEnum = TaskTypes.BuildCase;

					model.SendRegisteredLettersTaskList.Tasks.AddRange(db.Tasks.Where(w => w.OrganizationId == cu.OrganizationId && w.TaskTypeId == (int)TaskTypes.SendRegisteredLetters && w.ClosedDate == null));
					model.SendRegisteredLettersTaskList.TaskTypeEnum = TaskTypes.SendRegisteredLetters;

					model.ScheduleRetrievalTaskList.Tasks.AddRange(db.Tasks.Where(
						w => w.OrganizationId == cu.OrganizationId
						&& w.TaskTypeId == (int)TaskTypes.ScheduleRetrieval
						&& w.ClosedDate == null
						&& (w.HideUntil == null || w.HideUntil.Value <= now)
					));

					model.ScheduleRetrievalTaskList.TaskTypeEnum = TaskTypes.ScheduleRetrieval;


					//model.PatientContactAttemptDueTaskList.Tasks.AddRange(db.Tasks.Where(w => (w.AssignedUserId == cu.Id || (w.OrganizationId == cu.OrganizationId && !w.AssignedUserId.HasValue)) && w.TaskTypeId == (int)TaskTypes.PatientContactDue && w.ClosedDate == null));
					//model.PatientContactAttemptDueTaskList.TaskTypeEnum = TaskTypes.PatientContactDue;

					model.ContactPCPTaskList.Tasks.AddRange(
						db.Tasks.Where(
							w => w.OrganizationId == cu.OrganizationId
							&& w.TaskTypeId == (int)TaskTypes.ContactPCP
							&& w.ClosedDate == null
							)
					);

					model.ContactPCPTaskList.TaskTypeEnum = TaskTypes.ContactPCP;
				}

				int role_id = await db.Roles.Where(w => w.Name == Roles.Physician).Select(s => s.Id).SingleOrDefaultAsync();

				var clinic_physicians = await db.Users
					.AsNoTracking()
					.Include(i => i.UserRoles)
					.Where(w => w.UserRoles.Any(a => a.RoleId == role_id))
					.Where(w => w.Active == true)
					.Where(w => w.OrganizationId == cu.OrganizationId)
					.Select(s => new { s.FirstName, s.LastName, s.Id })
					.ToListAsync();


				foreach (var cp in clinic_physicians)
				{
					model.ClinicPhysicians.Add(new SelectListItem() { Text = $"{cp.FirstName} {cp.LastName}", Value = cp.Id.ToString() });
				}

				model.QuickNotes.AddRange(db.QuickNotes.Where(w => w.UserId == cu.Id).OrderBy(o => o.Id).ToList());

			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				model.ErrorMessage = GetErrorMessage(now);
			}

			return View("Dashboard", "~/Views/Shared/_Layout2.cshtml", model);
		}

		[HttpPost]
		[Authorize()]
		public async Task<ActionResult> MIPSHistory()
		{
			DateTime now = DateTime.UtcNow;
			MIPSHistoryReturn returned = new MIPSHistoryReturn();
			var cu = CurrentUser();

			try
			{
				var previous = new List<decimal>();
				var current = new List<decimal>();

				DateTime start = new DateTime(now.Year, 1, 1);
				DateTime end = now.Date;
				var all_current = db.MIPSHistories.Where(w => w.UserId == cu.Id && w.Date >= start && w.Date <= end).ToList();
				var grouped = all_current.GroupBy(g => g.Date.Month);
				foreach (var group in grouped)
				{
					current.Add(group.Average(a => a.MIPS));
				}

				start = new DateTime(now.Year - 1, 1, 1);
				end = new DateTime(now.Year - 1, 12, 31);
				var all_previous = db.MIPSHistories.Where(w => w.UserId == cu.Id && w.Date >= start && w.Date <= end).ToList();
				grouped = all_previous.GroupBy(g => g.Date.Month);
				foreach (var group in grouped)
				{
					previous.Add(group.Average(a => a.MIPS));
				}

				returned.Success = "true";
				returned.CurrentYear = current;
				returned.PreviousYear = previous;

			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
			}

			return Json(returned);
		}

		[HttpPost]
		[Authorize()]
		public async Task<ActionResult> FilterDwellTime()
		{
			//Avg and std deviation of filter dwell time over past 12 months

			DateTime now = DateTime.UtcNow;
			FilterDwellTimeReturn returned = new FilterDwellTimeReturn();
			var cu = CurrentUser();

			try
			{
				DateTime start = now.Date.AddYears(-1);
				DateTime end = now.Date;

				var patient_filters = await db.PatientFilters.AsNoTracking()
					.Where(w => w.ProcedurePhysicianId == cu.Id)
					.Where(w => w.ProcedureDate >= start && w.ProcedureDate <= end)
					.OrderBy(o => o.ProcedureDate)
					.ToListAsync();  // prune the result set later


				var grouped = patient_filters.GroupBy(g => g.ProcedureDate.Value.Month);

				grouped.ForEach(f =>
				{
					returned.Avg.Add((decimal)f.Average(a => a.DwellTime));
					returned.StdDev.Add((decimal)f.StdDev(s => s.DwellTime));
				});

				returned.Success = "true";
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
			}

			return Json(returned);
		}

		[HttpGet]
		[Authorize()]
		public async Task<ActionResult> FilterDwellTimeReport()
		{
			var model = new FilterDwellTimeReportModel();
			model.Start = null;
			model.End = null;
			return View(model);
		}

		[HttpPost]
		[Authorize()]
		public async Task<ActionResult> FilterDwellTimeReport(DateTime Start, DateTime End)
		{
			DateTime now = DateTime.UtcNow;
			FilterDwellTimeReportModel returned = new FilterDwellTimeReportModel();
			var cu = CurrentUser();

			try
			{
				var patient_filters = await db.PatientFilters.AsNoTracking()
					.Where(w => w.OrganizationId == cu.OrganizationId)
					.Where(w => w.ProcedureDate >= Start && w.ProcedureDate <= End)
					.OrderBy(o => o.ProcedureDate)
					.ToListAsync();

				returned.Placed = patient_filters.Count().ToString();

				// Average
				var removed = patient_filters.Where(w => w.ActualRemovalDate.HasValue);

				List<float> dwell_days_list = new List<float>();

				removed.ForEach(f =>
				{
					dwell_days_list.Add((f.ActualRemovalDate - f.ProcedureDate).Value.Days);
				});

				var inplace = patient_filters.Where(w => w.ActualRemovalDate.HasValue == false && w.MadePermanent.HasValue == false);
				inplace.ForEach(f =>
				{
					dwell_days_list.Add((now - f.ProcedureDate).Value.Days);
				});

				returned.Average = dwell_days_list.Average().ToString();

				dwell_days_list.Sort();

				returned.Median = MathNet.Numerics.Statistics.Statistics.Median(dwell_days_list).ToString();

				returned.Minimum = dwell_days_list.Min().ToString();

				returned.Maximum = dwell_days_list.Max().ToString();

				returned.Removed = patient_filters.Where(w => w.ActualRemovalDate.HasValue).Count().ToString();

				returned.MadePermanent = patient_filters.Count(c => c.MadePermanent.HasValue == true).ToString();

				returned.MIPS = CalculateMIPS(Start, End, cu.OrganizationId).ToString("D2");

				returned.IsPost = true;

			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
			}

			return View(returned);
		}

		private decimal CalculateMIPS(DateTime start, DateTime end, int organizationId)
		{
			var now = DateTime.UtcNow;

			decimal returned = 1.0M;
			int numerator = 0;
			int denominator = 0;

			try
			{
				var filters = db.PatientFilters
					.Where(w => w.ProcedureDate.HasValue)
					.Where(w => w.ProcedureDate >= start && w.ProcedureDate <= end)
					.Where(w => w.OrganizationId == organizationId)
					.Where(w => w.MadePermanent.HasValue == false)
					.Select(s => new
					{
						s.ProcedureDate,
						s.Id,
						s.ActualRemovalDate,
						s.MadePermanent,
						s.PatientContactAttempts,
						s.Patient
					}).ToList();

				foreach (var filter in filters)
				{
					DateTime cutoff = filter.ProcedureDate.Value.AddMonths(3);

					if (filter.Patient.Active && cutoff.Date >= now)
						denominator++;

					if (filter.ActualRemovalDate.HasValue)
					{
						if (filter.ActualRemovalDate.Value <= cutoff)
						{
							numerator++;
							continue;
						}
					}

					int contact_attempts = filter
						.PatientContactAttempts
						.Count(c => c.Timestamp <= cutoff);

					if (contact_attempts >= 2)
					{
						numerator++;
						continue;
					}


					bool have_reassessment = false;
					have_reassessment = db.Tasks.Any(w => w.PatientFilterId == filter.Id && w.TaskTypeId == (int)TaskTypes.ReviewCase && w.ClosedDate.HasValue);
					if (have_reassessment)
					{
						numerator++;
						continue;
					}
				}

				if (denominator > 0)
					returned = (decimal)numerator / (decimal)denominator;
			}
			catch (Exception ex)
			{
				Logger.LogException(ex);
				Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
			}

			return returned;
		}


		[HttpPost]
		[Authorize()]
		public async Task<ActionResult> RetrievalRates()
		{
			//Avg and std deviation of filter dwell time over past 12 months

			DateTime now = DateTime.UtcNow;
			RetrievalRatesReturn returned = new RetrievalRatesReturn();
			var cu = CurrentUser();

			try
			{
				DateTime start = new DateTime(now.Date.Year, 1, 1);
				DateTime end = now.Date;

				int total_placed = db.PatientFilters.Count(c => c.ProcedureDate != null && c.OrganizationId == cu.OrganizationId);
				int total_removed = db.PatientFilters.Count(c => c.ProcedureDate != null && c.ActualRemovalDate != null && c.OrganizationId == cu.OrganizationId);
				decimal overall_rate = (decimal)total_removed / (decimal)total_placed;

				var patient_filters = await db.PatientFilters.AsNoTracking()
					.Where(w => w.ProcedurePhysicianId == cu.Id)
					.Where(w => w.ProcedureDate >= start && w.ProcedureDate <= end)
					.OrderBy(o => o.ProcedureDate)
					.ToListAsync();  // prune the result set later


				var grouped = patient_filters.GroupBy(g => g.ProcedureDate.Value.Month);

				grouped.ForEach(f =>
				{
					returned.Avg.Add((decimal)f.Count(c => c.ActualRemovalDate != null) / (decimal)f.Count());
				});

				returned.Overall = overall_rate;

				returned.Success = "true";
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
			}

			return Json(returned);
		}


		[HttpPost]
		[Authorize()]
		public async Task<ActionResult> OpenTaskCounts()
		{
			DateTime now = DateTime.UtcNow;
			var returned = new ReportReturn();
			var cu = CurrentUser();

			Dictionary<int, int> cd = new Dictionary<int, int>();

			try
			{
				var grouped = db.Tasks.AsNoTracking()
					.Where(w => w.OrganizationId == cu.OrganizationId && w.ClosedDate == null)
					.GroupBy(k => k.TaskTypeId);

				foreach (var group in grouped)
				{
					cd.Add(group.Key, group.Count());
				}

				if (!cd.ContainsKey((int)TaskTypes.BuildCase))
					cd.Add((int)TaskTypes.BuildCase, 0);

				if (!cd.ContainsKey((int)TaskTypes.ContactPCP))
					cd.Add((int)TaskTypes.ContactPCP, 0);

				//if (!cd.ContainsKey((int)TaskTypes.PatientContactDue))
				//	cd.Add((int)TaskTypes.PatientContactDue, 0);

				if (!cd.ContainsKey((int)TaskTypes.RetrievalDatePassed))
					cd.Add((int)TaskTypes.RetrievalDatePassed, 0);

				if (!cd.ContainsKey((int)TaskTypes.ReviewPCPPreferences))
					cd.Add((int)TaskTypes.ReviewPCPPreferences, 0);

				if (!cd.ContainsKey((int)TaskTypes.ScheduleRetrieval))
					cd.Add((int)TaskTypes.ScheduleRetrieval, 0);

				if (!cd.ContainsKey((int)TaskTypes.SendRegisteredLetters))
					cd.Add((int)TaskTypes.SendRegisteredLetters, 0);


				returned.Counts.Add(cd[(int)TaskTypes.BuildCase]);

				returned.Counts.Add(cd[(int)TaskTypes.ContactPCP]);

				//returned.Counts.Add(cd[(int)TaskTypes.PatientContactDue]);

				returned.Counts.Add(cd[(int)TaskTypes.RetrievalDatePassed]);

				returned.Counts.Add(cd[(int)TaskTypes.ScheduleRetrieval]);

				returned.Counts.Add(cd[(int)TaskTypes.SendRegisteredLetters]);

				returned.Success = "true";
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
			}

			return Json(returned);
		}

		[HttpPost]
		[Authorize()]
		public async Task<ActionResult> PatientFilterHistoryGraph(int patientFilterId)
		{
			DateTime now = DateTime.UtcNow;
			var returned = new PatientFilterHistoryGraphReturn();
			var cu = CurrentUser();

			var data = db.Tasks.AsNoTracking()
				.Where(w => w.OrganizationId == cu.OrganizationId)
				.Where(w => w.PatientFilterId == patientFilterId)
				.OrderBy(o => o.CreateTimestamp)
				.ToList();

			string date;
			foreach (var item in data)
			{
				date = $"{ item.CreateTimestamp.Year}/{ item.CreateTimestamp.Month.ToString("D2")}/{item.CreateTimestamp.Day.ToString("D2")}";
				returned.Dates.Add(date);
				returned.Tasks.Add(item.TaskType.Name);
			}

			var pf = db.PatientFilters
			.Where(w => w.OrganizationId == cu.OrganizationId)
			.Where(w => w.Id == patientFilterId)
			.SingleOrDefault();

			if (pf != null && pf.Id == patientFilterId)
			{
				if (pf.MadePermanent.HasValue)
				{
					DateTime tmp = pf.MadePermanent.Value;
					date = $"{tmp.Year}/{tmp.Month.ToString("D2")}/{tmp.Day.ToString("D2")}";
					returned.Dates.Add(date);
					returned.Tasks.Add("Made Permanent");
				}

				if (pf.ActualRemovalDate.HasValue)
				{
					DateTime tmp = pf.ActualRemovalDate.Value;
					date = $"{tmp.Year}/{tmp.Month.ToString("D2")}/{tmp.Day.ToString("D2")}";
					returned.Dates.Add(date);
					returned.Tasks.Add("Removed");
				}
			}
			returned.Success = "true";

			return Json(returned);
		}

		[HttpPost]
		[Authorize()]
		public async Task<ActionResult> ClinicSuccessRate()
		{
			DateTime now = DateTime.UtcNow;
			var returned = new DatedReportReturn();
			var cu = CurrentUser();

			int numerator = 0;
			int denominator = 0;

			Dictionary<int, int> cd = new Dictionary<int, int>();

			try
			{
				var data = db.PatientFilters.AsNoTracking()
					.Where(w => w.OrganizationId == cu.OrganizationId)
					.Where(w => w.IsTemporary == true)
					.Where(w => w.MadePermanent == null).ToList();

				var successes = data
					.Where(w => w.ActualRemovalDate != null)
					.Where(w => w.ActualRemovalDate <= w.ProcedureDate.Value.AddDays(90))
					.ToList();

				var failures = data.Except(successes);

				//if (data.Count() < 20)
				//{
				//	foreach (var item in data)
				//	{
				//		if (successes.Contains(item))
				//		{
				//			numerator++;
				//		}
				//		denominator++;

				//		returned.Scores.Add((decimal)numerator / (decimal)denominator);
				//		returned.Dates.Add($"{item.ProcedureDate.Value.Year}/{item.ProcedureDate.Value.Month.ToString("D2")}/{item.ProcedureDate.Value.Day.ToString("D2")}");
				//	}
				//}
				//else
				//{

				var grouped = successes.GroupBy(g => g.ProcedureDate.Value.Year).OrderBy(o => o.Key);

				foreach (var group in grouped)
				{
					int year = group.Key;
					int monthnumber = 0;

					var subgroup = group.GroupBy(g => g.ProcedureDate.Value.Month).OrderBy(o => o.Key);
					foreach (var month in subgroup)

					{
						monthnumber = month.Key;
						numerator += month.Count();
						denominator += month.Count() + failures.Where(w => w.ProcedureDate.Value.Year == year && w.ProcedureDate.Value.Month == monthnumber).Count();

						returned.Scores.Add((decimal)numerator / (decimal)denominator);
						returned.Dates.Add($"{monthnumber.ToString("D2")}/{year}");
					}

				}
				//}


				returned.Success = "true";
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
			}

			return Json(returned);
		}

		[HttpPost]
		[Authorize()]
		public async Task<ActionResult> PatientContactResults()
		{
			DateTime now = DateTime.UtcNow;
			var returned = new DatedReportReturn();
			var cu = CurrentUser();

			int numerator = 0;
			int denominator = 0;

			var data = db.PatientContactAttempts.AsNoTracking()
				.Where(w => w.CreateUserId == cu.Id)
				.OrderBy(o => o.Timestamp).ToList();

			if (data.Count > 20)
			{

				var grouped = data.GroupBy(g => g.Timestamp.Year);
				foreach (var group in grouped)
				{
					int year = group.Key;
					int monthnumber = 0;
					var subgroup = group.GroupBy(g2 => g2.Timestamp.Month);

					foreach (var month in subgroup)
					{
						monthnumber = month.Key;
						numerator += month.Count(w => w.ContactResultCodeId == (int)ContactResultTypes.Success);
						denominator += month.Count();

						string date = $"{ year}/{ monthnumber.ToString("D2")}/15";
						returned.Dates.Add(date);
						returned.Scores.Add((decimal)(numerator) / (decimal)(denominator));
					}


				}
				returned.Success = "true";
			}
			else
			{
				foreach (var item in data)
				{
					if (item.ContactResultCodeId == (int)ContactResultTypes.Success)
					{
						numerator++;
					}
					denominator++;

					string date = $"{ item.Timestamp.Year}/{ item.Timestamp.Month.ToString("D2")}/{item.Timestamp.Day.ToString("D2")}";
					returned.Dates.Add(date);
					returned.Scores.Add((decimal)(numerator) / (decimal)(denominator));
				}
				returned.Success = "true";
			}

			return Json(returned);
		}

		private string GetMonth(int month, int year)
		{
			return $"{year}/{month.ToString("D2")}/15";
		}


		[HttpGet]
		[Authorize()]
		public async Task<ActionResult> ActiveCases()
		{
			ActiveCasesModel model = new ActiveCasesModel();
			var cu = CurrentUser();

			try
			{
				model.Patients.AddRange(db.Patients.AsNoTracking().Where(w => w.OrganizationId == cu.OrganizationId).ToList());
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return View(model);
		}

		[HttpGet]
		[Authorize()]
		public async Task<ActionResult> PatientList()
		{
			PatientListModel model = new PatientListModel();
			var cu = CurrentUser();
			try
			{
				var qry = db.Patients
					.AsNoTracking()
					.Where(w => w.Active);


				qry = qry.OrderBy(o => o.LastName).ThenBy(tb => tb.FirstName);
				if (User.IsInRole("SuperUsers"))
				{
					model.Patients = qry.ToList();
				}
				else
				{
					qry = qry.Where(w => w.OrganizationId == cu.OrganizationId);
					model.Patients = qry.ToList();
				}

			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				//model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return View(model);
		}

		[Authorize()]
		[HttpGet]
		public async Task<ActionResult> CreatePatient()
		{
			var cu = CurrentUser();
			var model = new PatientEditorModel();
			try
			{

				model.Active = true;

				User u = cu;
				if (u != null || u.Id > 0)
				{
					model.OrganizationId = u.OrganizationId;
				}
				else
				{
					Logger.Log.Error($"Unable to find OrganizationId for logged on user: {User.Identity.Name }.");
					ModelState.AddModelError("", "Unable to validate logged on user details.");
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return View(model);
		}

		[Authorize()]
		[HttpPost]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> CreatePatient(PatientEditorModel model)
		{
			if (!ModelState.IsValid)
			{
				var errors = GetModelsStateErrors(ModelState);
				model.ErrorMessage = String.Join("<br/>", errors);
				return View(model);
			}

			var cu = CurrentUser();

			DateTime now = DateTime.UtcNow;

			try
			{

				ViewModelValidationResult validation = model.IsValid();

				if (validation.IsValid)
				{
					User u = cu;

					if (u != null || u.Id > 0)
					{

						Patient inserted = model.ToPatient();
						inserted.CreateTimestamp = now;
						inserted.CreateUserId = u.Id;
						inserted.UpdateTimestamp = inserted.CreateTimestamp;
						inserted.UpdateUserId = u.Id;

						db.Patients.Add(inserted);

						int changes = await db.SaveChangesAsync();

						return Json(new { Status = "OK", Message = "", Id = inserted.Id });

					}
					else
					{
						Logger.Log.Error($"Unable to find OrganizationId for logged on user: {User.Identity.Name }.");
						return Json(new { Status = "FAIL", Error = "Unable to validate logged on user details." });
					}
				}
				else
				{
					return Json(new { Status = "FAIL", Message = "", Errors = validation.Errors });
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				model.ErrorMessage = GetErrorMessage(now);
			}
			string msg = GetErrorMessage(now);
			return Json(new { Status = "FAIL", Message = msg });
		}

		[Authorize()]
		[HttpGet]
		public async Task<ActionResult> EditPatient(int id)
		{
			var model = new PatientEditorModel();

			var cu = CurrentUser();
			try
			{
				Patient patient = await db.Patients.SingleOrDefaultAsync(sd => sd.Id == id && sd.OrganizationId == cu.OrganizationId);
				if (patient != null && patient.Id == id)
				{
					model.PopulateFromPatient(patient);
				}
				else
				{
					model.ErrorMessage = "Not found.";
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return View(model);
		}

		[Authorize()]
		[HttpPost]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> EditPatient(PatientEditorModel model)
		{
			var cu = CurrentUser();
			DateTime now = DateTime.UtcNow;
			try
			{
				if (model != null && model.Id > 0)
				{
					Patient patient = db.Patients.SingleOrDefault(sd => sd.Id == model.Id && sd.OrganizationId == cu.OrganizationId);
					if (patient != null && patient.Id == model.Id)
					{
						bool isDeceased = patient.DeceasedDate.HasValue;

						if (Patient.Overlay(model, ref patient))
						{

							if (!isDeceased && patient.DeceasedDate.HasValue)
							{
								// User provided a deceased date for patient
								// go find all tasks and close them.
								foreach (var pf in patient.PatientFilters)
								{
									foreach (var t in pf.Tasks)
									{
										if (t.ClosedDate.HasValue == false)
											t.ClosedDate = now;
									}
								}
							}

							patient.UpdateTimestamp = now;
							patient.UpdateUserId = cu.Id;
							await db.SaveChangesAsync();
							return RedirectToAction("PatientList");
						}
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
				model.ErrorMessage = GetErrorMessage(now);
			}

			return View(model);
		}

		[Authorize()]
		[HttpGet]
		public ActionResult PatientDetails(int id)
		{
			var cu = CurrentUser();
			var model = new PatientDetailsModel(id, cu.OrganizationId, ref db, ref cu);

			return View(model);
		}


		[Authorize()]
		[HttpGet]
		public async Task<ActionResult> CreatePatientFilter(int id)
		{
			var model = new PatientFilterEditorModel();
			var cu = CurrentUser();
			try
			{
				model.AmEditing = false;
				model.OrganizationId = cu.OrganizationId;
				model.PatientId = id;
				model.IsTemporary = true;

				model.HydrateLookups(model.OrganizationId, ref db);
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return View(model);
		}

		[Authorize()]
		[HttpPost]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> CreatePatientFilter(PatientFilterEditorModel model)
		{
			DateTime now = DateTime.UtcNow;
			var cu = CurrentUser();

			try
			{
				PatientFilter patientFilter = model.ToPatientFilter();

				if (patientFilter.IsValid() && model.OrganizationId == cu.OrganizationId)
				{

					patientFilter.CreateUserId = cu.Id;
					patientFilter.CreateTimestamp = now;
					patientFilter.UpdateUserId = patientFilter.CreateUserId;
					patientFilter.UpdateTimestamp = patientFilter.CreateTimestamp;

					db.PatientFilters.Add(patientFilter);

					await db.SaveChangesAsync();

					//var officeadmin = GetOfficeAdmin(cu.OrganizationId);

					//var task = new Models.Task();

					//task.OrganizationId = cu.OrganizationId;
					//task.AssignedUserId = officeadmin.Id;
					//task.CreateTimestamp = now;
					//task.CreateUserId = cu.Id;
					//task.PatientFilterId = patientFilter.Id;
					//task.PatientId = patientFilter.PatientId;
					//task.TaskTypeId = (int)Models.TaskTypes.ReviewPCPPreferences;
					//task.UpdateTimestamp = now;
					//task.UpdateUserId = cu.Id;

					//db.Tasks.Add(task);

					var filter_age = cu.Organization.OrganizationDefaults.FirstOrDefault().FilterAge;
					var default_filter_age = db.OrganizationDefaults.Single(s => s.Id == 1).FilterAge;

					var task2 = new Models.Task();

					DateTime marker = patientFilter.ProcedureDate.HasValue ? patientFilter.ProcedureDate.Value : now;

					task2.HideUntil = marker.AddDays(filter_age.HasValue && filter_age.Value > 0 ? default_filter_age.Value : default_filter_age.Value);
					task2.OrganizationId = cu.OrganizationId;
					task2.CreateTimestamp = now;
					task2.CreateUserId = cu.Id;
					task2.PatientFilterId = patientFilter.Id;
					task2.PatientId = patientFilter.PatientId; ;
					task2.TaskTypeId = (int)Models.TaskTypes.BuildCase;
					task2.UpdateTimestamp = now;
					task2.UpdateUserId = cu.Id;

					db.Tasks.Add(task2);

					int changes = await db.SaveChangesAsync();
				}
				else
				{
					model.Errors.AddRange(patientFilter.ValidationErrors);
					return View(model);
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				model.Errors.Add(GetErrorMessage(now));
				return View(model);
			}

			return RedirectToAction("PatientDetails", new { Id = model.PatientId });
		}

		[Authorize()]
		[HttpGet]
		public async Task<ActionResult> EditPatientFilter(int id)
		{
			var cu = CurrentUser();
			var model = new PatientFilterEditorModel();
			try
			{
				model.OrganizationId = cu.OrganizationId;
				model.PatientFilterId = id;

				var pf = await db.PatientFilters.AsNoTracking().SingleOrDefaultAsync(sd => sd.Id == id);
				model.PatientId = pf.PatientId;

				model.ActualRemovalDate = pf.ActualRemovalDate.HasValue ? pf.ActualRemovalDate.Value.ToShortDateString() : "";
				model.Location = pf.Location;
				model.Notes = pf.Notes;
				model.ProcedureDate = pf.ProcedureDate.HasValue ? pf.ProcedureDate.Value.ToShortDateString() : "";
				model.SelectedComplicatingFactorId = pf.ComplicatingFactorId.HasValue ? pf.ComplicatingFactorId.Value : 0;
				model.SelectedFilterId = pf.FilterId;
				model.SelectedIndicationId = pf.IndicationId.HasValue ? pf.IndicationId.Value : 0;
				model.SelectedOrderingPhysicianId = pf.OrderingPhysicianId.HasValue ? pf.OrderingPhysicianId.Value : 0;
				model.SelectedPrimaryCarePhysicianId = pf.PrimaryCarePhysicianId.HasValue ? pf.PrimaryCarePhysicianId.Value : 0;
				model.TargetRemovalDate = pf.TargetRemovalDate.HasValue ? pf.TargetRemovalDate.Value.ToShortDateString() : "";
				model.SelectedProcedurePhysicianId = pf.ProcedurePhysicianId.HasValue ? pf.ProcedurePhysicianId.Value : 0;
				model.HydrateLookups(model.OrganizationId, ref db);
				model.IsTemporary = pf.IsTemporary;
				model.AmEditing = true;
				model.MadePermanent = pf.MadePermanent;
				model.MadePermanentBy = pf.MadePermanentBy;
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				model.ErrorMessage = GetErrorMessage(DateTime.UtcNow);
			}

			return View(model);
		}

		[Authorize()]
		[HttpPost]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> EditPatientFilter(PatientFilterEditorModel model)
		{
			DateTime now = DateTime.UtcNow;
			var cu = CurrentUser();

			try
			{
				PatientFilter pf = model.ToPatientFilter();

				if (pf.IsValid() && pf.OrganizationId == cu.OrganizationId)
				{


					PatientFilter entity = await db.PatientFilters.SingleOrDefaultAsync(sd => sd.Id == model.PatientFilterId);

					if (entity != null && entity.Id == model.PatientFilterId)
					{
						bool changed = false;
						bool close_all_tasks = false;

						if (entity.ActualRemovalDate != pf.ActualRemovalDate)
						{
							entity.ActualRemovalDate = pf.ActualRemovalDate;
							changed = true;
							close_all_tasks = true;
						}

						if (entity.ComplicatingFactorId != pf.ComplicatingFactorId)
						{
							entity.ComplicatingFactorId = pf.ComplicatingFactorId;
							changed = true;
						}

						if (entity.FilterId != pf.FilterId)
						{
							entity.FilterId = pf.FilterId;
							changed = true;
						}

						if (entity.IndicationId != pf.IndicationId)
						{
							entity.IndicationId = pf.IndicationId;
							changed = true;
						}

						if (entity.Location != pf.Location)
						{
							entity.Location = pf.Location;
							changed = true;
						}

						if (entity.Notes != pf.Notes)
						{
							entity.Notes = pf.Notes;
							changed = true;
						}

						if (entity.OrderingPhysicianId != pf.OrderingPhysicianId)
						{
							entity.OrderingPhysicianId = pf.OrderingPhysicianId;
							changed = true;
						}

						if (entity.PrimaryCarePhysicianId != pf.PrimaryCarePhysicianId)
						{
							entity.PrimaryCarePhysicianId = pf.PrimaryCarePhysicianId;
							changed = true;
						}

						if (entity.ProcedureDate != pf.ProcedureDate)
						{
							entity.ProcedureDate = pf.ProcedureDate;
							changed = true;
						}

						if (entity.ProcedurePhysicianId != pf.ProcedurePhysicianId)
						{
							entity.ProcedurePhysicianId = pf.ProcedurePhysicianId;
							changed = true;
						}

						if (entity.TargetRemovalDate != pf.TargetRemovalDate)
						{
							entity.TargetRemovalDate = pf.TargetRemovalDate;
							changed = true;
						}

						if (entity.IsTemporary != pf.IsTemporary)
						{
							entity.IsTemporary = pf.IsTemporary;
							changed = true;
						}

						if (entity.MadePermanentBy != pf.MadePermanentBy)
						{
							entity.MadePermanentBy = pf.MadePermanentBy;
							changed = true;
						}

						if (entity.MadePermanent != pf.MadePermanent)
						{
							entity.MadePermanent = pf.MadePermanent;
							if (entity.MadePermanent.HasValue)
							{
								close_all_tasks = true;
							}

							changed = true;
						}

						if (changed)
						{
							entity.UpdateUserId = cu.Id;
							entity.UpdateTimestamp = now;

							if (close_all_tasks)
							{
								entity.Tasks.ForEach(f =>
								{
									f.ClosedDate = now;
									f.UpdateUserId = cu.Id;
									f.UpdateTimestamp = now;
								});
							}
						}
					}

					await db.SaveChangesAsync();
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				model.ErrorMessage = GetErrorMessage(now);
			}

			return RedirectToAction("PatientDetails", new { Id = model.PatientId });
		}

		[Authorize()]
		[HttpGet]
		public async Task<ActionResult> GetTaskEditor(int TaskId)
		{

			var cu = CurrentUser();
			try
			{

				Models.Task task = db.Tasks.AsNoTracking()
				.Include(i => i.PatientQuestionResponses).AsNoTracking()
				.Include(i2 => i2.PhysicianQuestionResponses).AsNoTracking()
				.SingleOrDefault(sd => sd.Id == TaskId && sd.OrganizationId == cu.OrganizationId);

				if (task != null)
				{
					switch (task.TaskTypeId)
					{
						case (int)TaskTypes.BuildCase:
							{
								var model = new BuildCaseTaskEditorModel(db, cu, task);
								return PartialView("_BuildCaseTaskEditor", model);
							}
						case (int)TaskTypes.RetrievalDatePassed:
							{
								var model = new RetrievalDatePassedEditorViewModel(task, db);

								return PartialView("_RetrievalDatePassedTaskEditor", model);
							}
						case (int)TaskTypes.ReviewCase:
							{
								var model = new ReviewCaseTaskEditorViewModel(task, cu, db);
								return PartialView("_ReviewCaseTaskEditor", model);
							}
						case (int)TaskTypes.ReviewPCPPreferences:
							{
								var model = new ReviewPCPPreferencesTaskEditorViewModel();
								return PartialView("_ReviewPCPPreferencesTaskEditor", model);
							}
						case (int)TaskTypes.ScheduleRetrieval:
							{
								var model = new ScheduleRetrievalTaskEditorViewModel(task, db, cu);
								return PartialView("_ScheduleRetrievalTaskEditor", model);
							}
						case (int)TaskTypes.SendRegisteredLetters:
							{
								var model = new SendRegisteredLettersTaskEditorViewModel(task);
								return PartialView("_SendRegisteredLetterTaskEditor", model);
							}
						case (int)TaskTypes.PatientContactDue:
							{
								var model = new PatientContactAttemptDueTaskEditorViewModel(task, db);
								return PartialView("_PatientContactTaskEditor", model);
							}
						case (int)TaskTypes.ContactPCP:
							{
								var model = new ContactPCPTaskEditorViewModel(task, db);
								return PartialView("_ContactPCPTaskEditor", model);
							}
						default:
							{
								BuildCaseTaskEditorModel model = new BuildCaseTaskEditorModel();
								return PartialView("", model);
							}
					}
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
			}

			return PartialView("", null);
		}

		[Authorize()]
		[HttpPost]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> SaveBuildCaseTask(BuildCaseTaskEditorViewModel model)
		{
			DateTime now = DateTime.UtcNow;
			bool patient_contacted = false;
			bool phys_contacted = false;
			string msg = "";

			UpdateBuildCaseTaskResult update_result = null;
			var cu = CurrentUser();
			try
			{
				Models.Task task = db.Tasks
				.Include(i => i.TaskType)
				.SingleOrDefault(sd => sd.Id == model.TaskId && sd.OrganizationId == cu.OrganizationId);

				if (task != null && task.Id == model.TaskId)
				{
					update_result = UpdateBuildCaseTask(model);
				}

				//if (!task.PatientContactAttempts.Any())
				//{
				string note;
				int contact_type_id = 0;
				int contact_result_id = 0;

				note = Request.Form["Note"];
				if (int.TryParse(Request.Form["ResultCodeId"], out contact_result_id))
				{
					if (int.TryParse(Request.Form["ContactTypeId"], out contact_type_id))
					{
						// save patient contact record
						var added = new PatientContactAttempt()
						{
							ContactTypeId = contact_type_id,
							ContactResultCodeId = contact_result_id,
							Notes = note,
							CreateTimestamp = now,
							CreateUserId = cu.Id,
							OrganizationId = cu.OrganizationId,
							PatientFilterId = task.PatientFilterId.Value,
							Timestamp = now,
							UpdateTimestamp = now,
							UpdateUserId = cu.Id,
							RelatedTaskId = task.Id
						};

						db.PatientContactAttempts.Add(added);

						if (contact_result_id == (int)ContactResultTypes.Failure)
						{
							if (task.ConsecutiveContacts.HasValue)
							{
								task.ConsecutiveContacts += 1;
							}
							else
							{
								task.ConsecutiveContacts = 1;
							}
						}
						//else
						//{
						//	task.ConsecutiveContacts = 0;
						//}

						if (!string.IsNullOrEmpty(note))
						{
							if (string.IsNullOrEmpty(task.Notes))
							{
								task.Notes = added.Notes;
							}
							else
							{
								task.Notes = $"<br/><br/>{added.Notes}";
							}
						}

						db.SaveChanges();
						patient_contacted = true;
					}
				}


				//}

				//if (!task.PhysicianContactAttempts.Any())
				//{
				if (model.PhysicianQuestionResponses.Count() > 0)
				{
					note = Request.Form["PhysicianContactNote"];
					if (int.TryParse(Request.Form["PhysicianContactResultCodeId"], out contact_result_id))
					{
						if (int.TryParse(Request.Form["PhysicianContactTypeId"], out contact_type_id))
						{
							// save patient contact record
							var added = new PhysicianContactAttempt()
							{
								ContactTypeId = contact_type_id,
								ContactResultCodeId = contact_result_id,
								Notes = note,
								CreateTimestamp = now,
								CreateUserId = cu.Id,
								OrganizationId = cu.OrganizationId,
								PatientFilterId = task.PatientFilterId.Value,
								Timestamp = now,
								UpdateTimestamp = now,
								UpdateUserId = cu.Id,
								RelatedTaskId = task.Id
							};

							db.PhysicianContactAttempts.Add(added);

							if (!string.IsNullOrEmpty(note))
							{
								if (string.IsNullOrEmpty(task.Notes))
								{
									task.Notes = $"Build Case Physician Contact Note: {added.Notes}";
								}
								else
								{
									task.Notes = $"<br/><br/>Build Case Physician Contact Note: {added.Notes}";
								}
							}
							db.SaveChanges();
							phys_contacted = true;
						}
					}
				}
				//}


			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				msg = GetErrorMessage(now);
			}

			if (!model.AmCompleting)
			{
				if (!patient_contacted && !phys_contacted && !update_result.Success)
				{
					msg += msg.Length > 0 ? "\r\n\r\nNothing to save." : "Nothing to save.";
					return new JsonResult() { Data = new { Success = "n", Message = msg } };
				}

				return new JsonResult() { Data = new { Success = "y", Message = "" } };
			}
			else
			{
				if (string.IsNullOrEmpty(msg))
				{
					return new JsonResult() { Data = new { Success = "y", Message = "" } };
				}

				return new JsonResult() { Data = new { Success = "n", Message = msg } };
			}
		}

		[Authorize()]
		[HttpPost]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> UploadTaskAttachment(int taskId)
		{
			string[] allowed_file_extensions = { ".doc", ".docx", ".png", ".jpg", ".pdf" };

			var cu = CurrentUser();
			//C:\Users\Darin\Documents\FilterTracker\Task_Descriptions.pdf
			Models.Task task = db.Tasks.SingleOrDefault(sd => sd.Id == taskId && sd.OrganizationId == cu.OrganizationId);

			string errors = "";
			string filename = "";
			string filesize = "";
			string uploaded = "";

			if (task != null && task.Id == taskId)
			{

				HttpPostedFileBase file = null;
				for (int i = 0; i < Request.Files.Count; i++)
				{
					bool filetype_allowed = false;
					string ext;

					try
					{
						file = Request.Files[i];
						ext = file.FileName.ToLower();
						if (!string.IsNullOrEmpty(ext))
						{
							int needle = ext.LastIndexOf('.');
							if (needle > 0)
							{
								ext = ext.Substring(needle);

								if (allowed_file_extensions.Contains(ext))
								{
									filetype_allowed = true;
								}
							}
						}

						if (!filetype_allowed)
						{
							Logger.Log.Warn($"Invalid file upload attempt for TaskId: {taskId.ToString()} for file: {file.FileName} with extension type: {ext}.");
							if (string.IsNullOrEmpty(errors))
								errors = "File type not allowed.";
							else
								errors += "\r\n" + "File type not allowed.";
							continue;
						}

						TaskAttachment ta = new TaskAttachment();

						ta.CreateTimestamp = DateTime.UtcNow;
						ta.CreateUserId = cu.Id;
						ta.TaskId = taskId;
						ta.UpdateTimestamp = ta.CreateTimestamp;
						ta.UpdateUserId = cu.Id;

						Attachment attachment = new Attachment();

						attachment.FileName = file.FileName.Split(new char[] { '\\' }).Last();
						attachment.FileData = new byte[file.ContentLength];
						file.InputStream.Read(attachment.FileData, 0, file.ContentLength);
						attachment.CreateTimestamp = ta.CreateTimestamp;
						attachment.CreateUserId = ta.CreateUserId;
						attachment.UpdateTimestamp = ta.UpdateTimestamp;
						attachment.UpdateUserId = ta.UpdateUserId;
						attachment.FileSize = file.ContentLength;

						ta.Attachment = attachment;

						task.TaskAttachments.Add(ta);

						db.SaveChanges();

						filename = attachment.FileName;
						filesize = file.ContentLength.ToString();
						uploaded = ta.CreateTimestamp.AddHours(cu.Organization.TimezoneOffset).ToString();

					}
					catch (Exception ex)
					{
						Logger.LogException(ex);
						string error = $"Error uploading file: {file.FileName}.";
						if (string.IsNullOrEmpty(errors))
							errors = error;
						else
							errors += "\r\n" + error;
					}
				}

				filesize = FormattingHelpers.GetFileSizeString(filesize);
			}
			else
			{
				errors = "Task not found.";
			}

			JsonResult returned = new JsonResult();
			returned.Data = new { Success = string.IsNullOrEmpty(errors) ? "true" : "false", FileName = filename, FileSize = filesize, DateUploaded = uploaded, Errors = errors };

			return returned;


		}

		[Authorize()]
		[HttpGet]
		public ActionResult DeleteUpload(int taskAttachmentId)
		{
			string error = "";
			bool isSuper = false;
			bool isOrgadmin = false;
			var cu = CurrentUser();

			isSuper = User.IsInRole(Roles.SuperUsers);

			try
			{
				if (isSuper)
				{
					var ta = db.TaskAttachments.SingleOrDefault(sd => sd.Id == taskAttachmentId);
					if (ta != null && ta.Id == taskAttachmentId)
					{
						var file_id = ta.AttachmentId;
						var attachment = db.Attachments.SingleOrDefault(sd => sd.Id == file_id);


						var all_related_task_attachments = db.TaskAttachments.Where(w => w.AttachmentId == attachment.Id);
						db.TaskAttachments.RemoveRange(all_related_task_attachments);
						db.Attachments.Remove(attachment);
						db.SaveChanges();
					}
				}
				else
				{
					var ta = db.TaskAttachments.SingleOrDefault(sd => sd.Id == taskAttachmentId);

					if (ta != null && ta.Id == taskAttachmentId)
					{
						if (ta.CreateUserId == cu.Id || (isOrgadmin && cu.OrganizationId == ta.Task.OrganizationId))
						{
							var file_id = ta.AttachmentId;
							var attachment = db.Attachments.SingleOrDefault(sd => sd.Id == file_id);


							var all_related_task_attachments = db.TaskAttachments.Where(w => w.AttachmentId == attachment.Id);
							db.TaskAttachments.RemoveRange(all_related_task_attachments);
							db.Attachments.Remove(attachment);
							db.SaveChanges();
						}
						else
						{
							error = "Unauthorized.";
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogException(ex, ex.Message);
				error = "Unable to delete file.  Please contact technical support.";
			}


			JsonResult returned = new JsonResult();
			returned.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
			returned.Data = new { Success = string.IsNullOrEmpty(error) ? "true" : "false", Error = error };

			return returned;
		}

		[Authorize()]
		[HttpGet]
		public async Task<ActionResult> DownloadFile(int id)
		{
			var cu = CurrentUser();

			try
			{
				var taskAttachment = await db.TaskAttachments
					.SingleOrDefaultAsync(sd => sd.Id == id && sd.Task.OrganizationId == cu.OrganizationId);

				if (taskAttachment != null)
				{
					return new FileContentResult(taskAttachment.Attachment.FileData, System.Net.Mime.MediaTypeNames.Application.Octet)
					{
						FileDownloadName = taskAttachment.Attachment.FileName
					};
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
			}

			return new HttpUnauthorizedResult();
		}

		[Authorize()]
		[HttpPost]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> CompleteTask(int taskId, int? antecedentTaskTargetUserId)
		{
			Models.Task task = db.Tasks.SingleOrDefault(sd => sd.Id == taskId);
			bool success = false;
			DateTime now = DateTime.UtcNow;

			var cu = CurrentUser();

			try
			{
				User task_user = task.AssignedUser;

				if (cu.Id == task.AssignedUser.Id && cu.OrganizationId == task.OrganizationId)
				{
					if (task.IsValid())
					{
						if (task.TaskTypeId == (int)TaskTypes.BuildCase)
						{
							int days = 0;
							int attempts = 0;
							if (cu.Organization.OrganizationDefaults.Any())
							{
								var defs = db.OrganizationDefaults.SingleOrDefault(s => s.OrganizationId == cu.OrganizationId);
								days = defs.PatientContactDays.Value;
								attempts = defs.ContactAttemptsBeforeRegisteredLetter.Value;
							}
							else
							{
								var defs = db.OrganizationDefaults.Single(s => s.OrganizationId == 1);
								days = defs.PatientContactDays.Value;
								attempts = defs.ContactAttemptsBeforeRegisteredLetter.Value;
							}
							//if (task.TaskAttachments.Count() == 0 && task.PatientContactAttempts.Count() == 0 && task.PhysicianContactAttempts.Count() == 0 && task.PatientQuestionResponses.Count == 0 && task.PhysicianQuestionResponses.Count() == 0)
							//{
							//	task.ValidationErrors.Add("This task has no content.  Unable to complete task.");
							//	success = false;
							//}

							// do we have any successful contact attempts?
							if (task.PatientContactAttempts.Count(c => c.ContactResultCodeId == (int)ContactResultTypes.Success) == 0)
							{
								// build case without successful contact
								if (task.ConsecutiveContacts > attempts)
								{
									// time for registered letter

									task.ClosedDate = now;

									// create a send registered letter task
									var lrt = new Models.Task();

									lrt.CreateUserId = cu.Id;
									lrt.UpdateUserId = cu.Id;
									lrt.CreateTimestamp = now;
									lrt.UpdateTimestamp = now;
									lrt.TaskTypeId = (int)TaskTypes.SendRegisteredLetters;
									lrt.OrganizationId = cu.OrganizationId;
									lrt.PatientFilterId = task.PatientFilterId;
									lrt.PatientId = task.PatientId;

									db.Tasks.Add(lrt);
								}
								else
								{
									task.HideUntil = now.AddDays(days).Date;
									task.UpdateTimestamp = now;
									task.UpdateUserId = cu.Id;
								}
								db.SaveChanges();
								success = true;
							}
							else
							{
								task.ClosedDate = now;
								task.ConsecutiveContacts = 0;

								var defphy = db.UserRoles
									.Where(w => w.Role.Name == Roles.Physician)
									.Where(w => w.User.OrganizationId == cu.OrganizationId)
									.Where(w => w.User.DefaultReviewer == true)
									.Select(s => s.User)
									.ToList();

								if (antecedentTaskTargetUserId.HasValue)
								{
									task.CreateAntecedentTasks(db, antecedentTaskTargetUserId.Value);
								}
								else
								{
									if (defphy != null && defphy.Count() > 0)
									{
										task.CreateAntecedentTasks(db, defphy.First().Id);
										
									}
								}

								db.SaveChanges();
								success = true;
							}

						}
						else if (task.TaskTypeId == (int)TaskTypes.ReviewCase)
						{
							string action = Request.Form["Action"];
							if (!string.IsNullOrEmpty(action))
							{


								switch (action)
								{
									case "ScheduleRemoval":
										bool override_pcp = Request.Form["Override"] == "on" || task.PatientFilter.IsPCPApproved;
										string schedule_retrieval_note = Request.Form["ScheduleRetrievalNote"];
										if (task.PatientFilterId.HasValue && task.PatientFilter != null)
										{

											if (task.PatientFilter.PrimaryCarePhysicianId.HasValue && task.PatientFilter.PrimaryCarePhysicianId.Value > 0)
											{
												if (task.PatientFilter.PrimaryCarePhysician.RequiresRemovalApproval)
												{
													if (!override_pcp)
													{
														task.ValidationErrors.Add("Primary care/referring MD approval requested.  Please choose the contact PCP option.");
														JsonResult jr = new JsonResult();
														jr.Data = new
														{
															Success = false,
															Errors = task.ValidationErrors.ToArray()
														};

														return jr;
													}
												}
											}

											// Create Schedule Removal Task
											var srtask = new Models.Task();

											srtask.CreateUserId = cu.Id;
											srtask.UpdateUserId = cu.Id;
											srtask.OrganizationId = cu.OrganizationId;
											srtask.AssignedUserId = null; // GetOfficeAdmin(cu.OrganizationId).Id;
											srtask.CreateTimestamp = DateTime.UtcNow;
											srtask.UpdateTimestamp = srtask.CreateTimestamp;
											srtask.TaskTypeId = (int)TaskTypes.ScheduleRetrieval;
											srtask.PatientId = task.PatientId;
											srtask.PatientFilterId = task.PatientFilterId;
											if (!string.IsNullOrEmpty(schedule_retrieval_note))
											{
												srtask.Notes = schedule_retrieval_note.Trim();
											}
											db.Tasks.Add(srtask);
										}
										break;
									case "Reassess":
										string reassess_days = Request.Form["ReassessDays"];
										if (task.PatientFilterId.HasValue)
										{
											var pf = db.PatientFilters.SingleOrDefault(sd => sd.Id == task.PatientFilterId.Value);
											if (pf != null)
											{
												int tmp;
												if (int.TryParse(reassess_days.Trim(), out tmp))
												{
													pf.ReassessmentDate = now.AddDays(tmp).Date;

													var reassess_task = new Models.Task();

													reassess_task.HideUntil = pf.ReassessmentDate;
													reassess_task.CreateTimestamp = now;
													reassess_task.CreateUserId = cu.Id;
													reassess_task.Notes = task.Notes;
													reassess_task.OrganizationId = cu.OrganizationId;
													reassess_task.PatientFilterId = pf.Id;
													reassess_task.PatientId = pf.PatientId;
													reassess_task.TargetCloseDate = now.AddDays(14);
													reassess_task.TaskTypeId = (int)TaskTypes.BuildCase;
													reassess_task.UpdateTimestamp = now;
													reassess_task.UpdateUserId = cu.Id;

													db.Tasks.Add(reassess_task);
													db.SaveChanges();

													bool needsave = false;
													foreach (var ta in task.TaskAttachments)
													{
														var added = new TaskAttachment();

														added.AttachmentId = ta.AttachmentId;
														added.CreateTimestamp = now;
														added.CreateUserId = cu.Id;
														added.TaskId = reassess_task.Id;
														added.UpdateTimestamp = now;
														added.UpdateUserId = cu.Id;

														reassess_task.TaskAttachments.Add(added);
														needsave = true;
													}

													if (needsave)
														db.SaveChanges();
												}
											}
										}
										break;
									case "ContactPCP":
										string contact_reason = Request.Form["ContactReason"];

										if (task.PatientFilterId.HasValue)
										{
											if (!string.IsNullOrEmpty(contact_reason))
											{
												contact_reason = contact_reason.Trim();

												var pf = db.PatientFilters.SingleOrDefault(sd => sd.Id == task.PatientFilterId.Value);
												if (pf != null)
												{
													pf.Notes = UpdatePatientNotes(pf.Notes, contact_reason);
												}
											}
											// Create Contact PCP Task.
											var pcptask = new Models.Task();

											pcptask.Notes = $"{cu.FirstName} {cu.LastName} -- {contact_reason}.";
											pcptask.CreateUserId = cu.Id;
											pcptask.UpdateUserId = cu.Id;
											pcptask.OrganizationId = cu.OrganizationId;
											pcptask.AssignedUserId = null; // GetOfficeAdmin(cu.OrganizationId).Id;
											pcptask.CreateTimestamp = DateTime.UtcNow;
											pcptask.UpdateTimestamp = pcptask.CreateTimestamp;
											pcptask.TaskTypeId = (int)TaskTypes.ContactPCP;
											pcptask.PatientId = task.PatientId;
											pcptask.PatientFilterId = task.PatientFilterId;

											db.Tasks.Add(pcptask);
										}
										break;
									case "MakePermanent":
										string mpnote = Request.Form["MakePermanentNote"];
										task.PatientFilter.MadePermanent = now;
										task.PatientFilter.MadePermanentBy = $"{cu.LastName},{cu.FirstName}";
										task.PatientFilter.UpdateTimestamp = now;
										task.PatientFilter.UpdateUserId = cu.Id;
										task.PatientFilter.Tasks.ForEach(f =>
										{
											f.ClosedDate = now;
											f.UpdateUserId = cu.Id;
											f.UpdateTimestamp = now;
										});
										if (!string.IsNullOrEmpty(mpnote))
										{
											task.Notes = mpnote.Trim();
										}
										break;
								}

								task.ClosedDate = now;
								task.UpdateTimestamp = now;
								task.UpdateUserId = cu.Id;
								success = true;

								db.SaveChanges();
							}
						}
						else if (task.TaskTypeId == (int)TaskTypes.RetrievalDatePassed)
						{

							string note;
							int contact_type_id = 0;
							int contact_result_id = 0;

							note = Request.Form["Note"];
							if (int.TryParse(Request.Form["ResultCodeId"], out contact_result_id))
							{
								if (int.TryParse(Request.Form["ContactTypeId"], out contact_type_id))
								{
									// save patient contact record
									var added = new PatientContactAttempt()
									{
										ContactTypeId = contact_type_id,
										ContactResultCodeId = contact_result_id,
										Notes = note,
										CreateTimestamp = now,
										CreateUserId = cu.Id,
										OrganizationId = cu.OrganizationId,
										PatientFilterId = task.PatientFilterId.Value,
										Timestamp = now,
										UpdateTimestamp = now,
										UpdateUserId = cu.Id,
										RelatedTaskId = task.Id
									};

									db.PatientContactAttempts.Add(added);

									if (!string.IsNullOrEmpty(note))
										task.Notes += $"\r\n\r\n{added.Notes}";

									if (contact_result_id == (int)ContactResultTypes.Failure)
									{
										int days = 0;
										int attempts = 0;
										if (cu.Organization.OrganizationDefaults.Any())
										{
											var defs = db.OrganizationDefaults.SingleOrDefault(s => s.OrganizationId == cu.OrganizationId);
											days = defs.PatientContactDays.Value;
											attempts = defs.ContactAttemptsBeforeRegisteredLetter.Value;
										}
										else
										{
											var defs = db.OrganizationDefaults.Single(s => s.OrganizationId == 1);
											days = defs.PatientContactDays.Value;
											attempts = defs.ContactAttemptsBeforeRegisteredLetter.Value;
										}

										task.ConsecutiveContacts += 1;

										if (task.ConsecutiveContacts > attempts)
										{
											// time for registered letter

											task.ClosedDate = now;

											// create a send registered letter task
											var lrt = new Models.Task();

											lrt.CreateUserId = cu.Id;
											lrt.UpdateUserId = cu.Id;
											lrt.CreateTimestamp = now;
											lrt.UpdateTimestamp = now;
											lrt.TaskTypeId = (int)TaskTypes.SendRegisteredLetters;
											lrt.OrganizationId = cu.OrganizationId;
											lrt.PatientFilterId = task.PatientFilterId;
											lrt.PatientId = task.PatientId;

											db.Tasks.Add(lrt);
										}
										else
										{
											task.HideUntil = now.AddDays(days).Date;
										}
									}
									else
									{
										task.ConsecutiveContacts = 0;
									}

									success = true;
								}
							}

							if (contact_result_id == (int)ContactResultTypes.Success)
							{
								DateTime tmp;
								string trd = Request.Form["TargetRetrievalDate"];
								if (!string.IsNullOrEmpty(trd))
								{
									trd = trd.Trim();
									if (DateTime.TryParse(Request.Form["TargetRetrievalDate"], out tmp))
									{
										task.PatientFilter.TargetRemovalDate = tmp;
										task.ClosedDate = now;
										success = true;
									}
									else
									{
										task.ValidationErrors.Add("The target removal date was in an uncrecognized format.  Please use MM/DD/YYYY.");
										success = false;
									}
								}
							}

							if (success)
								db.SaveChanges();
						}
						else if (task.TaskTypeId == (int)TaskTypes.SendRegisteredLetters)
						{
							DateTime tmp;
							if (DateTime.TryParse(Request.Form["SendDate"], out tmp))
							{
								string tn = Request.Form["TrackingNumber"];
								string note = Request.Form["Note"];

								var added = new PatientContactAttempt()
								{
									ContactTypeId = (int)ContactTypes.RegisteredLetter,
									ContactResultCodeId = (int)ContactResultTypes.Success,
									CreateTimestamp = now,
									CreateUserId = cu.Id,
									OrganizationId = cu.OrganizationId,
									PatientFilterId = task.PatientFilterId.Value,
									Timestamp = tmp.Date,
									TrackingNumber = tn,
									UpdateTimestamp = now,
									UpdateUserId = cu.Id,
									RelatedTaskId = task.Id,
									Notes = note,
								};

								task.PatientFilter.PatientContactAttempts.Add(added);

								task.PatientFilter.UpdateTimestamp = now;
								task.PatientFilter.UpdateUserId = cu.Id;
								task.PatientFilter.Tasks.ForEach(f =>
								{
									f.ClosedDate = now;
									f.UpdateUserId = cu.Id;
									f.UpdateTimestamp = now;
								});

								task.ClosedDate = now;

								task.UpdateTimestamp = now;
								task.UpdateUserId = cu.Id;

								db.SaveChanges();
								success = true;

							}
							else
							{
								task.ValidationErrors.Add("Please provide the send date in the format MM/DD/YYYY.");
							}
						}
						else if (task.TaskTypeId == (int)TaskTypes.ContactPCP)
						{
							var result_note = Request.Form["ContactNote"];
							if (!string.IsNullOrEmpty(result_note))
							{
								result_note = result_note.Trim();
								task.Notes += $"\r\n\r\n {result_note}";
							}
							task.ClosedDate = now;
							task.UpdateTimestamp = now;
							task.UpdateUserId = cu.Id;

							int contact_result_id, contact_type_id;
							contact_result_id = 0;
							contact_type_id = 0;

							if (int.TryParse(Request.Form["ResultCode"], out contact_result_id))
							{
								if (int.TryParse(Request.Form["ContactType"], out contact_type_id))
								{
									var added = new PhysicianContactAttempt()
									{
										ContactTypeId = contact_type_id,
										ContactResultCodeId = contact_result_id,
										Notes = result_note,
										CreateTimestamp = now,
										CreateUserId = cu.Id,
										OrganizationId = cu.OrganizationId,
										PatientFilterId = task.PatientFilterId.Value,
										Timestamp = now,
										UpdateTimestamp = now,
										UpdateUserId = cu.Id,
										RelatedTaskId = task.Id
									};

									db.PhysicianContactAttempts.Add(added);
								}
							}

							string removal_approval = Request.Form["RemovalApproval"];
							if (removal_approval.ToLower() == "true")
							{
								if (task.PatientFilter != null)
									task.PatientFilter.IsPCPApproved = true;
							}

							var related_review_case_task = db.Tasks
								.Where(w => w.PatientFilterId == task.PatientFilterId && w.TaskTypeId == (int)TaskTypes.ReviewCase)
								.OrderByDescending(obd => obd.CreateTimestamp)
								.FirstOrDefault();

							if (related_review_case_task != null && related_review_case_task.Id > 0)
							{
								related_review_case_task.ClosedDate = null;
								related_review_case_task.Notes += task.Notes;
							}

							db.SaveChanges();
							success = true;

						}
						else if (task.TaskTypeId == (int)TaskTypes.PatientContactDue)
						{
							string note;
							int contact_type_id = 0;
							int contact_result_id = 0;
							bool have_data = false;

							note = Request.Form["Note"];
							if (int.TryParse(Request.Form["ResultCodeId"], out contact_result_id))
							{
								if (int.TryParse(Request.Form["ContactTypeId"], out contact_type_id))
								{
									have_data = true;
								}
							}

							if (have_data)
							{

								var added = new PatientContactAttempt()
								{
									ContactTypeId = contact_type_id,
									ContactResultCodeId = contact_result_id,
									Notes = note,
									CreateTimestamp = now,
									CreateUserId = cu.Id,
									OrganizationId = cu.OrganizationId,
									PatientFilterId = task.PatientFilterId.Value,
									Timestamp = now,
									UpdateTimestamp = now,
									UpdateUserId = cu.Id,
									RelatedTaskId = task.Id
								};

								task.PatientFilter.PatientContactAttempts.Add(added);

								task.Notes += $"\r\n\r\n--{cu.FirstName} {cu.LastName} Patient Contact Note: {added.Notes}";
								task.ClosedDate = now;

								task.UpdateTimestamp = now;
								task.UpdateUserId = cu.Id;
								db.SaveChanges();
								success = true;

							}
						}
						else if (task.TaskTypeId == (int)TaskTypes.ScheduleRetrieval)
						{
							DateTime tmp;
							if (DateTime.TryParse(Request.Form["TargetRetrievalDate"], out tmp))
							{
								task.PatientFilter.TargetRemovalDate = tmp;
							}


							string note;
							int contact_type_id = 0;
							int contact_result_id = 0;

							note = Request.Form["Note"];
							if (int.TryParse(Request.Form["ResultCodeId"], out contact_result_id))
							{
								if (int.TryParse(Request.Form["ContactTypeId"], out contact_type_id))
								{
									// save patient contact record
									var added = new PatientContactAttempt()
									{
										ContactTypeId = contact_type_id,
										ContactResultCodeId = contact_result_id,
										Notes = note,
										CreateTimestamp = now,
										CreateUserId = cu.Id,
										OrganizationId = cu.OrganizationId,
										PatientFilterId = task.PatientFilterId.Value,
										Timestamp = now,
										UpdateTimestamp = now,
										UpdateUserId = cu.Id,
										RelatedTaskId = task.Id
									};

									task.PatientFilter.PatientContactAttempts.Add(added);

									if (!string.IsNullOrEmpty(note))
										task.Notes += $"\r\n\r\n{added.Notes}";
								}
							}

							db.SaveChanges();
							success = true;


							if (task.PatientContactAttempts.Count(c => c.ContactResultCodeId == (int)ContactResultTypes.Success) > 0)
							{
								task.ClosedDate = now;
							}
							else
							{
								int days = 0;
								int attempts = 0;
								if (cu.Organization.OrganizationDefaults.Any())
								{
									var defs = db.OrganizationDefaults.SingleOrDefault(s => s.OrganizationId == cu.OrganizationId);
									days = defs.PatientContactDays.Value;
									attempts = defs.ContactAttemptsBeforeRegisteredLetter.Value;
								}
								else
								{
									var defs = db.OrganizationDefaults.Single(s => s.OrganizationId == 1);
									days = defs.PatientContactDays.Value;
									attempts = defs.ContactAttemptsBeforeRegisteredLetter.Value;
								}

								if (task.PatientContactAttempts.Count(c => c.ContactResultCodeId == (int)ContactResultTypes.Failure) >= attempts)
								{
									// create send reg letter
									task.ClosedDate = now;

									// create a send registered letter task
									var lrt = new Models.Task();

									lrt.CreateUserId = cu.Id;
									lrt.UpdateUserId = cu.Id;
									lrt.CreateTimestamp = now;
									lrt.UpdateTimestamp = now;
									lrt.TaskTypeId = (int)TaskTypes.SendRegisteredLetters;
									lrt.OrganizationId = cu.OrganizationId;
									lrt.PatientFilterId = task.PatientFilterId;
									lrt.PatientId = task.PatientId;

									db.Tasks.Add(lrt);
								}
								else
								{
									task.HideUntil = now.AddDays(days);
								}
							}
							db.SaveChanges();
						}
					}
				}
				else
				{
					task.ValidationErrors.Add("Not found.");
					success = false;
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				task.ValidationErrors.Add($"An error has occurred.  Please contact tech support.  Your error report id is: U{cu.Id}T{task.Id}{now.ToString("MMddyyyy.hhmmss")}");
			}


			JsonResult returned = new JsonResult();
			returned.Data = new
			{
				Success = success,
				Errors = success ? null : task.ValidationErrors.ToArray()
			};

			return returned;
		}


		[Authorize()]
		[HttpPost]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> ClaimTask(int taskId)
		{

			var cu = CurrentUser();

			Models.Task task = db.Tasks.SingleOrDefault(sd => sd.Id == taskId && sd.OrganizationId == cu.OrganizationId);
			bool success = false;
			string message = "";
			DateTime now = DateTime.UtcNow;

			if (task != null && task.Id == taskId)
			{
				try
				{
					//if (task.AssignedUserId.HasValue)
					//{
					//	success = true;
					//	message = "This task has already been claimed.";
					//}
					//else
					//{
					task.AssignedUserId = cu.Id;
					task.UpdateTimestamp = now;
					task.UpdateUserId = cu.Id;
					db.SaveChanges();

					success = true;
					//}
				}
				catch (Exception ex)
				{
					LogException(ex, $"UserId: {cu.Id}");
					task.ValidationErrors.Add($"An error has occurred.  Please contact tech support.  Your error report id is: U{cu.Id}T{task.Id}{now.ToString("MMddyyyy.hhmmss")}");
				}
			}
			else
			{
				success = false;
				message = "Not found.";
			}


			JsonResult returned = new JsonResult();
			returned.Data = new
			{
				Success = success,
				Message = success ? null : message
			};

			return returned;
		}



		[Authorize()]
		[HttpPost]
		[ValidateAntiForgeryToken()]
		public async Task<ActionResult> UnclaimTask(int taskId)
		{

			var cu = CurrentUser();

			Models.Task task = db.Tasks.SingleOrDefault(sd => sd.Id == taskId && sd.OrganizationId == cu.OrganizationId);
			bool success = false;
			string message = "";
			DateTime now = DateTime.UtcNow;

			if (task != null && task.Id == taskId)
			{
				try
				{
					if (task.AssignedUserId.HasValue)
					{
						success = true;
						task.AssignedUserId = null;
						db.SaveChanges();
					}
				}
				catch (Exception ex)
				{
					LogException(ex, $"UserId: {cu.Id}");
					task.ValidationErrors.Add($"An error has occurred.  Please contact tech support.  Your error report id is: U{cu.Id}T{task.Id}{now.ToString("MMddyyyy.hhmmss")}");
				}
			}
			else
			{
				success = false;
				message = "Not found.";
			}


			JsonResult returned = new JsonResult();
			returned.Data = new
			{
				Success = success,
				Message = success ? null : message
			};

			return returned;
		}

		private string UpdatePatientNotes(string notes, string contact_reason)
		{

			var cu = CurrentUser();
			string name = $"{cu.LastName}, {cu.FirstName}";

			return $"{notes}\r\n-----------------------------\r\n{name} - {contact_reason}";
		}


		[Authorize]
		[HttpGet]
		public ActionResult ContactPatient(int patientFilterId)
		{
			var cu = CurrentUser();
			var model = new PatientContactAttemptModel(patientFilterId, cu.OrganizationId);

			return View(model);
		}

		[Authorize]
		[HttpPost]
		public async Task<ActionResult> ContactPatient(PatientContactAttemptModel model)
		{
			DateTime now = DateTime.UtcNow;

			var cu = CurrentUser();
			try
			{

				if (!string.IsNullOrEmpty(model.SelectedContactType))
				{
					if (!string.IsNullOrEmpty(model.SelectedResultCode))
					{
						if (model.PatientFilterId > 0)
						{
							var inserted = new PatientContactAttempt();

							var tasks = db.Tasks
								.Where(w => w.PatientFilterId == model.PatientFilterId && w.OrganizationId == cu.OrganizationId)
								.ToList();


							// If success attempt, check for hidden Build Case or hidden Schedule Retrieval and ONE is found, unhide it, 
							// and tie this contact attempt to it.
							if (model.SelectedResultCode == "1")
							{
								// do we have a snoozing build case task?

								var contactables = tasks
									.Where(w => w.TaskTypeId == (int)TaskTypes.BuildCase || w.TaskTypeId == (int)TaskTypes.ScheduleRetrieval)
									.Where(w => w.ClosedDate == null)
									.Where(w => w.HideUntil > now);

								int cnt = contactables.Count();
								if (cnt == 1)
								{
									// yes, wake it
									var task = tasks.First();
									task.ConsecutiveContacts = 0;
									task.HideUntil = null;
									db.SaveChanges();
									inserted.RelatedTaskId = task.Id;
								}
								else if (cnt == 0)
								{
									// did patient contact come from a registered letter attempt?
									if (tasks.Count(c => c.TaskTypeId == (int)TaskTypes.SendRegisteredLetters) > 0)
									{
										// restart build case cycle
										// -reopen task, attach this contact to it, set task contact counter to 1
										var bc = tasks.Where(w => w.TaskTypeId == (int)TaskTypes.BuildCase)
											.OrderByDescending(obd => obd.UpdateTimestamp)
											.FirstOrDefault();

										if (bc.Id > 0)
										{
											bc.HideUntil = null;
											bc.ClosedDate = null;
											inserted.RelatedTaskId = bc.Id;
											bc.ConsecutiveContacts = 0;

											// close any open send registered letter tasks
											var srtasks = tasks.Where(w => w.TaskTypeId == (int)TaskTypes.SendRegisteredLetters)
												.Where(w => w.ClosedDate == null);

											srtasks.ForEach(f =>
											{
												f.ClosedDate = now;
											});

											db.SaveChanges();
										}

									}
								}
							}
							else
							{
								// unsuccessful contact attempt
								// look for related task
								// increment the task counter
								// tie contact attempt to related task
								var contactables = tasks
										.Where(w => w.TaskTypeId == (int)TaskTypes.BuildCase || w.TaskTypeId == (int)TaskTypes.ScheduleRetrieval)
										.Where(w => w.ClosedDate == null);

								if (contactables.Count() == 1)
								{
									var target = contactables.First();
									target.ConsecutiveContacts += 1;
									inserted.RelatedTaskId = target.Id;

									int days = 0;
									int attempts = 0;
									if (cu.Organization.OrganizationDefaults.Any())
									{
										var defs = db.OrganizationDefaults.SingleOrDefault(s => s.OrganizationId == cu.OrganizationId);
										days = defs.PatientContactDays.Value;
										attempts = defs.ContactAttemptsBeforeRegisteredLetter.Value;
									}
									else
									{
										var defs = db.OrganizationDefaults.Single(s => s.OrganizationId == 1);
										days = defs.PatientContactDays.Value;
										attempts = defs.ContactAttemptsBeforeRegisteredLetter.Value;
									}

									if (target.ConsecutiveContacts > attempts)
									{
										// Per David Heister email on Sep 16, 2020, 4:54 AM, close build case and open send registered letter task.
										target.ClosedDate = now;

										// create a send registered letter task
										var lrt = new Models.Task();

										lrt.CreateUserId = cu.Id;
										lrt.UpdateUserId = cu.Id;
										lrt.CreateTimestamp = now;
										lrt.UpdateTimestamp = now;
										lrt.TaskTypeId = (int)TaskTypes.SendRegisteredLetters;
										lrt.OrganizationId = cu.OrganizationId;
										lrt.PatientFilterId = target.PatientFilterId;
										lrt.PatientId = target.PatientId;

										db.Tasks.Add(lrt);
									}

									db.SaveChanges();
								}
							}

							var pf = db.PatientFilters.SingleOrDefault(sd => sd.Id == model.PatientFilterId && sd.OrganizationId == cu.OrganizationId);

							if (pf != null && pf.Id == model.PatientFilterId)
							{
								var contact_result_codes = db.ContactResultCodes.Where(w => w.Active && (w.OrganizationId == pf.OrganizationId || w.OrganizationId == 1)).Select(s => s.Id).ToList();
								var contact_types = db.ContactTypes.Select(s => s.Id).ToList();

								inserted.OrganizationId = pf.OrganizationId;
								inserted.PatientFilterId = pf.Id;


								if (!string.IsNullOrEmpty(model.Note))
								{
									string note = model.Note.Trim();

									inserted.Notes = note; //$"--Patient Contact Note By--\r\n--User: {cu.FirstName} {cu.LastName}--\r\n\r\n{note}--End Patient Contact Note.--";
								}

								int id;
								if (int.TryParse(model.SelectedResultCode, out id))
								{
									if (contact_result_codes.Contains(id))
									{
										inserted.ContactResultCodeId = id;

										if (int.TryParse(model.SelectedContactType, out id))
										{
											if (contact_types.Contains(id))
											{
												inserted.ContactTypeId = id;

												if (!string.IsNullOrEmpty(model.ContactDate))
												{
													DateTime contact_date;
													if (DateTime.TryParse(model.ContactDate, out contact_date))
													{
														inserted.Timestamp = contact_date.ToUniversalTime();
														inserted.CreateTimestamp = now;
														inserted.CreateUserId = cu.Id;
														inserted.UpdateUserId = cu.Id;
														inserted.UpdateTimestamp = now;

														// look for any open patient contact tasks for this patient filter id?
														// would saving a patient contact here not fulfill the task?

														db.PatientContactAttempts.Add(inserted);

														db.SaveChanges();

														return RedirectToAction("PatientDetails", new { id = pf.PatientId });
													}
												}
											}
										}
									}
								}
							}
							else
							{
								model.ErrorMessage = "Not found.";
							}

						}
					}
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				model.ErrorMessage = GetErrorMessage(now);
			}


			return View(model);
		}

		[Authorize()]
		[HttpGet]
		public async Task<ActionResult> FilterPatients(string term)
		{
			List<LabelAndValue> returned = new List<LabelAndValue>();

			var cu = CurrentUser();
			if (!string.IsNullOrEmpty(term))
			{
				var patients = db.PatientFilters.AsNoTracking()
					.Include(i => i.Patient).AsNoTracking()
					.Where(w => w.Patient.Active == true)
					.Where(w => w.OrganizationId == cu.OrganizationId)
					.Where(w => w.Patient.FirstName.Contains(term) || w.Patient.LastName.Contains(term) || w.Patient.MRN.Contains(term))
					.Select(s => new { LastName = s.Patient.LastName, FirstName = s.Patient.FirstName, FilterName = s.Filter.Name, PatientFilterId = s.Id })
					.ToList();

				patients.ForEach(f =>
				{
					returned.Add(new LabelAndValue() { label = $"{f.LastName}, {f.FirstName} ({f.FilterName})", value = f.PatientFilterId.ToString() });
				});
			}

			return Json(returned, JsonRequestBehavior.AllowGet);
		}

		[Authorize()]
		[HttpPost]
		public async Task<ActionResult> SaveNote()
		{

			var cu = CurrentUser();
			string h; string b;

			h = Request.Form["Heading"];
			b = Request.Form["Body"];

			var model = new QuickNote();
			if (!string.IsNullOrEmpty(h) || !string.IsNullOrEmpty(b))
			{
				if (!string.IsNullOrEmpty(h))
					model.Heading = h.Trim();

				if (!string.IsNullOrEmpty(b))
					model.Body = b.Trim();

				model.UserId = cu.Id;
				if (db.QuickNotes.Count(c => c.UserId == model.UserId) < 10)
				{
					db.QuickNotes.Add(model);
					db.SaveChanges();
				}
			}

			return PartialView("_QuickNote", model);
		}

		[Authorize()]
		[HttpPost]
		public ActionResult EditNote()
		{
			bool success = false;
			try
			{
				var cu = CurrentUser();
				string id; string h; string b;

				id = Request.Form["id"];
				h = Request.Form["heading"];
				b = Request.Form["body"];

				var model = new QuickNote();
				if (!string.IsNullOrEmpty(id) || !string.IsNullOrEmpty(h) || !string.IsNullOrEmpty(b))
				{
					id = id.Trim();
					b = b.Trim();
					h = h.Trim();

					int i;

					if (int.TryParse(id, out i))
					{
						var persisted = db.QuickNotes.SingleOrDefault(sd => sd.Id == i && sd.UserId == cu.Id);
						if (persisted != null && persisted.Id == i)
						{

							if (persisted.Body != b)
								persisted.Body = b;

							if (persisted.Heading != h)
								persisted.Heading = h;

							db.SaveChanges();
						}
					}
				}

				success = true;
			}
			catch (Exception ex)
			{
				LogException(ex);
			}

			return Json(new { Success = success ? "true" : "false" });
		}

		[Authorize()]
		[HttpPost]
		public async Task<ActionResult> DeleteNote()
		{

			var cu = CurrentUser();
			string id = Request.Form["noteId"];
			string returned = "Ok";

			try
			{
				if (!string.IsNullOrEmpty(id))
				{
					int qnid;
					if (int.TryParse(id, out qnid))
					{
						var tgt = db.QuickNotes.SingleOrDefault(sd => sd.Id == qnid && sd.UserId == cu.Id);
						if (tgt != null && tgt.Id == qnid)
						{
							db.QuickNotes.Remove(tgt);
							db.SaveChanges();
						}
						else
						{
							returned = "Fail";
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogException(ex);
				returned = "Fail";
			}

			return Json(returned);
		}


		[Authorize]
		[HttpPost]
		public async Task<ActionResult> AddFilter(string Name, string Manufacturer, bool IsPermanent)
		{
			List<OptionModel> model = new List<OptionModel>();

			var cu = CurrentUser();
			try
			{
				if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Manufacturer))
				{
					var added = new Models.Filter();

					added.Name = Name.Trim();
					added.Manufacturer = Manufacturer.Trim();
					added.OrganizationId = cu.OrganizationId;

					added.CreateTimestamp = DateTime.UtcNow;
					added.UpdateTimestamp = added.CreateTimestamp;
					added.CreateUserId = cu.Id;
					added.UpdateUserId = cu.Id;
					added.Permanent = IsPermanent;

					db.Filters.Add(added);
					db.SaveChanges();

					var filters = db.Filters.AsNoTracking().Where(w => w.OrganizationId == 1 || w.OrganizationId == cu.OrganizationId).ToList();
					filters.ForEach(f =>
					{
						model.Add(new OptionModel() { Text = f.Name, Value = f.Id.ToString(), Selected = f.Name == added.Name });
					});
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
			}


			return Json(model);
		}

		[Authorize]
		[HttpPost]
		public async Task<ActionResult> AddComplicatingFactor(string Name, string Description)
		{

			var cu = CurrentUser();
			List<OptionModel> model = new List<OptionModel>();

			try
			{
				if (!string.IsNullOrEmpty(Name))
				{
					var added = new Models.ComplicatingFactor();

					added.Name = Name.Trim();
					added.Description = string.IsNullOrEmpty(Description) ? null : Description.Trim();
					added.OrganizationId = cu.OrganizationId;

					added.CreateTimestamp = DateTime.UtcNow;
					added.UpdateTimestamp = added.CreateTimestamp;
					added.CreateUserId = cu.Id;
					added.UpdateUserId = cu.Id;

					db.ComplicatingFactors.Add(added);
					db.SaveChanges();

					var returned = db.ComplicatingFactors.AsNoTracking().Where(w => w.OrganizationId == 1 || w.OrganizationId == cu.OrganizationId).ToList();
					returned.ForEach(f =>
					{
						string label = f.Name;
						if (!string.IsNullOrEmpty(f.Description))
							label += $" {f.Description}";

						model.Add(new OptionModel() { Text = label, Value = f.Id.ToString(), Selected = f.Name == added.Name });
					});
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
			}


			return Json(model);
		}

		[Authorize]
		[HttpPost]
		public async Task<ActionResult> AddPCP(string Name, string Email, string Phone, bool RequiresApproval)
		{
			List<OptionModel> model = new List<OptionModel>();

			var cu = CurrentUser();

			if (!string.IsNullOrEmpty(Name))
				Name = Name.Trim();

			if (!string.IsNullOrEmpty(Email))
				Email = Email.Trim();

			if (!string.IsNullOrEmpty(Phone))
				Phone = Phone.Trim();


			try
			{
				if (!string.IsNullOrEmpty(Name))
				{
					var added = new Physician();

					added.Name = Name.Trim();
					added.Email = Email;
					added.Phone = Phone;
					added.OrganizationId = cu.OrganizationId;
					added.RequiresRemovalApproval = RequiresApproval;

					added.CreateTimestamp = DateTime.UtcNow;
					added.UpdateTimestamp = added.CreateTimestamp;
					added.CreateUserId = cu.Id;
					added.UpdateUserId = cu.Id;

					db.Physicians.Add(added);
					db.SaveChanges();

					var physicians = db.Physicians.AsNoTracking().Where(w => w.OrganizationId == 1 || w.OrganizationId == cu.OrganizationId).ToList();
					physicians.ForEach(f =>
					{
						model.Add(new OptionModel() { Text = f.Name, Value = f.Id.ToString(), Selected = f.Name == added.Name });
					});
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
			}


			return Json(model);
		}

		public class LabelAndValue
		{
			public string label { get; set; }
			public string value { get; set; }
		}

		[Authorize()]
		public ActionResult About()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}

		[Authorize()]
		public ActionResult Contact()
		{
			ViewBag.Message = "Your contact page.";

			return View();
		}

		[HttpGet]
		[Authorize(Roles = "OrganizationAdmins,Physician,SuperUsers")]
		public async Task<ActionResult> ExportCaseData(int patientFilterId)
		{
			string message = "";

			var cu = CurrentUser();

			PatientFilter target = await db.PatientFilters
				.Include(i => i.Patient)
				.Include(i => i.PatientContactAttempts)
				.Include(i => i.PhysicianContactAttempts)
				.Include(i => i.Tasks)
				.SingleOrDefaultAsync(s => s.Id == patientFilterId && s.OrganizationId == cu.OrganizationId);

			if (target == null || target.OrganizationId != cu.OrganizationId)
			{
				return Json(new { Success = false, ErrorMessage = "Access denied" }, JsonRequestBehavior.AllowGet);
			}



			try
			{
				DataSet results = new DataSet();

				var p = target.Patient;

				var ps = new ObjectShredder<Patient>();
				var psdt = ps.Shred(new Patient[] { p }, null, LoadOption.PreserveChanges);
				psdt.Columns.Remove("Tasks");
				psdt.Columns.Remove("Organization");
				psdt.Columns.Remove("PatientFilters");
				if (psdt.Columns.Contains("_entityWrapper"))
					psdt.Columns.Remove("_entityWrapper");
				psdt.TableName = "Patient";
				results.Tables.Add(psdt);


				PatientFilterExport pfe = new PatientFilterExport();

				pfe.ActualRemovalDate = target.ActualRemovalDate.HasValue ? target.ActualRemovalDate.Value.ToShortDateString() : null;
				pfe.ComplicatingFactor = target.ComplicatingFactor != null ? target.ComplicatingFactor.Name : null;
				pfe.FilterName = target.Filter != null ? target.Filter.Name : null;
				pfe.Indication = target.Indication != null ? target.Indication.Name : null;
				pfe.IsTemporary = target.IsTemporary;
				pfe.IVCFilterPlacedBy = target.ProcedurePhysician != null ? $"{target.ProcedurePhysician.LastName}, {target.ProcedurePhysician.FirstName}" : null;
				pfe.IVCFilterPlacementDate = target.ProcedureDate.HasValue ? target.ProcedureDate.Value.ToShortDateString() : null;
				pfe.MadePermanent = target.MadePermanent;

				pfe.MadePermanentBy = null;
				Models.User mpb = null;
				if (!string.IsNullOrEmpty(target.MadePermanentBy))
				{
					//mpb = db.Users.SingleOrDefault(s => s.Id == target.MadePermanentBy.Value);

					pfe.MadePermanentBy = target.MadePermanentBy;
				}

				pfe.Notes = target.Notes;
				pfe.OrderingPhysician = target.OrderingPhysician != null ? target.OrderingPhysician.Name : null;
				pfe.PatientFilterId = target.Id;
				pfe.PatientId = target.PatientId;
				pfe.PrimaryCareProvider = target.PrimaryCarePhysician != null ? target.PrimaryCarePhysician.Name : null;
				pfe.TargetRemovalDate = target.TargetRemovalDate.HasValue ? target.TargetRemovalDate.Value.ToShortDateString() : null;

				var pfShredder = new ObjectShredder<PatientFilterExport>();
				var dt = pfShredder.Shred(new PatientFilterExport[] { pfe }, null, LoadOption.PreserveChanges);
				dt.TableName = "Patient Filter";
				results.Tables.Add(dt);

				List<ContactAttemptExport> patientContactAttempts = new List<ContactAttemptExport>();
				foreach (var item in target.PatientContactAttempts)
				{
					var pca = new ContactAttemptExport()
					{
						ContactResultCode = item.ContactResultCode.ResultCode,
						ContactType = item.ContactType.Name,
						CreateTimestamp = item.CreateTimestamp,
						UpdateTimestamp = item.UpdateTimestamp,
						Id = item.Id,
						Notes = item.Notes,
						RelatedTaskId = item.RelatedTaskId,
						Timestamp = item.Timestamp,
						TrackingNumber = item.TrackingNumber
					};

					var cru = db.Users.SingleOrDefault(s => s.Id == item.CreateUserId);
					pca.CreateUser = $"{cru.LastName}, {cru.FirstName}";

					var uu = db.Users.SingleOrDefault(s => s.Id == item.UpdateUserId);
					pca.UpdateUser = $"{uu.LastName}, {uu.FirstName}";
					patientContactAttempts.Add(pca);
				}

				var pcShredder = new ObjectShredder<ContactAttemptExport>();
				var pca_dt = pcShredder.Shred(patientContactAttempts, null, LoadOption.PreserveChanges);
				pca_dt.TableName = "Patient Contact Attempts";
				results.Tables.Add(pca_dt);

				var task_exports = new List<TaskExport>();

				var task_entities = target.Tasks.ToList();


				foreach (var task in task_entities)
				{
					var added = new TaskExport()
					{
						AssignedTo = task.AssignedUser != null ? $"{task.AssignedUser.LastName}, {task.AssignedUser.FirstName}" : null,
						CreateTimestamp = task.CreateTimestamp,
						CreateUser = $"{task.CreateUser.LastName}, {task.CreateUser.FirstName}",
						Id = task.Id,
						Notes = task.Notes,
						TaskType = task.TaskType.Name,
						UpdateTimestamp = task.UpdateTimestamp,
						UpdateUser = $"{task.UpdateUser.LastName}, {task.UpdateUser.FirstName}",
						ClosedDate = task.ClosedDate,
						TargetCloseDate = task.TargetCloseDate
					};

					task_exports.Add(added);


					if (task.PatientQuestionResponses != null && task.PatientQuestionResponses.Count() > 0)
					{
						List<SurveyResponse> surveys = new List<SurveyResponse>();
						foreach (var item in task.PatientQuestionResponses)
						{
							var sr = new SurveyResponse
							{
								Answer = item.Response,
								Question = db.PatientQuestions.SingleOrDefault(sd => sd.Id == item.QuestionId)?.Question
							};
						}

						var shredder = new ObjectShredder<SurveyResponse>();
						var dtsr = shredder.Shred(surveys, null, LoadOption.PreserveChanges);
						dtsr.TableName = "Patient Surveys";
						results.Tables.Add(dtsr);

					}

					if (task.PhysicianQuestionResponses != null && task.PhysicianQuestionResponses.Count() > 0)
					{

						List<SurveyResponse> surveys = new List<SurveyResponse>();
						foreach (var item in task.PhysicianQuestionResponses)
						{
							var sr = new SurveyResponse
							{
								Answer = item.Response,
								Question = db.PhysicianQuestions.SingleOrDefault(sd => sd.Id == item.QuestionId)?.Question
							};
						}

						var shredder = new ObjectShredder<SurveyResponse>();
						var dtsr2 = shredder.Shred(surveys, null, LoadOption.PreserveChanges);
						dtsr2.TableName = "Physician Surveys";
						results.Tables.Add(dtsr2);
					}
				}


				var ts = new ObjectShredder<TaskExport>();
				var tsdt = ts.Shred(task_exports, null, LoadOption.PreserveChanges);
				tsdt.TableName = "Tasks";
				results.Tables.Add(tsdt);

				List<ContactAttemptExport> phyContactAttempts = new List<ContactAttemptExport>();
				foreach (var item in target.PhysicianContactAttempts)
				{
					var pca = new ContactAttemptExport()
					{
						ContactResultCode = item.ContactResultCode.ResultCode,
						ContactType = item.ContactType.Name,
						CreateTimestamp = item.CreateTimestamp,
						UpdateTimestamp = item.UpdateTimestamp,
						Id = item.Id,
						Notes = item.Notes,
						RelatedTaskId = item.RelatedTaskId,
						Timestamp = item.Timestamp
					};

					var cru = db.Users.SingleOrDefault(s => s.Id == item.CreateUserId);
					pca.CreateUser = $"{cru.LastName}, {cru.FirstName}";

					var uu = db.Users.SingleOrDefault(s => s.Id == item.UpdateUserId);
					pca.UpdateUser = $"{uu.LastName}, {uu.FirstName}";
					phyContactAttempts.Add(pca);
				}

				var phcaShredder = new ObjectShredder<ContactAttemptExport>();
				var phca_dt = phcaShredder.Shred(phyContactAttempts, null, LoadOption.PreserveChanges);
				phca_dt.TableName = "Physician Contact Attempts";
				results.Tables.Add(phca_dt);


				this.Response.ClearContent();
				MemoryStream streamFromDataSet = ExcelUtility.GetStreamFromDataSet(results);
				streamFromDataSet.Seek(0L, SeekOrigin.Begin);
				string str = $"FilterTrackerCaseExport_{target.Id}{DateTime.Now.ToShortDateString().Replace("/", ".")}.xlsx";
				FileStreamResult fileStreamResult = new FileStreamResult((Stream)streamFromDataSet, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
				fileStreamResult.FileDownloadName = str;
				return (ActionResult)fileStreamResult;
			}
			catch (Exception ex)
			{
				LogException(ex);
				message = ex.Message;
			}

			return Json(new { Success = false, ErrorMessage = message }, JsonRequestBehavior.AllowGet);
		}

		[HttpGet]
		[Authorize(Roles = "Users,OrganizationAdmins,Physician,SuperUsers")]
		public async Task<ActionResult> DisplayCaseHistory(int patientFilterId)
		{
			DisplayCaseHistoryViewModel model = new DisplayCaseHistoryViewModel();

			var cu = CurrentUser();

			PatientFilter target = await db.PatientFilters
				.Include(i => i.Patient)
				.Include(i => i.PatientContactAttempts)
				.Include(i => i.PhysicianContactAttempts)
				.Include(i => i.Tasks)
				.SingleOrDefaultAsync(s => s.Id == patientFilterId && s.OrganizationId == cu.OrganizationId);


			if (target == null || target.OrganizationId != cu.OrganizationId)
			{
				model.ErrorMessage = "Access is denied.";
				return View(model);
			}

			target.Tasks = target.Tasks.OrderBy(o => o.CreateTimestamp).ToList();

			model.PatientFilter = target;

			var pcas = db.PatientContactAttempts
				.Where(w => w.PatientFilterId == patientFilterId && w.OrganizationId == cu.OrganizationId && w.RelatedTaskId == null)
				.Select(s => new ContactAttempt { ContactType = s.ContactType, Notes = s.Notes, ResultCode = s.ContactResultCode, Timestamp = s.Timestamp, TrackingNumber = s.TrackingNumber })
				.ToList();

			model.NonTaskedContactAttempts.AddRange(pcas);

			var phcas = db.PhysicianContactAttempts
				.Where(w => w.PatientFilterId == patientFilterId && w.OrganizationId == cu.OrganizationId && w.RelatedTaskId == null)
				.Select(s => new ContactAttempt { ContactType = s.ContactType, Notes = s.Notes, ResultCode = s.ContactResultCode, Timestamp = s.Timestamp, TrackingNumber = null })
				.ToList();

			model.NonTaskedContactAttempts.AddRange(phcas);

			model.NonTaskedContactAttempts = model.NonTaskedContactAttempts.OrderBy(o => o.Timestamp).ToList();



			return View(model);
		}

		private class UpdateBuildCaseTaskResult
		{
			public bool Success { get; set; }
			public bool AllPatientResponsesInserted { get; set; }
		}

		// put stuff that should be in another set of classes here for now.

		// Perhaps a task management class?
		private UpdateBuildCaseTaskResult UpdateBuildCaseTask(BuildCaseTaskEditorViewModel model)
		{
			UpdateBuildCaseTaskResult returned = new UpdateBuildCaseTaskResult();

			var cu = CurrentUser();
			int insert_count = 0;
			int total_answered = 0;
			bool need_save = false;

			if (model == null)
			{
				returned.Success = false;
				return returned;
			}

			try
			{

				if (model.TaskId <= 0)
				{
					Logger.Log.Error("UpdateBuildCaseTask() - model.TaskId is NULL.");
					returned.Success = false;
					return returned;
				}

				int? task_id = model.TaskId;
				Models.Task task = db.Tasks.SingleOrDefault(sd => sd.Id == model.TaskId && sd.OrganizationId == cu.OrganizationId);
				if (task == null)
				{
					Logger.Log.Error($"Could not locate task associated with model.TaskId:  {model.TaskId.ToString()}.");
					returned.Success = false;
					return returned;
				}

				using (var scope = new TransactionScope())
				{
					try
					{
						if (model.PatientQuestionResponses != null && model.PatientQuestionResponses.Count() > 0)
						{
							DateTime now = DateTime.UtcNow;
							foreach (var item in model.PatientQuestionResponses)
							{
								var pqr = db.PatientQuestionResponses.SingleOrDefault(w => w.TaskId == task_id && w.QuestionId == item.QuestionId);
								if (pqr != null && pqr.QuestionId == item.QuestionId)
								{
									if (!string.IsNullOrEmpty(item.Response))
									{
										string response = item.Response.Trim();
										if (pqr.Response != response)
										{
											pqr.Response = response;
										}
										total_answered++;
									}
									else
									{
										//Logger.Log.Warn($"Response to patient question is empty, deleting row. UserId is: {cu.Id.ToString()}.");
										db.PatientQuestionResponses.Remove(pqr);
										need_save = true;
									}

								}
								else
								{
									if (!string.IsNullOrEmpty(item.Response))
									{
										var added = new PatientQuestionResponses()
										{
											OrganizationId = task.OrganizationId,
											CreateUserId = cu.Id,
											CreateTimestamp = now,
											QuestionId = item.QuestionId,
											Response = item.Response.Trim(),
											TaskId = task.Id,
											UpdateTimestamp = now,
											UpdateUserId = cu.Id
										};

										db.PatientQuestionResponses.Add(added);
										insert_count++;
										total_answered++;
										need_save = true;
									}
								}
							}

							if (need_save)
								db.SaveChanges();
						}

						if (model.PhysicianQuestionResponses != null && model.PhysicianQuestionResponses.Count() > 0)
						{
							DateTime now = DateTime.UtcNow;
							foreach (var item in model.PhysicianQuestionResponses)
							{
								var pqr = db.PhysicianQuestionResponses.SingleOrDefault(w => w.TaskId == task_id && w.QuestionId == item.QuestionId);
								if (pqr != null && pqr.QuestionId == item.QuestionId)
								{
									if (!string.IsNullOrEmpty(item.Response))
									{
										string response = item.Response.Trim();
										if (pqr.Response != response)
										{
											pqr.Response = response;
										}
									}
									else
									{
										//Logger.Log.Warn($"Response to physician question is empty, deleting row. UserId is: {cu.Id.ToString()}.");
										db.PhysicianQuestionResponses.Remove(pqr);
										need_save = true;
									}
								}
								else
								{
									if (!string.IsNullOrEmpty(item.Response))
									{
										var added = new PhysicianQuestionResponses()
										{
											OrganizationId = task.OrganizationId,
											CreateUserId = cu.Id,
											CreateTimestamp = now,
											QuestionId = item.QuestionId,
											Response = item.Response.Trim(),
											TaskId = task.Id,
											UpdateTimestamp = now,
											UpdateUserId = cu.Id
										};

										db.PhysicianQuestionResponses.Add(added);
										need_save = true;
									}
								}
							}

							if (need_save)
								db.SaveChanges();
						}

						if (need_save)
						{
							scope.Complete();

							returned.Success = true;
						}
						else
						{
							returned.Success = false;
						}
					}
					catch (Exception ex)
					{
						LogException(ex);
						returned.Success = false;
					}
				}
			}
			catch (Exception ex)
			{
				LogException(ex, $"UserId: {cu.Id}");
				returned.Success = false;
			}


			int total_patient_questions = model.PatientQuestionResponses.Length;
			if (insert_count > 0)
			{
				if (total_answered == total_patient_questions)
					returned.AllPatientResponsesInserted = true;
			}

			return returned;
		}


		public class FilterDwellTimeReturn
		{
			public string Success { get; set; }

			public List<decimal> Avg { get; set; } = new List<decimal>();
			public List<decimal> StdDev { get; set; } = new List<decimal>();
		}

		public class RetrievalRatesReturn
		{
			public string Success { get; set; }

			public List<decimal> Avg { get; set; } = new List<decimal>();
			public decimal Overall { get; set; }
		}

		public class MIPSHistoryReturn
		{
			public string Success { get; set; }
			public List<decimal> CurrentYear { get; set; } = new List<decimal>();
			public List<decimal> PreviousYear { get; set; } = new List<decimal>();
		}

		public class ReportReturn
		{
			public string Success { get; set; }
			public List<int> Counts { get; set; } = new List<int>();
		}

		public class DatedReportReturn
		{
			public string Success { get; set; }

			public List<string> Dates { get; set; } = new List<string>();
			public List<decimal> Scores { get; set; } = new List<decimal>();
		}

		public class PatientFilterHistoryGraphReturn
		{
			public string Success { get; set; }

			public List<string> Tasks { get; set; } = new List<string>();
			public List<string> Dates { get; set; } = new List<string>();
		}

		public class MIPSHistoryItem
		{
			public string Month { get; set; }
			public decimal AvgScore { get; set; }
		}

		public class FilterDwellTimeReportModel
		{
			public string Median { get; set; }
			public string Average { get; set; }
			public string Maximum { get; set; }
			public string Minimum { get; set; }

			public string MadePermanent { get; set; }
			public string Placed { get; set; }
			public string Removed { get; set; }


			public string MIPS { get; set; }

			public string Errors { get; set; }

			[Display(Name = "Beginning Date")]
			[Required()]
			[DataType(DataType.Date)]
			public DateTime? Start { get; set; }


			[Display(Name = "Ending Date")]
			[Required()]
			[DataType(DataType.Date)]
			public DateTime? End { get; set; }

			public bool IsPost { get; set; }
		}
	}

	
}