// ***********************************************************************
// <copyright file="CodeBehindMetadata.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Collections.Generic;

namespace Starcounter.XSON.Metadata {
    /// <summary>
    /// Class containing metadata information parsed from a codebehind file for a TypedJson object.
    /// </summary>
    public class CodeBehindMetadata {
        /// <summary>
        /// An empty instance of the codebehind metadata. Used when there is no codebehind file.
        /// </summary>
        public static CodeBehindMetadata Empty = new CodeBehindMetadata();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeBehindMetadata" /> class.
        /// </summary>
        public CodeBehindMetadata() { }

        /// <summary>
        /// Finds and return an existing instance of <c>CodeBehindClassInfo</c> which
        /// have the same classpath.
        /// </summary>
        /// <remarks>
        /// The classpath looks like this example "*.SomeChild.SomeGrandChild.SomeProp"
        /// wherein the asterix denotes the root class of the JSON object.
        /// </remarks>
        /// <param name="fullClassPath"></param>
        /// <returns></returns>
        public CodeBehindClassInfo FindClassInfo(string fullClassPath) {
            foreach (var ci in CodeBehindClasses) {
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
        /// A list of classes from the code-behind file that reference the JSON-by-example
        /// object's object nodes.
        /// </summary>
        public List<CodeBehindClassInfo> CodeBehindClasses = new List<CodeBehindClassInfo>();

        /// <summary>
        /// Class information for the root in the Json object, i.e. the one that correspond
        /// to the most outer object node in the JSON-by-example file.
        /// </summary>
        public CodeBehindClassInfo RootClassInfo {
            get {
                foreach (var ci in CodeBehindClasses) {
                    if (ci.IsRootClass)
                        return ci;
                }
                return null;
            }
        }
    }
}
