
using System;
using System.IO;
namespace Starcounter.Server.PublicModel {
    /// <summary>
    /// Exposes the properties of a Starcounter application.
    /// </summary>
    public sealed class AppInfo {
        /// <summary>
        /// Gets or sets the logical name of the application.
        /// </summary>
        public readonly string Name;
        
        /// <summary>
        /// Gets the path of the application binary file of the
        /// current application.
        /// </summary>
        /// <remarks>
        /// This path is not neccessary (and even most likely not)
        /// the path to the executable really loaded, since Starcounter
        /// will process App executables in between them being launched
        /// and when they are actually becoming hosted, and hosting is
        /// normally done from a copy, running in another directory.
        /// </remarks>
        public readonly string BinaryFilePath;

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
        /// Gets the working directory of the App.
        /// </summary>
        public readonly string WorkingDirectory;

        /// <summary>
        /// Gets or sets the full argument set passed to the executable when
        /// started, possibly including both arguments targeting Starcounter
        /// and/or the actual App Main.
        /// </summary>
        public readonly string[] Arguments;

        /// <summary>
        /// Gets or sets the path from which the represented executable
        /// actually runs (governed by the server).
        /// </summary>
        public string ExecutionPath {
            get;
            internal set;
        }

        /// <summary>
        /// Gets or sets the server key for this executable. A key must
        /// be assured to be unique within the scope of a single database.
        /// </summary>
        public string Key {
            get;
            internal set;
        }

        /// <summary>
        /// Initializes a new <see cref="AppInfo"/>.
        /// </summary>
        /// <param name="name">The name of the application.</param>
        /// <param name="applicationFile">The application file, as given by the user.</param>
        /// <param name="applicationBinaryFile">The application binary.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="arguments">The arguments with which the application was started.</param>
        public AppInfo(string name, string applicationFile, string applicationBinaryFile, string workingDirectory, string[] arguments) {
            if (string.IsNullOrEmpty(applicationFile)) {
                throw new ArgumentNullException("applicationFile");
            }

            this.FilePath = applicationFile;
            this.Name = name ?? Path.GetFileName(applicationFile);
            this.BinaryFilePath = applicationBinaryFile ?? applicationFile;
            this.WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(BinaryFilePath);
            this.Arguments = arguments;
        }

        /// <summary>
        /// Gets a value indicating if the current instance represents
        /// an application that runs a binary from the same path as
        /// <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The application to compare against.</param>
        /// <returns><c>true</c> if the current application reference the
        /// same binary file as <paramref name="other"/>; <c>false otherwise.
        /// </c></returns>
        public bool EqualBinaryFile(AppInfo other) {
            return BinaryFilePath.Equals(other.BinaryFilePath, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Creates a full clone of the current <see cref="AppInfo"/>.
        /// </summary>
        /// <returns>A clone of the current <see cref="AppInfo"/>.
        /// </returns>
        internal AppInfo DeepClone() {
            // As long as we got only primitites, we can back
            // this one up by the built-in shallow cloning in
            // .NET.
            return (AppInfo)MemberwiseClone();
        }
    }
}