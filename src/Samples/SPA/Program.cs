using Starcounter;
using Starcounter.Advanced;
using Starcounter.Internal;
using System.Diagnostics;

namespace SPA {
    class Program {
        static void Main(string[] args) {
     
            Debugger.Launch();

            Handle.GET("/emails", () =>
            {
                Master m = new Master() { View = "master.html" };
                Session.Data = m;
                return m;
            });

            Handle.GET("/emails/{?}", (int id) => {
                Master m = (Master)NodeX.GET("/emails");
                var page = new MailPage() { View = "email.html" };
                page.Title = "Hello there!";
                page.Content = "Email ID: " + id + ", session ID: " + Session.Current.SessionIdString;

                m.Focused = page;
                return page;
            });
        }
    }
}
