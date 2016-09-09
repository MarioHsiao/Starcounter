using System;
using Starcounter.Templates;

namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// 
    /// </summary>
    public class AstPrimitiveType : AstInstanceClass {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstPrimitiveType(Gen2DomGenerator gen)
            : base(gen) {
        }

        public override bool IsPrimitive {
            get {
                var type = NTemplateClass.Template.InstanceType;
				if (type == null)
					return true;
                return type.IsPrimitive || type == typeof(string) || type == typeof(Decimal);
            }
        }
        
        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public override string ClassStemIdentifier
        {
            get
            {
                if (NTemplateClass.Template is TTrigger)
                    return "Action";

                var type = NTemplateClass.Template.InstanceType;
                if (type == typeof(Int64))
                {
                    return "long";
                }
                else if (type == typeof(Boolean))
                {
                    return "bool";
                }
                else if (type == typeof(string)) {
                    return "string";
                }
                return type.Name;
            }
        }

        public override string Namespace {
            get {
                var type = NTemplateClass.Template.InstanceType;
                if (type.IsPrimitive)
                    return null;
                return type.Namespace;
            }
            set {
                throw new Exception();
            }
        }

        public override string GlobalClassSpecifier {
            get {
                return ClassStemIdentifier;
            }
        }
    }
}
