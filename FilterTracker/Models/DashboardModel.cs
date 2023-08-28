using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Linq;
using System;
using System.Data.Entity;

namespace FilterTracker.Models
{
    public class DashboardModel : ModelBase
    {
        public TaskListModel RetrievalDatePassedTaskList { get; set; } 

        public TaskListModel ReviewPCPPreferencesTaskList { get; set; }

        public TaskListModel SendRegisteredLettersTaskList { get; set; }
        public TaskListModel BuildCaseTaskList { get; set; }
        public TaskListModel ScheduleRetrievalTaskList { get; set; }
       // public TaskListModel PatientContactAttemptDueTaskList { get; set; } = new TaskListModel();

        public TaskListModel ReviewCaseTaskList { get; set; }
        public TaskListModel ContactPCPTaskList { get; set; }

        public List<SelectListItem> ClinicPhysicians {get;set;} = new List<SelectListItem>();

        public List<QuickNote> QuickNotes { get; set; } = new List<QuickNote>();

        public DashboardModel(User user)
        {
            this.CurrentUser = user;
            RetrievalDatePassedTaskList = new TaskListModel(user);
            ReviewPCPPreferencesTaskList = new TaskListModel(user);
            SendRegisteredLettersTaskList = new TaskListModel(user);
            BuildCaseTaskList = new TaskListModel(user);
            ScheduleRetrievalTaskList = new TaskListModel(user);
            ReviewCaseTaskList = new TaskListModel(user);
            ContactPCPTaskList = new TaskListModel(user);
        }


    }
}