using System;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;

namespace Starcounter.InstallerWPF.Rules {
    public class PortRule : ValidationRule {
        public bool CheckIfAvailable { get; set; }
        public bool UseWarning { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {

            int portNumber;

            if (value == null) {
                //return new ValidationResult(false, "Invalid TCP port number");
                return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid TCP port number" });

            }

            if (int.TryParse(value.ToString(), out portNumber) == false) {
                //return new ValidationResult(false, "Invalid TCP port number");
                return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid TCP port number" });
            }

            if (portNumber > IPEndPoint.MaxPort || portNumber < IPEndPoint.MinPort) {
                //return new ValidationResult(false, "Invalid TCP port number");
                return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid TCP port number" });
            }

            if (this.CheckIfAvailable) {
                if (!this.IsPortAvailable(portNumber)) {
                    //return new ValidationResult(false, "TCP port " + portNumber + " is currently occupied");
                    return new ValidationResult(false, new ErrorObject() { IsError = !UseWarning, Message = "TCP port " + portNumber + " is currently occupied" });
                }
            }

            return new ValidationResult(true, null);
        }

        private bool IsPortAvailable(int port) {

            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] endPoints = properties.GetActiveTcpListeners();
            foreach (IPEndPoint endPoint in endPoints) {
                if (endPoint.Port == port) {
                    return false;
                }
            }
            return true;
        }
    }
    

}
