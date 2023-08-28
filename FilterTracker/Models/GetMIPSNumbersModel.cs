using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Linq;
using System;
using System.Data.Entity;

namespace FilterTracker.Models
{
	public class GetMIPSNumbersModel
	{
		public int Denominator { get; set; }
		public int N1 { get; set; }
		public int N2 { get; set; }
		public int N3 { get; set; }
		public int N4 { get; set; }

		public int Numerator { get { return N1 + N2 + N3; } }

		public List<Patient> DPatients { get; set; } = new List<Patient>();
		public List<Patient> N1Patients { get; set; } = new List<Patient>();
		public List<Patient> N2Patients { get; set; } = new List<Patient>();

		public List<Patient> N3Patients { get; set; } = new List<Patient>();

		public List<Patient> N4Patients { get; set; } = new List<Patient>();

		public int SelectedOrganizationId { get; set; }

		public List<SelectListItem> Organizations { get; private set; }

		public bool IsCalculated { get; set; }

		public GetMIPSNumbersModel()
		{
			Denominator = 0;
			N1 = 0;
			N2 = 0;
			N3 = 0;
			N4 = 0;

			using (var db = new FilterTrackerEntities())
			{
				Organizations = db.Organizations
					.Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() })
					.ToList();
			}
		}
	}
}