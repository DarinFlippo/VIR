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
    
    public partial class ContactResultCode
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ContactResultCode()
        {
            this.PatientContactAttempts = new HashSet<PatientContactAttempt>();
            this.PhysicianContactAttempts = new HashSet<PhysicianContactAttempt>();
        }
    
        public int Id { get; set; }
        public int OrganizationId { get; set; }
        public string ResultCode { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public System.DateTime CreateTimestamp { get; set; }
        public int CreateUserId { get; set; }
        public System.DateTime UpdateTimestamp { get; set; }
        public int UpdateUserId { get; set; }
    
        public virtual Organization Organization { get; set; }
        public virtual User User { get; set; }
        public virtual User User1 { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PatientContactAttempt> PatientContactAttempts { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PhysicianContactAttempt> PhysicianContactAttempts { get; set; }
    }
}
