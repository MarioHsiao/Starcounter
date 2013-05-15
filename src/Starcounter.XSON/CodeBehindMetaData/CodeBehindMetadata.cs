// ***********************************************************************
// <copyright file="CodeBehindMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace Starcounter.XSON.Metadata
{
    /// <summary>
    /// Class containing metadata information parsed from a codebehind file for an App
    /// </summary>
    public class CodeBehindMetadata
    {
        /// <summary>
        /// An empty instance of the codebehind metadata. Used when there is no
        /// codebehind file.
        /// </summary>
        public static readonly CodeBehindMetadata Empty
            = new CodeBehindMetadata();

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeBehindMetadata" /> class.
        /// </summary>
        public CodeBehindMetadata() { }

        /// <summary>
        /// The root namespace of the main app.
        /// </summary>
        public String RootNamespace;

        /// <summary>
        /// Boolean telling if this app inherits from a generic App and the properties
        /// in the app should be automatically bound to the dataobject.
        /// </summary>
        public bool AutoBindToDataObject;

        /// <summary>
        /// Contains the generic argument (if any) for the class.
        /// </summary>
        public string GenericArgument;

        /// <summary>
        /// A list of classes from the code-behind file that should be connected
        /// to the correct app in the generated code.
        /// </summary>
        public List<JsonMapInfo> JsonPropertyMapList = new List<JsonMapInfo>();

        /// <summary>
        /// A list of inputbindings which should be registered in the generated code.
        /// </summary>
        public List<InputBindingInfo> InputBindingList = new List<InputBindingInfo>();
    }
}
