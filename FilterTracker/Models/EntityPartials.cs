using FilterTracker.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace FilterTracker.Models
{
	public partial class User
	{
		//[Required()]
		//[MaxLength(256)]
		//[EmailAddress]
		//public string Email { get; set; }


		public static async Task<bool> IncrementAccessFailedCount(string email)
		{
			bool returned = false;

			using (var db = new FilterTrackerEntities())
			{
				var user_entity = db.Users.SingleOrDefault(sd => sd.Email == email);
				if (user_entity != null && user_entity.Id > 0)
				{
					user_entity.AccessFailedCount++;
					returned = await db.SaveChangesAsync() > 0;
				}
			}
			return returned;
		}

		public static async Task<bool> ResetAccessFailedCount(string email)
		{
			bool returned = false;
			using (var db = new FilterTrackerEntities())
			{
				var user_entity = db.Users.SingleOrDefault(sd => sd.Email == email);
				if (user_entity != null && user_entity.Id > 0)
				{
					user_entity.AccessFailedCount = 0;
					returned = await db.SaveChangesAsync() > 0;
				}
			}
			return returned;
		}

		internal static async Task<bool> UpdateLoginTimestamp(string userId, DateTime now)
		{
			bool returned = false;

			using (var db = new FilterTrackerEntities())
			{
				var entity = db.Users.SingleOrDefault(sd => sd.Email == userId);
				if (entity != null && entity.Email == userId)
				{
					entity.LastSuccessfulLogin = now;
					returned = await db.SaveChangesAsync() == 1;
				}
			}

			return returned;
		}
	}

	public partial class UserRole
	{

	}

	public partial class Organization
	{
		public bool HydrateFrom(OrganizationEditorViewModel model)
		{
			if (model == null)
				return false;

			this.Name = model?.Name;
			this.Description = model?.Description;
			this.CreateTimestamp = DateTime.UtcNow;
			this.UpdateTimestamp = this.CreateTimestamp;
			this.Active = model.Active;
			this.AddressLine1 = model?.AddressLine1;
			this.AddressLine2 = model?.AddressLine2;
			this.City = model?.City;
			this.ContactEmail = model?.ContactEmail;
			this.ContactName = model?.ContactName;
			this.ContactPhone = model?.ContactPhone;
			this.MaxUsers = model.MaxUsers;
			this.State = model?.State;
			this.Zipcode = model?.Zipcode;

			return true;
		}

		public bool UpdateFrom(OrganizationEditorViewModel model)
		{
			if (model == null)
				return false;

			this.Name = model?.Name;
			this.Description = model?.Description;
			this.UpdateTimestamp = DateTime.UtcNow;
			this.Active = model.Active;
			this.AddressLine1 = model?.AddressLine1;
			this.AddressLine2 = model?.AddressLine2;
			this.City = model?.City;
			this.ContactEmail = model?.ContactEmail;
			this.ContactName = model?.ContactName;
			this.ContactPhone = model?.ContactPhone;
			this.MaxUsers = model.MaxUsers;
			this.State = model?.State;
			this.Zipcode = model?.Zipcode;
			return true;
		}
	}

	public partial class Patient
	{
		[Display(Name = "Full Name")]
		[System.ComponentModel.DataAnnotations.MaxLength(300)]
		public string FullName
		{
			get
			{
				if (!string.IsNullOrEmpty(MiddleName))
				{
					return $"{LastName}, {FirstName} {MiddleName}";
				}

				return $"{LastName}, {FirstName}";
			}
		}

		public static bool Overlay(PatientEditorModel model, ref Patient entity)
		{
			bool returned = true;

			if (entity != null && entity.Id == model.Id)
			{
				if (string.IsNullOrEmpty(model.AddressLine1))
					entity.AddressLine1 = null;
				else if (model.AddressLine1 != entity.AddressLine1)
					entity.AddressLine1 = model.AddressLine1.Trim();

				if (string.IsNullOrEmpty(model.AddressLine2))
					entity.AddressLine2 = null;
				else if (model.AddressLine2 != entity.AddressLine2)
					entity.AddressLine2 = model.AddressLine2.Trim();

				if (string.IsNullOrEmpty(model.City))
					entity.City = null;
				else if (model.City != entity.City)
					entity.City = model.City.Trim();

				if (!string.IsNullOrEmpty(model.DateOfBirth))
				{
					DateTime tmp;
					if (DateTime.TryParse(model.DateOfBirth, out tmp))
					{
						if (tmp != entity.DateOfBirth)
							entity.DateOfBirth = tmp.Date;
					}
				}
				else
				{
					returned = false;
				}

				if (!string.IsNullOrEmpty(model.DeceasedDate))
				{
					DateTime tmp;
					if (DateTime.TryParse(model.DeceasedDate, out tmp))
					{
						if (tmp != entity.DeceasedDate)
							entity.DeceasedDate = tmp.Date;
					}
				}

				if (!string.IsNullOrEmpty(model.FirstName))
				{
					if (model.FirstName != entity.FirstName)
						entity.FirstName = model.FirstName.Trim();
				}
				//else
				//	returned = false;

				if (string.IsNullOrEmpty(model.MiddleName))
					entity.MiddleName = null;
				else if (model.MiddleName != entity.MiddleName)
					entity.MiddleName = model.MiddleName.Trim();

				if (!string.IsNullOrEmpty(model.LastName))
				{
					if (model.LastName != entity.LastName)
						entity.LastName = model.LastName.Trim();
				}
				//else
				//	returned = false;

				//if (!string.IsNullOrEmpty(model.Gender))
				//{
				if (model.SelectedGender != entity.Gender)
					entity.Gender = model.SelectedGender;
				//}
				//else
				//	returned = false;

				if (!string.IsNullOrEmpty(model.MRN))
				{
					model.MRN = model.MRN.Trim().TrimStart(new char[] { '0' });
					if (model.MRN != entity.MRN)
						entity.MRN = model.MRN;
				}
				//else
				//	returned = false;

				if (string.IsNullOrEmpty(model.PrimaryEmailAddress))
					entity.PrimaryEmailAddress = null;
				else if (model.PrimaryEmailAddress != entity.PrimaryEmailAddress)
					entity.PrimaryEmailAddress = model.PrimaryEmailAddress.Trim();

				if (string.IsNullOrEmpty(model.PrimaryPhoneNumber))
					entity.PrimaryPhoneNumber = null;
				else if (model.PrimaryPhoneNumber != entity.PrimaryPhoneNumber)
					entity.PrimaryPhoneNumber = model.PrimaryPhoneNumber.Trim();

				if (string.IsNullOrEmpty(model.SecondaryEmailAddress))
					entity.SecondaryEmailAddress = null;
				else if (model.SecondaryEmailAddress != entity.SecondaryEmailAddress)
					entity.SecondaryEmailAddress = model.SecondaryEmailAddress.Trim();

				if (string.IsNullOrEmpty(model.SecondaryPhoneNumber))
					entity.SecondaryPhoneNumber = null;
				else if (model.SecondaryPhoneNumber != entity.SecondaryPhoneNumber)
					entity.SecondaryPhoneNumber = model.SecondaryPhoneNumber.Trim();

				if (string.IsNullOrEmpty(model.State))
					entity.State = null;
				else if (model.State != entity.State)
					entity.State = model.State.Trim();

				if (string.IsNullOrEmpty(model.Zipcode))
					entity.Zipcode = null;
				else if (model.Zipcode != entity.Zipcode)
					entity.Zipcode = model.Zipcode.Trim();

				if (model.Active != entity.Active)
					entity.Active = model.Active;
			}
			else
			{
				returned = false;
			}

			return returned;
		}
	}

	public partial class PatientFilter
	{
		public bool IsValid()
		{
			//if (FilterId <= 0)
			//{
			//	ValidationErrors.Add("Filter is requird.");
			//}

			if (OrganizationId < 0)
			{
				ValidationErrors.Add("Organization is required.");
			}

			if (PatientId < 0)
			{
				ValidationErrors.Add("Patient is missing.");
			}

			return !ValidationErrors.Any();
		}

		public List<string> ValidationErrors { get; set; } = new List<string>();

		public int DwellTime
		{
			get
			{
				if (this.ProcedureDate.HasValue)
				{
					if (ActualRemovalDate.HasValue)
					{
						return (this.ActualRemovalDate - this.ProcedureDate).Value.Days;
					}
					else
					{
						return (DateTime.UtcNow - this.ProcedureDate).Value.Days;
					}
				}
				return 0;
			}
		}
	}

	public partial class Task
	{
		public List<string> ValidationErrors { get; set; } = new List<string>();
		public bool IsValid()
		{
			bool returned = false;
			switch (this.TaskTypeId)
			{
				case (int)TaskTypes.BuildCase:
					//if (this.TaskAttachments.Count() == 0)
					//{
					//	ValidationErrors.Add("No case files have been uploaded.");
					//}
					if (!this.Patient.Active)
					{
						ValidationErrors.Add("Cannot complete task for an inactive patient.  Please activate the patient to proceed.");
					}

					if (string.IsNullOrEmpty(this.Patient.FirstName))
					{
						ValidationErrors.Add("Patient first name is missing.");
					}
					if (string.IsNullOrEmpty(this.Patient.LastName))
					{
						ValidationErrors.Add("Patient last name is missing.");
					}
					////if (string.IsNullOrEmpty(this.Patient.MRN))
					////{
					////	ValidationErrors.Add("Patient MRN is missing.");
					////}
					//if (string.IsNullOrEmpty(this.Patient.PrimaryEmailAddress) && string.IsNullOrEmpty(this.Patient.PrimaryPhoneNumber))
					//{
					//	ValidationErrors.Add("A primary email address and primary phone number for this patient are missing.  Please supply one or the other.");
					//}
					//if (this.PatientQuestionResponses.Count() == 0 && this.PhysicianQuestionResponses.Count() == 0)
					//{
					//	ValidationErrors.Add("Patient survey and physician survey are both missing.  Please supply one or the other.");
					//}

					if (ValidationErrors.Count() == 0)
					{
						returned = true;
					}
					break;
				case (int)TaskTypes.RetrievalDatePassed:
					if (this.PatientFilter == null)
					{
						ValidationErrors.Add("No patient filter associated with this task.");
					}

					if (ValidationErrors.Count() == 0)
					{
						returned = true;
					}
					break;
				case (int)TaskTypes.ReviewCase:
					if (!this.PatientFilterId.HasValue)
					{
						ValidationErrors.Add("No patient filter associated with this task.");
					}

					if (ValidationErrors.Count() == 0)
					{
						returned = true;
					}
					break;
				case (int)TaskTypes.ReviewPCPPreferences:
					break;
				case (int)TaskTypes.ScheduleRetrieval:
					if (!this.PatientFilterId.HasValue)
					{
						ValidationErrors.Add("No patient filter associated with this task.");
					}

					if (ValidationErrors.Count() == 0)
					{
						returned = true;
					}
					break;
				case (int)TaskTypes.SendRegisteredLetters:
					if (!this.PatientFilterId.HasValue)
					{
						ValidationErrors.Add("No patient filter associated with this task.");
					}

					if (ValidationErrors.Count() == 0)
					{
						returned = true;
					}
					break;
				case (int)TaskTypes.ContactPCP:
					returned = true;
					break;
				case (int)TaskTypes.PatientContactDue:
					if (!this.PatientFilterId.HasValue)
					{
						ValidationErrors.Add("No patient filter associated with this task.");
					}

					if (ValidationErrors.Count() == 0)
					{
						returned = true;
					}
					break;
				default:
					break;
			}


			return returned;
		}

		public int Age
		{
			get
			{
				if (this.HideUntil.HasValue && this.HideUntil >= this.CreateTimestamp)
				{
					return (DateTime.UtcNow - this.HideUntil.Value).Days;
				}
				else
				{
					return (DateTime.UtcNow - this.CreateTimestamp).Days;
				}
			}
		}

		public string AgeCSSClass
		{
			get
			{
				int age = this.Age;

				int lower = int.Parse(ConfigurationManager.AppSettings["AgeThresholdLower"] ?? "7");
				int upper = int.Parse(ConfigurationManager.AppSettings["AgeThresholdUpper"] ?? "21");

				if (age <= lower)
					return "bg-success text-white";
				else if (age > lower && age <= upper)
					return "bg-warning text-dark";
				else
					return "bg-danger text-white";
			}
		}

		public bool IsClaimed
		{
			get
			{
				return AssignedUserId.HasValue;
			}
		}

		public bool CreateAntecedentTasks(FilterTrackerEntities db, int? targetUserId)
		{
			bool returned = false;
			DateTime now = DateTime.UtcNow;

			List<Task> added = new List<Task>();

			try
			{
				if (TaskTypeId == (int)TaskTypes.BuildCase)
				{
					// Need a ReviewCase Task.
					if (!targetUserId.HasValue)
						return returned;

					Task brct = BuildReviewCaseTask(targetUserId.Value);
					db.Tasks.Add(brct);
					db.SaveChanges();

					var tas = db.TaskAttachments.Where(w => w.TaskId == this.Id).ToList();

					foreach (TaskAttachment ta in tas)
					{
						var inserted = new TaskAttachment()
						{
							AttachmentId = ta.AttachmentId,
							CreateTimestamp = now,
							CreateUserId = this.AssignedUserId.Value,
							TaskId = brct.Id,
							UpdateTimestamp = now,
							UpdateUserId = this.AssignedUserId.Value
						};

						db.TaskAttachments.Add(inserted);
					}
				}
				returned = true;
			}
			catch (Exception ex)
			{
				Logger.LogException(ex);
			}

			return returned;
		}

		private Task BuildReviewCaseTask(int userId)
		{
			Task returned = new Task();

			returned.AssignedUserId = userId;
			returned.CreateTimestamp = DateTime.UtcNow;
			returned.CreateUserId = this.AssignedUserId.HasValue ? this.AssignedUserId.Value : userId;
			returned.OrganizationId = this.OrganizationId;
			returned.PatientFilterId = this.PatientFilterId.HasValue ? this.PatientFilterId.Value : (int?)null;
			returned.PatientId = this.PatientId;
			returned.TaskTypeId = (int)TaskTypes.ReviewCase;
			returned.UpdateTimestamp = returned.CreateTimestamp;
			returned.UpdateUserId = returned.CreateUserId;


			return returned;
		}
	}

	public partial class QuickNote
	{
		public string Color { get; set; }

		public QuickNote()
		{
			var r = new Random();
			int red = r.Next(1, 255);
			int green = r.Next(1, 255);
			int blue = r.Next(1, 255);


			Color = $"rgba({red},{green},{blue},1.0";
		}
	}
}