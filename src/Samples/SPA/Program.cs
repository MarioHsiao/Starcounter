using Starcounter;
using Starcounter.Internal;
using System.Diagnostics;

namespace SPA {
    class Program {
        static void Main(string[] args) {            

            AppsBootstrapper.Bootstrap(@".\s\SPA");
            Debugger.Launch();

<<<<<<< Updated upstream
            Handle.GET("/", (Request req) => {
                // TODO: Example code for redirection. Should probably be handled in a better way.
                return (new Node("127.0.0.1", 8080)).GET("/main.html", null, req);
            });
=======
//            Console.WriteLine("Should have registred a handler!");
>>>>>>> Stashed changes

            Handle.GET("/about", () => {
                return "<h1>Single bb Page Application in Starcounter.</h1>";
            });

            Handle.GET("/", () => {
                var master = new Master() {
                    UserID = "admin",
                    View="<div>{{UserId}}</div>"
                };
                Session.Data = master;
                return master;
            });


            Handle.GET("/page1", () => {
                Master master = Node.GET("/");
                master.Page = new Page1() {
                    View = "<div>{{FirstName}}</div>"
                };
                return master;
            });

        }
    }

    public class Node {
        public static dynamic GET(string uri) {
            return null;
        }
    }

}
