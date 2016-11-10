
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Starcounter.Weaver.MsBuild
{
    internal class MsBuildWeaverHost : IWeaverHost
    {
        const string exceptionSourceIdentity = "64B54F54-869E-4371-A4C1-B33D2AB73EF2";
        
        List<ErrorOrWarning> errorsAndWarnings = new List<ErrorOrWarning>();

        int maxErrorCount = int.MaxValue;

        public static bool IsOriginatorOf(Exception e)
        {
            return e.Source == exceptionSourceIdentity; 
        }

        public static IEnumerable<ErrorOrWarning> DeserializeErrorsAndWarnings(Exception e)
        {
            var result = new List<ErrorOrWarning>(e.Data.Count);
            foreach (DictionaryEntry de in e.Data)
            {
                result.Add(ErrorOrWarning.Deserialize(de.Value as string));
            }
            return result;
        }
        
        public void OnWeaverDone(bool result)
        {
            if (!result)
            {
                var containReportedErrors = errorsAndWarnings.Any(ew => !ew.IsWarning);
                if (!containReportedErrors)
                {
                    var e = new ErrorOrWarning();
                    e.ErrorCode = Error.SCERRUNSPECIFIED;
                    e.Message = ErrorCode.ToMessage(Error.SCERRUNSPECIFIED, 
                        $"IWeaverHost.OnWeaverDone(false) invoked by no reported errors");
                    errorsAndWarnings.Add(e);
                }

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

            var error = new ErrorOrWarning();
            error.ErrorCode = code;
            error.Message = string.Format(message, parameters);

            errorsAndWarnings.Add(error);
            var errorCount = errorsAndWarnings.Count(e => !e.IsWarning);
            
            if (errorCount >= maxErrorCount)
            {
                RaiseExceptionFromErrors();
            }
        }

        public void WriteInformation(string message, params object[] parameters)
        {
        }

        public void WriteWarning(string message, params object[] parameters)
        {
            var error = new ErrorOrWarning();
            error.ErrorCode = 0;
            error.Message = string.Format(message, parameters);

            errorsAndWarnings.Add(error);
        }

        void RaiseExceptionFromErrors()
        {
            var e = new Exception();
            e.Source = exceptionSourceIdentity;

            int id = 0;
            foreach (var errorOrWarning in errorsAndWarnings)
            {
                e.Data.Add(id, errorOrWarning.Serialize());
                id++;
            }
            
            throw e;
        }
    }
}