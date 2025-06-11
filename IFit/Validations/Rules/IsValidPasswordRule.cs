using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IFit.Validations.Rules
{
    internal class IsValidPasswordRule<T> : Plugin.ValidationRules.Interfaces.IValidationRule<T>
    {
        private readonly BigInteger minimunSize = 8;
        private readonly BigInteger maximumSize = 20;

        private readonly Regex _hasUpperCase = new(@"[A-Z]");
        private readonly Regex _hasLowerCase = new(@"[a-z]");
        private readonly Regex _hasDigit = new(@"[0-9]");
        private readonly Regex _hasSpecialChar = new(@"[!@#$%^&*(),.?""':;{}|<>]");


        public string ValidationMessage { get; set; } = string.Empty;

        public bool Check(T value) =>
            value is string str
                && str.Length >= minimunSize
                && _hasUpperCase.IsMatch(str)
                && _hasLowerCase.IsMatch(str)
                && _hasDigit.IsMatch(str)
                && _hasSpecialChar.IsMatch(str)
                && str.Length <= maximumSize;
    }
}
