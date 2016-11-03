// ***********************************************************************
// <copyright file="JsonMapInfo.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter.Templates;

namespace Starcounter.XSON.Metadata {
    /// <summary>
    /// Class holding information on how to connect an app-property to
    /// a class in the codebehind file.
    /// </summary>
    public class CodeBehindClassInfo {
        private bool explicitlyBound;

        /// <summary>
        /// The name of the class in the codebehind file.
        /// </summary>
        public string ClassName;

        /// <summary>
        /// The namespace of the class from the codebehind file.
        /// </summary>
        public string Namespace;
        
        /// <summary>
        /// The name of the baseclass (if any) specified for the class.
        /// </summary>
        public string BaseClassName;

        /// <summary>
        /// If IBound-interface is specified for the class, this field contains the name 
        /// of the type that should be used for binding.
        /// </summary>
        public string BoundDataClass { get; set; }

        /// <summary>
        /// The classname including any path specified by the user in the .cs file.
        /// The root class name is denoted with an asterix (*) as it should always
        /// match to the root JSON object in the JSON-by-example
        /// </summary>
        public string ClassPath;

        /// <summary>
        /// 
        /// </summary>
        public BindingStrategy BindChildren = BindingStrategy.Auto;

        /// <summary>
        /// 
        /// </summary>
        public bool ExplicitlyBound {
            get { return explicitlyBound; }
            set {
                BindChildren = (value) ? BindingStrategy.Bound : BindingStrategy.Auto;
                explicitlyBound = value;
            }
        }

        /// <summary>
        /// If the code-behind contains a partial class for this class, this property is true
        /// </summary>
        public bool IsDeclaredInCodeBehind;

        /// <summary>
        /// The original untouched attribute string (excluding enclosing brackets)
        /// </summary>
        public string JsonMapAttribute;

        /// <summary>
        /// If set to false the 'global::' specifier will not be added to the full name.
        /// </summary>
        public bool UseGlobalSpecifier = true;

        /// <summary>
        /// All parent classes of the specified class in the codebehind file.
        /// If the class is not declared as an inner class this will be empty.
        /// </summary>
        public List<String> ParentClasses = new List<string>();
        
        /// <summary>
        /// 
        /// </summary>
        public bool IsRootClass = false;

        /// <summary>
        /// A list of all declared methods that should be used for input-validation.
        /// </summary>
        public List<InputBindingInfo> InputBindingList = new List<InputBindingInfo>();

        /// <summary>
        /// A list of all properties that exists in the code-behind class.
        /// </summary>
        public List<CodeBehindFieldOrPropertyInfo> FieldOrPropertyList = new List<CodeBehindFieldOrPropertyInfo>();

        /// <summary>
        /// 
        /// </summary>
        public CodeBehindClassInfo(string raw) {
            JsonMapAttribute = raw;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool DerivesDirectlyFromJson {
            get {
                bool cls;
                switch (BaseClassName) {
                    case "":
                    case "Json":
                    case "Starcounter.Json":
                        cls = true;
                        break;
                    default:
                        cls = false;
                        break;
                }
                return cls;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsMapped {
            get {
                return (JsonMapAttribute != null);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public string GlobalClassSpecifier {
            get {
                var str = "global::";

				if (!UseGlobalSpecifier)
					str = "";

                if (Namespace != null)
                    str += Namespace + ".";

				for (int i = (ParentClasses.Count - 1); i >= 0; i--)
					str += ParentClasses[i] + ".";
				
                str += ClassName;
                return str;
            }
        }
        
        /// <summary>
        /// Create a new instance of JsonMapInfo
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static CodeBehindClassInfo EvaluateAttributeString(string attribute, CodeBehindClassInfo existing) {
            if (attribute == null)
                return null;
            
            var strings = attribute.Split('.');

            // Example: "[MyFile_json.Somestuff]
            for (int t = 0; t < strings.Length; t++) {
                if (strings[t].EndsWith("_json")) {
					if (existing == null)
						existing = new CodeBehindClassInfo(attribute);
					else
						existing.JsonMapAttribute = attribute;

					existing.IsRootClass = (t == strings.Length - 1);
					existing.ClassPath = CreateClassPathFromSplitAttributeString(strings);
					return existing;
                }
            }

            return null;
        }

        private static string CreateClassPathFromSplitAttributeString(string[] raw) {
            string output = "*";
            bool inFileClassQualifier = true;

            for (int t=0;t<raw.Length;t++) {
                if (inFileClassQualifier) {
                    if (raw[t].EndsWith("_json")) {
                        inFileClassQualifier = false;
                    }
                }
                else {
                    output += "." + raw[t]; 
                }
            }
            return output;
        }
    }
}
