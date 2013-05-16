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
        /// Initializes a new instance of the <see cref="JsonMapInfo" /> class.
        /// </summary>
        public JsonMapInfo() { }

        /// <summary>
        /// The namespace of the class from the codebehind file.
        /// </summary>
        public string Namespace;

        /// <summary>
        /// The name of the class in the codebehind file.
        /// </summary>
        public string ClassName;

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

        /// <summary>
        /// The name of the property in the json-file to connect this class to.
        /// </summary>
        public String JsonMapName;
    }
}
