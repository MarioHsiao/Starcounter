namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// Adds input classes (event handler classes)
    /// </summary>
    internal class GeneratorPhase3 {
        private Gen2DomGenerator generator;

        internal GeneratorPhase3(Gen2DomGenerator generator) {
            this.generator = generator;
        }

        internal void RunPhase3(AstJsonClass acn) {
            GenerateInputAttributes(generator.Root);
        }

        private void GenerateInputAttributes(AstBase node) {
            if (node is AstJsonClass) {
                GenerateInputAttributesForASingleClass((AstJsonClass)node);
            }
            foreach (var kid in node.Children) {
                GenerateInputAttributes(kid);
            }
        }

        /// <summary>
        /// Creates the Input attributes to be used by the code-behind source code
        /// </summary>
        /// <param name="acn">The Json class</param>
        private void GenerateInputAttributesForASingleClass(AstJsonClass acn) {
            var input = new AstOtherClass(generator) {
                Parent = acn,
                ClassStemIdentifier = "Input",
                IsStatic = true
            };
            GeneratePrimitiveValueEvents(input, acn, "Input");
        }
        
        /// <summary>
        /// Used to generate Handle( ... ) event classes used by the user programmer
        /// to catch events such as the Input event.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="app">The app.</param>
        /// <param name="eventName">The name of the event (i.e. "Input").</param>
        public void GeneratePrimitiveValueEvents(AstBase parent, AstClass app, string eventName) {
            foreach (var kid in app.Children) {
                if (kid is AstProperty) {
                    var mn = kid as AstProperty;
                    if (mn.Template.IsPrimitive) {
                        new AstEventClass(generator) {
                            NMember = mn,
                            Parent = parent,
                            EventName = eventName
                        };
                    }
                }
            }
        }
    }
}
