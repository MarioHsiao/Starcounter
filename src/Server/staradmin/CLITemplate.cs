﻿using Starcounter.Internal;
using System;
using System.IO;

namespace staradmin {
    /// <summary>
    /// Expose the functionality supported to trigger and use the
    /// CLI template features.
    /// </summary>
    internal sealed class CLITemplate {
        const string templatesFolder = "cli-templates";
        const string defaultFileExtension = ".cs";

        private CLITemplate(string templatePath) {
            TemplateFile = templatePath;
        }

        /// <summary>
        /// Gets the full path to the root directory of all CLI
        /// templates.
        /// </summary>
        public static string TemplateHome {
            get {
                return Path.Combine(StarcounterEnvironment.InstallationDirectory, templatesFolder);
            }
        }

        /// <summary>
        /// Gets the full path of the current template.
        /// </summary>
        public readonly string TemplateFile;

        /// <summary>
        /// Gets a template by name, or <c>null</c> if it doesn't exist.
        /// </summary>
        /// <param name="template">The named template to find.</param>
        /// <returns>A template if one with the given name was found;
        /// <c>null</c> otherwise.</returns>
        public static CLITemplate GetTemplate(string template) {
            CLITemplate result = null;

            if (Directory.Exists(TemplateHome)) {
                var templateFileName = template;
                if (Path.GetExtension(templateFileName) == string.Empty) {
                    templateFileName = Path.Combine(templateFileName, defaultFileExtension);
                }

                var templatePath = Path.Combine(TemplateHome, templateFileName);
                if (File.Exists(templatePath)) {
                    result = new CLITemplate(templatePath);
                }
            }

            return result;
        }

        public string Instantiate(string name = "app", string directory = null) {
            directory = directory ?? Environment.CurrentDirectory;

            if (Path.GetExtension(name) == string.Empty) {
                name = Path.Combine(name, Path.GetExtension(TemplateFile));
            }

            var candidate = Path.Combine(directory, name);
            int number = 0;
            while (File.Exists(candidate)) {
                number++;
                candidate = Path.GetFileNameWithoutExtension(name);
                candidate += number.ToString();
                candidate = Path.Combine(directory, candidate, Path.GetExtension(name));
            }

            name = candidate;
            File.WriteAllText(name, File.ReadAllText(TemplateFile));
            return name;
        }
    }
}