

using Starcounter.Internal;
using Starcounter.Internal.MsBuild.Codegen;
using Starcounter.Templates;
using Starcounter.XSON.Metadata;
using System;
using System.Collections.Generic;
using TJson = Starcounter.Templates.Schema<Starcounter.Json<object>>;


namespace Starcounter.Internal.MsBuild.Codegen {


    /// <summary>
    /// Replaces class paths with class aliases     
    /// </summary>
    internal class GeneratorPhase6 {

        internal Gen2DomGenerator Generator;

        private Dictionary<string, int> Used = new Dictionary<string, int>();


        internal void RunPhase6(AstJsonClass acn) {
            CreateClassAliases(acn);
            MoveAliasesToTop();
            Generator.Root.AliasesActive = true;
        }

        private void MoveAliasesToTop() {

            Comparison<AstBase> c = (AstBase a, AstBase b) => {
                if (!(a is AstClassAlias)) {
                    if (b is AstClassAlias) {
                        return 1;
                    }
                };
                if ((a is AstClassAlias)) {
                    if (!(b is AstClassAlias)) {
                        return -1;
                    }
                };
                return 0;
            };
            Generator.Root.Children.Sort(c);
        }

        private void CreateClassAliases(AstBase node) {
            
            if (node.IsProcessedForAlias)
                return;

            node.IsProcessedForAlias = true;

            if (node is AstProperty) {
                var prop = (node as AstProperty);
                CreateClassAliases(prop.Type);
            }
            else if (node is AstClass ) {
                var cls = node as AstClass;

                if (cls.ClassAlias == null && !cls.IsPrimitive) {

                    if (cls.Generic != null) {
                        foreach (var gen in cls.Generic) {
                            CreateClassAliases(gen);
                        }
                    }

                    AstClassAlias alias;
                    int variant;
                    var id = "__" + cls.CalculateClassAliasIdentifier(8).ToUpper() + "__" ;
                    if (Used.TryGetValue(id, out variant)) {
                        variant++;
                        Used[id] = variant;
                        id += variant;
                    }
                    Used.Add(id, 0);

                    alias = new AstClassAlias(node.Generator) {
                        Alias = id,
                        Specifier = cls.GlobalClassSpecifierWithoutGenerics
                    };
                    alias.Parent = Generator.Root;

                    cls.ClassAlias = alias;

                    if (cls.InheritedClass != null) {
                        CreateClassAliases(cls.InheritedClass);
                    }
                    if (cls is AstJsonClass) {
                        CreateClassAliases((cls as AstJsonClass).NTemplateClass);
                      //  CreateClassAliases((cls as AstJsonClass).NMetadataClass);
                    }
                }
            }
            foreach (var kid in node.Children) {
                CreateClassAliases(kid);
            }

        }

    }
}
