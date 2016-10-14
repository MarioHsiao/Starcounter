using System.Collections.Generic;

namespace Starcounter.XSON.PartialClassGenerator {    
    /// <summary>
    /// Moves the nested classes below the properties for easier reading of of generator code
    /// (although the generated code should not be used by the developer).
    /// Creates the mapping attributes to be used by the user code-behind source code.
    /// </summary>
    internal class GeneratorPhase2 {
        private Gen2DomGenerator generator;

        internal GeneratorPhase2(Gen2DomGenerator generator) {
            this.generator = generator;
        }

        internal void RunPhase2( AstJsonClass acn, AstSchemaClass tcn, AstMetadataClass mcn ) {
            MoveNestedClassToBottom(generator.Root);
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
                if (kid is AstJsonClass) {
                    move.Add(kid);
                }
            }
            foreach (var kid in node.Children) {
                if (kid is AstSchemaClass) {
                    move.Add(kid);
                }
            }
            foreach (var kid in node.Children) {
                if (kid is AstMetadataClass) {
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
