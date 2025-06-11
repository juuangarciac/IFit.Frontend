using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IFit.Validations.Rules
{
    public class IsValidEmailRule<T> : Plugin.ValidationRules.Interfaces.IValidationRule<T>
    {
        private readonly Regex _regex = new(@"^([w.-]+)@([w-]+)((.(w){2,3})+)$");

        public string ValidationMessage { get; set; } = string.Empty;

        public bool Check(T value) =>
            value is string str && _regex.IsMatch(str);
    }
}
