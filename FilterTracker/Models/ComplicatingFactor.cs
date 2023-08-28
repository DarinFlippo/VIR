//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FilterTracker.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class ComplicatingFactor
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ComplicatingFactor()
        {
            this.PatientFilters = new HashSet<PatientFilter>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public System.DateTime CreateTimestamp { get; set; }
        public int CreateUserId { get; set; }
        public System.DateTime UpdateTimestamp { get; set; }
        public int UpdateUserId { get; set; }
        public int OrganizationId { get; set; }
    
        public virtual User CreateUser { get; set; }
        public virtual User UpdateUser { get; set; }
        public virtual Organization Organization { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PatientFilter> PatientFilters { get; set; }
    }
}
