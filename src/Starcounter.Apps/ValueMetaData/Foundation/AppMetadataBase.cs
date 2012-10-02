

namespace Starcounter.Templates {
    public class AppMetadataBase  {

        public AppMetadataBase(App app, Template template) {
            _App = app;
            _Template = template;
        }

        private App _App;
        private Template _Template;

        public App App { get { return _App; } }
        public Template Template { get { return _Template; } }

    }
}

