using System;
using Starcounter;
using Starcounter.Internal;

namespace CoreSystemApp {
    class Program {
        static void Main() {
            if (StarcounterEnvironment.RegisterHTMLCompositions) {
                Starcounter.HTMLComposition.Register();
            }            
        } 
    }
}