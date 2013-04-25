// ***********************************************************************
// <copyright file="RepositoryStructure.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.IO;
using Starcounter.Configuration;
using Starcounter.Internal;

namespace Starcounter.Server.Setup {

    /// <summary>
    /// Represents the disk structure of a server repository.
    /// </summary>
    /// <remarks>
    /// A <see cref="RepositoryStructure"/> is used in the process when
    /// creating repositories (and ultimately, servers). Instances of this
    /// class can be created to present to clients the suggested paths to
    /// use for a particular repository, to allow clients to modify these
    /// paths and to finally, assist in creating the directory structure
    /// on the local disk.
    /// </remarks>
    public sealed class RepositoryStructure {
        /// <summary>
        /// Gets the repository. This field is read-only. It can be
        /// specified in the constructor only.
        /// </summary>
        public readonly string RepositoryDirectory;

        /// <summary>
        /// Gets or sets the database directory.
        /// </summary>
        public string DatabaseDirectory {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the data directory.
        /// </summary>
        public string DataDirectory {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the temporary directory.
        /// </summary>
        public string TempDirectory {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the log directory.
        /// </summary>
        public string LogDirectory {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the Administrator Tcp port.
        /// </summary>
        public UInt16 SystemHttpPort {
            get;
            set;
        }

        /// <summary>
        /// Creates a <see cref="RepositoryStructure"/> with a default structure
        /// and a name based on <paramref name="repositoryDirectory"/>.
        /// </summary>
        /// <param name="repositoryDirectory">
        /// The directory to use as the repository directory for the repository
        /// to be created.
        /// </param>
        /// <returns>A <see cref="RepositoryStructure"/> with default directory
        /// specifications based on <paramref name="repositoryDirectory"/>.
        /// </returns>
        public static RepositoryStructure NewDefault(string repositoryDirectory) {
            return new RepositoryStructure(repositoryDirectory, null, null, null, null);
        }

        /// <summary>
        /// Initializes a new <see cref="RepositoryStructure"/> with the given
        /// values.
        /// </summary>
        /// <param name="repositoryDirectory">
        /// The repository directory to use. Must be specified and must be a valid
        /// path specification.
        /// </param>
        /// <param name="databaseDirectory">Specifies an optional database directory.
        /// Pass <see langword="null"/> to use the default database directory for the
        /// specified <paramref name="repositoryDirectory"/>.</param>
        /// <param name="dataDirectory">Specifies an optional data directory.
        /// Pass <see langword="null"/> to use the default data directory for the
        /// specified <paramref name="repositoryDirectory"/></param>
        /// <param name="tempDirectory">Specifies an optional temporary directory.
        /// Pass <see langword="null"/> to use the default temporary directory for the
        /// specified <paramref name="repositoryDirectory"/></param>
        /// <param name="logDirectory">Specifies an optional logging directory.
        /// Pass <see langword="null"/> to use the default logging directory for the
        /// specified <paramref name="repositoryDirectory"/></param>
        public RepositoryStructure(
            string repositoryDirectory,
            string databaseDirectory,
            string dataDirectory,
            string tempDirectory,
            string logDirectory
        ) {
            if (string.IsNullOrEmpty(repositoryDirectory)) {
                throw new ArgumentNullException("repositoryDirectory");
            }
            if (repositoryDirectory.IndexOfAny(Path.GetInvalidPathChars()) != -1) {
                throw new ArgumentOutOfRangeException("repositoryDirectory");
            }

            repositoryDirectory = Path.GetFullPath(repositoryDirectory);
            this.RepositoryDirectory = repositoryDirectory;

            // Setting to default system port.
            this.SystemHttpPort = StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort;

            UseDirectoryPathOrCreateDefault(
                ref databaseDirectory, Path.Combine, repositoryDirectory, "Databases");
            UseDirectoryPathOrCreateDefault(
                ref dataDirectory, Path.Combine, repositoryDirectory, "Data");
            UseDirectoryPathOrCreateDefault(
                ref tempDirectory, Path.Combine, repositoryDirectory, "Temp");
            UseDirectoryPathOrCreateDefault(
                ref logDirectory, Path.Combine, repositoryDirectory, "Logs");
            this.DatabaseDirectory = databaseDirectory;
            this.DataDirectory = dataDirectory;
            this.TempDirectory = tempDirectory;
            this.LogDirectory = logDirectory;
        }

        string serverName_ = null;

        /// <summary>
        /// Gets the name of the <see cref="RepositoryStructure"/>.
        /// </summary>
        public string Name {
            // TODO: Name of the server is either Personal or System
            // and is set explicitly during installation.
            // (arbitrary name derived from the folder name caused lots of problems).
            get {
                if (serverName_ == null)
                    return Path.GetFileName(RepositoryDirectory);

                return serverName_;
            }

            set {
                serverName_ = value;
            }
        }

        /// <summary>
        /// Gets the path to the server configuration file for the current
        /// <see cref="RepositoryStructure"/>
        /// </summary>
        public string ServerConfigurationPath {
            get {
                return Path.Combine(RepositoryDirectory,
                                    string.Format("{0}{1}", this.Name, ServerConfiguration.FileExtension)
                                   );
            }
        }

        /// <summary>
        /// Creates the current <see cref="RepositoryStructure"/> on disk.
        /// </summary>
        public void Create() {
            AssureDirectory(this.RepositoryDirectory);
            AssureDirectory(this.DatabaseDirectory);
            AssureDirectory(this.DataDirectory);
            AssureDirectory(this.TempDirectory);
            AssureDirectory(this.LogDirectory);
            if (File.Exists(this.ServerConfigurationPath)) {
                File.Delete(this.ServerConfigurationPath);
            }
        }

        /// <summary>
        /// Assures the directory specified by <paramref name="path"/> is
        /// created on disk if it does not exist.
        /// </summary>
        /// <param name="path">Path to assure.</param>
        /// <returns>The path to the directory.</returns>
        string AssureDirectory(string path) {
            if (Directory.Exists(path) == false) {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        /// <summary>
        /// Assures a path, either by using the supplied value or by creating
        /// a new one using the supplied function.
        /// </summary>
        /// <param name="path">Path to assure.</param>
        /// <param name="createDefaultPath">Function to use if a new path must
        /// be created.</param>
        /// <param name="root">Root of the path to create. Only used if a new
        /// path must be created.</param>
        /// <param name="directory">Directory of the path to create. Only used
        /// if a new path must be created.</param>
        void UseDirectoryPathOrCreateDefault(
            ref string path,
            Func<string, string, string> createDefaultPath,
            string root,
            string directory
        ) {
            path = string.IsNullOrEmpty(path) ? createDefaultPath(root, directory) : path;
        }
    }
}