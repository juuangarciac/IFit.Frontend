using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace IFit.Models.Dtos
{
    public class SignUpRequestDto
    {
        public string name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;

        public string toString()
        {
            return "SignUpRequestDto: " + name + ", Email: " + email;
        }
    }
}
