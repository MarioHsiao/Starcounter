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
        public static StringProperty StringProperty = new StringProperty();
        /// <summary>
        /// The int property
        /// </summary>
        public static IntProperty IntProperty = new IntProperty();
        /// <summary>
        /// The decimal property
        /// </summary>
        public static DecimalProperty DecimalProperty = new DecimalProperty();
        /// <summary>
        /// The app template
        /// </summary>
        public static AppTemplate AppTemplate = new AppTemplate();
        /// <summary>
        /// The double property
        /// </summary>
        public static DoubleProperty DoubleProperty = new DoubleProperty();
        /// <summary>
        /// The bool property
        /// </summary>
        public static BoolProperty BoolProperty = new BoolProperty();
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
            Classes[StringProperty] = new NPropertyClass {Template = StringProperty};
            Classes[IntProperty] = new NPropertyClass {Template = IntProperty};
            Classes[DecimalProperty] = new NPropertyClass {Template = DecimalProperty};
            Classes[DoubleProperty] = new NPropertyClass {Template = DoubleProperty};
            Classes[BoolProperty] = new NPropertyClass {Template = BoolProperty};
            Classes[ActionProperty] = new NPropertyClass {Template = ActionProperty};
            Classes[AppTemplate] = new NAppTemplateClass {Template = AppTemplate};
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
            if (template is StringProperty) {
                return StringProperty;
            }
            else if (template is IntProperty) {
                return IntProperty;
            }
            else if (template is DoubleProperty) {
                return DoubleProperty;
            }
            else if (template is DecimalProperty) {
                return DecimalProperty;
            }
            else if (template is BoolProperty) {
                return BoolProperty;
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
