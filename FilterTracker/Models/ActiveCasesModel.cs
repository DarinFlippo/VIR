using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Linq;
using System;
using System.Data.Entity;

namespace FilterTracker.Models
{
    public class ActiveCasesModel : ModelBase
    {
        public ActiveCasesModel()
        {
        }


        public List<Patient> Patients { get; private set; } = new List<Patient>();
    }
}