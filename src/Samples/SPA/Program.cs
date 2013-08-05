
using Starcounter;
using Starcounter.Internal;
using System.Diagnostics;

class Program {
    static void Main(string[] args) {

        AppsBootstrapper.Bootstrap(@"z:\Dropbox\Puppets");
        Debugger.Launch();

        Handle.POST("/add-demo-data", () => {
            Db.Transaction(() => {

                new Email() {
                    Id = "123",
                    Title = "Hi there",
                    Content = "How are you"
                };
                new Email() {
                    Id = "124",
                    Title = "Buy viagra",
                    Content = "It's good for you"
                };
                new Email() {
                    Id = "125",
                    Title = "Business opportunity in Nigeria",
                    Content = "My uncle died and somehow you're getting money from this. Good, huh?"
                };

            });
            return 201;
        });

        Handle.GET("/emails", () => {
            Master m = new Master() { Html = "master.html" };
            Session.Data = m;
            m.Transaction2 = new Transaction();
            m.Emails = Db.SQL("SELECT e FROM Email e");
            return m;
        });

        Handle.GET("/emails/{?}", (string id) => {
            Master m = (Master)X.GET("/emails");
            var page = new MailPage() { 
                Html = "email.html",
                Data = Db.SQL("SELECT e FROM Email e WHERE Id=?",id).First
            };
            m.Focused = page;
            return page;
        });
    }
}
