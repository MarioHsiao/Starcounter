
using Starcounter;
using Starcounter.Internal;
using System.Diagnostics;

class Program {
    static void Main(string[] args) {

        AppsBootstrapper.Bootstrap(@"Z:\Dropbox\Puppets");
        Debugger.Launch();

        Handle.POST("/add-demo-data", () => {
            Db.Transaction(() => {

                new Email() {
                    Uri = "/emails/123",
                    Title = "Hi there",
                    Content = "How are you"
                };
                new Email() {
                    Uri = "/emails/124",
                    Title = "Buy viagra",
                    Content = "It's good for you"
                };
                new Email() {
                    Uri = "/emails/125",
                    Title = "Business opportunity in Nigeria",
                    Content = "My uncle died and somehow you're getting money from this. Good, huh?"
                };

            });
            return 201;
        });

        Handle.GET("/emails", () => {
            Master m = new Master() { View = "master.html" };
            m.Emails = Db.SQL("SELECT e FROM Emails e");
            return m;
        });

        Handle.GET("/emails/{?}", (string id) => {
            Master m = (Master)NodeX.GET("/emails");
            var page = new MailPage() { 
                View = "email.html",
                Data = Db.SQL("SELECT e FROM Emails e WHERE Uri=?",id).First
            };
            m.Focused = page;
            return page;
        });
    }
}
