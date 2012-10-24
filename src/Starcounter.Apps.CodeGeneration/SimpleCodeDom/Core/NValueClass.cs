// ***********************************************************************
// <copyright file="NValueClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System.Collections.Generic;
namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// Class NValueClass
    /// </summary>
    public abstract class NValueClass : NClass {

        /// <summary>
        /// Gets or sets the N template class.
        /// </summary>
        /// <value>The N template class.</value>
        public NTemplateClass NTemplateClass { get; set; }

        /// <summary>
        /// The classes
        /// </summary>
        public static Dictionary<Template, NValueClass> Classes = new Dictionary<Template, NValueClass>();

        /// <summary>
        /// Finds the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>NValueClass.</returns>
        public static NValueClass Find(Template template) {
            template = NTemplateClass.GetPrototype(template);
            return NValueClass.Classes[template];
        }

        /// <summary>
        /// Initializes static members of the <see cref="NValueClass" /> class.
        /// </summary>
        static NValueClass() {
            Classes[NTemplateClass.StringProperty] = new NPrimitiveType { NTemplateClass = NTemplateClass.Classes[NTemplateClass.StringProperty] };
            Classes[NTemplateClass.IntProperty] = new NPrimitiveType { NTemplateClass = NTemplateClass.Classes[NTemplateClass.IntProperty] };
            Classes[NTemplateClass.DecimalProperty] = new NPrimitiveType { NTemplateClass = NTemplateClass.Classes[NTemplateClass.DecimalProperty] };
            Classes[NTemplateClass.DoubleProperty] = new NPrimitiveType { NTemplateClass = NTemplateClass.Classes[NTemplateClass.DoubleProperty] };
            Classes[NTemplateClass.BoolProperty] = new NPrimitiveType { NTemplateClass = NTemplateClass.Classes[NTemplateClass.BoolProperty] };
            Classes[NTemplateClass.ActionProperty] = new NPrimitiveType { NTemplateClass = NTemplateClass.Classes[NTemplateClass.ActionProperty] };
            Classes[NTemplateClass.AppTemplate] = new NAppClass { NTemplateClass = NTemplateClass.Classes[NTemplateClass.AppTemplate] };
        }


    }
}
