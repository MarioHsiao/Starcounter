
#if CLIENT
using Starcounter.Client.Template;
#else
using Starcounter.Templates;
#endif

using Starcounter.Templates.Interfaces;
namespace Starcounter.Client {
    public class AppFactory : IAppFactory {

        public IApp CreateApp() {
            return new App();
        }

        public IAppTemplate CreateAppTemplate() {
            return new AppTemplate();
        }

        public IStringTemplate CreateStringTemplate() {
            return new StringProperty();
        }

        public IDoubleTemplate CreateDoubleTemplate() {
            return new DoubleProperty();
        }

        public IBoolTemplate CreateBoolTemplate() {
            return new BoolProperty();
        }
    }
}
