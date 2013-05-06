using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Globalization;
using System.Windows;

namespace Starcounter.InstallerWPF.Rules {
    public class DirectoryContainsFilesRule : ValidationRule {

        public bool UseWarning { get; set; }
        public bool CheckEmptyString { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {

            if (value == null || string.IsNullOrEmpty(value.ToString()) || string.IsNullOrEmpty(value.ToString().Trim())) {
                if (this.CheckEmptyString) {
                    return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Please enter a path" });
                }
                else {
                    return new ValidationResult(true, null);
                }
            }

            try {

                bool bDirectoryContainsFiles = MainWindow.DirectoryContainsFiles(value.ToString(), true);
                if (bDirectoryContainsFiles) {
                    return new ValidationResult(true, new ErrorObject() { IsError = !UseWarning, Message = "Directory contains files" });
                }
            }
            catch (System.UnauthorizedAccessException e) {
                return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid path" + Environment.NewLine + e.Message });
            }

            return new ValidationResult(true, null);

        }
    }


    public class DuplicatPathCheckRule : ValidationRule {

        public string InstallationPath { get; set; }
        public string PersonalServerPath { get; set; }
        public string SystemServerPath { get; set; }

        public SelfType Type { get; set; }

        [Flags]
        public enum SelfType {
            InstallationPath,
            PersonalServerPath,
            SystemServerPath
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {

            if (value is string) {

                string enteredPath = (string)value;
                char[] charsToTrim = { ' ', '/', '\\' };
                enteredPath = enteredPath.TrimEnd(charsToTrim);

                if (this.Type == SelfType.PersonalServerPath) {

                    if (this.InstallationPath != null && string.Compare(enteredPath, this.InstallationPath.TrimEnd(charsToTrim)) == 0) {
                        return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid path, You can not use the same path as the main installation path" + Environment.NewLine });
                    }

                    if (this.SystemServerPath != null && string.Compare(enteredPath, this.SystemServerPath.TrimEnd(charsToTrim)) == 0) {
                        return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid path, You can not use the same path as the system server path" + Environment.NewLine });
                    }

                }
                else if (this.Type == SelfType.SystemServerPath) {

                    if (this.InstallationPath != null && string.Compare(enteredPath, this.InstallationPath.TrimEnd(charsToTrim)) == 0) {
                        return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid path, You can not use the same path as the main installation path" + Environment.NewLine });
                    }

                    if (this.PersonalServerPath != null && string.Compare(enteredPath, this.PersonalServerPath.TrimEnd(charsToTrim)) == 0) {
                        return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid path, You can not use the same path as the personal server path" + Environment.NewLine });
                    }

                }
                else if (this.Type == SelfType.InstallationPath) {
                }
                else {
                    // Unknown..
                }

            }
            return new ValidationResult(true, null);
        }
    }
    public class ErrorObject {
        public bool IsError { get; set; }
        public string Message { get; set; }

        public ErrorObject() {
        }

        public override string ToString() {
            if (string.IsNullOrEmpty(this.Message)) {
                return base.ToString();
            }
            return this.Message;
        }
    }
}
