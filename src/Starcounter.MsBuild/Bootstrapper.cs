
using Modules;
namespace Starcounter.Internal.MsBuild {
    internal class Bootstrapper {

        internal static bool initialized = false;

        internal static void Bootstrap () {
            if (!initialized) {
                initialized = true;
                Starcounter_XSON_JsonByExample.Initialize();
            }
        }

    }
}
