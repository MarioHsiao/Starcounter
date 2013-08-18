// ***********************************************************************
// <copyright file="JsonMapInfo.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;

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
            RawJsonMapAttribute = raw;
        }

        /// <summary>
        /// If the code-behind contains a partial class for this class, this property is true
        /// </summary>
        public bool IsDeclaredInCodeBehind;

        /// <summary>
        /// The original untouched attribute string (excluding enclosing brackets)
        /// </summary>
        public string RawJsonMapAttribute;

        /// <summary>
        /// The namespace of the class from the codebehind file.
        /// </summary>
        public string Namespace;

        /// <summary>
        /// The name of the class in the codebehind file.
        /// </summary>
        public string ClassName;

        /// <summary>
        /// The name of the baseclass (if any) specified for the class.
        /// </summary>
        public string BaseClassName;

        /// <summary>
        /// Contains the generic argument (if any) for the class.
        /// </summary>
        public string GenericArgument;

        /// <summary>
        /// Boolean telling if this app inherits from a generic App and the properties
        /// in the app should be automatically bound to the dataobject.
        /// </summary>
        public bool AutoBindToDataObject;

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
                var str = RawJsonMapAttribute;

                if (str.StartsWith("json.")) {
                    str = str.Substring(5);
                }
                else {
                    int index = str.IndexOf(".json.");
                    if (index != -1) {
                        // Remove the json part before searching for the template.
                        str = str.Substring(index + 6);
                    }
                    else {
                        if (str.EndsWith(".json"))
                            return "*"; // This is a root class
                    }
                }
                return "*." + str;
            }
        }

        /// <summary>
        /// Create a new instance of JsonMapInfo
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        public static CodeBehindClassInfo EvaluateAttributeString(string attribute) {

            if (attribute == null)
                return null;
            var strings = attribute.Split('.');
            for (int t = 0; t < strings.Length; t++) {
                if (strings[t].Equals("json")) {
                    var i = new CodeBehindClassInfo(attribute);
                    i.IsRootClass = (t == strings.Length - 1);
                    return i;
                }
            }
#if DEBUG
            if (attribute.Contains("json.")) {
                throw new Exception("Internal error when detecting .json attribute in code-behind");
            }
#endif
            return null;
        }

  

    }
}
