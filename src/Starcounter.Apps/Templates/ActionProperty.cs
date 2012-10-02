
using Starcounter.Templates.Interfaces;
#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

    public class ActionProperty : Template
#if IAPP
        , IActionTemplate
#endif
    {
        public override bool HasInstanceValueOnClient {
            get { return true; }
        }

        public override string JsonType {
            get { return "function"; }
        }

        public string OnRun { get; set; }

        public override object CreateInstance(AppNode parent) {
            return false;
        }
    }
}
