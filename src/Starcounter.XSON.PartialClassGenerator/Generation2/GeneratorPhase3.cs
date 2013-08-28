﻿


using Starcounter.Templates;
using Starcounter.XSON.Metadata;
using System;
using System.Collections.Generic;
namespace Starcounter.Internal.MsBuild.Codegen {

    /// <summary>
    /// Adds input classes (event handler classes)
    /// </summary>
    internal class GeneratorPhase3 {


        internal Gen2DomGenerator Generator;

        internal void RunPhase3(AstJsonClass acn) {
            GenerateInputAttributes(Generator.Root);
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
            var input = new AstOtherClass(Generator) {
                Parent = acn,
                _ClassName = "Input",
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
                    if (mn.Type is AstPrimitiveType) {
                        new AstEventClass(Generator) {
                            NMember = mn,
                            Parent = parent,
                            //                                NApp = app,
                            EventName = eventName
                        };
                    }
                }
            }
        }
    }
}
