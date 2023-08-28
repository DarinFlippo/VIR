using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FilterTracker.Models
{
	public class ModelBase
	{
		public string ErrorMessage { get; set; }

		public User CurrentUser { get; set; }
	}
}