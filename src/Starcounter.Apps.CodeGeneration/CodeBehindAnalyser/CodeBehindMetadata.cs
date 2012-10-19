// ***********************************************************************
// <copyright file="CodeBehindMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace Starcounter.Internal.Application.CodeGeneration
{
    /// <summary>
    /// Class CodeBehindMetadata
    /// </summary>
    public class CodeBehindMetadata
    {
        /// <summary>
        /// The empty
        /// </summary>
        public static readonly CodeBehindMetadata Empty
            = new CodeBehindMetadata("", new List<JsonMapInfo>(), new List<InputBindingInfo>());

        /// <summary>
        /// The root namespace
        /// </summary>
        public readonly String RootNamespace;
        /// <summary>
        /// The json property map list
        /// </summary>
        public readonly List<JsonMapInfo> JsonPropertyMapList;
        /// <summary>
        /// The input binding list
        /// </summary>
        public readonly List<InputBindingInfo> InputBindingList;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeBehindMetadata" /> class.
        /// </summary>
        /// <param name="ns">The ns.</param>
        /// <param name="mapList">The map list.</param>
        /// <param name="inputList">The input list.</param>
        internal CodeBehindMetadata(String ns, 
                                    List<JsonMapInfo> mapList, 
                                    List<InputBindingInfo> inputList)
        {
            RootNamespace = ns;
            JsonPropertyMapList = mapList;
            InputBindingList = inputList;
        }
    }
}
