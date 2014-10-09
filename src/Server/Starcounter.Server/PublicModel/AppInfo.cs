
using Starcounter.Hosting;
using System;
using System.IO;

namespace Starcounter.Server.PublicModel {
    /// <summary>
    /// Represents a Starcounter application as maintained by the
    /// server.
    /// </summary>
    public sealed class AppInfo : ApplicationBase {
        /// <summary>
        /// Gets or sets the server key for this application. A key must
        /// be assured to be unique within the scope of a single database.
        /// </summary>
        public string Key {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the time when the current application started.
        /// </summary>
        /// <seealso cref="LastRestart"/>
        public DateTime? Started {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the time when the current application was last
        /// restarted, due to the code host it lived in needed to
        /// restart.
        /// </summary>
        /// <seealso cref="LastRestart"/>
        public DateTime? LastRestart {
            get;
            internal set;
        }

        /// <summary>
        /// Gets a value holding information about who started the
        /// current application (what client and, possibly, user).
        /// </summary>
        public string StartedBy {
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
        /// <param name="startedBy">The context from where the application was started.</param>
        public AppInfo(string name, string applicationFile, string applicationBinaryFile, string workingDirectory, string[] arguments, string startedBy)
            : base(name, applicationFile, applicationBinaryFile, workingDirectory, arguments) {
            StartedBy = startedBy;
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
        /// Gets a value indicating if the current instance represents
        /// an application that was launched using the same application file as
        /// <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The application to compare against.</param>
        /// <returns><c>true</c> if the current application reference the
        /// same application file as <paramref name="other"/>; <c>false otherwise.
        /// </c></returns>
        public bool EqualApplicationFile(AppInfo other) {
            return this.FilePath.Equals(other.FilePath, StringComparison.InvariantCultureIgnoreCase);
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