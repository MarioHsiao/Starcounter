using Starcounter;
using Starcounter.Advanced;
using Starcounter.Internal;

namespace SPA {
    class Program {
        static void Main(string[] args) {
            AppsBootstrapper.Bootstrap(@".\s\SPA");

            Handle.GET("/", (Request req) => {
                // TODO: Example code for redirection. Should probably be handled in a better way.
                return (new Node("127.0.0.1", 8080)).GET("/main.html", null, req);
            });

            Handle.GET("/about", () => {
                return "Single Page Application in Starcounter.";
            });

            Handle.POST("/message", () => {
                var msg = new TestMsg() {
                    Name = "First Test!",
                    Value = "Hello SPA!"
                };
                Session.Data = msg;
                return msg;
            });
        }
    }
}