using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Linq;
using System;
using System.Data.Entity;

namespace FilterTracker.Models
{
    public class TaskListModel : ModelBase
    {
        public TaskListModel(User user)
        {
            CurrentUser = user;
        }


        public TaskType TaskType { get; set; }

        public TaskTypes TaskTypeEnum { get; set; }

        public List<Task> Tasks { get; private set; } = new List<Task>();
    }
}