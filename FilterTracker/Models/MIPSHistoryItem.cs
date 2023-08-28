using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FilterTracker.Models
{
	public class MIPSHistoryItem
	{
		public MIPSHistoryItem(string date, string score)
		{
			this.Date = date;
			this.Score = score;
		}
		public string Date { get; set; }
		public string Score { get; set; }
	}
}