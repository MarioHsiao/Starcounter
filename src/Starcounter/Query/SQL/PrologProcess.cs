
//using se.sics.prologbeans;
//using Starcounter;
//using Sc.Server.Binding;
//using Sc.Server.Internal;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Globalization;
//using System.IO;
//using System.Xml;

//namespace Starcounter.Query.Sql
//{
//internal class PrologProcess
//{
//    static readonly LogSource logSource = LogSources.Sql;
//    static System.Diagnostics.Process process = null;
//    static Int32 processPort = -1;
//    static String schemaFilePath = null;
//    static DateTime schemaTime = DateTime.MinValue;

//    internal static String SchemaTimeString
//    {
//        get
//        {
//            //return schemaTime.ToString("yyMMddHHmmss");
//            return schemaTime.ToString(DateTimeFormatInfo.InvariantInfo);
//        }
//    }

//    internal PrologProcess(String processFile, Int32 processPort, String schemaFile)
//        : base()
//    {
//        this.processPort = processPort;
//        this.schemaFilePath = schemaFile;
//        schemaTime = DateTime.Now;

//        // TEMP
//        String strSchemaTime = schemaTime.ToString("yyMMddHHmmss");

//        // Export schema to file.
//        try
//        {
//            Int32 time = Environment.TickCount;
//            ExportSchemaFile(schemaFilePath);
//            time = Environment.TickCount - time;
//            //TODO: Replace to logSource.Trace(...).
//            logSource.LogNotice("Exported SQL schema to {0} in {1} ms.", schemaFilePath, time);
//        }
//        catch (Exception exception)
//        {
//            logSource.LogException(exception,
//                                   "Failed to export SQL schema to {0}.",
//                                   schemaFilePath);
//            throw ErrorCode.ToException(
//                Error.SCERRSQLEXPORTSCHEMAFAILED,
//                exception,
//                "Failed to export SQL schema to " + schemaFilePath + "."
//                );
//        }
//        // Start SQL process.
//        try
//        {
//            String schemaFileEncoded = schemaFilePath.Replace("\\", "/").Replace(' ', '?'); // TODO: Use some appropriate standard encoding?
//            process = new Process();
//            process.StartInfo.FileName = processFile;
//            process.StartInfo.CreateNoWindow = true;
//            process.StartInfo.UseShellExecute = true;
//            process.StartInfo.Arguments = processPort.ToString() + " " + schemaFileEncoded;
//            process.Start();
//        }
//        catch (Exception exception)
//        {
//            logSource.LogException(
//                exception,
//                "Failed to start process: {0} {1}",
//                processFile,
//                process.StartInfo.Arguments
//            );
//            throw ErrorCode.ToException(
//                Error.SCERRSQLSTARTPROCESSFAILED,
//                exception,
//                "Failed to start process: " + processFile + " " + process.StartInfo.Arguments
//                );
//        }
//    }

//    internal static void ExportSchemaFile(String schemaFile)
//    {
//        StreamWriter streamWriter = null;
//        IEnumerator<TypeBinding> typeEnumerator = null;
//        IEnumerator<ExtensionBinding> extEnumerator = null;
//        TypeBinding typeBind = null;
//        ExtensionBinding extBind = null;
//        IPropertyBinding propBind = null;

//        schemaFilePath = schemaFile;

//        // Get schema time if schema already exists.
//        if (TryGetSchemaTime())
//            return;

//        // Otherwise export schema to file.
//        schemaTime = DateTime.Now;

//        Int32 tickCount = Environment.TickCount;
//        try
//        {
//            streamWriter = new StreamWriter(schemaFilePath);
            
//            // Set meta-info of the current schema export.
//            streamWriter.WriteLine("/* THIS FILE WAS AUTO-GENERATED. DO NOT EDIT! */");
//            streamWriter.WriteLine(":- module(schema,[]).");
//            streamWriter.WriteLine("schema_time('" + SchemaTimeString + "').");

