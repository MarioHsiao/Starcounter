
using System;

namespace Starcounter.UnitTesting
{
    /// <summary>
    /// Framework agnostic result of running a configured set of
    /// sets, targeting one application in a specified database, but
    /// possibly expanding over several test assemblies. Correspond
    /// to a single invocation of running a given host.
    /// </summary>
    [Serializable]
    public class TestResult
    {
        /// <summary>
        /// The database tests executed in.
        /// </summary>
        public string Database { get; internal set; }

        /// <summary>
        /// The target application of tests.
        /// </summary>
        public string Application { get; internal set; }

        /// <summary>
        /// The time the host was asked to start executing tests.
        /// </summary>
        public DateTime Started { get; internal set; }

        /// <summary>
        /// The time the host responded with a result.
        /// </summary>
        public DateTime Finished { get; internal set; }

        /// <summary>
        /// Number of succeded tests.
        /// </summary>
        public int TestsSucceeded { get; set; }

        /// <summary>
        /// Number of tests that failed.
        /// </summary>
        public int TestsFailed { get; set; }

        /// <summary>
        /// Gets or sets the number of tests that were skipped.
        /// </summary>
        public int TestsSkipped { get; set; }

        public byte[] ToBytes()
        {
            return SimpleSerializer.SerializeToByteArray(this);
        }

        public static TestResult FromBytes(byte[] bytes)
        {
            return SimpleSerializer.DeserializeFromByteArray<TestResult>(bytes);
        }
    }
}
