using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FilterTracker.Models
{
    public class ResetPasswordViewModel : ModelBase
    {   
        [Required()]
        [MaxLength(20)]
        public string Password { get; set; }

        [Display(Name ="Organization")]
        public int SelectedOrganizationId { get; set; }

        [Display(Name ="User")]
        public int SelectedUserId { get; set; }

        public string Message { get; set; }

        public List<SelectListItem> Organizations { get; set; }

        public List<SelectListItem> Users { get; set; } 

        public ResetPasswordViewModel() { }

        public ResetPasswordViewModel(string username, string role)
        {
            try
            {
                using (var db = new FilterTrackerEntities())
                {
                    if (role == "SuperUsers")
                    {
                        Users = db.Users.AsNoTracking().Where(w => w.Active).Select(s => new SelectListItem() { Value = s.Id.ToString(), Text = s.Email }).ToList();
                        Organizations = db.Organizations.AsNoTracking().Where(w => w.Active).Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() }).ToList();
                    }
                    else
                    {
                        // OrganizationAdmins - only sees their users and organization
                        var org = db.Users.AsNoTracking().Single(s => s.Email == username)?.Organization;
                        Users = db.Users.AsNoTracking().Where(w => w.Active && w.OrganizationId == org.Id).Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Email }).ToList();
                        Organizations.Add(new SelectListItem { Text = org.Name, Value = org.Id.ToString() });
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.LogException(ex);
            }
        }
    }
}