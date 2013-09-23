
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Starcounter {

    /// <summary>
    /// Represents a Starcounter application, executing in or configured
    /// to run in, a Starcounter code host.
    /// </summary>
    public sealed class Application {
        static object monitor = new object();
        static Dictionary<string, Application> indexLoadPath = new Dictionary<string, Application>(StringComparer.InvariantCultureIgnoreCase);
        static Dictionary<string, Application> indexFileName = new Dictionary<string, Application>(StringComparer.InvariantCultureIgnoreCase);

        [ThreadStatic]
        internal static Application CurrentAssigned;

        /// <summary>
        /// Gets the current application, running in the current Starcounter
        /// code host.
        /// </summary>
        /// <remarks>
        /// The current application is determined by context where this property
        /// is read. The code host has several ways to figure out the which is
        /// the current application, based on the calling code.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the application can't
        /// be resolved based on the given context.</exception>
        public static Application Current {
            get {
                var current = CurrentAssigned;
                try {
                    current = current ?? GetApplication(Assembly.GetCallingAssembly());
                } catch (ArgumentNullException ne) {
                    throw CreateInvalidOperationExceptionWithCode(null, ne);
                } catch (ArgumentException ae) {
                    throw CreateInvalidOperationExceptionWithCode(null, ae);
                }
                return current;
            }
        }

        /// <summary>
        /// Gets the <see cref="Application"/> the given <paramref name="assembly"/>
        /// represent or is part of.
        /// </summary>
        /// <param name="assembly">The assembly whose <see cref="Application"/> are
        /// being requested.</param>
        /// <returns>The application <paramref name="assembly"/> represent or runs
        /// as a part of.</returns>
        /// <exception cref="AgrumentNullException">Thrown when <paramref name="assembly"/>
        /// is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the application can't
        /// be resolved based on the given assembly.</exception>
        public static Application GetApplication(Assembly assembly) {
            string location = null;
            try {
                location = assembly.Location;
            }
            catch (NullReferenceException) {
                throw new ArgumentNullException("assembly");
            } catch (NotSupportedException nse) {
                throw CreateArgumentExceptionWithCode(null, nse);
            }

            try {
                return indexLoadPath[location];
            } catch (KeyNotFoundException knfe) {
                var detail = string.Format("Assembly \"{0}\" does not represent a known application.", assembly.FullName);
                throw CreateArgumentExceptionWithCode(detail, knfe);
            }
        }

        /// <summary>
        /// Gets the <see cref="Application"/> the given <paramref name="fileName"/>
        /// represent. Both short names, such as "foo.exe", and full names, such as
        /// "\path\to\foo.exe", will be considered.
        /// </summary>
        /// <param name="fileName">The file name whose <see cref="Application"/> are
        /// being requested.</param>
        /// <returns>The <see cref="Application"/> launched by the given file.
        /// <exception cref="AgrumentNullException">Thrown when <paramref name="fileName"/>
        /// is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when the application can't
        /// be resolved based on the given file.</exception>
        public static Application GetApplication(string fileName) {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException("fileName");
            Application application;
            var found = indexLoadPath.TryGetValue(fileName, out application);
            if (!found) {
                found = indexFileName.TryGetValue(fileName, out application);
                if (!found) {
                    throw CreateArgumentExceptionWithCode(string.Format("File \"{0}\" does not represent a known application.", fileName));
                } else if (application == null) {
                    throw CreateArgumentExceptionWithCode(string.Format("File name \"{0}\" is ambiguous. Specify the full name to resolve.", fileName));
                }
            }
            return application;
        }

        /// <summary>
        /// Returns a copy of all applications currently indexed.
        /// </summary>
        /// <returns>An array of all running, indexed applications.</returns>
        internal static Application[] GetAllApplications() {
            lock (monitor) {
                var apps = new Application[indexLoadPath.Values.Count];
                indexLoadPath.Values.CopyTo(apps, 0);
                return apps;
            }
        }

        /// <summary>
        /// Assures the given <see cref="Application"/> is properly indexed,
        /// allowing it to be later retrived from any of the supported lookup
        /// methods.
        /// </summary>
        /// <param name="application">The application to index.</param>
        internal static void Index(Application application) {
            if (application == null) throw new ArgumentNullException("application");
            lock (monitor) {
                indexLoadPath.Add(application.LoadPath, application);

                var fileName = Path.GetFileName(application.FileName);
                if (indexFileName.ContainsKey(fileName)) {
                    // If the index already contains an entry with the same
                    // short name, the short name is ambiguous and we just
                    // disable querying the application on short name.
                    indexFileName[fileName] = null;
                } else {
                    indexFileName.Add(fileName, application);
                }
            }
        }

        /// <summary>
        /// Gets the name, including the full path, of the applications
        /// primary file.
        /// </summary>
        /// <remarks>
        /// An application can be launched by several types of files,
        /// including executables (.exe), source code (e.g. .cs) and
        /// libraries (.dll) and future, yet not known files, on a variety
        /// of host systems. This property is designed to return the full
        /// file name, including the path, of the file responsible for
        /// starting the current application.
        /// </remarks>
        public string FileName { get; internal set; }
        
        /// <summary>
        /// Gets the full path of the file actually loaded in the code
        /// host.
        /// </summary>
        public string LoadPath { get; internal set; }

        /// <summary>
        /// Gets the logical working directory of the current <see cref="Application"/>.
        /// </summary>
        public string WorkingDirectory { get; internal set; }
        
        /// <summary>
        /// Gets the arguments with which the current <see cref="Application"/>
        /// was started. These are the arguments passed to a possible entrypoint,
        /// semantically comparable to <see cref="Environment.CommandLine"/>.
        /// </summary>
        public string[] Arguments { get; internal set; }

        static Exception CreateArgumentExceptionWithCode(string postfix = null, Exception innerException = null) {
            return ErrorCode.ToException(Error.SCERRAPPLICATIONCANTBERESOLVED, innerException, postfix, (msg, ex) => {
                return new ArgumentException(msg, ex);
            });
        }

        static Exception CreateInvalidOperationExceptionWithCode(string postfix = null, Exception innerException = null) {
            return ErrorCode.ToException(Error.SCERRAPPLICATIONCANTBERESOLVED, innerException, postfix, (msg, ex) => {
                return new InvalidOperationException(msg, ex);
            });
        }
    }
}
