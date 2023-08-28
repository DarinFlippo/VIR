using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Runtime.Caching;
using System.Web.Mvc;
using FilterTracker.Models;
using log4net;
using System.Text;
using System.Data.Entity.Core.Objects;

namespace FilterTracker.Controllers
{
	public class ControllerBase : Controller
	{
		internal FilterTrackerEntities db = new FilterTrackerEntities();

		//private ILog _ilog;
		//public ILog Log
		//{
		//    get
		//    {
		//        if (_ilog == null)
		//        {
		//            _ilog = LogManager.GetLogger("ControllerBase");
		//        }
		//        return _ilog;
		//    }
		//}

		public void LogException(Exception ex, string msg = null)
		{
			Logger.LogException(ex, msg);
		}

		public IEnumerable<string> GetModelsStateErrors(ModelStateDictionary target)
		{
			return target.Values.SelectMany(sm => sm.Errors.Select(s => s.ErrorMessage).ToList()).AsEnumerable();
		}

		public string BuildErrorListElement(IEnumerable<string> errors)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<ul class='error-list'>");
			foreach (var err in errors)
			{
				sb.AppendLine($"	<li class='error-list-item'>{err}</li>");
			}
			sb.AppendLine("</ul>");

			return sb.ToString();
		}

		internal async Task<int> GetCurrentUserIdAsync()
		{
			User u = await db.Users.AsNoTracking().SingleOrDefaultAsync(sd => sd.Email == User.Identity.Name);
			if (u != null)
				return u.Id;

			return -1;
		}

		//internal static object _lock = new object();
		private User _user;
		public User CurrentUser()
		{
			if (_user == null)
			{
				User u = db.Users.AsNoTracking().SingleOrDefault(sd => sd.Email == User.Identity.Name);
				_user = u;
			}

			return _user;
		}

		public bool CurrentUserIsInRole(string role)
		{
			User u = CurrentUser();
			var rolenames = u.UserRoles.Select(s => s.Role.Name).ToList();
			return rolenames.Contains(role) ;
		}

		internal string GetErrorMessage(DateTime? now)
		{
			var u = CurrentUser();
			if (!now.HasValue)
			{
				DateTime _now = DateTime.UtcNow;
				return $"An error has occurred.  Please contact tech support.  Your error report id is: U{u.Id}:{_now:MMddyyyy.hhmmss}.";
			}

			return $"An error has occurred.  Please contact tech support.  Your error report id is: U{u.Id}:{now.Value:MMddyyyy.hhmmss}.";
		}

		internal User GetOfficeAdmin(int organizationId)
		{
			var orgusers = db.Users.AsNoTracking().Include("UserRoles").AsNoTracking().Where(w => w.OrganizationId == organizationId).ToList();

			foreach (var orguser in orgusers)
			{
				if (orguser.UserRoles.Any(a => a.Role.Name == Roles.OrgAdmins))
					return orguser;
			}

			return null;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				db.Dispose();
			}
			base.Dispose(disposing);
		}

		private List<Physician> _physicians;

		public IEnumerable<Physician> Physicians(int organizationId)
		{
			if (_physicians == null)
			{
				if (MemoryCache.Default["Physicians"] == null)
				{
					MemoryCache.Default["Physicians"] = db.Physicians.AsNoTracking().Where(w => w.OrganizationId == organizationId).ToList();
				}

				_physicians = (List<Physician>)MemoryCache.Default["Physicians"];
			}
			return _physicians;
		}


	}

}