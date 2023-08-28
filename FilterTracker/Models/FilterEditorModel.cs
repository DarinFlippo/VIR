using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FilterTracker.Models
{
    public class FilterEditorModel : ModelBase
    {
        public int? Id { get; set; }

        [Required()]
        public int OrganizationId { get; set; }
        
        [Required()]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required()]
        [MaxLength(100)]
        public string Manufacturer { get; set; }
        
        public bool Active { get; set; }
        
        public bool Permanent { get; set; }
    }
}