using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FilterTracker.Models
{
	public class ExportOrgDataModel : ModelBase
	{
		public List<SelectListItem> Organizations { get; set; } = new List<SelectListItem>();

		public int SelectedOrganizationId { get; set; }
	}
}