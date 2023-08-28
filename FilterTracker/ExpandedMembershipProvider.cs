using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Data.Entity;
using System.Web;
using System.Web.Security;
using FilterTracker.Models;

namespace FilterTracker
{
	public class ExpandedMembershipProvider : MembershipProvider
	{
		public override bool EnablePasswordRetrieval => throw new NotImplementedException();

		public override bool EnablePasswordReset => throw new NotImplementedException();

		public override bool RequiresQuestionAndAnswer => throw new NotImplementedException();

		public override string ApplicationName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public override int MaxInvalidPasswordAttempts => throw new NotImplementedException();

		public override int PasswordAttemptWindow => throw new NotImplementedException();

		public override bool RequiresUniqueEmail => throw new NotImplementedException();

		public override MembershipPasswordFormat PasswordFormat => throw new NotImplementedException();

		public override int MinRequiredPasswordLength => throw new NotImplementedException();

		public override int MinRequiredNonAlphanumericCharacters => throw new NotImplementedException();

		public override string PasswordStrengthRegularExpression => throw new NotImplementedException();

		public override bool ChangePassword( string username, string oldPassword, string newPassword )
		{
			throw new NotImplementedException();
		}

		public override bool ChangePasswordQuestionAndAnswer( string username, string password, string newPasswordQuestion, string newPasswordAnswer )
		{
			throw new NotImplementedException();
		}

		public override MembershipUser CreateUser( string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status )
		{
			throw new NotImplementedException();
		}

		public override bool DeleteUser( string username, bool deleteAllRelatedData )
		{
			throw new NotImplementedException();
		}

		public override MembershipUserCollection FindUsersByEmail( string emailToMatch, int pageIndex, int pageSize, out int totalRecords )
		{
			throw new NotImplementedException();
		}

		public override MembershipUserCollection FindUsersByName( string usernameToMatch, int pageIndex, int pageSize, out int totalRecords )
		{
			throw new NotImplementedException();
		}

		public override MembershipUserCollection GetAllUsers( int pageIndex, int pageSize, out int totalRecords )
		{
			throw new NotImplementedException();
		}

		public override int GetNumberOfUsersOnline()
		{
			throw new NotImplementedException();
		}

		public override string GetPassword( string username, string answer )
		{
			throw new NotImplementedException();
		}

		public override MembershipUser GetUser( object providerUserKey, bool userIsOnline )
		{
			throw new NotImplementedException();
		}

		public override MembershipUser GetUser( string username, bool userIsOnline )
		{
			throw new NotImplementedException();
		}

		public override string GetUserNameByEmail( string email )
		{
			throw new NotImplementedException();
		}

		public override string ResetPassword( string username, string answer )
		{
			throw new NotImplementedException();
		}

		public override bool UnlockUser( string userName )
		{
			throw new NotImplementedException();
		}

		public override void UpdateUser( MembershipUser user )
		{
			throw new NotImplementedException();
		}

		public override bool ValidateUser( string username, string password )
		{
			byte[] hashBytes = null;
			using ( var db = new Models.FilterTrackerEntities() )
			{
				var entity = db.Users.SingleOrDefault( s => s.Email == username && s.Active == true && s.LockoutEnabled == false );
				if ( entity != null && entity.Id > 0 )
				{
					hashBytes = entity.PasswordHash;
				}

			}
			if ( hashBytes != null && hashBytes.Length > 0 )
			{
				PasswordHash hash = new PasswordHash( hashBytes );
				return hash.Verify( password );
			}
			return false;
		}
	}

	public class ExpandedRoleProvider : RoleProvider
	{
		public override string ApplicationName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public override void AddUsersToRoles( string[] usernames, string[] roleNames )
		{

			using ( var db = new FilterTrackerEntities() )
			{
				Dictionary<string, Tuple<int, string>> roles = db.Roles.AsNoTracking()
					.Select(s => new Tuple<int, string>(s.Id, s.Name))
					.ToDictionary( d => d.Item2 );

				foreach ( var username in usernames )
				{
					Models.User u = db.Users.SingleOrDefault( sd => sd.Email == username );
					if ( u != null && u.Id > 0 )
					{
						foreach ( var rolename in roleNames )
						{
							if ( roles.ContainsKey( rolename ) )
							{
								UserRole added = new UserRole();
								added.UserId = u.Id;
								added.RoleId = roles[ rolename ].Item1;

								string logged_in_username = HttpContext.Current.User.Identity.Name;
								User logged_in_user = db.Users.SingleOrDefault( sd => sd.Email == logged_in_username );
								if (logged_in_user != null && logged_in_user.Id > 0)
								{
									added.CreateUserId = logged_in_user.Id;
									added.UpdateUserId = logged_in_user.Id;
								}

								added.CreateTimestamp = DateTime.UtcNow;
								added.UpdateTimestamp = added.CreateTimestamp;

								db.UserRoles.Add( added );
							}
						}
					}
				}
				db.SaveChanges();
			}
		}

		public override void CreateRole( string roleName )
		{
			throw new NotImplementedException();
		}

		public override bool DeleteRole( string roleName, bool throwOnPopulatedRole )
		{
			throw new NotImplementedException();
		}

