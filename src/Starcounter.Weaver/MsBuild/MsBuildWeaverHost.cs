
using System;
using System.Collections;
using System.Collections.Generic;

namespace Starcounter.Weaver.MsBuild
{
    internal class MsBuildWeaverHost : IWeaverHost
    {
        const string exceptionSourceIdentity = "64B54F54-869E-4371-A4C1-B33D2AB73EF2";

        List<string> errors = new List<string>();
        int maxErrorCount = int.MaxValue;

        // TODO: Move to Error/exception propagation helpers
        public static bool IsOriginatorOf(Exception e)
        {
            return e.Source == exceptionSourceIdentity; 
        }

        // TODO: Move to Error/exception propagation helpers
        public static IEnumerable<string> EnumerateErrorsFrom(Exception e)
        {
            var result = new List<string>(e.Data.Count);
            foreach (DictionaryEntry de in e.Data)
            {
                result.Add(de.Value as string);
            }

            return result;
        }

        // TODO: Move to Error/exception propagation helpers
        public static bool ContainOnlyWarnings(Exception e)
        {
            // Let's use this to output warnings but still allow
            // weaver result to be true
            // TODO:
            return e.Source == exceptionSourceIdentity;
        }

        public void OnWeaverDone(bool result)
        {
            if (!result)
            {
                RaiseExceptionFromErrors();
            }
        }

        public void OnWeaverSetup(WeaverSetup setup)
        {
            if (setup.HostProperties.ContainsKey("MsBuildWeaverHost.MaxErrors"))
            {
                maxErrorCount = int.Parse(setup.HostProperties["MsBuildWeaverHost.MaxErrors"]);
            }
        }

        public void OnWeaverStart()
        {
        }

        public void WriteDebug(string message, params object[] parameters)
        {
        }

        public void WriteError(uint code, string message, params object[] parameters)
        {
            errors.Add($"{code} - {string.Format(message, parameters)}");

            if (errors.Count >= maxErrorCount)
            {
                RaiseExceptionFromErrors();
            }
        }

        public void WriteInformation(string message, params object[] parameters)
        {
        }

        public void WriteWarning(string message, params object[] parameters)
        {
        }

        void RaiseExceptionFromErrors()
        {
            var e = new Exception();
            e.Source = exceptionSourceIdentity;

            // TODO: Figure out a good storage of errors
            foreach (var error in errors)
            {
                e.Data.Add(Guid.NewGuid().ToString(), error);
            }

            throw e;
        }
    }
}