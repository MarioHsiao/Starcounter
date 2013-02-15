// ***********************************************************************
// <copyright file="NTemplateClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Collections.Generic;

namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// Class NTemplateClass
    /// </summary>
    public abstract class NTemplateClass : NClass {

        /// <summary>
        /// The string property
        /// </summary>
        public static TString TString = new TString();
        /// <summary>
        /// The int property
        /// </summary>
        public static TLong TLong = new TLong();
        /// <summary>
        /// The decimal property
        /// </summary>
        public static TDecimal TDecimal = new TDecimal();
        /// <summary>
        /// The app template
        /// </summary>
        public static TApp TApp = new TApp();
        /// <summary>
        /// The double property
        /// </summary>
        public static TDouble TDouble = new TDouble();
        /// <summary>
        /// The bool property
        /// </summary>
        public static TBool TBool = new TBool();
        /// <summary>
        /// The action property
        /// </summary>
        public static ActionProperty ActionProperty = new ActionProperty();

        /// <summary>
        /// The classes
        /// </summary>
        public static Dictionary<Template, NTemplateClass> Classes = new Dictionary<Template, NTemplateClass>();

        /// <summary>
        /// The template
        /// </summary>
        public Template Template;

        /// <summary>
        /// The _ N value class
        /// </summary>
        private NValueClass _NValueClass;

        /// <summary>
        /// Gets or sets the N value class.
        /// </summary>
        /// <value>The N value class.</value>
        public NValueClass NValueClass {
            get {
                if (_NValueClass != null)
                    return _NValueClass;
                return NValueClass.Classes[this.Template];
            }
            set { _NValueClass = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public NProperty NValueProperty;

        /// <summary>
        /// Gets or sets the N metadata class.
        /// </summary>
        /// <value>The N metadata class.</value>
        public NMetadataClass NMetadataClass { get; set; }

        /// <summary>
        /// Initializes static members of the <see cref="NTemplateClass" /> class.
        /// </summary>
        static NTemplateClass() {
            Classes[TString] = new NPropertyClass {Template = TString};
            Classes[TLong] = new NPropertyClass {Template = TLong};
            Classes[TDecimal] = new NPropertyClass {Template = TDecimal};
            Classes[TDouble] = new NPropertyClass {Template = TDouble};
            Classes[TBool] = new NPropertyClass {Template = TBool};
            Classes[ActionProperty] = new NPropertyClass {Template = ActionProperty};
            Classes[TApp] = new NTAppClass {Template = TApp};
        }


        /// <summary>
        /// Finds the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>NTemplateClass.</returns>
        public static NTemplateClass Find(Template template) {
           // template = GetPrototype(template);
            return NTemplateClass.Classes[template];
        }

        /// <summary>
        /// Gets the prototype.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>Template.</returns>
        internal static Template GetPrototype(Template template) {
            if (template is TString) {
                return TString;
            }
            else if (template is TLong) {
                return TLong;
            }
            else if (template is TDouble) {
                return TDouble;
            }
            else if (template is TDecimal) {
                return TDecimal;
            }
            else if (template is TBool) {
                return TBool;
            }
            else if (template is ActionProperty) {
                return ActionProperty;
            }
            return template;
        }

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public override string ClassName {
            get {
                return Template.GetType().Name;
            }
        }
    }
}
