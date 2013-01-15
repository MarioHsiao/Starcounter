using System;
using Starcounter;
using Starcounter.Internal;

namespace HelloApps {
    partial class Master : App {
        static void Main(string[] args) {

            AppsBootstrapper.Bootstrap(8080);

            GET("/master", () => {
                return new Master() {
                    View = "Master.html",
                    SomeNo = 146,
                    Message = "Click the button!"
                };
            });


            GET("/empty", () => {
                return "empty";
            });

        }

        void Handle(Input.TheButton input) {
            this.Message = "I clicked the button!";
        }
    }
}
