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
            = new CodeBehindMetadata("", null, false, new List<JsonMapInfo>(), new List<InputBindingInfo>());

        /// <summary>
        /// The root namespace of the main app.
        /// </summary>
        public readonly String RootNamespace;

        /// <summary>
        /// Boolean telling if this app inherits from a generic App and the properties
        /// in the app should be automatically bound to the dataobject.
        /// </summary>
        public readonly bool AutoBindToDataObject;

        /// <summary>
        /// Contains the generic argument (if any) for the class.
        /// </summary>
        public readonly string GenericArgument;

        /// <summary>
        /// A list of classes from the code-behind file that should be connected
        /// to the correct app in the generated code.
        /// </summary>
        public readonly List<JsonMapInfo> JsonPropertyMapList;

        /// <summary>
        /// A list of inputbindings which should be registered in the generated code.
        /// </summary>
        public readonly List<InputBindingInfo> InputBindingList;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeBehindMetadata" /> class.
        /// </summary>
        /// <param name="ns">The root namespace</param>
        /// <param name="genericArgument">
        /// the generic argument if any of the class
        /// </param>
        /// <param name="autoBindToDataObject">
        /// If true all properties in the json file should be bound 
        /// to the underlying dataobject
        /// </param>
        /// <param name="mapList">The list of mappings</param>
        /// <param name="inputList">The list of inputbindings.</param>
        public CodeBehindMetadata(string ns,
                                    string genericArgument,
                                    bool autoBindToDataObject,
                                    List<JsonMapInfo> mapList, 
                                    List<InputBindingInfo> inputList)
        {
            RootNamespace = ns;
            GenericArgument = genericArgument;
            AutoBindToDataObject = autoBindToDataObject;
            JsonPropertyMapList = mapList;
            InputBindingList = inputList;
        }
    }
}
