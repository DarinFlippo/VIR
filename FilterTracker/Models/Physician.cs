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
    
    public partial class Physician
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Physician()
        {
            this.PatientFilters = new HashSet<PatientFilter>();
            this.PatientFilters1 = new HashSet<PatientFilter>();
        }
    
        public int Id { get; set; }
        public int OrganizationId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public bool RequiresRemovalApproval { get; set; }
        public bool Active { get; set; }
        public System.DateTime CreateTimestamp { get; set; }
        public int CreateUserId { get; set; }
        public System.DateTime UpdateTimestamp { get; set; }
        public int UpdateUserId { get; set; }
        public string Fax { get; set; }
    
        public virtual Organization Organization { get; set; }
        public virtual User User { get; set; }
        public virtual User User1 { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PatientFilter> PatientFilters { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PatientFilter> PatientFilters1 { get; set; }
    }
}