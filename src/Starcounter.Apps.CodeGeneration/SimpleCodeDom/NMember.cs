using Starcounter.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// Represents a property, a field or a function
    /// </summary>
    public class NProperty : NBase {
        public NClass Type { get; set; }
        public string MemberName {
            get {
                return Template.PropertyName;
            }
        }

        public NClass FunctionGeneric {
            get {
                if (Type is NListingXXXClass) {
                    return (Type as NListingXXXClass).NApp;
                }
                else if (Type is NApp) {
                    return Type;
                }
                return null;
            }
        }

        public Template Template { get; set; }

        public override string ToString() {
            string str = MemberName;
            if (FunctionGeneric != null) {
                str += "<" + FunctionGeneric.FullClassName + ">";
            }
            return Type.FullClassName + " " + str;
        }
    }
}
