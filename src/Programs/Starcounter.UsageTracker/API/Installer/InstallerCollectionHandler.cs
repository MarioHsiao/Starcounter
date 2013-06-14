
namespace Starcounter.Programs.UsageTrackerApp.API.Installer {
    internal static class InstallerCollectionHandler {


        public static void Bootstrap(ushort port) {

            AbortCollectionHandler.Setup_POST(port);
            StartCollectionHandler.Setup_POST(port);
            ExecutingCollectionHandler.Setup_POST(port);
            FinishCollectionHandler.Setup_POST(port);
            EndCollectionHandler.Setup_POST(port);


        }

    }
}
