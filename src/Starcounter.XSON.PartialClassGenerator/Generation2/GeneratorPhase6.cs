using System;
using System.Collections.Generic;

namespace Starcounter.Internal.MsBuild.Codegen {
    /// <summary>
    /// Replaces class paths with class aliases     
    /// </summary>
    internal class GeneratorPhase6 {
        private Gen2DomGenerator Generator;
        private Dictionary<string, int> Used;

        internal GeneratorPhase6(Gen2DomGenerator generator) {
            this.Generator = generator;
            this.Used = new Dictionary<string, int>();
        }

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
            } else if (node is AstClass ) {
                var cls = node as AstClass;

                if (cls.ClassAlias == null && !cls.IsPrimitive) {

                    if (cls.Generic != null) {
                        foreach (var gen in cls.Generic) {
                            CreateClassAliases(gen);
                        }
                    }

					if (cls.UseClassAlias) {
						AstClassAlias alias;
						int variant;
						var id = "__" + cls.CalculateClassAliasIdentifier(8) + "__";
						if (Used.TryGetValue(id, out variant)) {
							variant++;
							Used[id] = variant;
							id = id.Substring(0, id.Length - 2) + variant + "__";
						}
						Used.Add(id, 0);

						alias = new AstClassAlias(node.Generator) {
							Alias = id,
							Specifier = cls.GlobalClassSpecifier
						};
						alias.Parent = Generator.Root;

						cls.ClassAlias = alias;
					}

                    if (cls.InheritedClass != null) {
                        CreateClassAliases(cls.InheritedClass);
                    }

                    var jsonClass = cls as AstJsonClass;
                    if (jsonClass != null && jsonClass.NTemplateClass != null) {
                        CreateClassAliases(jsonClass.NTemplateClass);
                    }
                }
            }
            foreach (var kid in node.Children) {
                CreateClassAliases(kid);
            }
        }
    }
}
