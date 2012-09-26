
using Starcounter.Binding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Sc.Server.Weaver.Schema;
using Starcounter.ABCIPC;
using Starcounter.ABCIPC.Internal;
using Starcounter.Internal.Weaver;

namespace Starcounter.Internal
{

    internal static class LoaderHelper
    {

        internal static void MapPropertyDefsToColumnDefs(ColumnDef[] columnDefs, PropertyDef[] propertyDefs)
        {
            for (int pi = 0; pi < propertyDefs.Length; pi++)
            {
                var columnName = propertyDefs[pi].ColumnName;
                if (columnName != null)
                {
                    try
                    {
                        int ci = 0;
                        for (; ; )
                        {
                            if (columnDefs[ci].Name == columnName)
                            {
                                propertyDefs[pi].ColumnIndex = ci;
                                break;
                            }
                            ci++;
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new Exception(); // TODO:
                    }
                }
            }
        }
    }

    internal static class SchemaLoader
    {

        private const string rootClassName = "Starcounter.Entity";

        internal static List<TypeDef> LoadAndConvertSchema(DirectoryInfo inputDir)
        {
            var schemaFiles = inputDir.GetFiles("*.schema");

            var databaseSchema = new DatabaseSchema();
            databaseSchema.AddStarcounterAssembly();

            var typeDefs = new List<TypeDef>();

            DatabaseAssembly databaseAssembly;

            for (int i = 0; i < schemaFiles.Length; i++)
            {
                databaseAssembly = DatabaseAssembly.Deserialize(schemaFiles[i].FullName);
                databaseSchema.Assemblies.Add(databaseAssembly);
            }

            databaseSchema.AfterDeserialization();

            var databaseClasses = new List<DatabaseEntityClass>();
            databaseSchema.PopulateDatabaseEntityClasses(databaseClasses);
            
            var databaseClassCount = databaseClasses.Count;
            for (int i = 0; i < databaseClassCount; i++)
            {
                var databaseClass = databaseClasses[i];
                databaseAssembly = databaseClass.Assembly;
                var assemblyName = new AssemblyName(databaseAssembly.FullName);
                var typeLoader = new TypeLoader(assemblyName, databaseClass.Name);
                typeDefs.Add(EntityClassToTypeDef(databaseClass, typeLoader));
            }

            return typeDefs;
        }

        private static TypeDef EntityClassToTypeDef(DatabaseEntityClass databaseClass, TypeLoader typeLoader)
        {
            var columnDefs = new List<ColumnDef>();
            var propertyDefs = new List<PropertyDef>();

            GatherColumnAndPropertyDefs(databaseClass, columnDefs, propertyDefs, false);
            var columnDefArray = columnDefs.ToArray();
            var propertyDefArray = propertyDefs.ToArray();
            LoaderHelper.MapPropertyDefsToColumnDefs(columnDefArray, propertyDefArray);

            string baseName = databaseClass.BaseClass == null ? null : databaseClass.BaseClass.Name;
            if (baseName == rootClassName) baseName = null;

            var tableDef = new TableDef(databaseClass.Name, baseName, columnDefArray);
            var typeDef = new TypeDef(databaseClass.Name, baseName, propertyDefArray, typeLoader, tableDef);

            return typeDef;
        }

        private static void GatherColumnAndPropertyDefs(DatabaseEntityClass databaseClass, List<ColumnDef> columnDefs, List<PropertyDef> propertyDefs, bool subClass)
        {
            var baseDatabaseClass = databaseClass.BaseClass as DatabaseEntityClass;
            if (baseDatabaseClass != null)
            {
                GatherColumnAndPropertyDefs(baseDatabaseClass, columnDefs, propertyDefs, true);
            }

            var databaseAttributes = databaseClass.Attributes;

            for (int i = 0; i < databaseAttributes.Count; i++)
            {
                var databaseAttribute = databaseAttributes[i];

                DbTypeCode type;
                string targetTypeName;

                var databaseAttributeType = databaseAttribute.AttributeType;
                var databasePrimitiveType = databaseAttributeType as DatabasePrimitiveType;
                if (databasePrimitiveType != null)
                {
                    type = PrimitiveToTypeCode(databasePrimitiveType.Primitive);
                    targetTypeName = null;
                }
                else
                {
                    var databaseEntityClass = databaseAttributeType as DatabaseEntityClass;
                    if (databaseEntityClass != null)
                    {
                        type = DbTypeCode.Object;
                        targetTypeName = databaseEntityClass.Name;
                    }
                    else
                    {
                        if (!databaseAttribute.IsPersistent) continue;

                        // Persistent attribute needs to be of a type supported
                        // by the database.

                        throw new NotSupportedException(); // TODO:
                    }
                }

                var isNullable = databaseAttribute.IsNullable;

                // Fix handling that always nullable types are correcly
                // tagged as nullable in the schema file.

                switch (type)
                {
                    case DbTypeCode.Object:
                    case DbTypeCode.String:
                    case DbTypeCode.Binary:
                    case DbTypeCode.LargeBinary:
                        isNullable = true;
                        break;
                }

                switch (databaseAttribute.AttributeKind)
                {
                case DatabaseAttributeKind.PersistentField:
                    columnDefs.Add(new ColumnDef(
                        databaseAttribute.Name,
                        type,
                        isNullable,
                        subClass
                        ));

                    if (databaseAttribute.IsPublicRead)
                    {
                       var propertyDef = new PropertyDef(
                            databaseAttribute.Name,
                            type,
                            isNullable,
                            targetTypeName
                            );
                        propertyDef.ColumnName = databaseAttribute.Name;
                        propertyDefs.Add(propertyDef);
                    }
                    break;
                case DatabaseAttributeKind.PersistentProperty:
                    if (databaseAttribute.IsPublicRead)
                    {
                        var propertyDef = new PropertyDef(
                            databaseAttribute.Name,
                            type,
                            isNullable,
                            targetTypeName
                            );
                        propertyDef.ColumnName = databaseAttribute.PersistentProperty.AttributeFieldIndex;
                        propertyDefs.Add(propertyDef);
                    }
                    break;
                case DatabaseAttributeKind.NotPersistentProperty:
                    if (databaseAttribute.IsPublicRead)
                    {
                        var propertyDef = new PropertyDef(
                            databaseAttribute.Name,
                            type,
                            isNullable,
                            targetTypeName
                            );

                        string columnName = null;
                        var backingField = databaseAttribute.BackingField;
                        if (backingField != null && backingField.AttributeKind == DatabaseAttributeKind.PersistentField)
                        {
                            columnName = backingField.Name;
                        }
                        propertyDef.ColumnName = columnName;

                        propertyDefs.Add(propertyDef);
                    }
                    break;
                }
            }
        }

