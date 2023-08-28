using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FilterTracker.Models
{
	public class QuickNoteViewModel
	{
		public int Id { get; set; }
		public string Heading { get; set; }
		public string Body { get; set; }

		public int UserId { get; set; }

		public string Success { get; set; }

		public string Message { get; set; }
	}
}