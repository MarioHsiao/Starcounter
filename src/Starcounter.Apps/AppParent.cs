
using Starcounter.Templates.Interfaces;
using System;

#if CLIENT
using Starcounter.Client.Template;
namespace Starcounter.Client {
using Starcounter.Template;
#else
using Starcounter.Templates;
namespace Starcounter {
#endif

    /// <summary>
    /// Base class for App and AppList instances.
    /// </summary>
    public abstract class AppNode : RequestHandler
#if IAPP
        , IAppNode
#endif
    {


        internal ParentTemplate _Template;

        public IParentTemplate Template {
            set {
                if (_Template != null) {
                    throw new Exception("Template is already set for App. Cannot change template once it is set");
                }
                _Template = (ParentTemplate)value;
                _Template.Sealed = true;
#if QUICKTUPLE
                _InitializeValues();
#endif
                if (this is App) {
                    ((App)this).CallInit();
                }
            }
            get {
                return _Template;
            }
        }

#if QUICKTUPLE
        protected virtual void _InitializeValues() {
        }

#endif
        internal virtual void OnSetParent(AppNode child) {
            child._parent = this;
        }

        internal AppNode _parent;

        public AppNode Parent {
            get {
                return _parent;
            }
            set {
                if (_parent != null && _parent != value) {
                    throw new Exception("Cannot change parent in Apps");
                }
                value.OnSetParent(this);
            }
        }

        IAppNode IAppNode.Parent {
            get {
                return Parent;
            }
            set {
                Parent = (AppNode)value;
            }
        }

    }
}
