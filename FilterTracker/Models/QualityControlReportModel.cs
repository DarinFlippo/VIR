using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Linq;
using System;
using System.Data.Entity;

namespace FilterTracker.Models
{
	public class QualityControlReportModel : ModelBase
	{
		public List<PatientFilter> AbnormalPatientFilters { get; set; } = new List<PatientFilter>();

		public QualityControlReportModel()
		{

		}

		public QualityControlReportModel(int organizationId, FilterTrackerEntities db)
		{

			try
			{
				// Look for filters without removal dates, that have no associated open tasks.
				var query = db.PatientFilters
					.Include(i => i.Patient)
					.Include(i => i.Tasks)
					.Where(w => w.OrganizationId == organizationId)
					.Where(w => w.ActualRemovalDate == null)
					.Where(w => w.Tasks.Count(c => c.ClosedDate.HasValue == false) == 0)
					.Where(w => w.MadePermanent.HasValue == false)
					.Where(w => w.TargetRemovalDate.HasValue == false)
					.Where(w => w.Patient.Active);

				AbnormalPatientFilters.AddRange(query.ToList());

			}
			catch (Exception ex)
			{
				Logger.LogException(ex);
				throw ex;
			}

		}
	}
}