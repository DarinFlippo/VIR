using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FilterTracker.Models
{
    public class ExportPatient
    {
        public int Id { get; set; }
        public int OrganizationId { get; set; }
        public string MRN { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public System.DateTime DateOfBirth { get; set; }
        public Nullable<int> Gender { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zipcode { get; set; }
        public string PrimaryPhoneNumber { get; set; }
        public string SecondaryPhoneNumber { get; set; }
        public string PrimaryEmailAddress { get; set; }
        public string SecondaryEmailAddress { get; set; }
        public bool Active { get; set; }
        public System.DateTime CreateTimestamp { get; set; }
        public int CreateUserId { get; set; }
        public System.DateTime UpdateTimestamp { get; set; }
        public int UpdateUserId { get; set; }
    }
}