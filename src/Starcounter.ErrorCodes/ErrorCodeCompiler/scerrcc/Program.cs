
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Globalization;
using System.Web;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Starcounter.Errors;

namespace scerrcc
{

public class ReplaceString
{
    static readonly IDictionary<string, string> m_replaceDict 
        = new Dictionary<string, string>();

    const string ms_regexEscapes = @"[\a\b\f\n\r\t\v\\""]";

    public static string StringLiteral(string i_string)
    {
        return Regex.Replace(i_string, ms_regexEscapes, match);
    }

    public static string CharLiteral(char c)
    {
        return c == '\'' ? @"'\''" : string.Format("'{0}'", c);
    }

    private static string match(Match m)
    {
        string match = m.ToString();
        if (m_replaceDict.ContainsKey(match))
        {
            return m_replaceDict[match];
        }

        throw new NotSupportedException();
    }

    static ReplaceString()
    {
        m_replaceDict.Add("\a", @"\a");
        m_replaceDict.Add("\b", @"\b");
        m_replaceDict.Add("\f", @"\f");
        m_replaceDict.Add("\n", @"\n");
        m_replaceDict.Add("\r", @"\r");
        m_replaceDict.Add("\t", @"\t");
        m_replaceDict.Add("\v", @"\v");

        m_replaceDict.Add("\\", @"\\");
        m_replaceDict.Add("\0", @"\0");

        //The SO parser gets fooled by the verbatim version 
        //of the string to replace - @"\"""
        //so use the 'regular' version
        m_replaceDict.Add("\"", "\\\""); 
    }
}

class Program
{
    private static readonly Regex MultipleWhitespace = new Regex(@"\s+");

    private static bool verbose = false;

    public static void Verbose(string s)
    {
        if (verbose)
        {
            Trace.TraceInformation(s);
            Console.Error.WriteLine("VERBOSE: {0}", s);
        }
    }
    public static void Verbose(string fmt, params object[] args)
    {
        if (verbose)
        {
            Trace.TraceInformation(fmt, args);
            Console.Error.WriteLine("VERBOSE: " + fmt, args);
        }
    }

    static void Main(string[] args)
    {
        Stream instream = null;
        TextWriter csfile = null;
        TextWriter orangestdcsfile = null;
        TextWriter orangeintcsfile = null;
        TextWriter mcfile = null;
        TextWriter exceptionAssistantContentFile = null;
        TextWriter scerrres_h = null;
        TextWriter scerrres_c = null;

        try
        {
            verbose = args.Any(str => str == "-v");
            CommandLine.ParseArgs(args, ref instream, ref csfile, ref orangestdcsfile, ref orangeintcsfile, ref mcfile, ref exceptionAssistantContentFile, ref scerrres_h, ref scerrres_c);
            bool anythingDone = false;
            
            IList<ErrorCode> allCodes = ErrorFileReader.ReadErrorCodes(instream).ErrorCodes;
            
            if (scerrres_h != null)
            {
                Verbose("Writing SCERRRES.H file...");
                anythingDone = true;
                WriteSCERRRES_H(allCodes, scerrres_h);
                scerrres_h.Flush();
                if (scerrres_h != Console.Out)
                {
                    scerrres_h.Close();
                }
                Verbose("SCERRRES.H file written.");
            }
            if (scerrres_c != null)
            {
                Verbose("Writing SCERRRES.C file...");
                anythingDone = true;
                WriteSCERRRES_C(allCodes, scerrres_c);
                scerrres_c.Flush();
                if (scerrres_c != Console.Out)
                {
                    scerrres_c.Close();
                }
                Verbose("SCERRRES.C file written.");
            }
            if (mcfile != null)
            {
                Verbose("Writing MC file...");
                anythingDone = true;
                WriteMcFile(allCodes, mcfile);
                mcfile.Flush();
                if (mcfile != Console.Out)
                {
                    mcfile.Close();
                }
                Verbose("MC file written.");
            }
            if (csfile != null)
            {
                Verbose("Writing C# file...");
                anythingDone = true;
                WriteCSharpFile(allCodes, csfile);
                csfile.Flush();
                if (csfile != Console.Out)
                {
                    csfile.Close();
                }
                Verbose("C# file written.");
            }
            if (orangestdcsfile != null)
            {
                Verbose("Writing C# file...");
                anythingDone = true;
                WriteCSharpFile2(allCodes, orangestdcsfile, "Starcounter");
                orangestdcsfile.Flush();
                if (orangestdcsfile != Console.Out)
                {
                    orangestdcsfile.Close();
                }
                Verbose("C# file written.");
            }
            if (orangeintcsfile != null)
            {
                Verbose("Writing C# file...");
                anythingDone = true;
                WriteCSharpFile2(allCodes, orangeintcsfile, "Starcounter.Internal");
                orangeintcsfile.Flush();
                if (orangeintcsfile != Console.Out)
                {
                    orangeintcsfile.Close();
                }
                Verbose("C# file written.");
            }
            if (exceptionAssistantContentFile != null)
            {
                Verbose("Writing Exception assistant content file...");
                anythingDone = true;
                WriteExceptionAssistantContentFile(allCodes, exceptionAssistantContentFile);
                Verbose("ExceptionAssistantContentFile file written.");
            }

            if (!anythingDone)
            {
                Verbose("No actions taken");
            }
            else
            {
                Verbose("All actions performed successfully");
            }
        }
        catch (Exception e)
        {
            Die("Exception occurred: " + e);
        }
    }

