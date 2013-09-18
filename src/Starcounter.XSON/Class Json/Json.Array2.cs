
using Starcounter.Internal.XSON;
using Starcounter.Templates;
using System.Collections.Generic;
namespace Starcounter {
    partial class Json {

        internal void InternalClear() {
            int indexesToRemove;
            var app = this.Parent;
            TObjArr property = (TObjArr)this.Template;
            indexesToRemove = list.Count;
            for (int i = (indexesToRemove - 1); i >= 0; i--) {
                app.ChildArrayHasRemovedAnElement(property, i);
            }
            list.Clear();
        }

        public Json Add() {
            var elementType = ((TObjArr)this.Template).ElementType;
            Json x;
            if (elementType == null) {
                x = new Json();
            }
            else {
                x = (Json)elementType.CreateInstance(this);
            }

            //            var x = new App() { Template = ((TArr)this.Template).App };
            Add(x);
            return x;
        }


    }
}
