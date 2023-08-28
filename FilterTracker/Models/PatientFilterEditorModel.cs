using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;

namespace FilterTracker.Models
{
	public class PatientFilterEditorModel : ModelBase
	{
		public PatientFilterEditorModel()
		{

		}

		public bool AmEditing { get; set; }

		public List<string> Errors { get; set; } = new List<string>();

		public void HydrateLookups(int organizationId, ref FilterTrackerEntities db)
		{
			// no organization specific indications for now
			Indications = db.Indications.AsNoTracking().Select(s => new SelectListItem() { Text = s.Name, Value = s.Id.ToString() }).ToList();

			// no organization specific complicating factors for now
			ComplicatingFactors = db.ComplicatingFactors.AsNoTracking().Where(w => w.OrganizationId == 1 || w.OrganizationId == organizationId).Select(s => new SelectListItem() { Text = s.Name, Value = s.Id.ToString() }).ToList();

			// Get base filters (Organization == 1...), and any filters specific to this organization
			var filters = db.Filters
				.AsNoTracking()
				.Where(w => w.OrganizationId == organizationId || w.OrganizationId == 1)
				.Select(s => new SelectListItem() { Text = s.Name, Value = s.Id.ToString() })
				.ToList();

			filters.Insert(0, new SelectListItem() { Value = "0", Text = "" });

			Filters = filters.AsEnumerable();

			var physicians = db.Physicians.AsNoTracking()
				.Where(w => w.OrganizationId == organizationId)
				.Select(s => new SelectListItem() { Text = s.Name, Value = s.Id.ToString() })
				.ToList();

			physicians.Insert(0, new SelectListItem() { Value = "0", Text = "" });

			Physicians = physicians.AsEnumerable();

			var clinicPhysicianUsers = db.Users
				.Where(w => w.OrganizationId == organizationId && w.UserRoles.Any(a => a.Role.Name == "Physician"))
				.ToList();

			var clinicPhysiciansSelectItems = new List<SelectListItem>();

			foreach (var item in clinicPhysicianUsers)
			{
				clinicPhysiciansSelectItems.Add(new SelectListItem() { Text = $"{item.FirstName} {item.LastName}", Value = item.Id.ToString() });
			}
			clinicPhysiciansSelectItems.Insert(0, new SelectListItem() { Value = "0", Text = "" });
			ClinicPhysicians = clinicPhysiciansSelectItems.AsEnumerable();

		}

		public IEnumerable<SelectListItem> Filters { get; private set; }

		public IEnumerable<SelectListItem> Indications { get; private set; }

		public IEnumerable<SelectListItem> ComplicatingFactors { get; private set; }

		public IEnumerable<SelectListItem> Physicians { get; private set; }

		public IEnumerable<SelectListItem> ClinicPhysicians { get; private set; }

		public int PatientId { get; set; }

		public int PatientFilterId { get; set; }

		private Patient _patient;
		public Patient Patient
		{
			get
			{
				if (_patient == null)
				{
					using (var db = new FilterTrackerEntities())
					{
						_patient = db.Patients.AsNoTracking().SingleOrDefault(sd => sd.Id == PatientId);
					}
				}
				return _patient;
			}
		}


		[Required]
		public int OrganizationId { get; set; }

		//[Required]
		public int? SelectedFilterId { get; set; }

		public int SelectedIndicationId { get; set; }

		[Display(Name = "Complications")]
		public int SelectedComplicatingFactorId { get; set; }

		[MaxLength(500)]
		public string Location { get; set; }

		public string Notes { get; set; }

		[Display(Name = "IVC Filter Placement Date")]
		[Required()]
		public string ProcedureDate { get; set; }

		public string TargetRemovalDate { get; set; }

		public string ActualRemovalDate { get; set; }

		[Display(Name = "Primary Care Provider")]
		public int? SelectedPrimaryCarePhysicianId { get; set; }

		public int? SelectedOrderingPhysicianId { get; set; }

		[Display(Name = "IVC Filter Placed By")]
		public int? SelectedProcedurePhysicianId { get; set; }

		//[Required()]
		[Display(Name = "Intended for removal at placement")]
		public bool IsTemporary { get; set; }

		[Display(Name = "Made permanent on")]
		public DateTime? MadePermanent { get; set; }

		[Display(Name = "Made permanent by")]
		[MaxLength(100)]
		public string MadePermanentBy { get; set; }

		internal PatientFilter ToPatientFilter()
		{
			PatientFilter returned = new PatientFilter();
			DateTime parsed;

			returned.PatientId = this.PatientId;

			returned.Id = this.PatientFilterId;

			if (!string.IsNullOrEmpty(ActualRemovalDate))
			{
				if (DateTime.TryParse(ActualRemovalDate, out parsed))
				{
					returned.ActualRemovalDate = parsed.Date.ToUniversalTime().Date;
				}
			}

			if (!string.IsNullOrEmpty(TargetRemovalDate))
			{
				if (DateTime.TryParse(TargetRemovalDate, out parsed))
				{
					returned.TargetRemovalDate = parsed.Date.ToUniversalTime().Date;
				}
			}

			if (!string.IsNullOrEmpty(this.ProcedureDate))
			{
				if (DateTime.TryParse(ProcedureDate, out parsed))
				{
					returned.ProcedureDate = parsed.Date.ToUniversalTime().Date;
				}
			}

			if (!string.IsNullOrEmpty(this.Location))
			{
				returned.Location = this.Location;
			}

			if (!string.IsNullOrEmpty(this.Notes))
			{
				returned.Notes = this.Notes;
			}

			if (this.SelectedPrimaryCarePhysicianId.HasValue)
			{
				returned.PrimaryCarePhysicianId = this.SelectedPrimaryCarePhysicianId.Value;
			}

			if (this.SelectedProcedurePhysicianId.HasValue)
			{
				returned.ProcedurePhysicianId = this.SelectedProcedurePhysicianId.Value;
			}

			if (this.SelectedOrderingPhysicianId.HasValue)
			{
				returned.OrderingPhysicianId = this.SelectedOrderingPhysicianId.Value;
			}

			returned.FilterId = this.SelectedFilterId;

			returned.OrganizationId = this.OrganizationId;

			returned.ComplicatingFactorId = this.SelectedComplicatingFactorId;

			returned.IndicationId = this.SelectedIndicationId;

			returned.IsTemporary = this.IsTemporary;

			returned.MadePermanent = this.MadePermanent;

			this.MadePermanentBy = this.MadePermanentBy?.Trim();
			if (!string.IsNullOrEmpty(this.MadePermanentBy))
				returned.MadePermanentBy = this.MadePermanentBy;

			return returned;

		}
	}
}