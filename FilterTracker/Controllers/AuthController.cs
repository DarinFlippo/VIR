using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FilterTracker;
using System.Web.Security;
using FilterTracker.Models;
using Microsoft.Ajax.Utilities;
using System.Threading;

namespace FilterTracker.Controllers
{
	public class AuthController : ControllerBase
	{
		[HttpGet]
		[Authorize]
		public ActionResult Logout()
		{
			FormsAuthentication.SignOut();
			Session.Abandon();

			LoginViewModel model = new LoginViewModel();
			return View("Login", "~/Views/Shared/_LoginLayout.cshtml", model);
		}

		[HttpGet]
		[AllowAnonymous]
		public ActionResult Login(string redirectTo = null)
		{
			LoginViewModel model = new LoginViewModel();
			if (redirectTo != null)
				model.RedirectTo = redirectTo;

			return View("Login", "~/Views/Shared/_LoginLayout.cshtml", model);
		}


		[HttpPost]
		[AllowAnonymous]
		public async Task<ActionResult> Login(LoginViewModel model)
		{
			DateTime now = DateTime.UtcNow;

			ExpandedMembershipProvider p = new ExpandedMembershipProvider();
			if (p.ValidateUser(model.Username, model.Password))
			{
				FormsAuthentication.SetAuthCookie(model.Username, false);

				await Models.User.ResetAccessFailedCount(model.Username);
				await Models.User.UpdateLoginTimestamp(model.Username, now);
			}
			else
			{
				Logger.Log.Error($"Failed login attempt - User: {model.Username}");

				await Models.User.IncrementAccessFailedCount(model.Username);
				model.ErrorMessage = "Invalid username or password.";
				
				// To make brute force attacks harder, sleep 5 seconds before returning.
				Thread.Sleep(5000);
				return View("Login", "~/Views/Shared/_LoginLayout.cshtml", model);
			}

			//if (!string.IsNullOrEmpty(model.RedirectTo))
			//{
			//    ///TODO:  this needs atttention
			//    var components = model.RedirectTo.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			//    return RedirectToAction(components[1], components[0]);
			//}
			var user = db.Users.SingleOrDefault(sd => sd.Email == model.Username);

			db.LoginHistories.Add(new LoginHistory()
			{
				Timestamp = now,
				UserId = user.Id,
			});
			db.SaveChanges();

			var roles = user.UserRoles.Select(s => s.Role.Name).ToList();

			ExpandedRoleProvider rp = new ExpandedRoleProvider();

			if (roles.Contains(Models.Roles.Physician))
			{
				//if (db.MIPSHistories.Count(c => c.UserId == user.Id && c.Date == now.Date) == 0)
				//{
				//	var mips = CalculateMIPS(user.Id, new DateTime(now.Year, 1, 1), now);
				//	db.MIPSHistories.Add(new MIPSHistory { Date = now.Date, MIPS = mips, UserId = user.Id });
				//	db.SaveChanges();
				//}
				return RedirectToAction("Dashboard", "Home");
			}
			else if (roles.Contains(Models.Roles.OrgAdmins))
			{
				return RedirectToAction("Dashboard", "Home");
			}
			else if (roles.Contains(Models.Roles.Users))
			{
				return RedirectToAction("Dashboard", "Home");
			}
			else if (roles.Contains(Models.Roles.SuperUsers))
			{
				return RedirectToAction("Index", "Admin");
			}
			else
			{
				return RedirectToAction("PatientList", "Home");
			}

		}

		[HttpGet]
		[Authorize]
		public ActionResult ChangePassword()
		{
			var model = new ChangePasswordViewModel();

			return View("ChangePassword", "~/Views/Shared/_Layout.cshtml", model);
		}

		[HttpPost]
		[Authorize]
		public ActionResult ChangePassword(ChangePasswordViewModel model)
		{
			var cu = CurrentUser();

			if (cu != null && cu.Id > 0)
			{
				if (model != null)
				{
					var current_hash = new PasswordHash(model.CurrentPassword);
					byte[] current_password = current_hash.ToArray();

					ExpandedMembershipProvider prov = new ExpandedMembershipProvider();
					if (prov.ValidateUser(cu.Email, model.CurrentPassword))
					{
						if (!string.IsNullOrEmpty(model.NewPassword) && !string.IsNullOrEmpty(model.RepeatedNewPassword))
						{
							if (model.NewPassword == model.RepeatedNewPassword)
							{
								var new_password_hash = new PasswordHash(model.NewPassword);
								byte[] new_password = new_password_hash.ToArray();

								var u = db.Users.SingleOrDefault(sd => sd.Id == cu.Id);
								if (u != null && u.Id == cu.Id)
								{
									u.PasswordHash = new_password;
									db.SaveChanges();

									return Json(new { Success = "true", ErrorMessage = "" });
								}
								else
								{
									model.ErrorMessage = "Unable to change your password at this time.  Please contact technical support.";
								}
							}
							else
							{
								model.ErrorMessage = "New passwords do not match.";
							}
						}
						else
						{
							model.ErrorMessage = "You must provide a new password to change your password.";
						}
					}
					else
					{
						model.ErrorMessage = "The password you provided is incorrect.";
					}
				}
				else
				{
					model.ErrorMessage = "No passwords were provided.";
				}
			}
			else
			{
				model.ErrorMessage = "Unable to change your password at this time.  Please contact technical support.";
			}


			return Json(new { Success = "false", model.ErrorMessage });
		}

		[HttpPost]
		[Authorize(Roles = ("SuperUsers"))]
		public async Task<ActionResult> UpdateOrganization(Organization organization)
		{
			return View();
		}

		[HttpPost]
		[Authorize(Roles = ("SuperUsers"))]
		public async Task<ActionResult> DeactivateOrganization(int organizationId)
		{
			return View();
		}

		[HttpPost]
		[Authorize(Roles = ("SuperUsers"))]
		public async Task<ActionResult> ActivateOrganization(int organizationId)
		{
			return View();
		}

	}
}
