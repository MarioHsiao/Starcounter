

using Starcounter.Templates;
using System.Collections.Generic;
namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// Represents a App class definition in template tree.
    /// </summary>
    public class NApp : NClass {
        public AppTemplate Template;
        public NApp AppClass;
        public NClass TemplateClass;
        public NClass MetaDataClass;

        public static Dictionary<AppTemplate, NClass> Instances = new Dictionary<AppTemplate, NClass>();

        public string _Inherits;

        /// <summary>
        /// Can be used to set a specific base class for the generated App class.
        /// </summary>
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
        public override string ClassName {
            get {
                if (Template.ClassName != null)
                    return Template.ClassName;
                else if (!IsCustomAppTemplate) {
                    return "App";
                }
                else if (Template.Parent is ListingProperty) {
                    var alt = (ListingProperty)Template.Parent;
                    return AppifyName(alt.PropertyName); // +"App";
                }
                else
                    return AppifyName(Template.PropertyName); // +"App";
            }
        }

        /// <summary>
        /// The class name is linked to the name of the ClassName in the
        /// App template tree. If there is no ClassName, the property name
        /// of the App in the parent App is used.
        /// </summary>
        public string Stem {
            get {
                if (Template.ClassName != null)
                    return Template.ClassName;
                else if (Template.Parent is ListingProperty) {
                    var alt = (ListingProperty)Template.Parent;
                    return alt.PropertyName;
                }
                else
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
        /// that uses this node as a type should instead use the generic App class inside 
        /// the Starcounter library. This is done by the NApp node pretending to be the App class
        /// node to make DOM generation easier (this cheating is intentional).
        /// </summary>
        public bool IsCustomAppTemplate {
            get {
                return (Template.Properties.Count > 0);
            }
        }


    }
}
