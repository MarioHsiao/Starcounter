using System;
using Starcounter;

namespace SimpleIndependentTests {
    class SimpleIndependentTests {

        static Int32 Main() {

            Int32 errCode = TestAppName.Run();
            if (0 != errCode) {
                return errCode;
            }

            return 0;
        }
    }
}