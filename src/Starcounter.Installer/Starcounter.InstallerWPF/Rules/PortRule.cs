using System.Globalization;
using System.Net;
using System.Windows.Controls;

namespace Starcounter.InstallerWPF.Rules
{
    public class PortRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {

            int portNumber;

            if (value == null)
            {
                return new ValidationResult(false, "Invalid TCP port number");
            }

            int.TryParse((string)value, out portNumber);

            if (portNumber > IPEndPoint.MaxPort || portNumber < IPEndPoint.MinPort)
            {
                return new ValidationResult(false, "Invalid TCP port number");
            }

            return new ValidationResult(true, null);
        }
    }
}
