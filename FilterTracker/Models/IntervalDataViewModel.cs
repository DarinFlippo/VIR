using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace  FilterTracker.Models {
	/// <summary>
	/// Summary description for Class1
	/// </summary>
	public class IntervalDataViewModel
	{

		public DateTime? start { get; set; }
		public DateTime? end { get; set; }
		public int FiltersPlaced { get; set; }
		public int A1 { get; set; }
		public List<PatientFilter> A1Popluation { get; set; } = new List<PatientFilter>();
		public int A3 { get; set; }
		public List<PatientFilter> A3Popluation { get; set; } = new List<PatientFilter>();
		public int A2 { get; set; }
		public List<PatientFilter> A2Popluation { get; set; } = new List<PatientFilter>();
		public int A4 { get; set; }
		public List<PatientFilter> A4Population { get; set; } = new List<PatientFilter>();

		public int SelectedOrganizationId { get; set; }
		public bool IsCalculated { get; set; }

		public List<SelectListItem> Organizations { get; private set; }

		public IntervalDataViewModel() {
			using (var db = new FilterTrackerEntities())
			{
				Organizations = db.Organizations
					.Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() })
					.ToList();
			}
		}
	}

	public class FilterDuration
	{
		public string MRN { get; set; }
		public DateTime PlacementDate { get; set; }
		public DateTime? RemovalDate { get; set; }
	}

	public class ContactLag
	{
		public string MRN { get; set; }
		public DateTime PlacementDate { get; set; }
		public DateTime? FirstContactDate { get; set; }
	}
}