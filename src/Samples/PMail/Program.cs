
using Starcounter;
using Starcounter.Internal;

class Program {
    static void Main(string[] args) {

        AppsBootstrapper.Bootstrap(@"C:\sc\level1\src\samples\PMail");

        Handle.POST("/add-demo-data", () => {
            Db.Transaction(() => {
                var inbox = new Mailbox() { Name = "Inbox" };
                new Mail() { Id = 123, Subject = "Hi there", Content = "How are you", Mailbox = inbox };
                new Mail() { Id = 124, Subject = "Buy diet pills", Content = "Guaranteed results.", Mailbox = inbox };
                new Mail() { Id = 125, Subject = "Business opportunity", Content = "Call me", Mailbox = inbox };
            });
            return 201;
        });

        Handle.GET("/", () => {
            Master m = new Master() { Html = "master.html" };
            Session.Data = m;
            return m;
        });

        Handle.GET("/pmail", () => {
            Master m = (Master)X.GET("/");
            PMail p = new PMail() { Html = "pmail.html" };
            p.Mailboxes = Db.SQL("SELECT m FROM Mailbox m");
            m.ApplicationPage = p;
            return p;
        });

        Handle.GET("/mailboxes/{?}", (string name) => {
            PMail p = (PMail)X.GET("/pmail");
            p.FocusedMailbox.Data = Db.SQL("SELECT m FROM Mailbox m WHERE Name=?", name).First;
            p.Mails = Db.SQL("SELECT e FROM Mail e WHERE Mailbox=?", p.FocusedMailbox.Data);
            return p;
        });

        Handle.GET("/mails/{?}", (int id) => {
            var mail = Db.SQL("SELECT e FROM Mail e WHERE Id=?", id).First;
            PMail p = (PMail)X.GET("/mailboxes/" + mail.Mailbox.Name);
            var page = new MailPage() {
                Html = "mail.html",
                Data = mail
            };
            p.FocusedMail = page;
            return page;
        });

        Handle.GET("/mails/new-email", () => {
            return null;
        });
    }
}