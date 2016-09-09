using Starcounter.Internal;
using Starcounter.Templates;

namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// Event classes (like the Input) class are used in the Handle functions
    /// defined by the user in the code behind to catch events. An example of
    /// an event class in the InputEvent class.
    /// </summary>
    public class AstEventClass : AstClass {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstEventClass(Gen2DomGenerator gen)
            : base(gen) {
        }

        /// <summary>
        /// Gets or sets the N member.
        /// </summary>
        /// <value>The N member.</value>
        public AstProperty NMember { get; set; }
        //public NClass NApp { get; set; }
        //public NClass NTemplate { get; set; }
        /// <summary>
        /// Gets or sets the name of the event.
        /// </summary>
        /// <value>The name of the event.</value>
        public string EventName { get; set; }

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public override string ClassStemIdentifier {
            get { return NMember.MemberName; }
        }

        /// <summary>
        /// Gets the N template.
        /// </summary>
        /// <value>The N template.</value>
        public AstTemplateClass NTemplate {
            get { return  ((AstInstanceClass)NMember.Type).NTemplateClass; }
        }

        /// <summary>
        /// Gets the N app.
        /// </summary>
        /// <value>The N app.</value>
        public AstJsonClass NApp {
            get { return (AstJsonClass)NMember.Parent; }
        }
        
        /// <summary>
        /// Gets the inherits.
        /// </summary>
        /// <value>The inherits.</value>
        public override string Inherits {
            get {
                var str = EventName + "<";
                str += NApp.GlobalClassSpecifier;
                str += ", ";

                if (NTemplate is AstSchemaClass) {
                    // this is an event for a single primitive value. Need to take the inherited class as generic parameter for the Input.
                    str += NTemplate.InheritedClass.GlobalClassSpecifier;
                } else {
                    str += NTemplate.GlobalClassSpecifier;
                }

                // Triggers have no valuetype, and uses another generic input class.
                if (!(NMember.Template is TTrigger)) {
                    str += ", ";

                    if (NMember.Type is AstJsonClass) {
                        // this is an event for a single primitive value. Need to take template instancetype as generic parameter for the Input.
                        str += HelperFunctions.GetGlobalClassSpecifier(NMember.Template.InstanceType, true);
                    } else {
                        str += NMember.Type.GlobalClassSpecifier;
                    }
                }
                
                str += ">";
                return str;
            }
        }
    }

}