using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FilterTracker.Models
{
	public class OptionModel
	{
		public bool Selected { get; set; }
		public string Text { get; set; }
		public string Value { get; set; }
	}
}