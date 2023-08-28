using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Data;
using System.Data.Entity;
using System.Linq;


namespace FilterTracker.Models
{
    public class OrganizationEditorViewModel : ModelBase
    {
        public OrganizationEditorViewModel()
        {
            using (var db = new FilterTrackerEntities())
            {
                var states = db.States.AsNoTracking().Select(s => new SelectListItem { Text = s.Name, Value = s.Abbreviation });
                StateList = states.ToList();
            }
        }
        public IEnumerable<SelectListItem> StateList { get; private set; }

        public int MaxUsers { get; set; } = 100;

        [Required()]
        public bool Active { get; set; }

        [Required()]
        [MaxLength(200)]
        public string Name { get; set; }


        [MaxLength(1000)]
        public string Description { get; set; }

        [Required()]
        [MaxLength(200)]
        [Display(Name="Address Line 1")]
        public string AddressLine1 { get; set; }

        [MaxLength(200)]
        [Display(Name="Address Line 2")]
        public string AddressLine2 { get; set; }

        [MaxLength(100)]
        [Required()]
        public string City { get; set; }

        [Required()]
        [MaxLength(2)]
        public string State { get; set; }

        [Required()]
        [MaxLength(5)]
        [MinLength(5)]
        public string Zipcode { get; set; }

        [Required()]
        [MaxLength(100)]
        [Display(Name="Contact Name")]
        public string ContactName { get; set; }

        [Required()]
        [MaxLength(200)]
        [EmailAddress]
        [Display(Name="Contact Email")]
        public string ContactEmail { get; set; }

        [Required()]
        [MaxLength(200)]
        [Phone]
        [Display(Name="Contact Phone")]
        public string ContactPhone { get; set; }

        public int OrganizationId { get; set; }

        [Display(Name="Delay Before Build Case")]
        public string FilterAge { get; set; }

        [Display(Name="Reassess days")]
        public string ReassessDays { get; set; }

        [Display(Name="Contact attempts before sending registered letter")]
        public string ContactAttemptsBeforeRegisteredLetter { get; set; }

        [Display(Name="Days Between Contact Attempts")]
        public string PatientContactDays { get; set; }

    }
}