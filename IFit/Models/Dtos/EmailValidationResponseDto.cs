using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.Models.Dtos
{
    public class EmailValidationResponseDto
    {
        public Boolean isVerified { get; set; }
        public String ServerResponse { get; set; }
    }
}
