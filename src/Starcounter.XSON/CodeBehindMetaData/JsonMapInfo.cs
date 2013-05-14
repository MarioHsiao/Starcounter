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
    public class JsonMapInfo {
        /// <summary>
        /// The namespace of the class from the codebehind file.
        /// </summary>
        public readonly string Namespace;

        /// <summary>
        /// The name of the class in the codebehind file.
        /// </summary>
        public readonly string ClassName;

        /// <summary>
        /// Contains the generic argument (if any) for the class.
        /// </summary>
        public readonly string GenericArgument;

        /// <summary>
        /// Boolean telling if this app inherits from a generic App and the properties
        /// in the app should be automatically bound to the dataobject.
        /// </summary>
        public readonly bool AutoBindToDataObject;

        /// <summary>
        /// All parent classes of the specified class in the codebehind file.
        /// If the class is not declared as an inner class this will be empty.
        /// </summary>
        public readonly List<String> ParentClasses;

        /// <summary>
        /// The name of the property in the json-file to connect this class to.
        /// </summary>
        public readonly String JsonMapName;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonMapInfo" /> class.
        /// </summary>
        /// <param name="ns">The namespace of the root app.</param>
        /// <param name="className">The name of the class to map</param>
        /// <param name="genericArgument">
        /// the generic argument if any of the class
        /// </param>
        /// <param name="autoBindToDataObject">
        /// If true the generated code will create a binding to the property in 
        /// the underlying dataobject for the app.
        /// </param>
        /// <param name="parentClasses">
        /// A list of parentclasses (if the class is an inner class
        /// </param>
        /// <param name="jsonMapName">
        /// The name of the property in the json-file
        /// </param>
        public JsonMapInfo(string ns,
                             string className,
                             string genericArgument,
                             bool autoBindToDataObject,
                             List<string> parentClasses,
                             string jsonMapName) {
            Namespace = ns;
            ClassName = className;
            GenericArgument = genericArgument;
            AutoBindToDataObject = autoBindToDataObject;
            ParentClasses = parentClasses;
            JsonMapName = jsonMapName;
        }
    }
}
