
using Starcounter.Binding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Sc.Server.Weaver.Schema;
using Starcounter;
using Starcounter.Internal;
using Starcounter.Internal.Weaver;

namespace StarcounterInternal.Hosting
{

    internal class BinBriefcase
    {

        private Dictionary<string, FileInfo> assemblyFileInfosByName_ = new Dictionary<string, FileInfo>();

        internal void AddFromDirectory(DirectoryInfo inputDir)
        {
            List<FileInfo> fileInfos = new List<FileInfo>();
            fileInfos.AddRange(inputDir.GetFiles("*.exe"));
            fileInfos.AddRange(inputDir.GetFiles("*.dll"));
            for (int i = 0; i < fileInfos.Count; i++)
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

    public class LoaderException : Exception
    {

        public LoaderException(string message) : base(message) { }
    }

    public static class Loader
    {

        private static readonly BinBriefcase privateBinBriefcase_ = new BinBriefcase();

        public static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly assembly = null;

            var assemblyName = args.Name;
            var assemblyNameElems = assemblyName.Split(',');
            var assemblyFileName = string.Concat(assemblyNameElems[0], ".dll");
            var assemblyFileInfo = privateBinBriefcase_.GetAssemblyFile(assemblyFileName);
            if (assemblyFileInfo == null)
            {
                assemblyFileName = string.Concat(assemblyNameElems[0], ".exe");
                assemblyFileInfo = privateBinBriefcase_.GetAssemblyFile(assemblyFileName);
            }

            if (assemblyFileInfo != null)
            {
                assembly = Assembly.LoadFile(assemblyFileInfo.FullName);
            }

            return assembly;
        }

        public static unsafe void AddBasePackage(void* hsched)
        {
            TableDef systemTableDef;

            systemTableDef = new TableDef(
                "sys_table",
                new ColumnDef[]
                {
                    new ColumnDef("internal_id", DbTypeCode.UInt64, false, false),
                    new ColumnDef("name", DbTypeCode.String, true, false),
                    new ColumnDef("base_name", DbTypeCode.String, true, false),
                    new ColumnDef("table_id", DbTypeCode.UInt16, false, false),
                }
                );

            TypeDef sysTableTypeDef = new TypeDef(
                "Starcounter.Metadata.SysTable",
                null,
                new PropertyDef[]
                {
                    new PropertyDef("Name", DbTypeCode.String, true) { ColumnName = "name" },
                    new PropertyDef("BaseName", DbTypeCode.String, true) { ColumnName = "base_name" }
                },
                new TypeLoader(new AssemblyName("Starcounter"), "Starcounter.Metadata.SysTable"),
                systemTableDef
                );

            systemTableDef = new TableDef(
                "sys_index",
                new ColumnDef[]
                {
                    new ColumnDef("internal_id", DbTypeCode.UInt64, false, false),
                    new ColumnDef("name", DbTypeCode.String, true, false),
                    new ColumnDef("table_name", DbTypeCode.String, true, false),
                    new ColumnDef("description", DbTypeCode.String, true, false),
                    new ColumnDef("unique", DbTypeCode.Boolean, false, false),
                }
                );

            TypeDef sysIndexTypeDef = new TypeDef(
                "Starcounter.Metadata.SysIndex",
                null,
                new PropertyDef[]
                {
                    new PropertyDef("Name", DbTypeCode.String, true) { ColumnName = "name" },
                    new PropertyDef("TableName", DbTypeCode.String, true) { ColumnName = "table_name" },
                    new PropertyDef("Description", DbTypeCode.String, true) { ColumnName = "description" },
                    new PropertyDef("Unique", DbTypeCode.Boolean, false) { ColumnName = "unique" },
                },
                new TypeLoader(new AssemblyName("Starcounter"), "Starcounter.Metadata.SysIndex"),
                systemTableDef
                );

            Package package = new Package(new TypeDef[] { sysTableTypeDef, sysIndexTypeDef }, null);
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

        public static unsafe void ExecApp(void* hsched, string filePath)
        {
            try
            {
                filePath = filePath.Trim('\"', '\\');
                filePath = Path.GetFullPath(filePath);
            }
            catch (ArgumentException pathEx)
            {
                throw new LoaderException(string.Format("{0} ({1})", pathEx.Message, filePath));
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new LoaderException(string.Format("File not found: {0}.", filePath));
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
