using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FilterTracker.Models
{
	public class BuildCaseTaskEditorModel : ModelBase
	{
		public BuildCaseTaskEditorModel()
		{

		}

		public Patient Patient { get; set; } = new Patient();

		public PatientFilter PatientFilter { get; set; } = new PatientFilter();

		public BuildCaseTaskEditorModel(FilterTrackerEntities db, User CurrentUser, Models.Task task)
		{
			// if there are no successful patient contact attempts, not going to review, so dong ask for phys
			//if (task.PatientContactAttempts.Count(c => c.ContactResultCodeId == 1) > 0)
			//{
			//	RequestPhysician = true;
			//}
			//else
			//{
			//	RequestPhysician = false;
			//}

			this.Patient = task.Patient;
			this.PatientFilter = task.PatientFilter;

			this.PatientQuestions = db.PatientQuestions.AsNoTracking()
									.Where(w => w.OrganizationId == CurrentUser.OrganizationId)
									.OrderBy(o => o.DisplayOrderIndex)
									.ToList();

			if (!this.PatientQuestions.Any())
			{
				this.PatientQuestions = db.PatientQuestions.AsNoTracking()
					.Where(w => w.OrganizationId == 1)
					.OrderBy(o => o.DisplayOrderIndex)
					.ToList();
			}

			this.PhysicianQuestions = db.PhysicianQuestions
					.AsNoTracking()
					.Where(w => w.OrganizationId == CurrentUser.OrganizationId)
					.OrderBy(o => o.DisplayOrderIndex)
					.ToList();

			if (!this.PhysicianQuestions.Any())
			{
				this.PhysicianQuestions = db.PhysicianQuestions
					.AsNoTracking()
					.Where(w => w.OrganizationId == 1)
					.OrderBy(o => o.DisplayOrderIndex)
					.ToList();
			}


			this.PatientQuestionResponses = task.PatientQuestionResponses.ToList();
			this.PhysicianQuestionResponses = task.PhysicianQuestionResponses.ToList();

			this.TaskId = task.Id;

			var bc_atts = db.TaskAttachments.Where(w => w.TaskId == task.Id)
				.Select(s => new { s.Attachment.FileName, s.Id, s.Attachment.FileSize, s.UpdateTimestamp });

			foreach (var item in bc_atts)
			{
				//double size = ( ( (double)item.FileSize ) / 1024 ) / 1024;
				TaskAttachments.Add(new FileUpload()
				{
					DateUploaded = item.UpdateTimestamp.AddHours(CurrentUser.Organization.TimezoneOffset).ToString("MM/dd/yyyy hh.mm.ss"),
					Description = "",
					Filename = item.FileName,
					Size = FormattingHelpers.GetFileSizeString(item.FileSize.ToString()),
					Id = item.Id
				});

			}

			ContactAttemptTypes = new List<SelectListItem>();
			ContactAttemptTypes.Add(new SelectListItem { Value = "", Text = "" });
			ContactAttemptTypes.Add(new SelectListItem { Value = ((int)ContactTypes.Phone).ToString(), Text = "Phone" });
			//ContactAttemptTypes.AddRange(db.ContactTypes.Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() }));

			ResultCodeTypes = new List<SelectListItem>();
			ResultCodeTypes.Add(new SelectListItem { Value = "", Text = "" });
			var crc = db.ContactResultCodes.Select(s => new { s.ResultCode, s.Description, s.Id }).ToList();
			foreach (var item in crc)
			{
				ResultCodeTypes.Add(new SelectListItem { Text = $"{item.ResultCode} - {item.Description}", Value = item.Id.ToString() });
			}

			//if (task.PatientContactAttempts.Any())
			//{
			//	RelatedPatientContactAttempt = task.PatientContactAttempts.Last();
			//	SelectedContactType = RelatedPatientContactAttempt.ContactTypeId.ToString();
			//	SelectedResultCode = RelatedPatientContactAttempt.ContactResultCodeId.ToString();
			//	Note = RelatedPatientContactAttempt.Notes;
			//}

			//if (task.PhysicianQuestionResponses.Any())
			//{
			//	RelatedPhysicianContactAttempt = task.PhysicianContactAttempts.Last();
			//	SelectedPhysicianContactType = RelatedPhysicianContactAttempt.ContactTypeId.ToString();
			//	SelectedPhysicianContactResultCode = RelatedPhysicianContactAttempt.ContactResultCodeId.ToString();
			//	Note = RelatedPhysicianContactAttempt.Notes;
			//}
		}
		public int TaskId { get; set; }

		[Required()]
		public int OrganizationId { get; set; }

		//public bool RequestPhysician { get; set; }

		public PatientContactAttempt RelatedPatientContactAttempt { get; set; }

		public PhysicianContactAttempt RelatedPhysicianContactAttempt { get; set; }

		public List<PatientQuestion> PatientQuestions { get; set; } = new List<PatientQuestion>();

		public List<PhysicianQuestion> PhysicianQuestions { get; set; } = new List<PhysicianQuestion>();

		public List<PatientQuestionResponses> PatientQuestionResponses { get; set; } = new List<PatientQuestionResponses>();

		public List<PhysicianQuestionResponses> PhysicianQuestionResponses { get; set; } = new List<PhysicianQuestionResponses>();

		public List<FileUpload> TaskAttachments { get; set; } = new List<FileUpload>();

		[Required(ErrorMessage="Please select the contact method used.")]
		public string SelectedContactType { get; set; }

		[Required(ErrorMessage = "Please select the contact result.")]
		public string SelectedResultCode { get; set; }

		public string Note { get; set; }

		public string SelectedPhysicianContactType { get; set; }

		public string SelectedPhysicianContactResultCode { get; set; }

		public string PhysicianContactNote { get; set; }

		public List<SelectListItem> ContactAttemptTypes { get; private set; }
		public List<SelectListItem> ResultCodeTypes { get; private set; }
	}

	public class FileUpload
	{
		public int Id { get; set; }
		public string Filename { get; set; }
		public string DateUploaded { get; set; }
		public string Size { get; set; }
		public string Description { get; set; }

	}
}