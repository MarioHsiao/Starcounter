﻿// ***********************************************************************
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
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeBehindClassInfo" /> class.
        /// </summary>
        public CodeBehindClassInfo( string raw ) {
            RawDebugJsonMapAttribute = raw;
        }

        /// <summary>
        /// </summary>
        public bool DerivesDirectlyFromJson {
            get {
//                bool gen;
                bool cls;
/*                switch (BaseClassGenericArg) {
                    case "Object":
                    case "system.Object":
                    case "System.Object":
                    case "object":
                        gen = true;
                        break;
                    default:
                        gen = false;
                        break;
                }
 */
                switch (BaseClassName) {
                    case "Json":
                    case "Starcounter.Json":
                        cls = true;
                        break;
                    default:
                        cls = false;
                        break;
                }
                //                return cls && gen;
                return cls;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsMapped {
            get {
                return (RawDebugJsonMapAttribute != null);
            }
        }

        /// <summary>
        /// If the code-behind contains a partial class for this class, this property is true
        /// </summary>
        public bool IsDeclaredInCodeBehind;

        /// <summary>
        /// The original untouched attribute string (excluding enclosing brackets)
        /// </summary>
        public string RawDebugJsonMapAttribute;

        /// <summary>
        /// The namespace of the class from the codebehind file.
        /// </summary>
        public string Namespace;

        /// <summary>
        /// The name of the class in the codebehind file.
        /// </summary>
        public string ClassName;


        /// <summary>
        /// 
        /// </summary>
        public string GlobalClassSpecifier {
            get {
                var str = "global::";
                if (Namespace != null)
                    str += Namespace + ".";

				for (int i = (ParentClasses.Count - 1); i >= 0; i--)
					str += ParentClasses[i] + ".";
				
                str += ClassName;
                if (GenericArg != null) {
                    str += "<" + GenericArg + ">";
                }
                return str;
            }
        }

        /// <summary>
        /// The name of the baseclass (if any) specified for the class.
        /// </summary>
        public string BaseClassName;

        /// <summary>
        /// Contains the generic argument (if any) for the class.
        /// </summary>
        public string GenericArg;
        /// <summary>
        /// Contains the generic argument (if any) for the inherited class.
        /// </summary>
        public string BaseClassGenericArg;

        /// <summary>
        /// 
        /// </summary>
        public Bound BindChildren = Bound.Auto;

        /// <summary>
        /// All parent classes of the specified class in the codebehind file.
        /// If the class is not declared as an inner class this will be empty.
        /// </summary>
        public List<String> ParentClasses = new List<string>();

 //       /// <summary>
 //       /// The name of the property in the json-file to connect this class to.
 //       /// </summary>
 //       public String JsonMapName;

        public bool IsRootClass = false;

        //        /// <summary>
        //        /// A list of inputbindings which should be registered in the generated code.
        //        /// </summary>
        public List<InputBindingInfo> InputBindingList = new List<InputBindingInfo>();



        /// <summary>
        /// The classname including any path specified by the user in the .cs file.
        /// The root class name is denoted with an asterix (*) as it should always
        /// match to the root JSON object in the JSON-by-example
        /// </summary>
        public string ClassPath {
            get {
                return _ClassPath;
            }
            internal set {
                _ClassPath = value;
            }
        }


        private string _ClassPath;



        /// <summary>
        /// Create a new instance of JsonMapInfo
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static CodeBehindClassInfo EvaluateAttributeString(string attribute, CodeBehindClassInfo existing) {
            if (attribute == null)
                return null;

			if (attribute.Contains("BindChildren")) {
				int count;
				int index = attribute.IndexOf("Bound.");
				if (index == -1)
					throw new Exception("Invalid value for BindChildren attribute");

				index += 6;
				count = attribute.Length - index - 1;

				Bound bound;
				if (!Enum.TryParse<Bound>(attribute.Substring(index, count), out bound))
					throw new Exception("Unable to get correct value from BindChildren attribute");

				if (existing == null)
					existing = new CodeBehindClassInfo(null);
				existing.BindChildren = bound;
				return existing;
			}

            var strings = attribute.Split('.');

            // First attempt the old syntax "[MyClass.json.Somestuff]
            for (int t = 0; t < strings.Length; t++) {
                if (strings[t].Equals("json")) {
					if (existing == null)
						existing = new CodeBehindClassInfo(attribute);
					else
						existing.RawDebugJsonMapAttribute = attribute;

					existing.IsRootClass = (t == strings.Length - 1);
					existing.ClassPath = CreateClassPathFromOldSyntax(attribute);
					return existing;
                }
            }

            // Then try the new syntax "[MyFile_json.Somestuff]
            for (int t = 0; t < strings.Length; t++) {
                if (strings[t].EndsWith("_json")) {
					if (existing == null)
						existing = new CodeBehindClassInfo(attribute);
					else
						existing.RawDebugJsonMapAttribute = attribute;

					existing.IsRootClass = (t == strings.Length - 1);
					existing.ClassPath = CreateClassPathFromNewSyntax(strings);
					return existing;
                }
            }


#if DEBUG
            if (attribute.Contains("json.")) {
                throw new Exception("Internal error when detecting .json attribute in code-behind");
            }
#endif
            return null;
        }





        private static string CreateClassPathFromOldSyntax(string raw) {

            if (raw.StartsWith("json.")) {
                raw = raw.Substring(5);
            }
            else {
                int index = raw.IndexOf(".json.");
                if (index != -1) {
                    // Remove the json part before searching for the template.
                    raw = raw.Substring(index + 6);
                }
                else {
                    if (raw == "json" || raw.EndsWith(".json")) {
                        return "*"; // This is a root class
                    }
                }
            }
            return "*." + raw;
        }



        private static string CreateClassPathFromNewSyntax(string[] raw) {

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
