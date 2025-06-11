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


        public Boolean isPresent()
        {
            return !String.IsNullOrEmpty(Name) && !String.IsNullOrEmpty(Username) && !String.IsNullOrEmpty(Email) && !String.IsNullOrEmpty(Password);
        }
    }
}
