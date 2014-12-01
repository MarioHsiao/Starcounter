using Starcounter.Internal;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Starcounter.Hosting {

    /// <summary>
    /// Represents a Starcounter application and defines its most
    /// rudimentary properties, as shared by all components and tools
    /// being part of application hosting.
    /// </summary>
    public class ApplicationBase {
        /// <summary>
        /// Gets the logical name of the application.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Path to the application file that was used to invoke the
        /// starting of the current application.
        /// </summary>
        /// <remarks>
        /// In the simplest scenario, this path will be equal to 
        /// <c>BinaryFilePath</c>, but in a scenario where there is a
        /// transform between the input and the actual executable
        /// (e.g when the input is a source code file), this property
        /// will return the path of the source code file while the
        /// <c>BinaryFilePath</c> will return the path to the assembly
        /// compiled on the fly.
        /// </remarks>
        public readonly string FilePath;

        /// <summary>
        /// Gets the path of the application binary file of the
        /// current application.
        /// </summary>
        /// <remarks>
        /// This path is not neccessary (and even most likely not)
        /// the path to the executable really loaded, since Starcounter
        /// will process application input in between it's being launched
        /// and when they actually become hosted, and hosting is
        /// normally done from a copy, running in another directory.
        /// </remarks>
        public readonly string BinaryFilePath;

        /// <summary>
        /// Gets the path from which the represented application
        /// actually runs (governed by the system).
        /// </summary>
        public string HostedFilePath {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the working directory of the application.
        /// </summary>
        public readonly string WorkingDirectory;

        /// <summary>
        /// Gets the arguments that are to be sent to a possible
        /// application entrypoint method.
        /// </summary>
        public readonly string[] Arguments;

        /// <summary>
        /// Initializes a new <see cref="ApplicationBase"/>.
        /// </summary>
        /// <param name="name">The name of the application.</param>
        /// <param name="applicationFile">The application file, as given by the user.</param>
        /// <param name="applicationBinaryFile">The application binary.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="arguments">The arguments with which the application was started.</param>
        internal ApplicationBase(string name, string applicationFile, string applicationBinaryFile, string workingDirectory, string[] arguments) {
            if (string.IsNullOrEmpty(applicationFile)) {
                throw new ArgumentNullException("applicationFile");
            }

            this.FilePath = applicationFile;
            this.Name = Path.GetFileNameWithoutExtension(name ?? applicationFile);

            // Checking if application name for correctness.
            if (!StarcounterEnvironment.IsApplicationNameLegal(this.Name))
                throw ErrorCode.ToException(Error.SCERRBADAPPLICATIONNAME, "Application name that is not allowed: " + this.Name);

            this.BinaryFilePath = applicationBinaryFile ?? applicationFile;
            this.WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(BinaryFilePath);
            this.Arguments = arguments;
        }

        /// <summary>
        /// Creates the full name for a given application, hosted in a
        /// specified and named database.
        /// </summary>
        /// <param name="databaseName">The database/host the application runs in.
        /// </param>
        /// <param name="applicationName">The short name of the application.</param>
        /// <returns>The application full name.</returns>
        internal static string CreateFullName(string databaseName, string applicationName) {
            return string.Concat(databaseName, @"\", applicationName);
        }

        /// <summary>
        /// Creates the display name for a given application, hosted in a
        /// specified and named database.
        /// </summary>
        /// <param name="databaseName">The database/host the application runs in.
        /// </param>
        /// <param name="applicationName">The short name of the application.</param>
        /// <returns>The application display name.</returns>
        internal static string CreateDisplayName(string databaseName, string applicationName) {
            var displayName = applicationName;
            if (!StarcounterConstants.DefaultDatabaseName.Equals(databaseName, StringComparison.InvariantCultureIgnoreCase)) {
                displayName = CreateFullName(databaseName, applicationName);
            }
            return displayName;
        }
    }
}