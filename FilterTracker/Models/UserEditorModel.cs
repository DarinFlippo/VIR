using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FilterTracker.Models
{
    public class UserEditorModel : UserModel
    {

        public UserEditorModel()
        {
            using ( var db = new FilterTrackerEntities() )
            {
                var roles = db.Roles.ToList();
                foreach ( var role in roles )
                {
                    var added = new SelectListItem() { Text = role.Name, Value = role.Id.ToString() };
                    Roles.Add( added );
                }

                var orgs = db.Organizations.Where(w => w.Active == true).ToList();
                foreach ( var org in orgs )
                {
                    var added = new SelectListItem() { Text = org.Name, Value = org.Id.ToString() };
                    Organizations.Add( added );
                }

            }
        }

    }

    public class CreateUserModel : UserModel
    {

        public CreateUserModel()
        {
            using ( var db = new FilterTrackerEntities() )
            {
                var roles = db.Roles.ToList();
                foreach ( var role in roles )
                {
                    var added = new SelectListItem() { Text = role.Name, Value = role.Id.ToString() };
                    Roles.Add( added );
                }

                var orgs = db.Organizations.ToList();
                foreach ( var org in orgs )
                {
                    var added = new SelectListItem() { Text = org.Name, Value = org.Id.ToString() };
                    Organizations.Add( added );
                }

            }
        }

        [RegularExpression(@"((?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[!@#$%^&*()_+-=~]).{8,40})", ErrorMessage="8-20 characters, min 1 uppercase, min 1 number, min 1 special.")]
        [Required()]
        public string Password { get; set; }
    }

    public class UserModel : ModelBase
    {
        public string Message { get; set; }

        [MaxLength( 250 )]
        [EmailAddress]
        [Required()]
        public string Email { get; set; }

        public int Id { get; set; }

        public bool Active { get; set; }

        [Required( ErrorMessage = "Please select an organization." )]
        public int OrganizationId { get; set; }

        [MinLength( 1 )]
        [Display( Name = "Selected Roles" )]
        [Required( ErrorMessage = "Please select 1 or more roles." )]
        public string[] SelectedRoles { get; set; } = new string[ 3 ];

        public bool DefaultReviewer { get; set; }


        [MaxLength( 50 )]
        [Required()]
        public string FirstName { get; set; }

        [MaxLength( 50 )]
        [Required()]
        public string LastName { get; set; }

        public List<SelectListItem> Roles { get; } = new List<SelectListItem>();

        public List<SelectListItem> Organizations { get; } = new List<SelectListItem>();

    }
}