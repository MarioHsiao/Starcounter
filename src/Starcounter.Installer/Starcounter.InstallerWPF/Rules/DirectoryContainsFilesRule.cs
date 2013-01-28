using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Globalization;

namespace Starcounter.InstallerWPF.Rules
{
    public class DirectoryContainsFilesRule : ValidationRule
    {

        public bool UseWarning { get; set; }
        public bool CheckEmptyString { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {

            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                if (this.CheckEmptyString)
                {
                    return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Please enter a path" });
                }
                else
                {
                    return new ValidationResult(true, null);
                }
            }

            try
            {

                bool bDirectoryContainsFiles = MainWindow.DirectoryContainsFiles(value.ToString(), true);
                if (bDirectoryContainsFiles)
                {
                    return new ValidationResult(true, new ErrorObject() { IsError = !UseWarning, Message = "Directory contains files" });
                }
            }
            catch (System.UnauthorizedAccessException e)
            {
                return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid path" + Environment.NewLine + e.Message });
            }

            return new ValidationResult(true, null);

        }
    }

    public class ErrorObject
    {
        public bool IsError { get; set; }
        public string Message { get; set; }

        public ErrorObject()
        {
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(this.Message))
            {
                return base.ToString();
            }
            return this.Message;
        }
    }
}
