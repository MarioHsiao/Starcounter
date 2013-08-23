// ***********************************************************************
// <copyright file="NApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using Starcounter.XSON.Metadata;
using System;
using System.Collections.Generic;
using TJson = Starcounter.Templates.Schema<Starcounter.Json<object>>;


namespace Starcounter.Internal.MsBuild.Codegen {

    /// <summary>
    /// Represents a Json instance class (a class that is 
    /// derived from Json&ltobject&gt or the Json&ltobject&gt
    /// class.
    /// </summary>
    public class AstJsonClass : AstInstanceClass {

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="generator">The code-dom generator instance</param>
        public AstJsonClass(Gen2DomGenerator generator)
            : base(generator) {
        }

        /// <summary>
        /// Gets the template.
        /// </summary>
        /// <value>The template.</value>
        public TContainer Template {
            get { return (TContainer)(NTemplateClass.Template); }
        }

       // public new NAppClass Parent {
       //     get {
       //         return (NAppClass)base.Parent;
       //     }
       //     set {
       //         base.Parent = value;
       //     }
       // }

        /*
        /// <summary>
        /// The _ inherits
        /// </summary>
        public string _Inherits {
            set {
                __inh = value;
            }
            get {
                return __inh;
            }
        }

        private string __inh;
        */

        //public AstClass InheritedClass;


        public override string Generics {
            get {
                throw new Exception();
//                if (MatchedClass == null)
//                    return null;
//                return MatchedClass.GenericArg;
            }
        }




        /// <summary>
        /// The class name is linked to the name of the ClassName in the
        /// App template tree. If there is no ClassName, the property name
        /// of the App in the parent App is used. If there is no manually
        /// set ClassName, the name will be amended such that it ends with
        /// the text "App".
        /// </summary>
        /// <value>The name of the class.</value>
        public override string ClassStemIdentifier {
            get {
//                string ret;
                if (MatchedClass != null) {
#if DEBUG
                    if (MatchedClass.ClassName.Contains("<"))
                        throw new Exception();
#endif
                    return MatchedClass.ClassName;
                }
                if (Template.ClassName != null) {
#if DEBUG
                    if (Template.ClassName.Contains("<"))
                        throw new Exception();
#endif
                    return Template.ClassName;
                }
//                else if (!IsCustomGeneratedClass) {
//                    ret = HelperFunctions.GetClassStemIdentifier(this.Generator.DefaultObjTemplate.InstanceType);
//#if DEBUG
//                    if (ret.Contains("<"))
//                        throw new Exception();
//#endif
//                    return ret;
//                }
                else if (Template is Schema<Json<object>> && Template.PropertyName != null && Template.PropertyName != "" ) {
                    return JsonifyName(Template.PropertyName); // +"App";
                }
                return HelperFunctions.GetClassStemIdentifier(this.Template.InstanceType);
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
                throw new Exception();
//                if (Template.ClassName != null)
//                    return Template.ClassName;
//                else if (Template.Parent is TObjArr) {
//                    var alt = (TObjArr)Template.Parent;
//                    return alt.PropertyName;
//                } else
//                    return Template.PropertyName;
            }
        }

        /// <summary>
        /// Anonymous classes (classes for inner JSON objects) should have a nice
        /// name. We choose to use the name of the property it was created from with
        /// an added JSON ending. We cannot simply use the same name as the property
        /// as CSharp does not allow us to have the same name for an inner class as
        /// for a property name.
        /// </summary>
        /// <param name="name">The name to amend</param>
        /// <returns>A name that ends with the text "Json"</returns>
        private static string JsonifyName(string name) {
            //            if (name.EndsWith("s")) {
            //                name = name.Substring(0, name.Length - 1);
            //            }
            return name + "Json";
        }

        /// <summary>
        /// Returns false if there are no children defined. This indicates that the property
        /// that uses this node as a type should instead use the default Obj class (Json,Puppet) inside
        /// the Starcounter library. This is done by the NApp node pretending to be the App class
        /// node to make DOM generation easier (this cheating is intentional).
        /// </summary>
        /// <value><c>true</c> if this instance is custom app template; otherwise, <c>false</c>.</value>
        public bool IsCustomGeneratedClass {
            get {
                if (Template is Schema<Json<object>>) {
                    return ((Schema<Json<object>>)Template).Properties.Count > 0;
                }
                return false;
            }
        }


    }
}
