using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Linq;
using System;
using System.Diagnostics.Contracts;

namespace FilterTracker.Models
{
    public class PatientListModel : ModelBase
    {
        public PatientListModel()
        {

        }

        public string SearchLastName {get;set;}
        public string SearchFirstName { get; set; }

        public string PrevSearchLastName { get; set; }
        public string PrevFirstName { get; set; }

        public string SortDirection { get; set; }

        public string SortColumn { get; set; }

        public int PageSize { get; set; }

        public int PageIndex { get; set; }

        public int PageCount { get; set; }

        public IEnumerable<Patient> Patients { get; set; }

    }
}