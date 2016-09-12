using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.UnitTesting
{
    /// <summary>
    /// Framework agnostic result of running a configured set of
    /// sets, targeting one application in a specified database, but
    /// possibly expanding over several test assemblies.
    /// </summary>
    public class TestResult
    {
        /// <summary>
        /// The database tests executed in.
        /// </summary>
        public string Database;

        /// <summary>
        /// The target application of tests.
        /// </summary>
        public string Application;

        /// <summary>
        /// The time the configured set of tests started.
        /// </summary>
        public DateTime Started;

        /// <summary>
        /// The time tests finished.
        /// </summary>
        public DateTime Finished;
    }
}
