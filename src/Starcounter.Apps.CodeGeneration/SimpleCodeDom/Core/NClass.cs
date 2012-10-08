using Starcounter.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal.Application.CodeGeneration {
    public abstract class NClass : NBase {

        public abstract string ClassName { get; }
        public abstract string Inherits { get; }
        public NClass Generic { get; set; }

        public bool IsPartial { get; set; }
        public bool IsStatic { get; set; }

        public virtual string FullClassName {
            get {
                var str = ClassName;
                if (Generic != null) {
                    str += "<" + Generic.FullClassName + ">";
                }
                if (Parent == null || !(Parent is NClass)) {
                    return str;
                }
                else {
                    return (Parent as NClass).FullClassName + "." + str;
                }
            }
        }

        public override string ToString() {
            if (ClassName != null) {
                var str = "class " + ClassName;
                if (Inherits != null) {
                    str += ":" + Inherits;
                }
                return str;
            }
            throw new Exception();
        }




    }
}
