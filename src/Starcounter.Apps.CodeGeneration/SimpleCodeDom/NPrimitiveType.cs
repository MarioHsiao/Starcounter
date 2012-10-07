

using System;

namespace Starcounter.Internal.Application.CodeGeneration {

    public class NPrimitiveType : NValueClass {
        public override string Inherits {
            get { throw new NotImplementedException(); }
        }

        public override string ClassName {
            get {
                var type = NTemplateClass.Template.InstanceType;
                if (type == typeof(Int32)) {
                    return "int";
                }
                return type.Name;
            }
        }
    }
}
