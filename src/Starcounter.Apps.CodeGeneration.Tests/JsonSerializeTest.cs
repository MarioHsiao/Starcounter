using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using Starcounter.Internal.Application.CodeGeneration;
using Starcounter.Internal.JsonTemplate;
using Starcounter.Templates;

namespace Starcounter.Apps.CodeGeneration.Tests {
    /// <summary>
    /// 
    /// </summary>
    public static class JsonSerializeTest {
        [Test]
        public static void GenerateSerializationParseTreeOverview() {
            TObj objTemplate;
            
//            objTemplate = TemplateFromJs.CreateFromJs(File.ReadAllText("TestMessage.json"), false);
            objTemplate = TemplateFromJs.CreateFromJs(File.ReadAllText("simple.json"), false);
           
            ParseNode parseTree = ParseTreeGenerator.BuildParseTree(objTemplate);
            Console.WriteLine(parseTree.ToString());
        }

        [Test]
        public static void GenerateSerializationAstTreeOverview() {
            TObj objTemplate;

            //            objTemplate = TemplateFromJs.CreateFromJs(File.ReadAllText("TestMessage.json"), false);
            objTemplate = TemplateFromJs.CreateFromJs(File.ReadAllText("simple.json"), false);
           
            AstNode tree = AstTreeGenerator.BuildAstTree(objTemplate);
            Console.WriteLine(tree.ToString());
        }

        [Test]
        public static void GenerateSerializationCsCode() {
            TObj objTemplate;

            objTemplate = TemplateFromJs.CreateFromJs(File.ReadAllText("simple.json"), false);

            AstNamespace tree = AstTreeGenerator.BuildAstTree(objTemplate);
            tree.Namespace = "__testing__";
            Console.WriteLine(tree.GenerateCsSourceCode());
        }

        [Test]
        public static void DebugPregeneratedCode() {
            TObj objTemplate;

            objTemplate = TemplateFromJs.CreateFromJs(File.ReadAllText("supersimple.json"), false);

            AstNamespace tree = AstTreeGenerator.BuildAstTree(objTemplate);
            tree.Namespace = "__testing__";

            string code = tree.GenerateCsSourceCode();

            Type t = JsonFactory.Compiler.CompileType(code, "__testing__." + objTemplate.ClassName + "Serializer");
            CodegeneratedJsonSerializer serializer = (CodegeneratedJsonSerializer)Activator.CreateInstance(t);

            dynamic simple = (Json)objTemplate.CreateInstance(null);
            simple.PlayerId = 666;

            byte[] buffer = new byte[4096];

            unsafe {
                fixed (byte* p = buffer) {
                    int i = serializer.Serialize((IntPtr)p, buffer.Length, simple);

                    simple = (Json)objTemplate.CreateInstance(null);
                    i = serializer.Populate((IntPtr)p, i, simple);
                }
            }
        }
    }
}
