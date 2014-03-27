using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Starcounter.Templates;

namespace Starcounter.Internal.XSON.Tests {
    [TestFixture]
    class TypedJsonInputHandlingTests {

        [Test]
        public static void TestSimpleInputHandling() {
            TString tStr;
            string strValue;
            TObject schema;
            
            schema = SetupSchemaWithSimpleHandlers();
            dynamic json = schema.CreateInstance();

            // Index (same order as declared in simple.json):
            // 0 - VirtualValue (string)
            // 1 - AbstractValue (string)
            // 2 - BaseValue (string)
            // 3 - OtherValue (long)

            // Value should be set without changes on property 'VirtualValue'
            strValue = "TestCase1";
            json.VirtualValue = "";
            tStr = (TString)schema.Properties[0];
            tStr.ProcessInput(json, strValue);
            Assert.AreEqual(strValue, json.VirtualValue);

            // Value should be changed and set on property 'AbstractValue'
            json.AbstractValue = "";
            strValue = "TestCase2";
            tStr = (TString)schema.Properties[1];
            tStr.ProcessInput(json, strValue);
            Assert.AreEqual("Changed", json.AbstractValue);

            // Input should be cancelled and no value set on property 'BaseValue'.
            json.BaseValue = "OldValue";
            strValue = "TestCase3";
            tStr = (TString)schema.Properties[2];
            tStr.ProcessInput(json, strValue);
            Assert.AreEqual("OldValue", json.BaseValue);
        }

        [Test]
        public static void TestInheritedInputHandling() {
            TString tStr;
            TLong tLong;
            string strValue;
            TObject schema;

            schema = SetupSchemaWithInheritedHandlers();
            dynamic json = schema.CreateInstance();

            // Index (same order as declared in simple.json):
            // 0 - VirtualValue (string)
            // 1 - AbstractValue (string)
            // 2 - BaseValue (string)
            // 3 - OtherValue (long)

            // Should not call base and change input value.
            strValue = "TestCase1";
            json.VirtualValue = "";
            tStr = (TString)schema.Properties[0];
            tStr.ProcessInput(json, strValue);
            Assert.AreEqual("Changed", json.VirtualValue);

            // Changed in both handlers, but base executed first.
            json.AbstractValue = "";
            strValue = "TestCase2";
            tStr = (TString)schema.Properties[1];
            tStr.ProcessInput(json, strValue);
            Assert.AreEqual("Ooops", json.AbstractValue);

            // Input should be cancelled and no value set on property 'BaseValue'.
            json.BaseValue = "OldValue";
            strValue = "TestCase3";
            tStr = (TString)schema.Properties[2];
            tStr.ProcessInput(json, strValue);
            Assert.AreEqual("OldValue", json.BaseValue);

            // No handler added on inherited template, should call base directly.
            json.OtherValue = 1;
            tLong = (TLong)schema.Properties[3];
            tLong.ProcessInput(json, 99);
            Assert.AreEqual(19, json.OtherValue);
        }

        private static TObject SetupSchemaWithSimpleHandlers() {
            var schema = TObject.CreateFromMarkup<Json, TObject>("json", File.ReadAllText("simple.json"), "Simple");
            
            // Index (same order as declared in simple.json):
            // 0 - VirtualValue (string)
            // 1 - AbstractValue (string)
            // 2 - BaseValue (string)
            // 3 - OtherValue (long)

            ((TString)schema.Properties[0]).AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    // Do nothing, accept the input and set it on the instance.
                }
            );
            ((TString)schema.Properties[1]).AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    // Change value in input, and accept it.
                    input.Value = "Changed";
                }
            );
            ((TString)schema.Properties[2]).AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    input.Cancel();
                }
            );
            ((TLong)schema.Properties[3]).AddHandler(
                Helper.CreateInput<long>,
                (Json pup, Starcounter.Input<long> input) => {
                    input.Value = 19;
                }
            );

            return schema;
        }

        private static TObject SetupSchemaWithInheritedHandlers() {
            TString baseProperty1;
            TString newProperty1;
            TLong baseProperty2;
            TLong newProperty2;

            var schema = SetupSchemaWithSimpleHandlers();
            schema.Properties.ClearExposed();

            baseProperty1 = (TString)schema.Properties[0];
            newProperty1 = schema.Add<TString>(baseProperty1.TemplateName);
            newProperty1.AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    // Don't call base, change input and set value.
                    input.Value = "Changed";
                }
            );

            baseProperty1 = (TString)schema.Properties[1];
            newProperty1 = schema.Add<TString>(baseProperty1.TemplateName);
            newProperty1.AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    input.Base();
                    if (!(input.Value == "Changed"))
                        throw new Exception("Value not changed in base handler");
                    input.Value = "Ooops";
                }
            );

            baseProperty1 = (TString)schema.Properties[2];
            newProperty1 = schema.Add<TString>(baseProperty1.TemplateName);
            newProperty1.AddHandler(
                Helper.CreateInput<string>,
                (Json pup, Starcounter.Input<string> input) => {
                    input.Base();
                    if (!input.Cancelled)
                        throw new Exception("Input should be cancelled!");
                }
            );

            baseProperty2 = (TLong)schema.Properties[3];
            newProperty2 = schema.Add<TLong>(baseProperty2.TemplateName);
            // No handler added, should call base directly.
            
            return schema;
        }
    }
}
