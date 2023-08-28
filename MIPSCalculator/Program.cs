

using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace MIPSCalculator
{
	class Program
	{
		static void Main(string[] args)
		{
			try
			{
				Logger.Log.Info("MIPSCalculator beginning.");
				Console.WriteLine("MIPSCalculator beginning.");
				Begin();
				Logger.Log.Info("MIPSCalculator ending.");
				Console.WriteLine("MIPSCalculator ending.");
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
				Logger.LogException(ex);
			}
		}




		private static void Begin()
		{
			DateTime now = DateTime.UtcNow;
			DateTime start = new DateTime(now.Year, 1, 1);
			DateTime end = new DateTime(now.Year, 9, 30);

			int count = 0;


			using (var db = new FilterTrackerEntities())
			{
				int role_id = db.Roles.Single(w => w.Name == Roles.Physician).Id;

				var clinic_physician_ids = db.UserRoles.AsNoTracking()
					.Where(w => w.RoleId == role_id)
					.Select(s => s.User.Id).ToList();

				count = clinic_physician_ids.Count();


				foreach (int userid in clinic_physician_ids)
				{
					try
					{
						var score = CalculateMIPS(userid, start, end, db);
						db.MIPSHistories.Add(new MIPSHistory() { Date = now.Date, MIPS = score, UserId = userid });
						db.SaveChanges();
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
						Logger.LogException(ex);
					}
				}
			}

			Logger.Log.Info($"MIPS Calculated for {count} Physicians.");

			Console.WriteLine($"MIPS Calculated for {count} Physicians.");
		}

		private static decimal CalculateMIPS(int userid, DateTime start, DateTime end, FilterTrackerEntities db)
		{
			var now = DateTime.UtcNow;

			decimal returned = 1.0M;
			int numerator = 0;
			int denominator = 0;

			try
			{
				var filters = db.PatientFilters
					.Where(w => w.ProcedureDate.HasValue)
					.Where(w => w.ProcedureDate >= start && w.ProcedureDate <= end)
					//.Where(w => w.ProcedureDate.Value.AddMonths(3) <= now)
					.Where(w => w.ProcedurePhysicianId == userid)
					.Where(w => w.IsTemporary == true)
					.Select(s => new
					{
						s.ProcedureDate,
						s.Id,
						s.ActualRemovalDate,
						s.MadePermanent,
						s.PatientContactAttempts,
						s.Patient
					}).ToList();

				foreach (var filter in filters)
				{
					DateTime cutoff = filter.ProcedureDate.Value.AddMonths(3);
					if (filter.Patient.Active && cutoff.Date >= now)
						denominator++;

					if (filter.ActualRemovalDate.HasValue)
					{
						if (filter.ActualRemovalDate.Value <= cutoff)
						{
							numerator++;
							continue;
						}
					}

					int contact_attempts = filter
						.PatientContactAttempts
						.Count(c => c.Timestamp <= cutoff);

					if (contact_attempts >= 2)
					{
						numerator++;
						continue;
					}


					bool have_reassessment = false;
					have_reassessment = db.Tasks.Any(w => w.PatientFilterId == filter.Id && w.TaskTypeId == (int)TaskTypes.ReviewCase && w.ClosedDate.HasValue);
					if (have_reassessment)
					{
						numerator++;
						continue;
					}
				}

				if (denominator > 0)
					returned = (decimal)numerator / (decimal)denominator;
			}
			catch (Exception ex)
			{
				Logger.LogException(ex);
				Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
			}

			return returned;
		}

	}
}