    private static void WriteSCERRRES_H(IEnumerable<ErrorCode> allCodes, TextWriter writer)
    {
        writer.WriteLine("/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *");
        writer.WriteLine(" *");
        writer.WriteLine(" * THIS FILE IS AUTOMATICALLY GENERATED. DO NOT EDIT.");
        writer.WriteLine(" *");
        writer.WriteLine(" */");
        writer.WriteLine("#ifndef SCERRRES_H");
        writer.WriteLine("#define SCERRRES_H {0:D4}{1:D2}{2:D2}", DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day);
        writer.WriteLine();
        writer.WriteLine("#ifdef __cplusplus");
        writer.WriteLine("extern \"C\" ");
        writer.WriteLine("#endif /* __cplusplus */");
        writer.WriteLine("const char* StarcounterErrorMessageTemplate(long ec); /* defined in scerrres.c */");
        writer.WriteLine();
        foreach (ErrorCode ec in allCodes)
        {
            writer.WriteLine("/* {0} (SCERR{1}): {2} */", ec.Name, ec.CodeWithFacility, ReplaceString.StringLiteral(ec.Description));
            foreach (string remparam in ec.RemarkParagraphs)
            {
                writer.WriteLine("/* {0} */", remparam);
            }
            writer.WriteLine("#define {0} ({1}L)", ec.ConstantName, ec.CodeWithFacility);
        }
        writer.WriteLine();
        writer.WriteLine("#endif /* SCERRRES_H */");
    }

    private static void WriteSCERRRES_C(IEnumerable<ErrorCode> allCodes, TextWriter writer)
    {
        writer.WriteLine("/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *");
        writer.WriteLine(" *");
        writer.WriteLine(" * THIS FILE IS AUTOMATICALLY GENERATED. DO NOT EDIT.");
        writer.WriteLine(" *");
        writer.WriteLine(" */");
        writer.WriteLine("#include \"scerrres.h\"");
        writer.WriteLine();
        writer.WriteLine("#define SCERRRES_C {0:D4}{1:D2}{2:D2}", DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day);
        writer.WriteLine("#if SCERRRES_H != SCERRRES_C");
        writer.WriteLine("# error (\"Expected SCERRRES_H to be the same as SCERRRES_C\")");
        writer.WriteLine("#endif");
        writer.WriteLine();
        writer.WriteLine("#ifdef __cplusplus");
        writer.WriteLine("extern \"C\" ");
        writer.WriteLine("#endif /* __cplusplus */");
        writer.WriteLine("const char* StarcounterErrorMessageTemplate(long ec) {");
    	writer.WriteLine("  switch (ec) {");
        foreach (ErrorCode ec in allCodes)
        {
            writer.WriteLine("    case {0}: return \"{1} (SCERR{2}): {3}\";", ec.ConstantName, ec.Name, ec.CodeWithFacility, ReplaceString.StringLiteral(ec.Description));
        }
    	writer.WriteLine("  }");
    	writer.WriteLine("  return 0;");
        writer.WriteLine("}");
        writer.WriteLine();
    }

