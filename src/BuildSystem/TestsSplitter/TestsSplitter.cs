using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Starcounter.TestFramework;

namespace TestsSplitter
{
class TestsSplitter
{
    /// <summary>
    /// Used for logging test messages.
    /// </summary>
    static TestLogger logger = null;

    /// <summary>
    /// Number of simultaneous client instances.
    /// </summary>
    Int32 NumOfInstances = 10;

    /// <summary>
    /// Name of the sub-test.
    /// </summary>
    String TestName = "TestsSplitter";

    static void Main(String[] args)
    {
        TestsSplitter testSplitter = new TestsSplitter();

        // Checking any arguments supplied.
        if (args.Length > 0)
        {
            Int32 _numInstances;

            // Taking the name of the test.
            testSplitter.TestName = args[0];

            // Checking if number of instances is specified.
            if (Int32.TryParse(args[1], out _numInstances))
            {
                testSplitter.NumOfInstances = _numInstances;
            }
        }

        // Creating logging system.
        logger = new TestLogger(testSplitter.TestName, true);

        logger.Log(testSplitter.TestName + " successfully finished!", TestLogger.LogMsgType.MSG_SUCCESS);
    }
}
}
