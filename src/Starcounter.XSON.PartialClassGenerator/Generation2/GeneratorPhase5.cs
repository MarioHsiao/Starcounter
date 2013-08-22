﻿

using Starcounter.Internal;
using Starcounter.Internal.MsBuild.Codegen;
using Starcounter.Templates;
using Starcounter.XSON.Metadata;
using System;
using System.Collections.Generic;
namespace Starcounter.Internal.MsBuild.Codegen {


    /// <summary>
    /// Moves the nested classes below the properties for easier reading of of generator code
    /// (although the generated code should not be used by the developer).
    /// Creates the mapping attributes to be used by the user code-behind source code.
    /// </summary>
    internal class GeneratorPhase5 {

        internal Gen2DomGenerator Generator;

        internal void RunPhase5(AstAppClass acn, AstTAppClass tcn, AstObjMetadata mcn) {
            CreateJsonAttribute(acn, tcn, mcn);
        }


        internal void CreateJsonAttribute(AstAppClass acn, AstTAppClass tcn, AstObjMetadata mcn) {

            var root = Generator.Root;
            var metadata = Generator.CodeBehindMetadata;

            if (metadata != CodeBehindMetadata.Empty) {
                // if there is codebehind and the class is not inherited from Json we need 
                // to change the inheritance on the template and metadata classes as well.
                var tmp = metadata.RootClassInfo.BaseClassName;
                if (!string.IsNullOrEmpty(tmp) && !tmp.Equals("Json")) {
                    tcn._Inherits = "T" + metadata.RootClassInfo.BaseClassName;
                    mcn._Inherits = tmp + "Metadata";
                }

                var json = new AstJsonAttributeClass(Generator) {
                    Parent = acn.Parent,
                    _Inherits = "TemplateAttribute",
                    _ClassName = acn.ClassName + "_json"
                };
                GenerateJsonAttributes(json, acn.Template );
            }
        }

        /// <summary>
        /// The JSON attributes is a set of source code attributes (C# Attributes)
        /// used to annotate which user classes should be used for which JSON tree
        /// nodes (objects). This allows the user to write classes that are not deeply
        /// nested (unless he/she wants the class declarations nested). The function
        /// is recursive and calls itself.
        /// </summary>
        /// <param name="appClass">The node to generate attributes for</param>
        /// <param name="parent">The DOM node to generate attributes for</param>
        public void GenerateJsonAttributes( AstBase parent, Template template ) {
            foreach (var kid in (((IReadOnlyTree)template).Children)) {
                var t = kid as Template;
                if (kid is TJson) {
                    string stem;
                    if (t.Parent is TObjArr) {
                        stem = t.Parent.PropertyName;
                    }
                    else {
                        stem = t.PropertyName;
                    }
                    var x = new AstJsonAttributeClass(Generator) {
                        _Inherits = "TemplateAttribute",
                        _ClassName = stem,
                        Parent = parent

                    };
                    GenerateJsonAttributes( x, t );
                }
                else {
                    GenerateJsonAttributes( parent, t );
                }
            }
        }
    }
}
