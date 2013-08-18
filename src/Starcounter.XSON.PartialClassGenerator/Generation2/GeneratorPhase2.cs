

using Starcounter.Internal;
using Starcounter.Internal.MsBuild.Codegen;
using Starcounter.Templates;
using Starcounter.XSON.Metadata;
using System;
using System.Collections.Generic;
namespace Starcounter.Internal.MsBuild.Codegen {

    
    internal class GeneratorPhase2 {

        internal Gen2DomGenerator Generator;


        internal void RunPhase2( AstAppClass acn, AstTAppClass tcn, AstObjMetadata mcn ) {
            MoveNestedClassToBottom(Generator.Root);
            CreateJsonAttribute(acn, tcn, mcn);
        }


        internal void CreateJsonAttribute( AstAppClass acn, AstTAppClass tcn, AstObjMetadata mcn ) { 

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
                    Parent = acn,
                    IsStatic = true,
                    _Inherits = null,
                    _ClassName = "json"
                };
                GenerateJsonAttributes(acn, json);
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
        public void GenerateJsonAttributes(AstAppClass appClass, AstBase parent) {
            foreach (var kid in appClass.Children) {
                if (kid is AstAppClass) {
                    var x = new AstJsonAttributeClass(Generator) {
                        _Inherits = "TemplateAttribute",
                        _ClassName = (kid as AstAppClass).Stem,
                        Parent = parent

                    };
                    GenerateJsonAttributes(kid as AstAppClass, x);
                }
            }
        }



        /// <summary>
        /// Provide a nicer default order of the generated classes such
        /// that the primitive properties (string, int, etc.) comes first and
        /// the nested classes comes in the end of the class declaration.
        /// Show the nested App classes before the template and metadata classes.
        /// </summary>
        /// <param name="node">The node containing the children to rearrange</param>
        private void MoveNestedClassToBottom(AstBase node) {
            var move = new List<AstBase>();
            foreach (var kid in node.Children) {
                if (kid is AstAppClass) {
                    move.Add(kid);
                }
            }
            foreach (var kid in node.Children) {
                if (kid is AstTAppClass) {
                    move.Add(kid);
                }
            }
            foreach (var kid in node.Children) {
                if (kid is AstObjMetadata) {
                    move.Add(kid);
                }
            }
            foreach (var kid in move) {
                kid.Parent = kid.Parent;
                MoveNestedClassToBottom(kid);
            }
        }
    }
}
