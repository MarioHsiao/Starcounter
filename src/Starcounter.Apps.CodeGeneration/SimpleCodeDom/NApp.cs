// ***********************************************************************
// <copyright file="NApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System.Collections.Generic;

namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// Represents a App class definition in template tree.
    /// </summary>
    public class NAppClass : NValueClass {
        //        public NAppClass AppClassClass;
        //        public NClass TemplateClass;
        //        public NClass MetaDataClass;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public NAppClass(DomGenerator gen)
            : base(gen) {
        }

        /// <summary>
        /// Gets the template.
        /// </summary>
        /// <value>The template.</value>
        public TObj Template {
            get { return (TObj)(NTemplateClass.Template); }
        }

       // public new NAppClass Parent {
       //     get {
       //         return (NAppClass)base.Parent;
       //     }
       //     set {
       //         base.Parent = value;
       //     }
       // }

        /// <summary>
        /// The _ inherits
        /// </summary>
        public string _Inherits;

        /// <summary>
        /// Can be used to set a specific base class for the generated App class.
        /// </summary>
        /// <value>The inherits.</value>
        public override string Inherits {
            get { return _Inherits; }
        }

        /// <summary>
        /// The class name is linked to the name of the ClassName in the
        /// App template tree. If there is no ClassName, the property name
        /// of the App in the parent App is used. If there is no manually
        /// set ClassName, the name will be amended such that it ends with
        /// the text "App".
        /// </summary>
        /// <value>The name of the class.</value>
        public override string ClassName {
            get {
                if (Template.ClassName != null)
                    return Template.ClassName;
                else if (!IsCustomGeneratedClass) {
                    return this.Generator.DefaultObjTemplate.InstanceType.Name; // "Puppet", "Message"
                } else if (Template.Parent is TObjArr) {
                    var alt = (TObjArr)Template.Parent;
                    return AppifyName(alt.PropertyName); // +"App";
                } else
                    return AppifyName(Template.PropertyName); // +"App";
            }
        }

        /// <summary>
        /// The class name is linked to the name of the ClassName in the
        /// App template tree. If there is no ClassName, the property name
        /// of the App in the parent App is used.
        /// </summary>
        /// <value>The stem.</value>
        public string Stem {
            get {
                if (Template.ClassName != null)
                    return Template.ClassName;
                else if (Template.Parent is TObjArr) {
                    var alt = (TObjArr)Template.Parent;
                    return alt.PropertyName;
                } else
                    return Template.PropertyName;
            }
        }

        /// <summary>
        /// Adds "App" to the end of a name
        /// </summary>
        /// <param name="name">The name to amend</param>
        /// <returns>A name that ends with the text "App"</returns>
        public static string AppifyName(string name) {
            //            if (name.EndsWith("s")) {
            //                name = name.Substring(0, name.Length - 1);
            //            }
            return name + "App";
        }

        /// <summary>
        /// Returns false if there are no children defined. This indicates that the property
        /// that uses this node as a type should instead use the default Obj class (Message,Puppet) inside
        /// the Starcounter library. This is done by the NApp node pretending to be the App class
        /// node to make DOM generation easier (this cheating is intentional).
        /// </summary>
        /// <value><c>true</c> if this instance is custom app template; otherwise, <c>false</c>.</value>
        public bool IsCustomGeneratedClass {
            get {
                return (Template.Properties.Count > 0);
            }
        }

        /// <summary>
        /// If set to true all properties in this appclass will be automatically 
        /// bound, if not specified otherwise on the property, to the underlying Entity.
        /// </summary>
        public bool AutoBindPropertiesToEntity { get; set; }

        /// <summary>
        /// Contains the generic type argument for this appclass or null
        /// if non-generic.
        /// </summary>
        public string GenericTypeArgument { get; set; }
    }
}
