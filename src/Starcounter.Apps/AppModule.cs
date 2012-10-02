

namespace Starcounter.Internal {
    internal class AppModule {

        internal IExeModule ExeModule;

        internal AppModule(IExeModule exeModule) {
            ExeModule = exeModule;
        }

    }
}
