using System;
using Starcounter;

namespace SimpleIndependentTests {
    class SimpleIndependentTests {

        static Int32 Main() {

            Int32 errCode;

            errCode = SchedulingPerfTest.Run();
            if (0 != errCode) {
                return errCode;
            }

            errCode = TestSelfPerformance.Run();
            if (0 != errCode) {
                return errCode;
            }

            errCode = HandlerDeletionTests.Run();
            if (0 != errCode) {
                return errCode;
            }

            // TODO: The following test is not working yet 
            // (but it shows how app name should work in non-sc thread when its fixed).
            //errCode = TestAppName.Run();
            if (0 != errCode) {
                return errCode;
            }

            return 0;
        }
    }
}