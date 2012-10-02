
using Starcounter.Templates;
namespace Starcounter.Internal.Application.CodeGeneration {
    
    /// <summary>
    /// Event classes (like the Input) class are used in the Handle functions 
    /// defined by the user in the code behind to catch events. An example of
    /// an event class in the InputEvent class.
    /// </summary>
    public class NEventClass : NClass {

        public NProperty NMember { get; set; }
        public NClass NApp { get; set; }
        public string EventName { get; set; }

        public override string ClassName {
            get { return NMember.MemberName; }
        }
        public override string Inherits {
            get {
                var str = EventName + "<";
                str += NApp.FullClassName;
                str += ", ";
                str += NMember.Type.FullClassName;
                str += ">";
                return str;
            }
        }
    }

}