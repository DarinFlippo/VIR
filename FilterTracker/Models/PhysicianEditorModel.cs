using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Linq;
using System;

namespace FilterTracker.Models
{
    public class PhysicianEditorModel : ModelBase
    {
        public PhysicianEditorModel()
        {
            
        }

        public int Id { get; set; }

        [Required()]
        public int OrganizationId { get; set; }

        [Display(Name ="Name")]
        [Required()]
        [MaxLength(1000)]
        public string Name { get; set; }

        [Display( Name = "Email" )]
        [Required()]
        [MaxLength( 200 )]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Display( Name = "Phone" )]
        [Required()]
        [MaxLength( 20 )]
        [DataType( DataType.PhoneNumber )]
        public string Phone { get; set; }

        [Display(Name = "Fax")]
        [MaxLength(20)]
        [DataType(DataType.PhoneNumber)]
        public string Fax { get; set; }

        [Display(Name="Requires Approval Prior to Removal")]
        public bool RequiresRemovalApproval { get; set; }

        [Required()]
        [Display(Name="Active")]
        public bool Active { get; set; }

    }
}