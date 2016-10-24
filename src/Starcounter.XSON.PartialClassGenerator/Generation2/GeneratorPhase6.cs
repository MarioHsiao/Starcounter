using System;
using System.Collections.Generic;

namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// Replaces class paths with class aliases     
    /// </summary>
    internal class GeneratorPhase6 {
        private Gen2DomGenerator generator;
        private Dictionary<string, int> used;

        internal GeneratorPhase6(Gen2DomGenerator generator) {
            this.generator = generator;
            this.used = new Dictionary<string, int>();
        }

        internal void RunPhase6(AstJsonClass acn) {
            CreateClassAliases(acn);
            AddPredefinedAliases();
            MoveAliasesToTop();
            generator.Root.AliasesActive = true;
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
            generator.Root.Children.Sort(c);
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
						while (used.TryGetValue(id, out variant)) {
							variant++;
							used[id] = variant;
							id = id.Substring(0, id.Length - 2) + variant + "__";
						}
						used.Add(id, 0);

						alias = new AstClassAlias(node.Generator) {
							Alias = id,
							Specifier = cls.GlobalClassSpecifier
						};
						alias.Parent = generator.Root;

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

        private void AddPredefinedAliases() {
            new AstClassAlias(generator) {
                Alias = "s",
                Specifier = "Starcounter",
                Parent = generator.Root
            };

            new AstClassAlias(generator) {
                Alias = "st",
                Specifier = "Starcounter.Templates",
                Parent = generator.Root
            };

            new AstClassAlias(generator) {
                Alias = "_ScTemplate_",
                Specifier = "Starcounter.Templates.Template",
                Parent = generator.Root
            };

            new AstClassAlias(generator) {
                Alias = "_GEN1_",
                Specifier = "System.Diagnostics.DebuggerNonUserCodeAttribute",
                Parent = generator.Root
            };

            new AstClassAlias(generator) {
                Alias = "_GEN2_",
                Specifier = "System.CodeDom.Compiler.GeneratedCodeAttribute",
                Parent = generator.Root
            };
        }
    }
}
