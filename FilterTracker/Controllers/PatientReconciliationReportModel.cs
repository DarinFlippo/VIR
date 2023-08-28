using FilterTracker.Models;

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;

namespace FilterTracker.Controllers
{
	public class PatientReconciliationReportModel
	{
		public PatientReconciliationReportModel()
		{
		}
		/*
		 Here are the counts to collect and the naming for 
			the counters that should make the report easy to follow. 
			-Total number of patients in database = Patients in 
				database (this is the number we would reconcile back to)
			-Filter made permanent  = counts patients with a made 
				permanent date populated (not on dashboard)
			--Sent registered letter = counts all patients that have a 
				sent registered letter date populated  (not on dashboard)
			--Filters removed = counts all patients with a filter removed 
				date populated (not on dashboard)
			--Patient build case pending = counts patients 
				where a filter was placed, but we’re waiting for the date 
				timer to pop to the let build case appear (hidden)
			--SKIP - covered by build case pending --Reassess pending  = counts any case where the Reassess was 
				marked by doc but the task is still hidden until time 
				to contact the patient again (hidden)

			--Build Case = counts patients with open Build Case tasks (on dashboard)
			--Review Case = counts patients with open Review Case tasks (on dashboard)
			--Contact PCP = counts patients with open Contact PCP tasks (on dashboard)
			--Schedule retrieval = counts patients with open Schedule Retrieval tasks (on dashboard)
			--Retrieval data passed = counts patients with open
			--	Retrieval Date passed tasks where patient doesn’t 
			--  have another open task (on dashboard).
			--Patients to research = Total number of patients in database minus sum of counts 2 thru 11 above
 
			Final recon would be:

			Patients in database – sum (all patients in the other buckets + patients to research) = 0

			If this doesn’t equal zero the reconciliation is out of balance
		 */
		public PatientReconciliationReportModel(int organizationId, FilterTrackerEntities db)
		{
			if (organizationId == 0)
				return;

			DateTime now = DateTime.UtcNow;

			try
			{
				var all_patient_filters = db.PatientFilters.Where(w => w.OrganizationId == organizationId);

				TotalPatientFilters = 0;
				FilterMadePermanentCount = 0;
				SendRegisteredLetterClosedTaskCount = 0;
				SendRegisteredLetterOpenTaskCount = 0;
				FiltersRemovedCount = 0;
				BuildCasePendingCount = 0;
				BuildCaseOpenCount = 0;
				ReviewCaseOpenCount = 0;
				ContactPCPOpenCount = 0;
				ScheduleRetrievalOpenCount = 0;
				RetrievalDatePassedTaskCount = 0;
				DeceasedCount = 0;
				ScheduleRetrievalPendingCount = 0;

				bool accountedFor = false;
				foreach (PatientFilter pf in all_patient_filters)
				{
					TotalPatientFilters++;

					if (pf.MadePermanent.HasValue)
					{
						FilterMadePermanentCount++;
						accountedFor = true;
						//continue;
					}

					if (pf.Patient.DeceasedDate.HasValue)
					{
						DeceasedCount++;
						accountedFor = true;
						//continue;
					}

					if (pf.ActualRemovalDate.HasValue)
					{
						FiltersRemovedCount++;
						accountedFor = true;
						//continue;
					}

					// try incrementing by the count instead of just 1 to account for multiple tasks per pf
					int bucket = pf.Tasks.Count(c => c.TaskTypeId == (int)TaskTypes.SendRegisteredLetters && c.ClosedDate.HasValue);
					if (bucket > 0)
					{
						SendRegisteredLetterClosedTaskCount += bucket;
						accountedFor = true;
						//continue;
					}

					bucket = pf.Tasks.Count(c => c.TaskTypeId == (int)TaskTypes.BuildCase
						&& c.ClosedDate.HasValue == false
						&& c.HideUntil.HasValue == true
						&& c.HideUntil.Value > now);

					if (bucket > 0)
					{
						BuildCasePendingCount += bucket;
						accountedFor = true;
						//continue;
					}

					bucket = pf.Tasks.Count(c =>
						c.TaskTypeId == (int)TaskTypes.ScheduleRetrieval
						&& c.ClosedDate.HasValue == false
						&& c.HideUntil.HasValue == true
						&& c.HideUntil.Value > now);

					if (bucket > 0)
					{
						ScheduleRetrievalPendingCount += bucket;
						accountedFor = true;
						//continue;
					}

					bucket = pf.Tasks.Count(c => c.TaskTypeId == (int)TaskTypes.SendRegisteredLetters && c.ClosedDate.HasValue == false);
					if (bucket > 0)
					{
						SendRegisteredLetterOpenTaskCount += bucket;
						accountedFor = true;
					}

					bucket = pf.Tasks.Count(c =>
					c.TaskTypeId == (int)TaskTypes.BuildCase
					&& c.ClosedDate.HasValue == false
					&& (c.HideUntil.HasValue == false || (c.HideUntil.HasValue && c.HideUntil.Value <= now)));

					if (bucket > 0)
					{
						BuildCaseOpenCount += bucket;
						accountedFor = true;
					}

					bucket = pf.Tasks.Count(c =>
						c.TaskTypeId == (int)TaskTypes.ReviewCase
						&& c.ClosedDate.HasValue == false);

					if (bucket > 0)
					{
						ReviewCaseOpenCount += bucket;
						accountedFor = true;
					}

					bucket = pf.Tasks.Count(c =>
						c.TaskTypeId == (int)TaskTypes.ContactPCP
						&& c.ClosedDate.HasValue == false);

					if (bucket > 0)
					{
						ContactPCPOpenCount += bucket;
						accountedFor = true;
					}

					bucket = pf.Tasks.Count(c =>
						c.TaskTypeId == (int)TaskTypes.ScheduleRetrieval
						&& c.ClosedDate.HasValue == false);

					if (bucket > 0)
					{
						ScheduleRetrievalOpenCount += bucket;
						accountedFor = true;
					}

					var retreival_date_passed_tasks = pf.Tasks.Where(w => w.TaskTypeId == (int)TaskTypes.RetrievalDatePassed);
					foreach (var t in retreival_date_passed_tasks)
					{
						if (t.ClosedDate.HasValue == false)
						{
							var others = pf.Tasks.Except(retreival_date_passed_tasks);
							if (!others.Any(a => a.ClosedDate.HasValue == false))
							{
								RetrievalDatePassedTaskCount++;
								accountedFor = true;
							}
						}
					}


					if (!accountedFor)
					{
						Ghosts.Add(pf.Patient);
						GhostCount++;
					}

					accountedFor = false;
				}

				//TotalPatientFilters = db.PatientFilters.Count(w => w.OrganizationId == organizationId);

				//FilterMadePermanentCount = all_patient_filters
				//	.Where(w => w.MadePermanent.HasValue)
				//	.Count();

				//var ghosts = db.PatientFilters
				//.Where(w => w.OrganizationId == organizationId)
				//.Where(w => w.MadePermanent.HasValue == false);
				//var ghosts = db.PatientFilters
				//.Where(w => w.OrganizationId == organizationId)
				//.Where(w => w.ActualRemovalDate.HasValue == false)
				//.Where(w => w.Patient.DeceasedDate.HasValue == false)
				//.Where(w => w.MadePermanent.HasValue == false)
				//.Where(w => w.Tasks.Count(c => c.ClosedDate.HasValue == false) == 0);

				//GhostCount = ghosts.Count();

				//Ghosts = ghosts.Select(s => s.Patient).ToList();
				

				//SendRegisteredLetterClosedTaskCount = db.Tasks
				//	.Where(w => w.OrganizationId == organizationId)
				//	.Where(w => w.TaskTypeId == (int)TaskTypes.SendRegisteredLetters)
				//	.Where(w => w.ClosedDate.HasValue == true)
				//	.Count();



				//SendRegisteredLetterOpenTaskCount = db.Tasks
				//	.Where(w => w.OrganizationId == organizationId)
				//	.Where(w => w.TaskTypeId == (int)TaskTypes.SendRegisteredLetters)
				//	.Where(w => w.ClosedDate.HasValue == false)
				//	.Count();

				//FiltersRemovedCount = db.PatientFilters
				//	.Where(w => w.OrganizationId == organizationId)
				//	.Where(w => w.ActualRemovalDate.HasValue == true)
				//	.Count();

				//BuildCasePendingCount = db.Tasks
				//	.Where(w => w.OrganizationId == organizationId)
				//	.Where(w => w.TaskTypeId == (int)TaskTypes.BuildCase)
				//	.Where(w => w.ClosedDate.HasValue == false)
				//	.Where(w => w.HideUntil.HasValue == true && w.HideUntil.Value > now)
				//	.Count();

				//BuildCaseOpenCount = db.Tasks
				//	.Where(w => w.OrganizationId == organizationId)
				//	.Where(w => w.TaskTypeId == (int)TaskTypes.BuildCase)
				//	.Where(w => w.ClosedDate.HasValue == false)
				//	.Where(w => w.HideUntil.HasValue == false || (w.HideUntil.HasValue && w.HideUntil.Value < now))
				//	.Count();

				//ReviewCaseOpenCount = db.Tasks
				//	.Where(w => w.OrganizationId == organizationId)
				//	.Where(w => w.TaskTypeId == (int)TaskTypes.ReviewCase)
				//	.Where(w => w.ClosedDate.HasValue == false)
				//	.Count();



				//ReviewCaseOpenCount = db.Tasks
				//	.Where(w => w.OrganizationId == organizationId)
				//	.Where(w => w.TaskTypeId == (int)TaskTypes.ReviewCase)
				//	.Where(w => w.ClosedDate.HasValue == false)
				//	.Count();

				//ContactPCPOpenCount = db.Tasks
				//	.Where(w => w.OrganizationId == organizationId)
				//	.Where(w => w.TaskTypeId == (int)TaskTypes.ContactPCP)
				//	.Where(w => w.ClosedDate.HasValue == false)
				//	.Count();

				//ScheduleRetrievalOpenCount = db.Tasks
				//	.Where(w => w.OrganizationId == organizationId)
				//	.Where(w => w.TaskTypeId == (int)TaskTypes.ScheduleRetrieval)
				//	.Where(w => w.ClosedDate.HasValue == false)
				//	.Count();

				//var RetrievalDatePassedTasks = db.Tasks
				//	.Where(w => w.OrganizationId == organizationId)
				//	.Where(w => w.TaskTypeId == (int)TaskTypes.RetrievalDatePassed)
				//	.Where(w => w.ClosedDate.HasValue == false);

				//RetrievalDatePassedTaskCount = RetrievalDatePassedTasks.Count();

				//foreach (Task t in RetrievalDatePassedTasks)
				//{
				//	var pf = t.PatientFilter;
				//	foreach (var task in pf.Tasks.Where(w => w.ClosedDate.HasValue == false))
				//	{
				//		if (task.Id != t.Id)
				//		{
				//			if (!task.ClosedDate.HasValue)
				//			{
				//				if (RetrievalDatePassedTaskCount > 0)
				//					RetrievalDatePassedTaskCount--;
				//			}
				//		}
				//	}
				//}
			}
			catch (Exception ex)
			{
				Logger.LogException(ex);
				ErrorMessage = "Unable to loat Patient Reconciliation Report at this time.  Please contact technical support.";
			}
		}

		public int GhostCount { get; set; }

		public string ErrorMessage { get; set; }

		public int TotalPatientFilters { get; set; }

		public int DeceasedCount { get; set; }

		//public int ActivePatients { get; set; }

		//public int InactivePatients { get; set; }

		public int FilterMadePermanentCount { get; set; }

		public int FiltersRemovedCount { get; set; }

		public int BuildCasePendingCount { get; set; }

		public int BuildCaseOpenCount { get; set; }

		public int ScheduleRetrievalPendingCount { get; set; }

		public int ReviewCaseOpenCount { get; set; }

		public int ContactPCPOpenCount { get; set; }

		public int ScheduleRetrievalOpenCount { get; set; }

		public int SendRegisteredLetterClosedTaskCount { get; set; }

		public int SendRegisteredLetterOpenTaskCount { get; set; }

		public int RetrievalDatePassedTaskCount { get; set; }

		public int ActivePatientCount { get; set; }

		public int PatientsToResearch { get { return Ghosts.Count; } }

		public List<Patient> Ghosts { get; set; } = new List<Patient>();
	}

}