		public override string[] FindUsersInRole( string roleName, string usernameToMatch )
		{
			List<string> returned;

			using ( var db = new Models.FilterTrackerEntities() )
			{
				var role_entity = db.Roles.Single( s => s.Name == roleName );

				if ( !string.IsNullOrEmpty( usernameToMatch ) )
				{
					var role_users = role_entity.UserRoles.Select( s => s.User ); // need to be able to limit this to 'active' users

					returned = role_users.Where( w => w.Email == usernameToMatch ).Select( s => s.Email ).ToList();
				}
				else
				{
					var role_users = role_entity.UserRoles.Select( s => s.User );
					returned = role_users.Select( s => s.Email ).ToList();
				}
			}

			return returned.ToArray();
		}

		public override string[] GetAllRoles()
		{
			List<string> returned = new List<string>();
			using ( var db = new Models.FilterTrackerEntities() )
			{
				returned.AddRange( db.Roles.Select( s => s.Name ).ToList() );
			}

			return returned.ToArray();
		}

		public override string[] GetRolesForUser( string username )
		{
			List<string> returned = new List<string>();
			using ( var db = new Models.FilterTrackerEntities() )
			{
				var user_entity = db.Users.SingleOrDefault( w => w.Email == username );
				if ( user_entity != null && user_entity.Id > 0 )
				{
					var roles = user_entity.UserRoles.Select( s => s.Role.Name ).ToList();
					returned.AddRange( roles );
				}
			}

			return returned.ToArray();
		}

		public override string[] GetUsersInRole( string roleName )
		{
			List<string> returned = new List<string>();

			using ( var db = new Models.FilterTrackerEntities() )
			{
				var role_entity = db.Roles.AsNoTracking().SingleOrDefault( sd => sd.Name == roleName );
				if ( role_entity != null && role_entity.Id > 0 )
				{
					var users = role_entity.UserRoles.Select( s => s.User.Email ).ToList();
					returned.AddRange( users );
				}
			}

			return returned.ToArray();
		}

		public override bool IsUserInRole( string username, string roleName )
		{
			bool returned = false;

			using ( var db = new Models.FilterTrackerEntities() )
			{
				var user = db.Users.SingleOrDefault( sd => sd.Email == username );
				if ( user != null && user.Id > 0 )
				{
					if ( user.UserRoles.Count( c => c.Role.Name == roleName ) >= 1 )
					{
						returned = true;
					}
				}
			}

			return returned;
		}

		public override void RemoveUsersFromRoles( string[] usernames, string[] roleNames )
		{
			using ( var db = new Models.FilterTrackerEntities() )
			{
				foreach ( string user in usernames )
				{
					Models.User u = db.Users.Include( i => i.UserRoles ).SingleOrDefault( sd => sd.Email == user );
					if ( u != null && u.Email == user )
					{
						foreach ( UserRole ur in u.UserRoles )
						{
							if ( roleNames.Contains( ur.Role.Name ) )
							{
								db.UserRoles.Remove( ur );
							}
						}
					}
				}

				db.SaveChanges();
			}
		}

		public override bool RoleExists( string roleName )
		{
			bool returned = false;

			using ( var db = new Models.FilterTrackerEntities() )
			{
				returned = db.Roles.Count( c => c.Name == roleName ) == 1;
			}

			return returned;
		}
	}

	public sealed class PasswordHash
	{
		const int SaltSize = 16, HashSize = 20, HashIter = 10000;
		readonly byte[] _salt, _hash;
		public PasswordHash( string password )
		{
			new RNGCryptoServiceProvider().GetBytes( _salt = new byte[ SaltSize ] );
			_hash = new Rfc2898DeriveBytes( password, _salt, HashIter ).GetBytes( HashSize );
		}
		public PasswordHash( byte[] hashBytes )
		{
			Array.Copy( hashBytes, 0, _salt = new byte[ SaltSize ], 0, SaltSize );
			Array.Copy( hashBytes, SaltSize, _hash = new byte[ HashSize ], 0, HashSize );
		}
		public PasswordHash( byte[] salt, byte[] hash )
		{
			Array.Copy( salt, 0, _salt = new byte[ SaltSize ], 0, SaltSize );
			Array.Copy( hash, 0, _hash = new byte[ HashSize ], 0, HashSize );
		}
		public byte[] ToArray()
		{
			byte[] hashBytes = new byte[ SaltSize + HashSize ];
			Array.Copy( _salt, 0, hashBytes, 0, SaltSize );
			Array.Copy( _hash, 0, hashBytes, SaltSize, HashSize );
			return hashBytes;
		}
		public byte[] Salt { get { return (byte[])_salt.Clone(); } }
		public byte[] Hash { get { return (byte[])_hash.Clone(); } }
		public bool Verify( string password )
		{
			byte[] test = new Rfc2898DeriveBytes( password, _salt, HashIter ).GetBytes( HashSize );
			for ( int i = 0; i < HashSize; i++ )
				if ( test[ i ] != _hash[ i ] )
					return false;
			return true;
		}

		/*
        //Store a password hash:
        PasswordHash hash = new PasswordHash("password");
        byte[] hashBytes = hash.ToArray();
 
        //Check password against a stored hash
        byte[] hashBytes = //read from store.
        PasswordHash hash = new PasswordHash(hashBytes);
        if(!hash.Verify("newly entered password"))
            throw new System.UnauthorizedAccessException();
    */
	}
}