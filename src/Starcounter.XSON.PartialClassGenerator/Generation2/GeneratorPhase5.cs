using Starcounter.Internal;
using Starcounter.Templates;
using Starcounter.XSON.Metadata;
using TJson = Starcounter.Templates.TObject;

namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// Creates the mapping attributes to be used by the user code-behind source code.
    /// </summary>
    internal class GeneratorPhase5 {
        private Gen2DomGenerator generator;

        internal GeneratorPhase5(Gen2DomGenerator generator) {
            this.generator = generator;
        }

        internal void RunPhase5(AstJsonClass acn, AstSchemaClass tcn, AstMetadataClass mcn) {
            CreateJsonAttribute(acn, tcn, mcn);
        }

        internal void CreateJsonAttribute(AstJsonClass acn, AstSchemaClass tcn, AstMetadataClass mcn) {
            var root = generator.Root;
            var metadata = generator.CodeBehindMetadata;

            if (metadata != CodeBehindMetadata.Empty) {
                // if there is codebehind and the class is not inherited from Json we need 
                // to change the inheritance on the template and metadata classes as well.
                //var tmp = metadata.RootClassInfo.BaseClassName;
              //  if (!string.IsNullOrEmpty(tmp) && !tmp.Equals("Json")) {
              //      tcn._Inherits = "T" + metadata.RootClassInfo.BaseClassName;
              //      mcn._Inherits = tmp + "Metadata";
              //  }

                var templateAttributeBaseClass = new AstOtherClass(generator) {
                    GlobalClassSpecifier = "s::TemplateAttribute"
                };

                var json = new AstJsonAttributeClass(generator) {
                    Parent = acn.Parent,
                    InheritedClass = templateAttributeBaseClass,
                    ClassStemIdentifier = acn.ClassStemIdentifier + "_json"
                };
                GenerateJsonAttributes(json, acn.Template,templateAttributeBaseClass );
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
        public void GenerateJsonAttributes( AstBase parent, Template template, AstOtherClass templateAttributeBaseClass ) {
            foreach (var kid in (((IReadOnlyTree)template).Children)) {
                var t = kid as Template;
                if (kid is TJson) {
                    // We dont allow mapping anonymous objects to code-behind classes.
                    if (kid.Children.Count == 0)
                        continue;

                    string stem;
                    if (t.Parent is TObjArr) {
                        stem = t.Parent.PropertyName;
                    }
                    else {
                        stem = t.PropertyName;
                    }
                    var x = new AstJsonAttributeClass(generator) {
                        InheritedClass = templateAttributeBaseClass,
                        ClassStemIdentifier = stem,
                        Parent = parent

                    };
                    GenerateJsonAttributes( x, t, templateAttributeBaseClass );
                } else {
                    GenerateJsonAttributes( parent, t, templateAttributeBaseClass );
                }
            }
        }
    }
}
