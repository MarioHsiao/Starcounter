
using System;
using System.Collections;
using System.Reflection;
using System.Text;

namespace Starcounter.Logging
{

    /// <summary>
    /// Exception formatter that adds property values to the standard exception
    /// formatting containing type, message and stack trace. Use to get a
    /// better output when logging an exception.
    /// </summary>
    internal static class ExceptionFormatter
    {

        private static readonly String[] _ignoredProps;

        static ExceptionFormatter()
        {
            _ignoredProps = new String[]
            {
                "Data",
                "HelpLink",
                "InnerException",
                "Message",
                "Source",
                "StackTrace",
                "TargetSite"
            };
        }

        /// <summary>
        /// Constructs a string containing exception data from the specified
        /// exception.
        /// </summary>
        public static String ExceptionToString(Exception exception)
        {
            StringBuilder sb;
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }
            sb = new StringBuilder();
            try
            {
                AppendException(sb, exception);
            }
            catch (Exception)
            {
                // The formatting failed for some reason. This must not stop us
                // from creating a message so we try the standard formatting
                // instead.
                return exception.ToString();
            }
            return sb.ToString();
        }

        private static void AppendException(StringBuilder sb, Exception ex)
        {
            Type exType;
            PropertyInfo[] propertyInfos;
            Int32 i;
            PropertyInfo propertyInfo;
            String name;
            Object value;
            Exception innerEx;
            String helpLink;
            Exception[] exceptions;
            exType = ex.GetType();
            sb.Append(exType.FullName);
            sb.Append(": ");
            sb.AppendLine(ex.Message);
            if (ex.StackTrace != null)
            {
                sb.AppendLine(ex.StackTrace);
            }
            AppendDataProperty(sb, ex.Data);
            propertyInfos = exType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            for (i = 0; i < propertyInfos.Length; i++)
            {
                propertyInfo = propertyInfos[i];
                if (propertyInfo.CanRead)
                {
                    name = propertyInfo.Name;
                    if (IsToIgnoreProperty(name))
                    {
                        continue;
                    }
                    try
                    {
                        value = propertyInfo.GetValue(ex, null);
                    }
                    catch (TargetParameterCountException)
                    {
                        // The property was an indexer so we couldn't read it.
                        // Let's just move on (why would someone put an indexer
                        // in an exception I wonder...).
                        continue;
                    }
                    if (value == null)
                    {
                        continue;
                    }
                    if (value is String || value.GetType().IsPrimitive)
                    {
                        AppendOtherProperty(sb, name, value.ToString());
                    }
                    else if (value is Exception[])
                    {
                        exceptions = value as Exception[];
                        for (Int32 ei = 0; ei < exceptions.Length; ei++)
                        {
                            sb.Append("---> ");
                            AppendException(sb, exceptions[ei]);
                        }
                    }
                }
            }
            helpLink = ex.HelpLink;
            if (helpLink != null)
            {
                sb.Append("HelpLink=");
                sb.AppendLine(helpLink);
            }
            innerEx = ex.InnerException;
            if (innerEx != null)
            {
                sb.Append("---> ");
                AppendException(sb, innerEx);
            }
        }

        private static void AppendDataProperty(StringBuilder sb, IDictionary data)
        {
            Boolean none;
            IDictionaryEnumerator e;
            Object key;
            Object value;
            none = true;
            e = data.GetEnumerator();
            while (e.MoveNext())
            {
                key = e.Key;
                if (key is String || key.GetType().IsPrimitive)
                {
                    if ((key as String) == ErrorCode.EC_TRANSPORT_KEY)
                    {
                        continue;
                    }
                    value = e.Value;
                    if (value != null && (value is String || value.GetType().IsPrimitive))
                    {
                        if (none)
                        {
                            none = false;
                            sb.AppendLine("Data {");
                        }
                        AppendOtherProperty(sb, key.ToString(), value.ToString());
                    }
                }
            }
            if (!none)
            {
                sb.AppendLine("}");
            }
        }

        private static void AppendOtherProperty(StringBuilder sb, String name, String value)
        {
            // Appends a the name and value of a property with a value that is
            // a string or is convertible to a string. The conversion has already been
            // made, this method assume that no invalid arguments are sent to it.
            sb.Append(name);
            sb.Append('=');
            sb.AppendLine(value);
        }

        private static Boolean IsToIgnoreProperty(String name)
        {
            String[] ignoredProps;
            Int32 i;
            ignoredProps = _ignoredProps;
            for (i = 0; i < ignoredProps.Length; i++)
            {
                if (ignoredProps[i] == name)
                {
                    return true;
                }
            }
            return false;
        }
    }
}