
using Starcounter.Binding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Sc.Server.Weaver.Schema;
using Starcounter;
using Starcounter.ABCIPC;
using Starcounter.ABCIPC.Internal;
using Starcounter.Internal;
using Starcounter.Internal.Weaver;

namespace StarcounterInternal.Bootstrap
{

    internal class BinBriefcase
    {

        private Dictionary<string, FileInfo> assemblyFileInfosByName_ = new Dictionary<string, FileInfo>();

        internal void AddFromDirectory(DirectoryInfo inputDir)
        {
            FileInfo[] fileInfos = inputDir.GetFiles("*.dll");
            for (int i = 0; i < fileInfos.Length; i++)
            {
                var fileInfo = fileInfos[i];
                var fileName = fileInfo.Name;
                FileInfo previouslyAddedFileInfo;
                if (!assemblyFileInfosByName_.TryGetValue(fileName, out previouslyAddedFileInfo))
                {
                    assemblyFileInfosByName_.Add(fileName, fileInfo);
                }
                else
                {
                    // TODO: Make sure that the files are the same. Checksum?
                }
            }
        }

        internal FileInfo GetAssemblyFile(string assemblyFileName)
        {
            FileInfo ret;
            assemblyFileInfosByName_.TryGetValue(assemblyFileName, out ret);
            return ret;
        }
    }

    public static class Loader
    {

        private static readonly BinBriefcase privateBinBriefcase_ = new BinBriefcase();

        internal static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly assembly = null;

            var assemblyName = args.Name;
            var assemblyNameElems = assemblyName.Split(',');
            var assemblyFileName = string.Concat(assemblyNameElems[0], ".dll");
            var assemblyFileInfo = privateBinBriefcase_.GetAssemblyFile(assemblyFileName);
            if (assemblyFileInfo != null)
            {
                assembly = Assembly.LoadFile(assemblyFileInfo.FullName);
            }

            return assembly;
        }

        public static unsafe void RunMessageLoop(void* hsched)
        {
            var appDomain = AppDomain.CurrentDomain;
            appDomain.AssemblyResolve += new ResolveEventHandler(ResolveAssembly);
            Server server;

            // Create the server.
            // If input has not been redirected, we let the server accept
            // requests in a simple text format from the console.
            // 
            // If the input has been redirected, we force the parent process
            // to use the "real" API's (i.e. the Client), just as the server
            // will do, once it has been moved into Orange.

            if (!Console.IsInputRedirected)
            {
                server = Utils.PromptHelper.CreateServerAttachedToPrompt();
            }
            else
            {
                server = new Server(Console.In.ReadLine, Console.Out.WriteLine);
            }

            // Install handlers for the type of requests we accept.

            // Handles execution requests for Apps
            server.Handle("Exec", delegate(Request r)
            {
                ExecApp(hsched, r);
            });

            // Some test handlers to show a little more.
            // To be removed.

            server.Handle("Ping", delegate(Request request)
            {
                request.Respond(true);
            });

            server.Handle("Echo", delegate(Request request)
            {
                var response = request.GetParameter<string>();
                request.Respond(response ?? "<NULL>");
            });

            // Receive until we are told to shutdown

            server.Receive();
        }

        static unsafe void ExecApp(void* hsched, Request request)
        {
            var filePath = request.GetParameter<string>();

            try
            {
                filePath = Path.GetFullPath(filePath);
            }
            catch (ArgumentException pathEx)
            {
                request.Respond(false, string.Format("{0} ({1})", pathEx.Message, filePath));
                return;
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                request.Respond(false, string.Format("File not found: {0}.", filePath));
                return;
            }

            var inputFile = new FileInfo(filePath);

            // TODO: Handle duplicates.

            privateBinBriefcase_.AddFromDirectory(inputFile.Directory);

            var typeDefs = SchemaLoader.LoadAndConvertSchema(inputFile.Directory);
            var unregisteredTypeDefs = new List<TypeDef>(typeDefs.Count);
            for (int i = 0; i < typeDefs.Count; i++)
            {
                var typeDef = typeDefs[i];
                var alreadyRegisteredTypeDef = Bindings.GetTypeDef(typeDef.Name);
                if (alreadyRegisteredTypeDef == null)
                {
                    unregisteredTypeDefs.Add(typeDef);
                }
                else
                {
                    // TODO:
                    // Assure that the already loaded type definition has
                    // the same structure.
                }
            }

            var assembly = Assembly.LoadFile(inputFile.FullName);

            Package package = new Package(unregisteredTypeDefs.ToArray(), assembly);
            IntPtr hPackage = (IntPtr)GCHandle.Alloc(package, GCHandleType.Normal);

            uint e = sccorelib.cm2_schedule(
                hsched,
                0,
                sccorelib_ext.TYPE_PROCESS_PACKAGE,
                0,
                0,
                0,
                (ulong)hPackage
                );
            if (e != 0) throw ErrorCode.ToException(e);

            // We only process one package at a time. Wait for the package
            // to be processed before accepting more input.
            //
            // (We can only handle one package at a time or we can not
            // evaluate if a type definition has already been loaded.)

            package.WaitUntilProcessed();
            package.Dispose();
        }
    }
}
