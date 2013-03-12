
namespace star {

    /*
     * A set of strongly typed types that we use to map data
     * interchange schemata between clients (in this case, star.exe)
     * and the primary, corresponding constructs in the admin
     * server (Starcounter.Adminstrator).
     * 
     * This will change to something more accurate once we have
     * decided on how to promote and use this kind of contracting.
     * See forum discussion at:
     * http://www.starcounter.com/forum/showthread.php?2492-Sharing-of-REST-JSON-data-and-schemata
     */

    internal sealed class ExecRequest {
        public string ExecutablePath { get; set; }
        public string CommandLineString { get; set; }
        public string ResourceDirectoriesString { get; set; }
        public bool NoDb { get; set; }
        public bool LogSteps { get; set; }
        public bool CanAutoCreateDb { get; set; }
    }

    internal sealed class ExecResponse201 {
        public string DatabaseUri { get; set; }
        public int DatabaseHostPID { get; set; }
        public bool DatabaseCreated { get; set; }
    }
}