﻿using Starcounter.Administrator.API.Utilities;
using Starcounter.Advanced;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest.Representations.JSON;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Starcounter.Administrator.API.Handlers {
    /// <summary>
    /// Provides the handlers for the admin server database
    /// executable resource.
    /// </summary>
    internal static partial class ExecutableHandler {
        static string uriTemplate;
        static string uriTemplateEngine;

        /// <summary>
        /// Provides a set of utility methods for working on strongly typed
        /// Engine JSON representations.
        /// </summary>
        internal static class JSON {
            /// <summary>
            /// Creates a JSON representation of the given application level
            /// executable state object.
            /// </summary>
            /// <param name="state">The database whose semantics the JSON
            /// representation represents.</param>
            /// <param name="exeState">The application level state representing
            /// the executable we are to create a representation for.</param>
            /// <param name="headers">Dictionary with headers that possibly
            /// will be added to if given and the state indicates we should
            /// do so.</param>
            /// <returns>A strongly typed JSON <see cref="Executable"/> instance.
            /// </returns>
            internal static Executable CreateRepresentation(DatabaseInfo state, AppInfo exeState, Dictionary<string, string> headers = null) {
                var admin = RootHandler.API;
                var exe = new Executable();
                var name = state.Name;
                var engineState = state.Engine;
                Trace.Assert(engineState != null, "We should never hand out executables instances with no backing state.");
                
                exe.Uri = uriTemplate.ToAbsoluteUri(name, exeState.Key);
                exe.Engine.Uri = uriTemplateEngine.ToAbsoluteUri(name);
                exe.Description = string.Format("\"{0}\" running in {1} (Exe key:{2}, Host PID:{3})",
                    Path.GetFileName(exeState.BinaryFilePath), name, exeState.Key, engineState.HostProcessId);
                exe.Path = exeState.BinaryFilePath;
                exe.ApplicationFilePath = exeState.FilePath;
                exe.Name = exeState.Name;
                exe.DefaultUserPort = state.Configuration.Runtime.DefaultUserHttpPort;
                exe.RuntimeInfo.LoadPath = exeState.HostedFilePath;
                exe.RuntimeInfo.Started = exeState.Started.HasValue ? 
                    exeState.Started.Value.ToString("s", CultureInfo.InvariantCulture) : 
                    string.Empty;
                exe.RuntimeInfo.LastRestart = exeState.LastRestart.HasValue ?
                    exeState.LastRestart.Value.ToString("s", CultureInfo.InvariantCulture) :
                    string.Empty;

                exe.WorkingDirectory = exeState.WorkingDirectory;

                if (headers != null) {
                    headers.Add("ETag", engineState.Fingerprint);
                }
                return exe;
            }

            // TODO:
            // At the moment it is not possible to specify an json to use for an array, which means
            // that when we want to add an executable to the list in Executables.json it is NOT the same 
            // as creating an instance of Executable.json and adding it.
            // Support needs to be added in typed json to allow resuse of json in arrays.
            // See issue: https://github.com/Starcounter/Starcounter/issues/2059
            // As a workaround the correct item is created here, but the code of the above method is duplicated.
            internal static Executables.ItemsElementJson CreateRepresentationForArray(DatabaseInfo state, AppInfo exeState, Dictionary<string, string> headers = null) {
                var admin = RootHandler.API;
                var exe = new Executables.ItemsElementJson();
                var name = state.Name;
                var engineState = state.Engine;
                Trace.Assert(engineState != null, "We should never hand out executables instances with no backing state.");

                exe.Uri = uriTemplate.ToAbsoluteUri(name, exeState.Key);
                exe.Engine.Uri = uriTemplateEngine.ToAbsoluteUri(name);
                exe.Description = string.Format("\"{0}\" running in {1} (Exe key:{2}, Host PID:{3})",
                    Path.GetFileName(exeState.BinaryFilePath), name, exeState.Key, engineState.HostProcessId);
                exe.Path = exeState.BinaryFilePath;
                exe.ApplicationFilePath = exeState.FilePath;
                exe.Name = exeState.Name;
                exe.DefaultUserPort = state.Configuration.Runtime.DefaultUserHttpPort;
                exe.RuntimeInfo.LoadPath = exeState.HostedFilePath;
                exe.RuntimeInfo.Started = exeState.Started.HasValue ?
                    exeState.Started.Value.ToString("s", CultureInfo.InvariantCulture) :
                    string.Empty;
                exe.RuntimeInfo.LastRestart = exeState.LastRestart.HasValue ?
                    exeState.LastRestart.Value.ToString("s", CultureInfo.InvariantCulture) :
                    string.Empty;

                exe.WorkingDirectory = exeState.WorkingDirectory;

                if (headers != null) {
                    headers.Add("ETag", engineState.Fingerprint);
                }
                return exe;
            }

            /// <summary>
            /// Creates a JSON representation of the given application level
            /// database state, looking in it's engine for the executable.
            /// </summary>
            /// <param name="state">The database whose semantics the JSON
            /// representation represents.</param>
            /// <param name="exePath">Full path to the exe whose state we
            /// will create the representation for.</param>
            /// <param name="headers">Dictionary with headers that possibly
            /// will be added to if given and the state indicates we should
            /// do so.</param>
            /// <returns>A strongly typed JSON <see cref="Executable"/> instance.
            /// </returns>
            internal static Executable CreateRepresentation(DatabaseInfo state, string exePath, Dictionary<string, string> headers = null) {
                var engineState = state.Engine;
                Trace.Assert(engineState != null, "We should never hand out executables instances with no backing state.");
                var exeState = engineState.GetExecutable(exePath);
                return CreateRepresentation(state, exeState, headers);
            }
        }

        /// <summary>
        /// Install handlers for the resource represented by this class and
        /// performs custom setup.
        /// </summary>
        internal static void Setup() {
            uriTemplate = RootHandler.API.Uris.Executable;
            uriTemplateEngine = RootHandler.API.Uris.Engine;

            // Handlers for the executable resource/abstraction
            Handle.GET<string, string, Request>(uriTemplate, OnGET);
            Handle.DELETE<string, string, Request>(uriTemplate, OnDELETE);
            RootHandler.Register405OnAllUnsupported(uriTemplate, new string[] { "GET", "DELETE" });
        }
    }
}