        private static DbTypeCode PrimitiveToTypeCode(DatabasePrimitive primitive)
        {
            switch (primitive)
            {
            case DatabasePrimitive.Boolean:
                return DbTypeCode.Boolean;
            case DatabasePrimitive.Byte:
                return DbTypeCode.Byte;
            case DatabasePrimitive.DateTime:
                return DbTypeCode.DateTime;
            case DatabasePrimitive.Decimal:
                return DbTypeCode.Decimal;
            case DatabasePrimitive.Double:
                return DbTypeCode.Double;
            case DatabasePrimitive.Int16:
                return DbTypeCode.Int16;
            case DatabasePrimitive.Int32:
                return DbTypeCode.Int32;
            case DatabasePrimitive.Int64:
                return DbTypeCode.Int64;
            case DatabasePrimitive.SByte:
                return DbTypeCode.SByte;
            case DatabasePrimitive.Single:
                return DbTypeCode.Single;
            case DatabasePrimitive.String:
                return DbTypeCode.String;
            case DatabasePrimitive.TimeSpan:
                throw new NotSupportedException();
            case DatabasePrimitive.UInt16:
                return DbTypeCode.UInt16;
            case DatabasePrimitive.UInt32:
                return DbTypeCode.UInt32;
            case DatabasePrimitive.UInt64:
                return DbTypeCode.UInt64;
            case DatabasePrimitive.Binary:
                return DbTypeCode.Binary;;
            case DatabasePrimitive.LargeBinary:
                return DbTypeCode.LargeBinary;
            case DatabasePrimitive.None:
            default:
                throw new NotSupportedException();
            }
        }
    }

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

            if (!Console.IsInputRedirected) {
                server = Utils.PromptHelper.CreateServerAttachedToPrompt();
            } else {
                server = new Server(Console.In.ReadLine, Console.Out.WriteLine);
            }

            // Install handlers for the type of requests we accept.

            // Handles execution requests for Apps
            server.Handle("Exec", delegate(Request r) {
                ExecApp(hsched, r);
            });
            
            // Some test handlers to show a little more.
            // To be removed.

            server.Handle("Ping", delegate(Request request) {
                request.Respond(true);
            });

            server.Handle("Echo", delegate(Request request) {
                var response = request.GetParameter<string>();
                request.Respond(response ?? "<NULL>");
            });

            // Receive until we are told to shutdown

            server.Receive();
        }

        static unsafe void ExecApp(void* hsched, Request request) {
            var filePath = request.GetParameter<string>();

            try {
                filePath = Path.GetFullPath(filePath);
            } catch (ArgumentException pathEx) {
                request.Respond(false, string.Format("{0} ({1})", pathEx.Message, filePath));
                return;
            }
                
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
                request.Respond(false, string.Format("File not found: {0}.", filePath));
                return;
            }
            
            var inputFile = new FileInfo(filePath);
            
            // TODO: Handle duplicates.

            privateBinBriefcase_.AddFromDirectory(inputFile.Directory);

            var typeDefs = SchemaLoader.LoadAndConvertSchema(inputFile.Directory);
            var unregisteredTypeDefs = new List<TypeDef>(typeDefs.Count);
            for (int i = 0; i < typeDefs.Count; i++) {
                var typeDef = typeDefs[i];
                var alreadyRegisteredTypeDef = Bindings.GetTypeDef(typeDef.Name);
                if (alreadyRegisteredTypeDef == null) {
                    unregisteredTypeDefs.Add(typeDef);
                } else {
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
