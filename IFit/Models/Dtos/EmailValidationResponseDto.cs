using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.Models.Dtos
{
    public class EmailValidationResponseDto
    {
        public String email { get; set; } = string.Empty;

        public String message { get; set; } = string.Empty;

        public Boolean isVerified { get; set; }
    }
}