//            // Export information about classes (tables).
//            streamWriter.WriteLine("class(fullClassNameUpper,fullClassName,baseClassName).");
//            streamWriter.WriteLine("class(shortClassNameUpper,fullClassName,baseClassName).");
//            String fullClassNameUpper = null;
//            String shortClassNameUpper = null;
//            typeEnumerator = TypeRepository.GetAllTypeBindings();
//            while (typeEnumerator.MoveNext())
//            {
//                typeBind = typeEnumerator.Current;
//                fullClassNameUpper = typeBind.Name.ToUpperInvariant();
//                shortClassNameUpper = GetShortName(typeBind.Name).ToUpperInvariant();
//                if (typeBind.Base != null)
//                {
//                    streamWriter.WriteLine("class('" + fullClassNameUpper + "','" + typeBind.Name + "','" + typeBind.Base.Name + "').");
//                    if (shortClassNameUpper != fullClassNameUpper)
//                        streamWriter.WriteLine("class('" + shortClassNameUpper + "','" + typeBind.Name + "','" + typeBind.Base.Name + "').");
//                }
//                else
//                {
//                    streamWriter.WriteLine("class('" + fullClassNameUpper + "','" + typeBind.Name + "','none').");
//                    if (shortClassNameUpper != fullClassNameUpper)
//                        streamWriter.WriteLine("class('" + shortClassNameUpper + "','" + typeBind.Name + "','none').");
//                }
//            }

//            // Export information about extensions.
//            streamWriter.WriteLine("extension(fullClassName,fullExtensionNameUpper,fullExtensionName).");
//            streamWriter.WriteLine("extension(fullClassName,shortExtensionNameUpper,fullExtensionName).");
//            String fullExtensionNameUpper = null;
//            String shortExtensionNameUpper = null;
//            typeEnumerator.Reset();
//            while (typeEnumerator.MoveNext())
//            {
//                typeBind = typeEnumerator.Current;
//                extEnumerator = typeBind.GetAllExtensionBindings();
//                if (extEnumerator != null)
//                {
//                    while (extEnumerator.MoveNext())
//                    {
//                        extBind = extEnumerator.Current;
//                        fullExtensionNameUpper = extBind.Name.ToUpperInvariant();
//                        shortExtensionNameUpper = GetShortName(extBind.Name).ToUpperInvariant();
//                        streamWriter.WriteLine("extension('" + typeBind.Name + "','" + fullExtensionNameUpper + "','" + extBind.Name + "').");
//                        if (shortExtensionNameUpper != fullExtensionNameUpper)
//                            streamWriter.WriteLine("extension('" + typeBind.Name + "','" + shortExtensionNameUpper + "','" + extBind.Name + "').");
//                    }
//                }
//            }
            
//            // Export information about properties (columns).
//            streamWriter.WriteLine("property(fullClassName,propertyNameUpper,propertyName,propertyType).");
//            typeEnumerator.Reset();
//            while (typeEnumerator.MoveNext())
//            {
//                typeBind = typeEnumerator.Current;
//                for (Int32 i = 0; i < typeBind.PropertyCount; i++)
//                {
//                    propBind = typeBind.GetPropertyBinding(i);
//                    if (propBind.TypeCode == DbTypeCode.Object)
//                    {
//                        if (propBind.TypeBinding != null)
//                        {
//                            streamWriter.WriteLine("property('" + typeBind.Name + "','" + propBind.Name.ToUpperInvariant() + "','" + 
//                                propBind.Name + "','" + propBind.TypeBinding.Name + "').");
//                        }
//                        else
//                        {
//                            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Object reference type without type binding.");
//                        }
//                    }
//                    else
//                    {
//                        streamWriter.WriteLine("property('" + typeBind.Name + "','" + propBind.Name.ToUpperInvariant() + "','" + 
//                            propBind.Name + "','" + propBind.TypeCode.ToString() + "').");
//                    }
//                }
//            }
            
//            // Export information about extension properties (columns).
//            typeEnumerator.Reset();
//            while (typeEnumerator.MoveNext())
//            {
//                typeBind = typeEnumerator.Current;
//                extEnumerator = typeBind.GetAllExtensionBindings();
//                if (extEnumerator != null)
//                {
//                    while (extEnumerator.MoveNext())
//                    {
//                        extBind = extEnumerator.Current;
//                        for (Int32 i = 0; i < extBind.PropertyCount; i++)
//                        {
//                            propBind = extBind.GetPropertyBinding(i);
//                            if (propBind.TypeCode == DbTypeCode.Object)
//                            {
//                                if (propBind.TypeBinding != null)
//                                {
//                                    streamWriter.WriteLine("property('" + extBind.Name + "','" + propBind.Name.ToUpperInvariant() + "','" + 
//                                        propBind.Name + "','" + propBind.TypeBinding.Name + "').");
//                                }
//                                else
//                                {
//                                    throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Object reference type without type binding.");
//                                }
//                            }
//                            else
//                            {
//                                streamWriter.WriteLine("property('" + extBind.Name + "','" + propBind.Name.ToUpperInvariant() + "','" + 
//                                    propBind.Name + "','" + propBind.TypeCode.ToString() + "').");
//                            }
//                        }
//                    }
//                }
//            }

