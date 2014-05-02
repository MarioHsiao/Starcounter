﻿
using System;
using System.IO;
using NUnit.Framework;
using Starcounter.Internal.JsonPatch;
using Starcounter.Templates;

namespace Starcounter.Internal.XSON.Tests {
    internal static class Helper {
        internal const string PATCH = "{{\"op\":\"replace\",\"path\":\"{0}\",\"value\":{1}}}";

        internal static AppAndTemplate CreateSampleApp() {
            dynamic template = TObject.CreateFromJson(File.ReadAllText("json\\SampleApp.json"));
            dynamic app = new Json() { Template = template };

            app.FirstName = "Cliff";
            app.LastName = "Barnes";

            var itemApp = app.Items.Add();
            itemApp.Description = "Take a nap!";
            itemApp.IsDone = false;

            itemApp = app.Items.Add();
            itemApp.Description = "Fix Apps!";
            itemApp.IsDone = true;

            return new AppAndTemplate(app, template);
        }

        internal static TObject CreateJsonTemplateFromFile(string filePath) {
            string json = File.ReadAllText("json\\" + filePath);
            string className = Path.GetFileNameWithoutExtension(filePath);
            var tobj = TObject.CreateFromMarkup<Json, TObject>("json", json, className);
            tobj.ClassName = className;
            return tobj;
        }

        internal static TObject CreateJsonTemplateFromContent(string filename, string json) {
            var className = Path.GetFileNameWithoutExtension(filename);
            var tobj = TObject.CreateFromMarkup<Json, TObject>("json",json, className);
            tobj.ClassName = className;
            return tobj;
        }

        internal static string Jsonify(string input) {
            return '"' + input + '"';
        }

        internal static Input<Json, Property<T>, T> CreateInput<T>(Json pup, Property<T> prop, T value) {
            return new Input<Json, Property<T>, T>() {
                App = pup,
                Template = prop,
                Value = value
            }; 
        }

#if DEBUG
        internal static void PrintTemplateDebugInfo<T>(Property<T> property) {
            string str = property.TemplateName + " (index: " + property.TemplateIndex;
            bool bound = (property.Bind != null);

            if (bound) {
                str += ", bind: " + property.Bind;
            }

            str += ", Type: " + property.GetType().Name;
            str += ")";

            Console.WriteLine("------------------------------------------");
            Console.WriteLine("Property:");
            Console.WriteLine(str);
            Console.WriteLine();
            Console.WriteLine("UnboundGetter:");
            Console.WriteLine(property.DebugUnboundGetter);
            Console.WriteLine();
            Console.WriteLine("UnboundSetter:");
            Console.WriteLine(property.DebugUnboundSetter);
            Console.WriteLine();

            if (bound) {
                Console.WriteLine("BoundGetter:");
                Console.WriteLine(property.DebugBoundGetter);
                Console.WriteLine();
                Console.WriteLine("BoundSetter:");
                Console.WriteLine(property.DebugBoundSetter);
                Console.WriteLine();
            }
        }

        internal static void PrintTemplateDebugInfo(TObject property) {
            string str = property.TemplateName + " (index: " + property.TemplateIndex;
            bool bound = (property.Bind != null);

            if (bound) {
                str += ", bind: " + property.Bind;
            }

            str += ", Type: " + property.GetType().Name;
            str += ")";

            Console.WriteLine("------------------------------------------");
            Console.WriteLine("Property:");
            Console.WriteLine(str);
            Console.WriteLine();
            Console.WriteLine("UnboundGetter:");
            Console.WriteLine(property.DebugUnboundGetter);
            Console.WriteLine();
            Console.WriteLine("UnboundSetter:");
            Console.WriteLine(property.DebugUnboundSetter);
            Console.WriteLine();

            if (bound) {
                Console.WriteLine("BoundGetter:");
                Console.WriteLine(property.DebugBoundGetter);
                Console.WriteLine();
                Console.WriteLine("BoundSetter:");
                Console.WriteLine(property.DebugBoundSetter);
                Console.WriteLine();
            }
        }
#endif

