// ***********************************************************************
// <copyright file="CodeBehindMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
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
        public static CodeBehindMetadata Empty;

        static CodeBehindMetadata() {
            Empty = new CodeBehindMetadata();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeBehindMetadata" /> class.
        /// </summary>
        public CodeBehindMetadata() { }

        /// <summary>
        /// The classpath looks like this example "*.SomeChild.SomeGrandChild.SomeProp"
        /// wherein the asterix denotes the root class of the JSON object.
        /// </summary>
        /// <param name="fullClassPath"></param>
        /// <returns></returns>
        public CodeBehindClassInfo FindClassInfo(string fullClassPath) {
            //throw new Exception(fullClassPath);
            foreach (var ci in JsonPropertyMapList) {
                if (ci.ClassPath == fullClassPath)
                    return ci;
            }
            return null;
        }

		/// <summary>
		/// A list containing all using directives in the codebehind file.
		/// </summary>
		public List<string> UsingDirectives = new List<string>();

        /// <summary>
        /// A list of classes from the code-behind file that should be connected
        /// to the correct app in the generated code.
        /// </summary>
        public List<CodeBehindClassInfo> JsonPropertyMapList = new List<CodeBehindClassInfo>();

        /// <summary>
        /// Class information for the root in the Json object
        /// </summary>
        public CodeBehindClassInfo RootClassInfo {
            get {
                foreach (var ci in JsonPropertyMapList) {
                    if (ci.IsRootClass)
                        return ci;
                }
                return null;
            }
        }

    }
}