//            // Export information about method EqualsOrIsDerivedFrom(Object).
//            streamWriter.WriteLine("method(fullClassName,methodNameUpper,methodName,argumentTypes,returnType).");
//            typeEnumerator.Reset();
//            while (typeEnumerator.MoveNext())
//            {
//                typeBind = typeEnumerator.Current;
//                streamWriter.WriteLine("method('" + typeBind.Name + "','EQUALSORISDERIVEDFROM','EqualsOrIsDerivedFrom',['Starcounter.IObjectView'],'Boolean').");
//            }

//            // Export information about generic method GetExtension<Type>().
//            streamWriter.WriteLine("gmethod(fullClassName,methodNameUpper,methodName,typeParameters,argumentTypes,returnType).");
//            typeEnumerator.Reset();
//            while (typeEnumerator.MoveNext())
//            {
//                typeBind = typeEnumerator.Current;
//                extEnumerator = typeBind.GetAllExtensionBindings();
//                while (extEnumerator.MoveNext())
//                {
//                    extBind = extEnumerator.Current;
//                    streamWriter.WriteLine("gmethod('" + typeBind.Name + "','GETEXTENSION','GetExtension',['" + 
//                        extBind.Name + "'],[],'" + extBind.Name + "').");
//                }
//            }

//            tickCount = Environment.TickCount - tickCount;
//            //TODO: Replace to logSource.Trace(...).
//            logSource.LogNotice("Exported SQL schema to " + schemaFilePath + " in " + tickCount.ToString() + " ms.");
//        }
//        catch (Exception exception)
//        {
//            String errMessage = "Failed to export SQL schema to " + schemaFilePath + ".";
//            logSource.LogException(exception, errMessage);
//            throw ErrorCode.ToException(Error.SCERRSQLEXPORTSCHEMAFAILED, exception, errMessage);
//        }
//        finally
//        {
//            if (streamWriter != null)
//            {
//                streamWriter.Close();
//            }
//        }
//    }

//    private static Boolean TryGetSchemaTime()
//    {
//        StreamReader streamReader = null;
//        DateTime schemaTime = DateTime.MinValue;
//        try
//        {
//            // Check if file exists.
//            if (!File.Exists(schemaFilePath))
//                return false;

//            streamReader = new StreamReader(schemaFilePath);

//            // Skip first two lines.
//            if (streamReader.EndOfStream)
//                return false;
//            streamReader.ReadLine();
//            if (streamReader.EndOfStream)
//                return false;
//            streamReader.ReadLine();
//            if (streamReader.EndOfStream)
//                return false;

//            // Read third line.
//            String str = streamReader.ReadLine();

//            // Find date time substring
//            Int32 begin = str.IndexOf('\'') + 1;
//            str = str.Substring(begin);
//            Int32 end = str.IndexOf('\'');
//            str = str.Substring(0, end);

//            // Parse date time value.
//            schemaTime = DateTime.Parse(str, DateTimeFormatInfo.InvariantInfo);

//            return true;
//        }
//        catch (Exception exception)
//        {
//            // TODO ...
//            throw exception;
//        }
//        finally
//        {
//            if (streamReader != null)
//                streamReader.Close();
//        }
//    }

//    private static String GetShortName(String fullName)
//    {
//        if (fullName == null)
//        {
//            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect fullName.");
//        }
//        Int32 index = fullName.LastIndexOf('.'); // If no occurrence then index is -1.
//        return fullName.Substring(index + 1);
//    }

//    internal static Boolean HasExited
//    {
//        get
//        {
//            return process.HasExited;
//        }
//    }

//    internal static void Dispose()
//    {
//        if (process != null && !process.HasExited)
//        {
//            process.Kill();
//            process.WaitForExit();
//        }
//    }

//    internal static PrologSession CreateSession()
//    {
//        PrologSession session = new PrologSession();
//        session.Port = processPort;
//        session.connect();
//        return session;
//    }
//}
//}
