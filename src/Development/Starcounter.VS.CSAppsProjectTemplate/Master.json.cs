using System;
using Starcounter;

namespace $safeprojectname$ {
    partial class Master : App {
        static void Main(string[] args) {
            GET("/master", () => {
                return new Master(){ 
                    View = "Master.html", 
                    SomeNo = 146,
                    Message= "Click the button!"
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
