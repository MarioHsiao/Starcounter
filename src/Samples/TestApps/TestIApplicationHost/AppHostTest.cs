
namespace TestIApplicationHost {
    using System;
    using System.Diagnostics;
    using Starcounter;
    using Starcounter.Hosting;
    
    public class Program : IApplicationHost {
        static Application application = null;
        
        public void HostApplication(Application app) {
            Trace.Assert(application == null);
            Trace.Assert(app != null);
            
            Console.WriteLine("Application host invoked on application: {0}", app);
            application = app;
        }
        
        static void Main() {
            var currentApp = Application.Current;
            if (application == null) {
              throw new Exception("Application host not properly invoked: application == null.");
            }
            if (application != currentApp) {
              throw new Exception("Inconsistency in application host: application != current applicaion.");
            }
            Console.WriteLine("Application host successfully invoked");
        }
    }
}

