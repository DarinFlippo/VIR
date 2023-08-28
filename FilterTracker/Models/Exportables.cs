using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace FilterTracker.Models
{

	public class PatientFilterExport
	{
		public PatientFilterExport()
		{

		}
		public int PatientId { get; set; }

		public int PatientFilterId { get; set; }

		public string FilterName { get; set; }

		public string Indication { get; set; }

		public string ComplicatingFactor { get; set; }

		public string Notes { get; set; }

		public string IVCFilterPlacementDate { get; set; }

		public string TargetRemovalDate { get; set; }

		public string ActualRemovalDate { get; set; }

		public string PrimaryCareProvider { get; set; }

		public string OrderingPhysician { get; set; }

		public string IVCFilterPlacedBy { get; set; }

		public bool IsTemporary { get; set; }

		public DateTime? MadePermanent { get; set; }

		public string MadePermanentBy { get; set; }
	}

	public class TaskExport
	{
		public TaskExport() { }

		public int Id { get; set; }
		public string TaskType { get; set; }

		public string AssignedTo { get; set; }

		public string Notes { get; set; }

		public DateTime CreateTimestamp { get; set; }

		public string CreateUser { get; set; }

		public DateTime UpdateTimestamp { get; set; }

		public string UpdateUser { get; set; }

		public DateTime? ClosedDate { get; set; }

		public DateTime TargetCloseDate { get; set; }



	}

	public class SurveyResponse
	{
		public SurveyResponse()
		{
		}

		public string Question { get; set; }
		public string Answer { get; set; }
	}

	public partial class ContactAttemptExport
	{
		public int Id { get; set; }
		public string ContactType { get; set; }
		public string ContactResultCode { get; set; }
		public int? RelatedTaskId { get; set; }
		public System.DateTime Timestamp { get; set; }
		public string Notes { get; set; }
		public string TrackingNumber { get; set; }
		public DateTime CreateTimestamp { get; set; }
		public string CreateUser { get; set; }
		public DateTime UpdateTimestamp { get; set; }
		public string UpdateUser { get; set; }
	}
}