    private static void WriteMcFile(IEnumerable<ErrorCode> allCodes, TextWriter writer)
    {
//        const string Indent = "    ";
        // write header
        writer.WriteLine(";#pragma once");
        writer.WriteLine(";/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *");
        writer.WriteLine("; *");
        writer.WriteLine("; * THIS FILE IS AUTOMATICALLY GENERATED. DO NOT EDIT.");
        writer.WriteLine("; *");
        writer.WriteLine("; */");
//        writer.WriteLine("OutputBase=16");
        writer.WriteLine("OutputBase=10");
        writer.WriteLine();
#if false
        // write severity names (we use default severities, but it's better to be a bit verbose)
        writer.WriteLine("SeverityNames=(");
        foreach (Severity s in(IEnumerable<Severity>)Enum.GetValues(typeof(Severity)))
        {
            writer.WriteLine(Indent + "{0}=0x{1:X}", s.ToString(), (uint)s);
        }
        writer.WriteLine(")");
        writer.WriteLine();
#endif
#if false
        // write facility names
        Dictionary<Facility, object> facilities = new Dictionary<Facility, object>();
        foreach (ErrorCode e in allCodes)
        {
            facilities[e.Facility] = null;
        }
        writer.WriteLine("FacilityNames=(");
        foreach (Facility f in facilities.Keys)
        {
//            writer.WriteLine(Indent + "{0} = 0x{1:X}", f.Name, f.Code);
            writer.WriteLine(Indent + "{0} = {1}", f.Name, f.Code);
        }
        writer.WriteLine(")");
        writer.WriteLine();
#endif
        // write messages
        foreach (ErrorCode ec in allCodes)
        {
            foreach (string remparam in ec.RemarkParagraphs)
            {
                writer.WriteLine(";// {0}", remparam);
            }
//            writer.WriteLine("MessageId    = 0x{0:X}", ec.Code);
            writer.WriteLine("MessageId    = {0}", ec.CodeWithFacility);
//            writer.WriteLine("Facility     = {0}", ec.Facility.Name);
            writer.WriteLine("SymbolicName = {0}", ec.ConstantName);
            writer.WriteLine("Language     = English");
//            writer.WriteLine("{0} (0x{1:X}): {2}%0", ec.Name, ec.CodeWithFacility, ec.Description);
            writer.WriteLine("{0} ({1}): {2}%0", ec.Name, String.Concat("SCERR", ec.CodeWithFacility), ec.Description);
            writer.WriteLine(".");
            writer.WriteLine();
        }
    }

