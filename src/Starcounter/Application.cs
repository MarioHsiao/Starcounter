
using Starcounter.Advanced;
using Starcounter.Hosting;
using Starcounter.Internal;
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
        readonly ICodeHost host;
        readonly ApplicationBase state;
        readonly Dictionary<MimeType, MimeProvider> mimeProviders = new Dictionary<MimeType, MimeProvider>(2);
        static object monitor = new object();
        static Dictionary<string, Application> indexName = new Dictionary<string, Application>(StringComparer.InvariantCultureIgnoreCase);
        static Dictionary<string, Application> indexLoadPath = new Dictionary<string, Application>(StringComparer.InvariantCultureIgnoreCase);
        static Dictionary<string, Application> indexFileName = new Dictionary<string, Application>(StringComparer.InvariantCultureIgnoreCase);

        internal static Application CurrentAssigned {
            get {
                var app = (StarcounterEnvironment.AppName != null) ? indexName[StarcounterEnvironment.AppName] : null;
                return app;
            }
            set {
                StarcounterEnvironment.AppName = ((value == null) ? null : value.Name);
            }
        }

        /// <summary>
        /// Gets the <see cref="ICodeHost"/> the current application
        /// execute within.
        /// </summary>
        public ICodeHost Host {
            get { return host;  }
        }

        /// <summary>
        /// Gets indicator if the host should wrap the entrypoint call in a
        /// write transaction.
        /// </summary>
        internal bool TransactEntrypoint {
            get { return state.TransactEntrypoint; }
        }

        /// <summary>
        /// Gets the name of the application.
        /// </summary>
        public string Name {
            get {
                return state.Name;
            }
        }

        /// <summary>
        /// Gets the display name of the current application.
        /// </summary>
        public string DisplayName {
            get {
                return ApplicationBase.CreateDisplayName(Db.Environment.DatabaseNameLower, Name);
            }
        }

        /// <summary>
        /// Gets the full name of the current application.
        /// </summary>
        public string FullName {
            get {
                return ApplicationBase.CreateFullName(Db.Environment.DatabaseNameLower, Name);
            }
        }

        /// <summary>
        /// Gets the file that was used to launch the current
        /// application.
        /// </summary>
        public string FilePath {
            get {
                return state.FilePath;
            }
        }

        /// <summary>
        /// Gets the working directory of the application.
        /// </summary>
        public string WorkingDirectory {
            get {
                return state.WorkingDirectory;
            }
        }

        /// <summary>
        /// Gets the set of resource directories for the current application.
        /// </summary>
        /// <remarks>
        /// This set contains resource directories specified when the application
        /// started, and are all resolved to their fully qualified paths by our
        /// standard algorithm to do so. This list does not contain all resource
        /// directories; its is possible Starcounter add convention-based directories
        /// to the full set, and these are not part of this list.
        /// </remarks>
        public string[] ResourceDirectories {
            get {
                return state.ResourceDirectories.ToArray();
            }
        }

        /// <summary>
        /// Gets the arguments that was used to start the
        /// current application.
        /// </summary>
        public string[] Arguments {
            get {
                return state.Arguments;
            }
        }

        /// <summary>
        /// <see cref="ApplicationBase.HostedFilePath"/>
        /// </summary>
        internal string HostedFilePath {
            get {
                return state.HostedFilePath;
            }
        }

        /// <summary>
        /// Gets a dictionary with installed mime providers.
        /// </summary>
        internal Dictionary<MimeType, MimeProvider> MimeProviders {
            get {
                return mimeProviders;
            }
        }

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
                if (current == null) {
                    throw CreateInvalidOperationExceptionWithCode();
                }
                return current;
            }
        }

        /// <summary>
        /// Register a request filter middleware, affecting the request
        /// pipeline, possibly filtering requests as they arrive.
        /// </summary>
        /// <param name="requestFilter">The request filter middleware the
        /// current application want to enable.</param>
        public void Use(Func<Request, Response> requestFilter) {
            Handle.InternalAddRequestFilter(requestFilter);
        }

        /// <summary>
        /// Register a response filter middleware, affecting the request
        /// pipeline, possibly customizing responses before they are sent
        /// back to the client.
        /// </summary>
        /// <param name="responseFilter">The response filter middleware the
        /// current application want to enable.</param>
        public void Use(Func<Request, Response, Response> responseFilter) {
            Handle.InternalAddResponseFilter(responseFilter);
        }

        /// <summary>
        /// Register a custom middleware exposed via <see cref="IMiddleware"/>.
        /// </summary>
        /// <param name="middleware">The middleware component the current
        /// application want to enable.
        /// </param>
        public void Use(IMiddleware middleware) {
            middleware.Register(this);
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
        /// Gets the <see cref="Application"/> the given <paramref name="identity"/>
        /// represent. Both logical names, short file names (such as "foo.exe") and
        /// full names, such as "\path\to\foo.exe", will be considered.
        /// </summary>
        /// <param name="identity">The identify of the <see cref="Application"/> that
        /// are being requested.</param>
        /// <returns>An <see cref="Application"/> matching the given identity.
        /// <exception cref="AgrumentNullException">Thrown when <paramref name="identity"/>
        /// is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when the application can't
        /// be resolved based on the given identity.</exception>
        public static Application GetApplication(string identity) {
            if (string.IsNullOrWhiteSpace(identity)) throw new ArgumentNullException("identity");

            Application application;
            var found = indexName.TryGetValue(identity, out application);
            found = found || indexLoadPath.TryGetValue(identity, out application);
            if (!found) {
                found = indexFileName.TryGetValue(identity, out application);
                if (!found) {
                    throw CreateArgumentExceptionWithCode(string.Format("File \"{0}\" does not represent a known application.", identity));
                } else if (application == null) {
                    throw CreateArgumentExceptionWithCode(string.Format("File name \"{0}\" is ambiguous. Specify the full name to resolve.", identity));
                }
            }
            return application;
        }

        /// <inheritdoc />
        public override string ToString() {
            return DisplayName;
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
            if (string.IsNullOrEmpty(application.state.HostedFilePath))  throw new ArgumentNullException("application.HostedFilePath");
            lock (monitor) {
                indexName.Add(application.Name, application);
                indexLoadPath.Add(application.state.HostedFilePath, application);

                var fileName = Path.GetFileName(application.FilePath);
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
        /// Initialize an <see cref="Application"/>.
        /// </summary>
        /// <param name="appBase">The underlying state.</param>
        /// <param name="host">The code host the application runs
        /// within.</param>
        internal Application(ApplicationBase appBase, ICodeHost host) {
            this.state = appBase;
            this.host = host;
        }

        /// <summary>
        /// 
        /// </summary>
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
