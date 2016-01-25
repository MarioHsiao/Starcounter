using System;
using Starcounter;

namespace SimpleIndependentTests {
    class SimpleIndependentTests {

        static Int32 Main() {

            Int32 errCode;

            errCode = TestSelfPerformance.Run();
            if (0 != errCode) {
                return errCode;
            }

            //errCode = TestAppName.Run();
            if (0 != errCode) {
                return errCode;
            }

            return 0;
        }
    }
}