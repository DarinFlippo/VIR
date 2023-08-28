using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;

namespace FilterTracker
{
    public static class PasswordVerifier
    {
        public static bool Verify(string password)
        {
            var regex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";
            var result = Regex.Match(password, regex);
            if (result.Success)
                return true;
            return false;
        }
    }
}