using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace IFit.Models
{
    public class AppUser
    {
        public String Name { get; set; } = string.Empty;
        public String Username { get; set; } = string.Empty;
        public String Email { get; set; } = string.Empty;
        public String Password { get; set; } = string.Empty;

        public Boolean IsVerified { get; set; } = false;
        public CoachModelType CoachModelType { get; set; } = new CoachModelType();

        public static Boolean isPresent(AppUser? appUser)
        {
            return appUser != null
                && !String.IsNullOrEmpty(appUser.Name) 
                && !String.IsNullOrEmpty(appUser.Username) 
                && !String.IsNullOrEmpty(appUser.Email) 
                && !String.IsNullOrEmpty(appUser.Password);
        }
    }
}
