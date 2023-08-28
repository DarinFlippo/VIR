//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MIPSCalculator
{
    using System;
    using System.Collections.Generic;
    
    public partial class PatientQuestion
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PatientQuestion()
        {
            this.PatientQuestionResponses = new HashSet<PatientQuestionResponses>();
        }
    
        public int Id { get; set; }
        public int OrganizationId { get; set; }
        public int DisplayOrderIndex { get; set; }
        public string Question { get; set; }
        public System.DateTime CreateTimestamp { get; set; }
        public int CreateUserId { get; set; }
        public System.DateTime UpdateTimestamp { get; set; }
        public int UpdateUserId { get; set; }
    
        public virtual Organization Organization { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PatientQuestionResponses> PatientQuestionResponses { get; set; }
    }
}