    private static void WriteCSharpFile(IEnumerable<ErrorCode> allCodes, TextWriter writer)
    {
        const string Indent = "    ";
        const string Indent2 = Indent + Indent;
        const string Indent3 = Indent2 + Indent;

        // write head
        writer.WriteLine("/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *");
        writer.WriteLine(" *");
        writer.WriteLine(" * THIS FILE IS AUTOMATICALLY GENERATED. DO NOT EDIT.");
        writer.WriteLine(" *");
        writer.WriteLine(" */");
        writer.WriteLine();
        writer.WriteLine("namespace Sc.Server.Internal");
        writer.WriteLine("{");
        writer.WriteLine(Indent + "/// <summary>");
        writer.WriteLine(Indent + "/// Class Error");
        writer.WriteLine(Indent + "/// </summary>");
        writer.WriteLine(Indent + "public static partial class ErrorCode");
        writer.WriteLine(Indent + "{");

        // Write categories/facilities

        IList<Facility> facilitesWritten;
        
        facilitesWritten = new List<Facility>();
        writer.WriteLine(Indent2 + "public enum Category");
        writer.WriteLine(Indent2 + "{");

        foreach (ErrorCode ec in allCodes)
        {
            if (facilitesWritten.Contains(ec.Facility))
                continue;

            facilitesWritten.Add(ec.Facility);
            writer.WriteLine("{0}{1} = {2},", Indent3, ec.Facility.Name, ec.Facility.Code * 1000);
        }

        writer.WriteLine(Indent2 + "}");
        writer.WriteLine();

        // write error codes
        foreach (ErrorCode ec in allCodes)
        {
            writer.WriteLine(Indent2 + "/// <summary> ");
            writer.Write(Indent2 + "/// ");
            HttpUtility.HtmlEncode(ec.Description, writer);
            writer.WriteLine();
            writer.WriteLine(Indent2 + "/// </summary>");
            if (ec.RemarkParagraphs.Count == 1)
            {
                writer.WriteLine(Indent2 + "/// <remarks>");
                writer.Write(Indent2 + "/// ");
                HttpUtility.HtmlEncode(ec.RemarkParagraphs[0], writer);
                writer.WriteLine();
                writer.WriteLine(Indent2 + "/// </remarks>");
            }
            else if (ec.RemarkParagraphs.Count > 1)
            {
                writer.WriteLine(Indent2 + "/// <remarks>");
                foreach (string remark in ec.RemarkParagraphs)
                {
                    writer.Write(Indent2 + "/// <para>");
                    HttpUtility.HtmlEncode(remark, writer);
                    writer.WriteLine(" </para>");
                }
                writer.WriteLine(Indent2 + "/// </remarks>");
            }
//            writer.WriteLine(Indent2 + "public const uint {0} = 0x{1:X};", ec.Name, ec.CodeWithFacility);
            writer.WriteLine(Indent2 + "public const uint {0} = {1};", ec.ConstantName, ec.CodeWithFacility);
        }
        // write end
        writer.WriteLine(Indent + "}");
        writer.WriteLine("}");
    }

    private static void WriteCSharpFile2(IEnumerable<ErrorCode> allCodes, TextWriter writer, string namespaceString)
    {
        const string Indent = "    ";
        const string Indent2 = Indent + Indent;
//        const string Indent3 = Indent2 + Indent;

        // write head
        writer.WriteLine("/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *");
        writer.WriteLine(" *");
        writer.WriteLine(" * THIS FILE IS AUTOMATICALLY GENERATED. DO NOT EDIT.");
        writer.WriteLine(" *");
        writer.WriteLine(" */");
        writer.WriteLine();
        writer.WriteLine(string.Concat("namespace ", namespaceString));
        writer.WriteLine("{");
        writer.WriteLine(Indent + "/// <summary>");
        writer.WriteLine(Indent + "/// Class Error");
        writer.WriteLine(Indent + "/// </summary>");
        writer.WriteLine(Indent + "public static partial class Error");
        writer.WriteLine(Indent + "{");

        // Write categories/facilities

#if false
        IList<Facility> facilitesWritten;

        facilitesWritten = new List<Facility>();
        writer.WriteLine(Indent2 + "public enum Category");
        writer.WriteLine(Indent2 + "{");

        foreach (ErrorCode ec in allCodes)
        {
            if (facilitesWritten.Contains(ec.Facility))
                continue;

            facilitesWritten.Add(ec.Facility);
            writer.WriteLine("{0}{1} = {2},", Indent3, ec.Facility.Name, ec.Facility.Code * 1000);
        }

        writer.WriteLine(Indent2 + "}");
        writer.WriteLine();
#endif

        // write error codes
        foreach (ErrorCode ec in allCodes)
        {
            writer.WriteLine(Indent2 + "/// <summary> ");
            writer.Write(Indent2 + "/// ");
            HttpUtility.HtmlEncode(ec.Description, writer);
            writer.WriteLine();
            writer.WriteLine(Indent2 + "/// </summary>");
            if (ec.RemarkParagraphs.Count == 1)
            {
                writer.WriteLine(Indent2 + "/// <remarks>");
                writer.Write(Indent2 + "/// ");
                HttpUtility.HtmlEncode(ec.RemarkParagraphs[0], writer);
                writer.WriteLine();
                writer.WriteLine(Indent2 + "/// </remarks>");
            }
            else if (ec.RemarkParagraphs.Count > 1)
            {
                writer.WriteLine(Indent2 + "/// <remarks>");
                foreach (string remark in ec.RemarkParagraphs)
                {
                    writer.Write(Indent2 + "/// <para>");
                    HttpUtility.HtmlEncode(remark, writer);
                    writer.WriteLine(" </para>");
                }
                writer.WriteLine(Indent2 + "/// </remarks>");
            }
            //            writer.WriteLine(Indent2 + "public const uint {0} = 0x{1:X};", ec.Name, ec.CodeWithFacility);
            writer.WriteLine(Indent2 + "public const uint {0} = {1};", ec.ConstantName, ec.CodeWithFacility);
        }
        // write end
        writer.WriteLine(Indent + "}");
        writer.WriteLine("}");
    }