        internal static TObject CreateSimplePersonTemplateWithDataBinding() {
            var personSchema = new TObject() { BindChildren = BindingStrategy.Bound };
            personSchema.Add<TString>("FirstName$"); // Bound to FirstName
            personSchema.Add<TString>("LastName", "Surname"); // Bound to Surname

            var t = personSchema.Add<TLong>("Age"); // Will not be bound
            t.BindingStrategy = BindingStrategy.Unbound;

            personSchema.Add<TString>("Created");
            personSchema.Add<TString>("Updated");
            personSchema.Add<TString>("AbstractValue");
            personSchema.Add<TString>("VirtualValue");

            var misc = personSchema.Add<TString>("Misc");
            misc.Bind = null; // Removing the binding for this specific template.

            var phoneNumber = personSchema.Add<TObject>("PhoneNumber", "Number");
            phoneNumber.BindChildren = BindingStrategy.Bound;
            phoneNumber.Add<TString>("Number"); // Bound to Number

            return personSchema;
        }

        internal static TObject CreateSimplePersonTemplate() {
            var personSchema = new TObject();
            personSchema.Add<TString>("FirstName$");
            personSchema.Add<TString>("LastName");
            personSchema.Add<TLong>("Age");

            var phoneNumber = new TObject();
            phoneNumber.Add<TString>("Number");
            personSchema.Add<TArray<Json>>("PhoneNumbers", phoneNumber);

            return personSchema;
        }

        internal static TObject CreateComplexPersonTemplate() {
            var personSchema = new TObject();
            personSchema.Add<TString>("FirstName$");
            personSchema.Add<TString>("LastName");
            personSchema.Add<TLong>("Age");
            personSchema.Add<TDecimal>("Stats");

            var field = new TObject();
            field.Add<TString>("Type");
            var info = field.Add<TObject>("Info");
            info.Add<TString>("Text");
            field.InstanceType = typeof(MyFieldMessage);
            personSchema.Add<TArray<MyFieldMessage>>("Fields", field);

            var extraInfo = personSchema.Add<TObject>("ExtraInfo");
            extraInfo.Add<TString>("Text");

            return personSchema;
        }

        internal static void AssertAreEqual(Json expected, Json actual) {
            TObject tExpected = (TObject)expected.Template;
            TObject tActual = (TObject)actual.Template;

            // We assume that the instances used the same Template.
            Assert.AreEqual(tExpected, tActual);
            foreach (Template child in tExpected.Properties) {
                if (child is TBool)
                    Assert.AreEqual(((TBool)child).Getter(expected), ((TBool)child).Getter(actual));
                else if (child is TDecimal)
                    Assert.AreEqual(((TDecimal)child).Getter(expected), ((TDecimal)child).Getter(actual));
                else if (child is TDouble)
                    Assert.AreEqual(((TDouble)child).Getter(expected), ((TDouble)child).Getter(actual));
                else if (child is TLong)
                    Assert.AreEqual(((TLong)child).Getter(expected), ((TLong)child).Getter(actual));
                else if (child is TString)
                    Assert.AreEqual(((TString)child).Getter(expected), ((TString)child).Getter(actual));
                else if (child is TObject)
                    AssertAreEqual(((TObject)child).Getter(expected), ((TObject)child).Getter(actual));
                else if (child is TObjArr) {
                    var arr1 = ((TObjArr)child).Getter(expected);
                    var arr2 = ((TObjArr)child).Getter(actual);
                    Assert.AreEqual(arr1.Count, arr2.Count);
                    for (int i = 0; i < arr1.Count; i++) {
                        AssertAreEqual((Json)arr1._GetAt(i), (Json)arr2._GetAt(i));
                    }
                } else
                    throw new NotSupportedException();
            }
        }

        internal static void AssertCorrectErrorCodeIsThrown(Action action, uint expectedErrorCode) {
            uint ec;

            try {
                action();
                Assert.Fail("An exception with error " + ErrorCode.ToFacilityCode(expectedErrorCode) + " should have been thrown");
            } catch (Exception ex) {
                if (ErrorCode.TryGetCode(ex, out ec)) {
                    if (ec != expectedErrorCode)
                        Assert.Fail("An exception with error " + ErrorCode.ToFacilityCode(expectedErrorCode) + " should have been thrown");
                } else {
                    Assert.Fail("An exception with error " + ErrorCode.ToFacilityCode(expectedErrorCode) + " should have been thrown");
                }
            }
        }

        /// <summary>
        /// Verifies the index path.
        /// </summary>
        /// <param name="expected">The expected.</param>
        /// <param name="received">The received.</param>
        internal static void VerifyIndexPath(Int32[] expected, Int32[] received) {
            Assert.AreEqual(expected.Length, received.Length);
            for (Int32 i = 0; i < expected.Length; i++) {
                Assert.AreEqual(expected[i], received[i]);
            }
        }
    }

    internal class MyFieldMessage : Json {
    }
}
