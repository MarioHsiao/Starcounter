

using Starcounter.Templates;
using System;
using System.Collections;
namespace Starcounter {
    partial class Json {

        /// <summary>
        /// Returns the depth of this Container.
        /// </summary>
        /// <value>The index path depth.</value>
        internal int IndexPathDepth {
            get {
                if (_cachePathDepth == -1) {
                    _cachePathDepth = (Parent == null) ? 0 : Parent.IndexPathDepth + 1;
                }
                return _cachePathDepth;
            }
        }

        /// <summary>
        /// Returns the depth of any child for this Container. Since all children
        /// will have the same depth, a specific childinstance is not needed.
        /// </summary>
        /// <value>The child path depth.</value>
        internal int ChildPathDepth {
            get { return IndexPathDepth + 1; }
        }

        /// <summary>
        /// Returns an array of indexes starting from the rootapp on how to get
        /// to this specific instance.
        /// </summary>
        /// <value>The index path.</value>
        internal Int32[] IndexPath {
            get {
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
        /// <param name="template">The template.</param>
        /// <returns>Int32[][].</returns>
        internal Int32[] IndexPathFor(Template template) {
            Int32[] path = new Int32[ChildPathDepth];
            path[path.Length - 1] = template.TemplateIndex;
            FillIndexPath(path, path.Length - 2);
            return path;
        }

        /// <summary>
        /// In order to support Json pointers (TODO REF), this method is called
        /// recursively to fill in a list of relative pointers from the root to
        /// a given node in the Json like tree (the Obj/Arr tree).
        /// </summary>
        /// <param name="path">The patharray to fill</param>
        /// <param name="pos">The position to fill</param>
        internal void FillIndexPath(Int32[] path, Int32 pos) {
            if (IsArray) {
                path[pos] = Template.TemplateIndex;
                Parent.FillIndexPath(path, pos - 1);
            }
            else {
                if (Parent != null) {
                    if (Parent.IsArray) {
                        if (_cacheIndexInArr == -1) {
                            _cacheIndexInArr = ((IList)Parent).IndexOf(this);
                        }
                        path[pos] = _cacheIndexInArr;
                    }
                    else {
                        path[pos] = IndexInParent;
                    }
                    Parent.FillIndexPath(path, pos - 1);
                }
            }
        }
    }
}