    #region Visual Studio Exception assistant content file writing methods

    static void WriteExceptionAssistantContentFile(IEnumerable<ErrorCode> errorCodes, TextWriter outputWriter)
    {
        IList<Facility> facilitesWritten;
        XmlWriterSettings settings;
        XmlWriter writer;

        facilitesWritten = new List<Facility>();
        settings = new XmlWriterSettings();
        settings.Indent = true;
        writer = XmlWriter.Create(outputWriter, settings);

        writer.WriteStartElement("AssistantContent", "urn:schemas-microsoft-com:xml-msdata:exception-assistant-content");
        writer.WriteAttributeString("Version", "1.0");

        writer.WriteStartElement("ContentInfo");
        writer.WriteElementString("ContentName", "Starcounter help content");
        writer.WriteElementString("ContentID", "urn:exception-content-microsoft-com:visual-studio-7-default-content");
        writer.WriteElementString("ContentFileVersion", "1.0");
        writer.WriteElementString("ContentAuthor", "Starcounter");
        writer.WriteElementString("ContentComment", "Starcounter-specific Exception Assistant Content for Visual Studio 10.0.");
        writer.WriteEndElement();

        // The first version included links to category summary pages,
        // one per error code category.
        //foreach (var errorCode in errorCodes)
        //{
        //    WriteErrorCodeContentAllExceptionsOnePerFacility(writer, facilitesWritten, errorCode);
        //}

        // The second version includes just a link to report the
        // exception (from a wiki page) and a link for general troubleshooting.
        WriteErrorCodeContentSimplestForm(writer);

        writer.WriteEndElement();

        writer.Flush();
        writer.Close();
    }

