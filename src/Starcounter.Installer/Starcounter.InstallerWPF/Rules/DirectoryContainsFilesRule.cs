using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Globalization;
using System.Windows;
using Starcounter.InstallerEngine;
using Starcounter.Internal;
using System.IO;

namespace Starcounter.InstallerWPF.Rules {

    public class InstallationFolderRule : ValidationRule {

        public bool UseWarning { get; set; }
        public bool CheckEmptyString { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {

            if (value == null || string.IsNullOrEmpty(value.ToString()) || string.IsNullOrEmpty(value.ToString().Trim())) {

                if (this.CheckEmptyString) {
                    return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Please enter a folder" });
                }
                else {
                    return new ValidationResult(true, null);
                }
            }

            try {

                //  Path.GetFullPath
                if (!this.IsPathRooted(value.ToString())) {
                    return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid path, specified path dosent contains a root" });
                }

                if (!Directory.Exists(value.ToString())) {
                    return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Folder does not exists" });
                }

                //string installationPath = System.IO.Path.Combine(value.ToString(), CurrentVersion.Version);

                //if (Directory.Exists(installationPath)) {
                //    return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Version folder (" + CurrentVersion.Version + ") already exists" });
                //}
            }
            catch (System.UnauthorizedAccessException e) {
                return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid folder" + Environment.NewLine + e.Message });
            }

            return new ValidationResult(true, null);
        }
        private bool IsPathRooted(string path) {

            try {
                return Path.IsPathRooted(path);
            }
            catch (Exception) {
                return false;
            }
        }
    }


    public class DatabaseRepositoryFolderRule : ValidationRule {

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

                string folder = value.ToString();

                // If folder points to an existing repository then it's all okey
                string config = Path.Combine(Path.Combine(folder, "Personal"), "Personal.server.config");
                if (File.Exists(config)) {
                    return new ValidationResult(true, null);
                }

                bool bDirectoryContainsFiles = MainWindow.DirectoryContainsFiles(folder, true);
                if (bDirectoryContainsFiles) {
                    return new ValidationResult(false, new ErrorObject() { IsError = !UseWarning, Message = "Directory contains files (" + folder + ")" });
                }
            }
            catch (System.UnauthorizedAccessException e) {
                return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid path" + Environment.NewLine + e.Message });
            }

            return new ValidationResult(true, null);

        }
    }


    public class DirectoryContainsFilesRule : ValidationRule {

        public bool UseWarning { get; set; }
        public bool CheckEmptyString { get; set; }
        public bool AddStarcounter { get; set; }

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

                string folder = value.ToString();

                if (this.AddStarcounter) {
                    folder = Path.Combine(folder, ConstantsBank.SCProductName);
                }

                bool bDirectoryContainsFiles = MainWindow.DirectoryContainsFiles(folder, true);
                if (bDirectoryContainsFiles) {
                    return new ValidationResult(false, new ErrorObject() { IsError = !UseWarning, Message = "Directory contains files (" + folder + ")" });
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
        //public string SystemServerPath { get; set; }

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

                // Installation path is not allowed to be in SystemServerPath or PersonalServerPath.
                if (this.IsInFolder(this.InstallationPath, enteredPath) || this.IsInFolder(enteredPath, this.InstallationPath)) {
                    return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid folder, Installation folder and server folder can not be placed inside eachothers folders" });
                }

                if (this.Type == SelfType.PersonalServerPath) {

                    //if (this.InstallationPath != null && string.Compare(enteredPath, this.InstallationPath.TrimEnd(charsToTrim), true) == 0) {
                    //    return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid path, You can not use the same path as the main installation path" + Environment.NewLine });
                    //}


                    //if (this.SystemServerPath != null && string.Compare(enteredPath, this.SystemServerPath.TrimEnd(charsToTrim)) == 0) {
                    //    return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid path, You can not use the same path as the system server path" + Environment.NewLine });
                    //}

                }
                else if (this.Type == SelfType.SystemServerPath) {

                    //if (this.InstallationPath != null && string.Compare(enteredPath, this.InstallationPath.TrimEnd(charsToTrim), true) == 0) {
                    //    return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid path, You can not use the same path as the main installation path" + Environment.NewLine });
                    //}

                    // Installation path is not allowed to be in SystemServerPath or PersonalServerPath.
                    if (this.IsInFolder(this.PersonalServerPath, enteredPath) || this.IsInFolder(enteredPath, this.PersonalServerPath)) {
                        return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid folder, Installation folder and server folder can not be placed inside eachothers folders" });
                    }

                    //if (this.PersonalServerPath != null && string.Compare(enteredPath, this.PersonalServerPath.TrimEnd(charsToTrim), true) == 0) {
                    //    return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid path, You can not use the same path as the personal server path" + Environment.NewLine });
                    //}

                }
                else if (this.Type == SelfType.InstallationPath) {
                }
                else {
                    // Unknown..
                }
            }
            return new ValidationResult(true, null);
        }

        private bool IsInFolder(string folder1, string folder2) {

            if (string.IsNullOrEmpty(folder1) || string.IsNullOrEmpty(folder2)) {
                return false;
            }

            if (Path.GetFullPath(folder1.ToUpperInvariant()).StartsWith(Path.GetFullPath(folder2.ToUpperInvariant()))) {
                return true;
            }
            return false;
        }
    }

    public class IsLocalPathRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {

            try {

                if (value == null ||
                    string.IsNullOrEmpty(value.ToString()) ||
                    string.IsNullOrEmpty(value.ToString().Trim()) ||
                    Utilities.IsLocalPath(value.ToString()) == false) {
                    return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Directory needs to point to a local drive" });
                }
            }
            catch (Exception e) {
                return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid path" + Environment.NewLine + e.Message });
            }

            return new ValidationResult(true, null);

        }
    }

    public class UserPersonalDirectoryRule : ValidationRule {

        public bool UseWarning { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {

            try {

                if (value != null && value is String) {
                    // Checking that server path is in user's personal directory.
                    if (!Utilities.ParentChildDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\..", (string)value)) {
                        return new ValidationResult(false, new ErrorObject() {
                            IsError = !UseWarning,
                            Message = "Personal server installation in non-user directory." + Environment.NewLine + "You are installing Personal Server not in user directory." + Environment.NewLine + "Make sure you have read/write access rights to the directory: " + value
                        });
                    }
                }
            }
            catch (Exception e) {
                return new ValidationResult(false, new ErrorObject() { IsError = true, Message = "Invalid path" + Environment.NewLine + e.Message });
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
