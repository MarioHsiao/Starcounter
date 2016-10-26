
using PostSharp;
using PostSharp.Extensibility;
using PostSharp.Hosting;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Extensibility;
using Starcounter.Internal;
using Starcounter.Internal.Weaver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Starcounter.Weaver
{
    /// <summary>
    /// Exposes the facade of the code weaver engine.
    /// </summary>
    [Serializable]
    internal class CodeWeaver : MarshalByRefObject, IWeaver, IPostSharpHost
    {
        const string AnalyzerProjectFileName = "ScAnalyzeOnly.psproj";
        const string WeaverProjectFileName = "ScTransform.psproj";
        
        ProjectInvocationParameters lastProject = null;
        string TempDirectoryPath;
        string AnalyzerProjectFile;
        string WeaverProjectFile;
        WeaverCache Cache;
        List<AssemblyName> activelyReferencedAssemblies;

        /// <summary>
        /// Gets a reference to the currently executing code weaver.
        /// </summary>
        public static CodeWeaver Current {
            get;
            private set;
        }

        /// <summary>
        /// Gets the weaver host.
        /// </summary>
        public IWeaverHost Host { get; private set; }

        // TODO Remove
        /// <summary>
        /// The directory where the weaver looks for input.
        /// </summary>
        public string InputDirectory {
            get {
                return Setup.InputDirectory;
            }
        }

        public string EditionLibrariesDirectory {
            get {
                return Path.Combine(Setup.WeaverRuntimeDirectory, "EditionLibraries");
            }
        }

        public string LibrariesWithDatabaseClassesDirectory {
            get {
                return StarcounterEnvironment.LibrariesWithDatabaseClassesDirectory;
            }
        }

        /// <summary>
        /// Gets a value that indicates the weaver will only include those
        /// edition libraries that are referenced by the application being
        /// weaved. The alternative is to always include edition librares
        /// as part of every application built on a certain edition.
        /// </summary>
        public bool OnlyIncludeEditionLibrariesReferenced {
            get { return false; }
        }

        // TODO Remove
        public string CacheDirectory {
            get {
                return Setup.CacheDirectory;
            }
        }

        // TODO Remove
        public string OutputDirectory {
            get {
                return Setup.OutputDirectory;
            }
        }

        // TODO Remove
        public string AssemblyFile {
            get {
                return Setup.AssemblyFile;
            }
        }

        // TODO Remove
        public bool RunWeaver {
            get {
                return !Setup.AnalyzeOnly;
            }
        }
        
        /// <summary>
        /// Gets the file manager used by the current weaver.
        /// </summary>
        public FileManager FileManager { get; private set; }
        
        WeaverSetup IWeaver.Setup {
            get {
                return this.Setup;
            }
        }

        internal readonly WeaverSetup Setup;

        /// <summary>
        /// Initialize a new <see cref="CodeWeaver"/> instance.
        /// This internal constructor does not validate arguments; it
        /// is presumed that's already done in the public APIs.
        /// </summary>
        /// <param name="setup">The setup to execute.</param>
        /// <param name="host">The weaver host.</param>
        internal CodeWeaver(WeaverSetup setup, IWeaverHost host)
        {
            Setup = setup;
            Host = host;

            if (string.IsNullOrEmpty(setup.WeaverRuntimeDirectory))
            {
                try
                {
                    setup.WeaverRuntimeDirectory = Path.GetDirectoryName(typeof(CodeWeaver).Assembly.Location);
                }
                catch
                {
                    setup.WeaverRuntimeDirectory = Environment.CurrentDirectory;
                }
            }

            host.OnWeaverSetup(setup);
        }

        static bool ExecuteCurrent(CodeWeaver weaver) {
            var result = false;
            try {
                CodeWeaver.Current = weaver;
                weaver.Host.OnWeaverStart();
                result = weaver.Execute();
            } finally {
                weaver.Host.OnWeaverDone(result);
                weaver = null;
            }

            return result;
        }
        
        void BootDiagnose() {
            var setup = Setup;

            Diagnose("=== Bootstrap diagnostics ===");

            Diagnose("Code weaver:");

            var props = new Dictionary<string, string>();
            props["Input directory"] = this.InputDirectory;
            props["Output directory"] = this.OutputDirectory;
            props["Application file"] = this.AssemblyFile;
            props["Disable edition libraries"] = setup.DisableEditionLibraries.ToString();
            props["Disable cache"] = setup.DisableWeaverCache.ToString();
            props["Weave only to cache"] = setup.WeaveToCacheOnly.ToString();

            foreach (var pair in props) {
                Diagnose("  {0}: {1}", pair.Key, pair.Value);
            }

            this.Cache.BootDiagnose();
            this.FileManager.BootDiagnose();

            Diagnose("======");
        }

        void FinalizationDiagnose(Domain domain) {
            Diagnose("=== Finalization diagnostics ===");
            Diagnose("Code weaver:");

            Diagnose("  {0} assemblies actually referenced:", activelyReferencedAssemblies.Count);
            foreach (var file in activelyReferencedAssemblies) {
                Diagnose("  {0}", file);
            }

            if (domain != null) {
                Diagnose("  {0} assemblies in weaver domain:", domain.Assemblies.Count);
                var files = new List<string>(domain.Assemblies.Count);
                foreach (var item in domain.Assemblies) {
                    files.Add(item.Location);
                }

                var filesByDirectory = new FilesByDirectory(files).Files;
                foreach (var hive in filesByDirectory) {
                    Host.WriteDebug("  {0}:", hive.Key);
                    foreach (var file in hive.Value) {
                        Host.WriteDebug("    {0}:", file);
                    }
                }
            }

            Diagnose("======");
        }

        void Diagnose(string message, params object[] parameters) {
            Host.WriteDebug(message, parameters);
        }

        bool Execute() {
            PostSharpObjectSettings postSharpSettings;

            if (SetupEngine() == false)
                return false;

            var specifiedAssemblyFullPath = Path.Combine(this.InputDirectory, this.AssemblyFile);
            if (!File.Exists(specifiedAssemblyFullPath)) {
                Host.WriteError(Error.SCERRWEAVERFILENOTFOUND, "Path: {0}", specifiedAssemblyFullPath);
                return false;
            }

            var fm = FileManager = FileManager.Open(Host, Setup, Cache);

            if (Setup.EmitBootAndFinalizationDiagnostics) {
                BootDiagnose();
            }

            Diagnose("Retieving files to weave.");

            fm.BuildState();

            if (fm.OutdatedAssemblies.Count == 0) {
                Host.WriteInformation("No assemblies needed to be weaved.");
            } else {

                Diagnose("Retreived {0} files to weave.", fm.OutdatedAssemblies.Count);

                // Prepare the underlying weaver and the execution of PostSharp.

                Messenger.Current.Message += this.OnWeaverMessage;

                postSharpSettings = new PostSharpObjectSettings(Messenger.Current);
                postSharpSettings.CreatePrivateAppDomain = false;
                postSharpSettings.ProjectExecutionOrder = ProjectExecutionOrder.Phased;
                postSharpSettings.OverwriteAssemblyNames = false;
                postSharpSettings.DisableReflection = true;
                postSharpSettings.SearchDirectories.Add(this.InputDirectory);

                if (!Setup.DisableEditionLibraries && Directory.Exists(this.EditionLibrariesDirectory)) {
                    postSharpSettings.SearchDirectories.Add(this.EditionLibrariesDirectory);
                }

                if (Directory.Exists(this.LibrariesWithDatabaseClassesDirectory)) {
                    postSharpSettings.SearchDirectories.Add(this.LibrariesWithDatabaseClassesDirectory);
                }
                
                postSharpSettings.LocalHostImplementation = typeof(CodeWeaverInsidePostSharpDomain).AssemblyQualifiedName;

                // Move all assemblies in the cached weaver schema to that of the
                // weaver engine.

                foreach (var cachedAssembly in this.Cache.Schema.Assemblies) {
                    ScAnalysisTask.DatabaseSchema.Assemblies.Add(cachedAssembly);
                }

                // Prepare indexing of actually referenced assemblies
                activelyReferencedAssemblies = new List<AssemblyName>();

                using (IPostSharpObject postSharpObject = PostSharpObject.CreateInstance(postSharpSettings, this)) {
                    ((PostSharpObject)postSharpObject).Domain.AssemblyLocator.DefaultOptions |= PostSharp.Sdk.CodeModel.AssemblyLocatorOptions.ForClrLoading;

                    Stopwatch stopwatch = Stopwatch.StartNew();
                    try {
                        // Have PostSharp run the weaver engine for us, analysing and/or
                        // transforming each assembly.

                        var strategies = new ModuleLoadStrategy[fm.OutdatedAssemblies.Count];
                        fm.OutdatedAssemblies.Values.CopyTo(strategies, 0);
                        postSharpObject.ProcessAssemblies(strategies);
                    } catch (MessageException) {
                        // The message handler will have reported the error already.
                        // Just return 1 indicating something went wrong.

                        return false;

                    } catch (AssemblyLoadException assemblyLoadEx) {
                        string lastAssembly, lastDir;
                        GetLastProjectInfo(out lastAssembly, out lastDir);

                        var hint = string.Format(
                            "Referenced assembly: {0}. Probable referrer: {1} (in {2}).",
                            assemblyLoadEx.Assembly.GetFullName(),
                            lastAssembly,
                            lastDir);

                        Host.WriteError(
                            Error.SCERRWEAVERFAILEDRESOLVEREFERENCE,
                            ErrorCode.ToMessage(Error.SCERRWEAVERFAILEDRESOLVEREFERENCE, hint)
                            );
                        return false;
                    } catch (Exception e) {
                        ErrorMessage error;
                        if (!ErrorCode.TryGetCodedMessage(e, out error)) {
                            // We have no clue what the exception may be. Don't
                            // try anything fancy. Just let it explode.
                            throw;
                        }

                        // Any exception we catch that orignates from us (i.e.
                        // is based on a Starcounter error code and error message),
                        // we report to the host as an error.
                        Host.WriteError(error.Code, error.ToString());
                        return false;
                    }
                    finally {

                        if (Setup.EmitBootAndFinalizationDiagnostics) {
                            FinalizationDiagnose(((PostSharpObject)postSharpObject).Domain);
                        }
                    }

                    stopwatch.Stop();
                    Diagnose("Time weaving: {0:00.00} s.", stopwatch.ElapsedMilliseconds / 1000d);
                }
            }

            if (!Setup.WeaveToCacheOnly) {
                fm.Synchronize();
            }

            return true;
        }

        bool SetupEngine() {
            bool boolResult;
            BufferedStream buff;
            Byte[] strArr;
            Stream stream;
            String licenseKey;
            uint errorCode;

            // Initialize the PostSharp license manager

            stream = GetType().Assembly.GetManifestResourceStream("Sc.Postsharp.license");
            buff = new BufferedStream(stream);
            strArr = new Byte[buff.Length];
            buff.Read(strArr, 0, strArr.Length);
            licenseKey = System.Text.Encoding.UTF8.GetString(strArr);
            boolResult = PostSharp.Sdk.Extensibility.Licensing.LicenseManager.Initialize(licenseKey);
            if (boolResult == false) {
                errorCode = Error.SCERRBADPOSTSHARPLICENSE;
                Host.WriteError(errorCode, ErrorCode.ToMessage(errorCode));
                return false;
            }

            // Assure the system project files exist and are reachable for this
            // utility. Raise an error if not.

            var analyzerProjectFile = Path.GetFullPath(Path.Combine(Setup.WeaverRuntimeDirectory, CodeWeaver.AnalyzerProjectFileName));
            if (!File.Exists(analyzerProjectFile)) {
                errorCode = Error.SCERRWEAVERPROJECTFILENOTFOUND;
                Host.WriteError(
                    errorCode,
                    ErrorCode.ToMessage(errorCode, string.Format("Path: {0}", analyzerProjectFile))
                    );
                return false;
            }
            this.AnalyzerProjectFile = analyzerProjectFile;

            var weaverProjectFile = Path.GetFullPath(Path.Combine(Setup.WeaverRuntimeDirectory, CodeWeaver.WeaverProjectFileName));
            if (!File.Exists(weaverProjectFile)) {
                errorCode = Error.SCERRWEAVERPROJECTFILENOTFOUND;
                Host.WriteError(
                    errorCode,
                    ErrorCode.ToMessage(errorCode, string.Format("Path: {0}", weaverProjectFile))
                    );
                return false;
            }
            this.WeaverProjectFile = weaverProjectFile;

            // Decide all finalized directory paths to use and make sure all
            // directories we might need is actually in place.

            this.TempDirectoryPath = Path.Combine(this.CacheDirectory, "WeaverTemp");
            if (!Directory.Exists(this.TempDirectoryPath)) Directory.CreateDirectory(this.TempDirectoryPath);

            // Create the cache

            this.Cache = new WeaverCache(Host, Setup.CacheDirectory);
            this.Cache.Disabled = Setup.DisableWeaverCache;
            this.Cache.AssemblySearchDirectories.Add(Setup.InputDirectory);
            this.Cache.AssemblySearchDirectories.Add(Setup.WeaverRuntimeDirectory);
            this.Cache.AssemblySearchDirectories.Add(EditionLibrariesDirectory);

            if (Directory.Exists(this.LibrariesWithDatabaseClassesDirectory)) {
                this.Cache.AssemblySearchDirectories.Add(LibrariesWithDatabaseClassesDirectory);
            }

            return true;
        }

        /// <summary>
        /// Called when a message is broadcasted from the weaver engine.
        /// The message can be either informal, a warning or report about
        /// an error.
        /// </summary>
        /// <param name="sender">Object sending the message.</param>
        /// <param name="e">Message event arguments, including a reference
        /// to the actual message.</param>
        void OnWeaverMessage(object sender, MessageEventArgs e) {
            Message message;
            string messageIdAndText;
            uint errorCode;

            message = e.Message;
            if (message.MessageId.Equals(message.MessageText)) {
                messageIdAndText = message.MessageText;
            } else {
                messageIdAndText = string.Format("{0} - {1}", message.MessageId, message.MessageText);
                // Semi-hack. There are a few places where multiple braces are used, and it will
                // make the stream writer fuck out. All output from the weaver should be gone through
                // later, so I don't really want to start tampering with it all right now.
                messageIdAndText = messageIdAndText.Replace("{", string.Empty).Replace("}", string.Empty);
            }

            switch (message.Severity) {
                case SeverityType.Debug:
                    Host.WriteDebug(messageIdAndText);
                    break;
                case SeverityType.Verbose:
                case SeverityType.CommandLine:
                case SeverityType.ImportantInfo:
                case SeverityType.Info:
                    Host.WriteInformation(messageIdAndText);
                    break;
                case SeverityType.Warning:
                    Host.WriteWarning(messageIdAndText);
                    break;
                case SeverityType.Error:
                case SeverityType.Fatal:
                    if (message.Source.Equals("Starcounter.Weaver")) {
                        // Errors coming from the weaver can carry location
                        // information. We should utilize it to provide even
                        // better feedback.
                        //
                        // If the message stems from an error code, we should
                        // also remove the ID from the message we write since
                        // the formatted error already contains the ID (as
                        // it's header). See ErrorMessage class.

                        if (ErrorCode.TryParseDecorated(message.MessageId, out errorCode)) {
                            // The message stems from an error code. Use that fact to
                            // produce a better output.

                            messageIdAndText = message.MessageText;
                        } else {
                            // Legacy weaver error output. We must convert it from the old
                            // for to the new.

                            errorCode = WeaverUtilities.WeaverMessageToErrorCode(message.MessageId);
                            if (errorCode == 0) {
                                errorCode = Error.SCERRUNSPECIFIED;
                            }
                        }

                        // Append location information at the end of the message if we
                        // are instructed to create error message parcels.

                        if (Setup.IncludeLocationInErrorMessages) {
                            // Every parceled error message should contain a file location
                            // by protocol. If the information is lacking, we still create
                            // an empty location.

                            FileLocation fileLocation;

                            if (message.Location == null || message.Location.Equals(MessageLocation.Unknown))
                                fileLocation = FileLocation.Unknown;
                            else {
                                fileLocation = new FileLocation(
                                    message.Location.File,
                                    message.Location.Line,
                                    message.Location.Column
                                    );
                            }

                            // Send the actual message as a parameter and the location
                            // information as another.

                            Host.WriteError(errorCode, "{0} {1}", messageIdAndText, fileLocation.ToString());
                        } else {
                            // Give no location information

                            Host.WriteError(errorCode, messageIdAndText);
                        }
                    } else {
                        // PostSharp can report errors too. Write these as warnings.

                        Host.WriteWarning(messageIdAndText);
                    }
                    break;
            }
        }

        internal ProjectInvocationParameters GetProjectInvocationParametersForAssemblies(ModuleDeclaration module) {
            ProjectInvocationParameters parameters = null;
            string file = module.FileName;
            string weaverProjectFile;

            // Check if the module is part of the set of assemblies we have
            // established we must process.
            //
            // If we return NULL, the assembly will not be analyzed and/or
            // transformed, and neither will any of it's dependencies (i.e.
            // they will never even be considered). To ignore an assmbly,
            // but to keep follow it's dependencies, return a project with
            // no tasks instead (like "ScIgnore.psproj").

            if (!FileManager.Contains(file)) {
                Diagnose("Not analyzing/weaving {0}: not part of inclusion set.", file);
                return null;
            }

            // Check if the file is one of the edition libraries. If
            // it is, we just analyze it if it has not been actively
            // referenced, in case we are not always including edition
            // libraries
            
            var runWeaver = RunWeaver;
            if (OnlyIncludeEditionLibrariesReferenced) {
                if (FileManager.IsEditionLibrary(file)) {
                    runWeaver = HasBeenReferenced(module);
                }
            }

            if (runWeaver) {
                weaverProjectFile = this.WeaverProjectFile;
                parameters = new ProjectInvocationParameters(weaverProjectFile);
                parameters.PreventOverwriteAssemblyNames = false;
                parameters.Properties["TempDirectory"] = this.TempDirectoryPath;
                parameters.Properties["ScOutputDirectory"] = Setup.OutputDirectory;
                parameters.Properties["UseStateRedirect"] = Setup.UseStateRedirect ? bool.TrueString : bool.FalseString;

            } else {
                // We are only analyzing. Do this straight from the input directory.

                parameters = new ProjectInvocationParameters(this.AnalyzerProjectFile);
                parameters.PreventOverwriteAssemblyNames = true;
                parameters.Properties["NoTransformation"] = bool.TrueString;
            }

            Host.WriteInformation("{0} {1}.", runWeaver ? "Weaving" : "Analyzing", file);

            // Apply all general, shared parameters

            parameters.Properties["ScInputDirectory"] = Path.GetDirectoryName(file);
            parameters.Properties["ScCacheDirectory"] = this.CacheDirectory;
            parameters.Properties["CacheTimestamp"] =
                XmlConvert.ToString(File.GetLastWriteTime(file),
                XmlDateTimeSerializationMode.RoundtripKind);
            parameters.ProcessDependenciesFirst = true;
            parameters.Properties["AssemblyName"] = Path.GetFileNameWithoutExtension(file);
            parameters.Properties["AssemblyExtension"] = Path.GetExtension(file);
            parameters.Properties["ResolvedReferences"] = "";
            parameters.Properties["DontCopyToOutput"] = Setup.WeaveToCacheOnly ? bool.TrueString : bool.FalseString;

            lastProject = parameters;

            return parameters;
        }

        bool HasBeenReferenced(ModuleDeclaration module) {
            var reference = activelyReferencedAssemblies.Find((candidate) => {
                return string.Equals(candidate.Name, module.Assembly.Name, StringComparison.InvariantCultureIgnoreCase);
            });
            return reference != null;
        }

        // Safe implementation. This is for diagnostic, we allow it to return not knowns
        // when things cant be resolved. No exceptions should slip.
        void GetLastProjectInfo(out string assembly, out string directory) {
            var proj = lastProject;
            assembly = directory = "n/a";
            if (proj != null) {
                try {
                    assembly = proj.Properties["AssemblyName"] + proj.Properties["AssemblyExtension"];
                    directory = proj.Properties["ScInputDirectory"];
                } catch {}
            }
        }

        #region IPostSharpHost Members (methods called back by PostSharp)

        /// <summary>
        /// Not supported (and never called).
        /// </summary>
        /// <param name="assemblyFileName"></param>
        /// <param name="moduleName"></param>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        ProjectInvocationParameters IPostSharpHost.GetProjectInvocationParameters(
            string assemblyFileName,
            string moduleName,
            AssemblyName assemblyName) {
            // This is managed by the local host.
            throw new NotSupportedException();
        }

        /// <summary>
        /// Method called when an assembly is renamed. We don't have anything to do.
        /// </summary>
        /// <param name="oldAssemblyName">Old  name.</param>
        /// <param name="newAssemblyName">New name.</param>
        void IPostSharpHost.RenameAssembly(AssemblyName oldAssemblyName, AssemblyName newAssemblyName) {
        }

        /// <summary>
        /// Method called when an assembly reference should be solved (that is, PostSharp knows the
        /// name of an assembly and wants to load it).
        /// </summary>
        /// <param name="assemblyName">Name of the assembly to load.</param>
        /// <returns>A <see cref="ModuleLoadStrategy"/>, or <b>null</b> to use the default mechanism.</returns>
        string IPostSharpHost.ResolveAssemblyReference(AssemblyName assemblyName) {
            Diagnose("Detected reference: {0}.", assemblyName);
            activelyReferencedAssemblies.Add(assemblyName);
            return null;
        }

        void IWeaver.Execute()
        {
            CodeWeaver.ExecuteCurrent(this);
        }

        #endregion
    }
}