    static void WriteErrorCodeContentSimplestForm(XmlWriter writer)
    {
        string precondition;

        precondition = "Message~\"SCERR\"";

        writer.WriteStartElement("Exception");
        writer.WriteElementString("Type", "*");
        writer.WriteElementString("Precondition", precondition);

        writer.WriteStartElement("Tip");
        writer.WriteAttributeString("HelpID", "http://www.starcounter.com/wiki/ReportExceptionFromVSAssistant");
        writer.WriteElementString("Description", "Tell Starcounter about this exception.");
        writer.WriteEndElement();

        writer.WriteEndElement();

        writer.WriteStartElement("Exception");
        writer.WriteElementString("Type", "*");
        writer.WriteElementString("Precondition", precondition);

        writer.WriteStartElement("Tip");
        writer.WriteAttributeString("HelpID", "http://www.starcounter.com/wiki/TroubleshootingHelpFromVSAssistant");
        writer.WriteElementString("Description", "See Starcounter troubleshooting tips.");
        writer.WriteEndElement();

        writer.WriteEndElement();

        precondition = "InnerException.Message~\"SCERR\"";

        writer.WriteStartElement("Exception");
        writer.WriteElementString("Type", "*");
        writer.WriteElementString("Precondition", precondition);

        writer.WriteStartElement("Tip");
        writer.WriteAttributeString("HelpID", "http://www.starcounter.com/wiki/ReportExceptionFromVSAssistant");
        writer.WriteElementString("Description", "Tell Starcounter about this exception.");
        writer.WriteEndElement();

        writer.WriteEndElement();

        writer.WriteStartElement("Exception");
        writer.WriteElementString("Type", "*");
        writer.WriteElementString("Precondition", precondition);

        writer.WriteStartElement("Tip");
        writer.WriteAttributeString("HelpID", "http://www.starcounter.com/wiki/TroubleshootingHelpFromVSAssistant");
        writer.WriteElementString("Description", "See Starcounter troubleshooting tips.");
        writer.WriteEndElement();

        writer.WriteEndElement();
    }

    static void WriteErrorCodeContentAllExceptionsOnePerFacility(
        XmlWriter writer,
        IList<Facility> facilitesWritten,
        ErrorCode code)
    {
        string precondition;

        if (facilitesWritten.Contains(code.Facility))
            return;

        facilitesWritten.Add(code.Facility);

        if (code.Facility.Code == 0)
        {
            precondition = "Message~\"SCERR\"";

            writer.WriteStartElement("Exception");
            writer.WriteElementString("Type", "*");
            writer.WriteElementString("Precondition", precondition);

            writer.WriteStartElement("Tip");
            writer.WriteAttributeString("HelpID", "http://www.starcounter.com/forum/search.php?query=[Enter error code here, for example SCERR1234]");
            writer.WriteElementString("Description", "Search Starcounter forums for this error (for example \"SCERR1234\").");
            writer.WriteEndElement();

            writer.WriteEndElement();

            precondition = "InnerException.Message~\"SCERR\"";

            writer.WriteStartElement("Exception");
            writer.WriteElementString("Type", "*");
            writer.WriteElementString("Precondition", precondition);

            writer.WriteStartElement("Tip");
            writer.WriteAttributeString("HelpID", "http://www.starcounter.com/forum/search.php?query=[Enter error code here, for example SCERR1234]");
            writer.WriteElementString("Description", "Search Starcounter forums for this error (for example \"SCERR1234\").");
            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        precondition = string.Format("Source=\"Starcounter({0})\"", code.Facility.Name);

        writer.WriteStartElement("Exception");
        writer.WriteElementString("Type", "*");
        writer.WriteElementString("Precondition", precondition);

        writer.WriteStartElement("Tip");
        writer.WriteAttributeString("HelpID", string.Format("http://www.starcounter.com/wiki/SCERR{0}", code.Facility.Name));
        writer.WriteElementString("Description", string.Format("Go to the Starcounter '{0}' category help page.", code.Facility.Name));
        writer.WriteEndElement();

        writer.WriteEndElement();

        precondition = string.Format("InnerException.Source=\"Starcounter({0})\"", code.Facility.Name);

        writer.WriteStartElement("Exception");
        writer.WriteElementString("Type", "*");
        writer.WriteElementString("Precondition", precondition);

        writer.WriteStartElement("Tip");
        writer.WriteAttributeString("HelpID", string.Format("http://www.starcounter.com/wiki/SCERR{0}", code.Facility.Name));
        writer.WriteElementString("Description", string.Format("Go to the Starcounter '{0}' category help page.", code.Facility.Name));
        writer.WriteEndElement();

        writer.WriteEndElement();
    }

    #endregion

    public static void Die(string msg)
    {
        Trace.TraceError(msg);
        Console.Error.WriteLine(msg);
        Environment.Exit(1);
    }
}
}
