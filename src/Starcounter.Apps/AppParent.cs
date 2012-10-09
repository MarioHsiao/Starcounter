
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

        /// <summary>
        /// Contains the depth of this AppNode. Used when creating the indexpath.
        /// </summary>
        private Int32 _cachePathDepth = -1;

        /// <summary>
        /// Returns the depth of this AppNode.
        /// </summary>
        internal int IndexPathDepth
        {
            get
            {
                if (_cachePathDepth == -1)
                {
                    _cachePathDepth = (Parent == null) ? 0 : Parent.IndexPathDepth + 1;
                }
                return _cachePathDepth;
            }
        }

        /// <summary>
        /// Returns the depth of any child for this AppNode. Since all children
        /// will have the same depth, a specific childinstance is not needed.
        /// </summary>
        internal int ChildPathDepth
        {
            get { return IndexPathDepth + 1; }
        }

        /// <summary>
        /// Returns an array of indexes starting from the rootapp on how to get
        /// to this specific instance.
        /// </summary>
        public Int32[] IndexPath
        {
            get
            {
                Int32[] ret = new Int32[IndexPathDepth];
                //ret[ret.Length - 1] = 
                FillIndexPath(ret, ret.Length - 2);
                return ret;
            }
        }

        /// <summary>
        /// Returns an array of indexes starting from the rootapp on how to get
        /// the instance of the specified template.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        internal Int32[] IndexPathFor(Template template)
        {
            Int32[] path = new Int32[ChildPathDepth];
            path[path.Length - 1] = template.Index;
            FillIndexPath(path, path.Length - 2);
            return path;
        }

        internal virtual void FillIndexPath(Int32[] path, Int32 pos)
        {
            path[pos] = Template.Index;
            Parent.FillIndexPath(path, pos - 1);
        }
    }
}
