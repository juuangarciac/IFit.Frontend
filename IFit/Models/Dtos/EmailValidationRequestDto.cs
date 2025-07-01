using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.Models.Dtos
{
    public class EmailValidationRequestDto
    {
        public String email { get; set; } = string.Empty;
        public String verificationCode { get; set; } = string.Empty;

    }
}
