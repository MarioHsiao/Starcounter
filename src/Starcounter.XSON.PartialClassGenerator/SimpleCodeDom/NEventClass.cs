// ***********************************************************************
// <copyright file="NEventClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// Event classes (like the Input) class are used in the Handle functions
    /// defined by the user in the code behind to catch events. An example of
    /// an event class in the InputEvent class.
    /// </summary>
    public class NEventClass : NClass {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public NEventClass(DomGenerator gen)
            : base(gen) {
        }

        /// <summary>
        /// Gets or sets the N member.
        /// </summary>
        /// <value>The N member.</value>
        public NProperty NMember { get; set; }
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
        public override string ClassName {
            get { return NMember.MemberName; }
        }

        /// <summary>
        /// Gets the N template.
        /// </summary>
        /// <value>The N template.</value>
        public NTemplateClass NTemplate {
            get { return  ((NValueClass)NMember.Type).NTemplateClass; }
        }

        /// <summary>
        /// Gets the N app.
        /// </summary>
        /// <value>The N app.</value>
        public NAppClass NApp {
            get { return (NAppClass)NMember.Parent; }
        }


        /// <summary>
        /// Gets the inherits.
        /// </summary>
        /// <value>The inherits.</value>
        public override string Inherits {
            get {
                var str = EventName + "<";
                str += NApp.FullClassName;
                str += ", ";
                str += NTemplate.FullClassName;
                str += ", ";
                str += NMember.Type.ClassName;
                str += ">";
                return str;
            }
        }
    }

}