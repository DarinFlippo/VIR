using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Linq;
using System;
using System.Data.Entity;

namespace FilterTracker.Models
{
	public class PatientDetailsModel : ModelBase
	{
		private User User { get; set; }

		public PatientDetailsModel(int id, int orgid, ref FilterTrackerEntities db, ref User user)
		{
			User = user;

			Patient = db.Patients.AsNoTracking()
				.Include(i => i.PatientFilters).AsNoTracking()
				.SingleOrDefault(sd => sd.Id == id && sd.OrganizationId == orgid);

			if (Patient != null && Patient.Id == id)
			{
				CurrentFilters = Patient.PatientFilters.Where(w => w.ActualRemovalDate == null).ToList();

				AllFilters = Patient.PatientFilters.ToList();

				ContactAttempts = Patient.PatientFilters
					.SelectMany(sm => sm.PatientContactAttempts).ToList();

				ContactAttempts.AddRange(
					Patient.PatientFilters
					.SelectMany(sm => sm.PhysicianContactAttempts).ToList()
					.Select(s => new PatientContactAttempt()
					{
						Id = s.Id,
						OrganizationId = s.OrganizationId,
						PatientFilterId = s.PatientFilterId,
						ContactTypeId = s.ContactTypeId,
						ContactResultCodeId = s.ContactResultCodeId,
						RelatedTaskId = s.RelatedTaskId,
						Timestamp = s.Timestamp,
						Notes = s.Notes,
						CreateTimestamp = s.CreateTimestamp,
						CreateUserId = s.CreateUserId,
						UpdateTimestamp = s.UpdateTimestamp,
						UpdateUserId = s.UpdateUserId,
						TrackingNumber = null,
						ContactResultCode = s.ContactResultCode,
						ContactType = s.ContactType,
						Organization = s.Organization,
						PatientFilter = s.PatientFilter,
						Task = s.Task
					}).ToList());


			}
			else
			{
				CurrentFilters = new List<PatientFilter>();
				AllFilters = new List<PatientFilter>();
				ContactAttempts = new List<PatientContactAttempt>();
			}
		}

		public List<PatientContactAttempt> ContactAttempts { get; set; }

		public List<PatientFilter> CurrentFilters { get; set; }

		public List<PatientFilter> AllFilters { get; set; }

		public Patient Patient { get; set; }

		public bool CanExport
		{
			get
			{
				return User.UserRoles.Any(a => a.Role.Name == Roles.OrgAdmins || a.Role.Name == Roles.SuperUsers || a.Role.Name == Roles.Physician);
			}
		}
	}

	public class ContactAttempt
	{

		public ContactType ContactType { get; set; }

		public ContactResultCode ResultCode { get; set; }

		public DateTime Timestamp { get; set; }

		public string TrackingNumber { get; set; }

		public string Notes { get; set; }

	}
}