using Starcounter;
using Starcounter.Advanced;

namespace SPA {
    class Program {
        static void Main(string[] args) {
            Handle.GET("/", (Request req) => {
                return Node.LocalhostSystemPortNode.GET("/main.html", null, req);
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