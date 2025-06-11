using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.Validations.Rules
{
    public class IsNotNullOrEmptyRule<T> : Plugin.ValidationRules.Interfaces.IValidationRule<T>
    {
        public string ValidationMessage { get; set; } = string.Empty;   

        public bool Check(T value) =>
            value is string str && !string.IsNullOrWhiteSpace(str);
    }
}
