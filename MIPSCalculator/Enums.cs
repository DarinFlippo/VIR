using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MIPSCalculator
{
    public enum TaskTypes
    {
        RetrievalDatePassed = 1,
        SendRegisteredLetters = 2,
        ReviewPCPPreferences = 3,
        ScheduleRetrieval = 4,
        ReviewCase = 5,
        BuildCase = 6,
        ContactPCP = 7,
        PatientContactDue = 8
    }

    public static class Roles
    {
        public static readonly string Users = "Users";
        public static readonly string OrgAdmins = "OrganizationAdmins";
        public static readonly string SuperUsers = "SuperUsers";
        public static readonly string Physician = "Physician";
    }
}