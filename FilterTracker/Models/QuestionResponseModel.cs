using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Linq;
using System;
using System.Data.Entity;

namespace FilterTracker.Models
{
	public class QuestionResponseViewModel : ModelBase
	{
		public QuestionResponseViewModel()
		{
		}

		public int QuestionId { get; set; }
		public string Response { get; set; }
	}
}