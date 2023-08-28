using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FilterTracker.Models
{
	public class PatientContactAttemptModel : ModelBase
	{
		public PatientContactAttemptModel()
		{
		}

		public PatientContactAttemptModel(int patientFilterId, int organizationId)
		{
			PatientFilterId = patientFilterId;

			using (var db = new FilterTrackerEntities())
			{
				var pf = db.PatientFilters.SingleOrDefault(sd => sd.Id == patientFilterId && sd.OrganizationId == organizationId);
				if (pf != null && pf.Id == patientFilterId)
				{
					Patient = pf.Patient;
					//PatientFilter = task.PatientFilter;
					FilterName = pf.Filter != null ? pf.Filter.Name : "";
					//FilterLocation = pf.Location;
					FilterTargetRemovalDate = pf.TargetRemovalDate.HasValue ? pf.TargetRemovalDate.Value.ToLocalTime().ToShortDateString() : "";
					FilterInsertionDate = pf.ProcedureDate.HasValue ? pf.ProcedureDate.Value.ToLocalTime().ToShortDateString() : "";
				}
				else
				{
					ErrorMessage = "Not found.";
				}

				ContactAttemptTypes = new List<SelectListItem>();
				ContactAttemptTypes.Add(new SelectListItem { Value = "", Text = "" });
				ContactAttemptTypes.Add(new SelectListItem { Value = ((int)ContactTypes.Phone).ToString(), Text = "Phone" });
				//ContactAttemptTypes.AddRange(db.ContactTypes.AsNoTracking().Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() }));

				ResultCodeTypes = new List<SelectListItem>();
				ResultCodeTypes.Add(new SelectListItem { Value = "", Text = "" });
				var crc = db.ContactResultCodes.AsNoTracking().Select(s => new { s.ResultCode, s.Description, s.Id }).ToList();
				foreach (var item in crc)
				{
					ResultCodeTypes.Add(new SelectListItem { Text = $"{item.ResultCode} - {item.Description}", Value = item.Id.ToString() });
				}
			}
		}


		public List<SelectListItem> ContactAttemptTypes { get; private set; }
		public List<SelectListItem> ResultCodeTypes { get; private set; }

		public Patient Patient { get; set; }

		//public PatientFilter PatientFilter { get; set; }
		public int PatientFilterId { get; set; }

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