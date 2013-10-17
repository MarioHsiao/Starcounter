
using Starcounter.Internal;
using StarcounterInternal.Bootstrap;

namespace scadminserver {
    class Program {
        static void Main(string[] args) {
            Diagnostics.WriteTimeStamp("SCADMINSERVER", "Started scadminserver Main()");
            Control.Main(args);
        }
    }
}