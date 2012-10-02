
using System;
using Starcounter.Templates.Interfaces;
#if CLIENT
using Starcounter.Client;
namespace Starcounter.Client.Template {
#else
using Starcounter.Templates;
namespace Starcounter {
#endif
    public abstract class Property : Template
#if IAPP
        , IValueTemplate
#endif
    {
        public override bool HasInstanceValueOnClient {
            get { return true; }
        }
    }
}
