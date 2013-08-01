

using Starcounter;
using Starcounter.Internal;
using System.Diagnostics;

class Program {
    static void Main(string[] args) {

        AppsBootstrapper.Bootstrap(@"Z:\Dropbox\Puppets");

        Debugger.Launch();

        Handle.GET("/emails", () => {
            Master m = new Master() { View = "master.html" };
            var x = m.Emails.Add();
            x.Title = "Hi there";
            x = m.Emails.Add();
            x.Title = "Buy viagra";
            x = m.Emails.Add();
            x.Title = "Business opportunity in Nigeria";
            x.Content = "My uncle died and somehow you're getting money from this. Good, huh?";
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
