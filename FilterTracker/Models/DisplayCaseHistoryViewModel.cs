using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Linq;
using System;
using System.Data.Entity;

namespace FilterTracker.Models
{
	public class DisplayCaseHistoryViewModel : ModelBase
	{
		public PatientFilter PatientFilter { get; set; }
		public List<ContactAttempt> NonTaskedContactAttempts { get; set; } = new List<ContactAttempt>();
	}

}