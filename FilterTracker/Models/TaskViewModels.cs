using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FilterTracker.Models
{
	public class SaveTaskViewModelBase : ModelBase
	{
		public int TaskId { get; set; }

		public string Message { get; set; }
	}

	public class RetrievalDatePassedEditorViewModel : ModelBase
	{
		public RetrievalDatePassedEditorViewModel(Models.Task task, FilterTrackerEntities db)
		{
			this.Patient = task.Patient;
			if (task.PatientFilter != null)
			{
				PatientFilterId = task.PatientFilterId;

				if (task.PatientFilter.TargetRemovalDate.HasValue)
					TargetRetrievalDate = task.PatientFilter.TargetRemovalDate.Value.ToShortDateString();
			}
			else
			{
				PatientFilterId = null;
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
		}

		public string TargetRetrievalDate { get; set; }

		public Patient Patient { get; set; }

		public int? PatientFilterId { get; set; }

		public int TaskId { get; set; }

		public string Note { get; set; }

		public string SelectedContactType { get; set; }

		public string SelectedResultCode { get; set; }

		public List<SelectListItem> ContactAttemptTypes { get; private set; }
		public List<SelectListItem> ResultCodeTypes { get; private set; }
	}

	public class BuildCaseTaskEditorViewModel : SaveTaskViewModelBase
	{
		public TaskTypes TaskType { get; } = TaskTypes.BuildCase;

		public QuestionResponseViewModel[] PatientQuestionResponses { get; set; }
		public QuestionResponseViewModel[] PhysicianQuestionResponses { get; set; }
		public bool AmCompleting { get; set; } = false;
	}


	public class ReviewCaseTaskEditorViewModel : SaveTaskViewModelBase
	{
		public ReviewCaseTaskEditorViewModel(Task task, User CurrentUser, FilterTrackerEntities db)
		{
			TaskId = task.Id;
			Task = task;
			var reassess_default = task.Organization.OrganizationDefaults.First().ReassessDays;

			if (!reassess_default.HasValue)
				reassess_default = db.Organizations.Single(s => s.Id == 1).OrganizationDefaults.First().ReassessDays;

			if (!reassess_default.HasValue)
				reassess_default = 7;

			ReassessDays = reassess_default.Value.ToString();
			ScheduleRemovalDefaultDate = DateTime.Now.AddDays(reassess_default.Value).ToString("MM/dd/yyyy");
			ReassessDefaultDate = ScheduleRemovalDefaultDate;
			TaskAttachments = new List<FileUpload>();

			var atts = db.TaskAttachments.Where(w => w.TaskId == task.Id)
				.Select(s => new { s.Id, s.Attachment.FileName, s.Attachment.FileSize, s.UpdateTimestamp })
				.ToList();

			foreach (var item in atts)
			{
				TaskAttachments.Add(new FileUpload
				{
					Id = item.Id,
					Description = "",
					Filename = item.FileName,
					DateUploaded = item.UpdateTimestamp.AddHours(CurrentUser.Organization.TimezoneOffset).ToString("MM/dd/yyyy hh.mm.ss"),
					Size = FormattingHelpers.GetFileSizeString(item.FileSize.ToString())
				});

			}
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


			var bctask = db.Tasks.Where(w => w.PatientFilterId == task.PatientFilterId)
				.Where(w => w.TaskTypeId == (int)TaskTypes.BuildCase)
				.OrderByDescending(o => o.CreateTimestamp)
				.FirstOrDefault();

			if (bctask != null && bctask.Id > 0)
			{
				this.PatientQuestionResponses = bctask.PatientQuestionResponses.ToList();
				this.PhysicianQuestionResponses = bctask.PhysicianQuestionResponses.ToList();

				this.PatientContactAttempts.AddRange(bctask.PatientContactAttempts);
				this.PhysicianContactAttempts.AddRange(bctask.PhysicianContactAttempts);

				var pcp_contacts = db.Tasks.Where(w => w.TaskTypeId == (int)TaskTypes.ContactPCP && w.PatientFilterId == task.PatientFilterId);
				foreach (var ptask in pcp_contacts)
				{
					this.PhysicianContactAttempts.AddRange(ptask.PhysicianContactAttempts);
				}
			}

			if (this.PatientFilter.PrimaryCarePhysician != null)
			{
				this.PCPRemovalApprovalRequired = this.PatientFilter.PrimaryCarePhysician.RequiresRemovalApproval;
				this.HavePCPRemovalApproval = task.PatientFilter.IsPCPApproved;
			}
			else
			{
				this.PCPRemovalApprovalRequired = false;
				this.HavePCPRemovalApproval = true;
			}


		}
		public string SelectedContactType { get; set; }

		public string SelectedResultCode { get; set; }

		public TaskTypes TaskType { get; } = TaskTypes.ReviewCase;

		public Task Task { get; private set; }

		public Patient Patient { get { return Task.Patient; } }

		public PatientFilter PatientFilter { get { return Task.PatientFilter; } }

		public string ReassessNote { get; set; }

		public string ScheduleRemovalDefaultDate { get; set; }

		public string ReassessDays { get; set; }

		public string ReassessDefaultDate { get; set; }

		public bool PCPRemovalApprovalRequired { get; set; }

		public bool PCPRemovalApprovalOverride { get; set; }

		public bool HavePCPRemovalApproval { get; set; }

		public List<PatientContactAttempt> PatientContactAttempts { get; set; } = new List<PatientContactAttempt>();

		public List<PhysicianContactAttempt> PhysicianContactAttempts { get; set; } = new List<PhysicianContactAttempt>();

		public List<FileUpload> TaskAttachments { get; set; }

		public List<PatientQuestion> PatientQuestions { get; set; } = new List<PatientQuestion>();

		public List<PhysicianQuestion> PhysicianQuestions { get; set; } = new List<PhysicianQuestion>();

		public List<PatientQuestionResponses> PatientQuestionResponses { get; set; } = new List<PatientQuestionResponses>();

		public List<PhysicianQuestionResponses> PhysicianQuestionResponses { get; set; } = new List<PhysicianQuestionResponses>();
	}



	public class ReviewPCPPreferencesTaskEditorViewModel : SaveTaskViewModelBase
	{
		public TaskTypes TaskType { get; } = TaskTypes.ReviewPCPPreferences;
		public ReviewPCPPreferencesTaskEditorViewModel()
		{
		}

		public ReviewPCPPreferencesTaskEditorViewModel(Task task, FilterTrackerEntities db, User CurrentUser)
		{

		}


	}

	public class ScheduleRetrievalTaskEditorViewModel : SaveTaskViewModelBase
	{
		public TaskTypes TaskType { get; } = TaskTypes.ScheduleRetrieval;
		public ScheduleRetrievalTaskEditorViewModel(Task task, FilterTrackerEntities db, User CurrentUser)
		{
			Patient = task.Patient;
			PatientFilter = task.PatientFilter;
			TaskId = task.Id;

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
		}

		[Required()]
		[Display(Name = "Target Removal Date")]
		public string TargetRemovalDate { get; set; }

		public Patient Patient { get; set; }

		public PatientFilter PatientFilter { get; set; }

		[Required(ErrorMessage = "Please select the contact method used.")]
		public string SelectedContactType { get; set; }

		[Required(ErrorMessage = "Please select the contact result.")]
		public string SelectedResultCode { get; set; }

		public string Note { get; set; }

		public List<SelectListItem> ContactAttemptTypes { get; private set; }
		public List<SelectListItem> ResultCodeTypes { get; private set; }

	}

	public class SendRegisteredLettersTaskEditorViewModel : SaveTaskViewModelBase
	{
		public SendRegisteredLettersTaskEditorViewModel(Task task)
		{
			Patient = task.Patient;
			PatientFilter = task.PatientFilter;
			TaskId = task.Id;
		}

		public TaskTypes TaskType { get; } = TaskTypes.SendRegisteredLetters;

		public Patient Patient { get; set; }

		public PatientFilter PatientFilter { get; set; }

		[DataType(DataType.Date)]
		[Required]
		[Display(Name = "Date Sent")]
		public string SendDate { get; set; } = DateTime.Now.ToShortDateString();

		[Display(Name = "Tracking Number")]
		[DataType(DataType.Text)]
		[MaxLength(50)]
		public string TrackingNumber { get; set; }

		[Display(Name = "Notes")]
		[DataType(DataType.MultilineText)]
		[MaxLength(2000)]
		public string Note { get; set; }
	}

	public class ContactPCPTaskEditorViewModel : SaveTaskViewModelBase
	{
		public ContactPCPTaskEditorViewModel(Task task, FilterTrackerEntities db)
		{
			Patient = task.Patient;
			PatientFilter = task.PatientFilter;
			TaskId = task.Id;
			ContactReason = task.Notes;

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
		}

		public TaskTypes TaskType { get; } = TaskTypes.ContactPCP;

		public List<SelectListItem> ContactAttemptTypes { get; private set; }
		public List<SelectListItem> ResultCodeTypes { get; private set; }

		public string SelectedContactType { get; set; }

		public string SelectedResultCode { get; set; }

		public Patient Patient { get; set; }

		public PatientFilter PatientFilter { get; set; }

		public string ContactReason { get; set; }

		public string ContactNote { get; set; }

		[Display(Name = "Removal Approval Obtained")]
		public bool PCPApproved { get; set; }
	}

	public class PatientContactAttemptDueTaskEditorViewModel : SaveTaskViewModelBase
	{
		public PatientContactAttemptDueTaskEditorViewModel(Task task, FilterTrackerEntities db)
		{
			Patient = task.Patient;
			//PatientFilter = task.PatientFilter;
			TaskId = task.Id;

			if (task.PatientFilter.Filter != null)
			{
				FilterName = task.PatientFilter.Filter.Name;
				PatientFilterId = task.PatientFilterId;
			}
			else
			{
				FilterName = "";
				PatientFilterId = null;
			}
			//FilterLocation = task.PatientFilter.Location;
			FilterTargetRemovalDate = task.PatientFilter.TargetRemovalDate.HasValue ? task.PatientFilter.TargetRemovalDate.Value.ToLocalTime().ToShortDateString() : "";
			FilterInsertionDate = task.PatientFilter.ProcedureDate.HasValue ? task.PatientFilter.ProcedureDate.Value.ToLocalTime().ToShortDateString() : "";

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
		}

		public TaskTypes TaskType { get; } = TaskTypes.PatientContactDue;

		public List<SelectListItem> ContactAttemptTypes { get; private set; }
		public List<SelectListItem> ResultCodeTypes { get; private set; }

		public Patient Patient { get; set; }

		public int? PatientFilterId { get; set; }

		//public PatientFilter PatientFilter { get; set; }

		public string FilterName { get; set; }
		public string FilterLocation { get; set; }
		public string FilterTargetRemovalDate { get; set; }
		public string FilterInsertionDate { get; set; }

		public string SelectedContactType { get; set; }

		public string SelectedResultCode { get; set; }

		public string Note { get; set; }

		[DataType(DataType.Date)]
		[Required]
		[Display(Name = "Contact Attempt Date")]
		public string ContactDate { get; set; } = DateTime.Now.ToShortDateString();
	}
}