using Starcounter.Templates;

namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// Represents a property, a field or a function
    /// </summary>
    public class AstProperty : AstBase {
        private string memberName;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstProperty(Gen2DomGenerator gen)
            : base(gen) {
            GenerateAccessorProperty = true;
        }

//        public AstInstanceClass ElementTypeProperty { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public AstClass Type { get; set; }
        /// <summary>
        /// Gets the name of the member.
        /// </summary>
        /// <value>The name of the member.</value>
        public string MemberName {
            get {
                if (memberName != null)
                    return memberName;
                return Template.PropertyName;
            }
            set {
                memberName = value;
            }
        }

		/// <summary>
		/// Returns the name of a backing field if this property is a value property.
		/// </summary>
		public string BackingFieldName {
			get {
				if (Template is TValue)
					return "__bf__" + MemberName + "__";
				return null;
			}
		}

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        public Template Template { get; set; }

        /// <summary>
        /// If set to true, a property to get and set the value will be generated. If false
        /// no accessor property will be added.
        /// </summary>
        public bool GenerateAccessorProperty { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString() {
            string str = MemberName;
//            if (FunctionGeneric != null) {
//                str += "<" + FunctionGeneric.FullClassName + ">";
//            }
            return "NMEMBER " + Type.ClassSpecifierWithoutOwners + " " + str;
        }
    }
}
