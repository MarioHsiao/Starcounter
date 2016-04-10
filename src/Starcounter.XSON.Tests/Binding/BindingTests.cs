
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using NUnit.Framework;
using Starcounter.Advanced;
using Starcounter.Internal.XSON.Tests.CompiledJson;
using Starcounter.Templates;
using TJson = Starcounter.Templates.TObject;


namespace Starcounter.Internal.XSON.Tests {

    public class BindingTests {
        private static string oldAppName;

        /// <summary>
        /// Sets up the test.
        /// </summary>
        [TestFixtureSetUp]
        public static void Setup() {
            // Initializing global sessions.
            GlobalSessions.InitGlobalSessions(1);
        }

        [SetUp]
        public static void SetupEachTest() {
            oldAppName = StarcounterEnvironment.AppName;
            StarcounterEnvironment.AppName = "Test";
        }

        [TearDown]
        public static void AfterEachTest() {
            StarcounterEnvironment.AppName = oldAppName;
            Session.Current = null;
        }

		[Test]
		public static void TestPathBindings() {
			Person person = new Person() { FirstName = "Arne", LastName = "Anka" };
			person.Address = new Address() { Street = "Nybrogatan" }; 
			Company company = new Company() { Person = person };
			
			Person person2 = new Person() { Address = null };
			Company company2 = new Company() { Person = person2 };

			var jsonTemplate = new TObject();
			var streetTemplate = jsonTemplate.Add<TString>("Street", "Person.Address.Street");
			streetTemplate.BindingStrategy = BindingStrategy.Bound;

			var firstNameTemplate = jsonTemplate.Add<TString>("FirstName", "Person.FirstName");
			firstNameTemplate.BindingStrategy = BindingStrategy.Bound;

			dynamic json = (Json)jsonTemplate.CreateInstance();
			json.Data = company;
			Assert.AreEqual(person.Address.Street, json.Street);
			Assert.AreEqual(person.FirstName, json.FirstName);

			json.Street = "Härjedalsvägen";
			json.FirstName = "Nisse";
			Assert.AreEqual("Härjedalsvägen", person.Address.Street);
			Assert.AreEqual("Nisse", person.FirstName);

			json.Data = company2;
			Assert.DoesNotThrow(() => {
				string value = json.Street;
				json.Street = "Härjedalsvägen";

				value = json.FirstName;
				json.FirstName = "Nisse";
			});
		}

        [Test]
        public static void TestBoundToCorrectDataType() {
            Person person = new Person() { FirstName = "Arne", LastName = "Anka" };
            person.Address = new Address() { Street = "Nybrogatan" };
            Company company = new Company() { Person = person };
            
            var jsonTemplate = new TObject();
            var streetTemplate = jsonTemplate.Add<TString>("Street", "Person.Address.Street");
            streetTemplate.BindingStrategy = BindingStrategy.Bound;

            var firstNameTemplate = jsonTemplate.Add<TString>("FirstName", "Person.FirstName");
            firstNameTemplate.BindingStrategy = BindingStrategy.Bound;

            dynamic json = (Json)jsonTemplate.CreateInstance();
            json.Data = company;

            // Make sure bindings are created.
            var tmp = json.FirstName;
            tmp = json.Street;
            
            Assert.AreEqual(typeof(Company), firstNameTemplate.dataTypeForBinding);
            Assert.AreEqual(typeof(Company), streetTemplate.dataTypeForBinding);
        }

        [Test]
        public static void TestDefaultAutoBinding() {
            Person p = new Person();
            p.FirstName = "Albert";
            dynamic j = new Json();
            j.Data = p;
            j.FirstName = "Abbe";
            Assert.AreEqual("Abbe", j.FirstName);
            Assert.AreEqual("Abbe", p.FirstName);
        }

        [Test]
        public static void TestSimpleBinding() {
            var p = new Person();
            p.FirstName = "Joachim";
            p.LastName = "Wester";

            dynamic j = new Json();
            var t = new TJson();
            var prop = t.Add<TString>("FirstName");
            prop.Bind = "FirstName";
            prop.BindingStrategy = BindingStrategy.Bound;
            j.Template = t;
            j.Data = p;

            var temp = (Json)j;

            Assert.AreEqual("Joachim", p.FirstName); // Get firstname using data object
            Assert.AreEqual("Joachim", prop.Getter(temp)); // Get firstname using JSON data binding using API
            Assert.AreEqual("Joachim", j.FirstName); // Get firstname using JSON data binding using dynamic code-gen

            j.FirstName = "Douglas";
            Assert.AreEqual("Douglas", p.FirstName);
            Assert.AreEqual("Douglas", j.FirstName);
        }

		[Test]
		public static void TestSimpleBindingWithoutIBindable() {
			var o = new ObjectWOBindable();
			o.FirstName = "Joachim";
			o.LastName = "Wester";

			var child = new ObjectWOBindable();
			child.FirstName = "Apa";
			child.LastName = "Papa";
			
			o.Items = new List<ObjectWOBindable>();
			o.Items.Add(child);


			dynamic j = new Json();
			var t = new TJson();
			var prop = t.Add<TString>("FirstName");
			prop.Bind = "FirstName";
			prop.BindingStrategy = BindingStrategy.Bound;

			var arrType = new TJson();
			arrType.Add<TString>("LastName");
			var prop2 = t.Add<TObjArr>("Items");
			prop2.ElementType = arrType;

			j.Template = t;
			j.Data = o;

//			var temp = (Json)j;

			Assert.AreEqual("Joachim", o.FirstName); // Get firstname using data object
			Assert.AreEqual("Joachim", prop.Getter(j)); // Get firstname using JSON data binding using API
			Assert.AreEqual("Joachim", j.FirstName); // Get firstname using JSON data binding using dynamic code-gen

			j.FirstName = "Douglas";
			Assert.AreEqual("Douglas", o.FirstName);
			Assert.AreEqual("Douglas", j.FirstName);

			Assert.AreEqual(1, j.Items.Count);
		}

		[Test]
		public static void TestAutoBinding() {
			var p = new Person();
			p.FirstName = "Joachim";
			p.LastName = "Wester";

			dynamic j = new Json();
			var t = new TJson();
			var prop = t.Add<TString>("FirstName");
			prop.BindingStrategy = BindingStrategy.Auto;

			var noteProp = t.Add<TString>("Notes");
			noteProp.BindingStrategy = BindingStrategy.Bound;

			j.Template = t;
			j.Data = p;

			Assert.Throws(typeof(Exception), () => { string notes = j.Notes; });
			noteProp.BindingStrategy = BindingStrategy.Unbound;
			Assert.DoesNotThrow(() => { string notes = j.Notes; });

			Assert.AreEqual("Joachim", p.FirstName);
			Assert.AreEqual("Joachim", j.FirstName);

			j.FirstName = "Douglas";
			Assert.AreEqual("Douglas", j.FirstName);
			Assert.AreEqual("Douglas", p.FirstName);
		}

		[Test]
		public static void TestArrayBinding() {
			Recursive r = new Recursive() { Name = "One" };
			Recursive r2 = new Recursive() { Name = "Two" };
			Recursive r3 = new Recursive() { Name = "Three" };

			r.Recursives.Add(r2);
			r2.Recursives.Add(r3);

			var mainTemplate = new TJson();

			var arrItemTemplate1 = new TJson();
			arrItemTemplate1.Add<TString>("Name");

			var arrItemTemplate2 = new TJson();
			arrItemTemplate2.Add<TString>("Name");

			var objArrTemplate = arrItemTemplate1.Add<TObjArr>("Recursives");
			objArrTemplate.ElementType = arrItemTemplate2;

			objArrTemplate = mainTemplate.Add<TObjArr>("Recursives");
			objArrTemplate.ElementType = arrItemTemplate1;

			var json = new Json();
			json.Template = mainTemplate;
			json.Data = r;
		}

        [Test]
        public static void TestInvalidBinding() {
            uint errorCode;

            var schema = new TObject();
            schema.ClassName = "PersonMsg";

            var tname = schema.Add<TString>("Name", "Invalid");
            tname.BindingStrategy = BindingStrategy.Bound;

            var tage = schema.Add<TLong>("Age", "FirstName");
            tname.BindingStrategy = BindingStrategy.Bound;

            dynamic json = new Json() { Template = schema };
            json.Data = new Person();

            Exception ex = Assert.Throws<Exception>(() => {
                string name = json.Name;
            });

            Assert.IsTrue(ErrorCode.IsFromErrorCode(ex));
            Assert.IsTrue(ErrorCode.TryGetCode(ex, out errorCode));
            Assert.AreEqual(Error.SCERRCREATEDATABINDINGFORJSON, errorCode);
            Assert.IsTrue(ex.Message.Contains("was not found in"));

            Helper.ConsoleWriteLine(ex.Message);
            Helper.ConsoleWriteLine("");

            ex = Assert.Throws<Exception>(() => {
                long age = json.Age;
            });

            Assert.IsTrue(ErrorCode.IsFromErrorCode(ex));
            Assert.IsTrue(ErrorCode.TryGetCode(ex, out errorCode));
            Assert.AreEqual(Error.SCERRCREATEDATABINDINGFORJSON, errorCode);
            Assert.IsTrue(ex.Message.Contains("Incompatible types for binding"));

            Helper.ConsoleWriteLine(ex.Message);

        }

        [Test]
        public static void TestAutoBindAndChangeBind() {
            // Making sure that we get the default schema and not a modified one since it is static.
            BaseJson.DefaultTemplate = new BaseJson.JsonByExample.Schema();

            var json = new BaseJson();
            var stringTemplate = (TString)json.Template.Properties.GetTemplateByPropertyName("SimpleValue");

            json.Data = new Person() {
                FirstName = "John",
                LastName = "Doe"
            };
            
            // Verify that the property SimpleValue is not bound and that it is verified as
            // unbound, meaning that the binding is not recreated each time.
            var value = json.SimpleValue;

            // Calling again for debugging purposes to see that the binding is not checked for again.
            value = json.SimpleValue;

            Assert.IsTrue(stringTemplate.isVerifiedUnbound);
            Assert.IsFalse(stringTemplate.isBoundToParent);
            Assert.IsNull(stringTemplate.dataTypeForBinding);

            Assert.AreEqual("Base", value);

            // Change Bind path, which will invalidate the binding.
            stringTemplate.Bind = "FirstName";

            Assert.IsFalse(stringTemplate.isVerifiedUnbound);
            Assert.IsFalse(stringTemplate.isBoundToParent);
            Assert.IsNull(stringTemplate.dataTypeForBinding);

            value = json.SimpleValue;

            Assert.IsFalse(stringTemplate.isVerifiedUnbound);
            Assert.IsFalse(stringTemplate.isBoundToParent);
            Assert.IsNotNull(stringTemplate.dataTypeForBinding);
            Assert.AreEqual("John", value);

            // Change Bind path again to a property in the codebehind.
            stringTemplate.Bind = "BaseStringValue";

            Assert.IsFalse(stringTemplate.isVerifiedUnbound);
            Assert.IsFalse(stringTemplate.isBoundToParent);
            Assert.IsNull(stringTemplate.dataTypeForBinding);

            value = json.SimpleValue;
            Assert.IsFalse(stringTemplate.isVerifiedUnbound);
            Assert.IsTrue(stringTemplate.isBoundToParent);
            Assert.IsNotNull(stringTemplate.dataTypeForBinding);
            Assert.AreEqual("CBValue", value);

        }

        [Test]
        public static void TestAccessingBoundPropertyFromMultipleThreadsWithDataAndNull() {
            // Making sure that we get the default schema and not a modified one since it is static.
            BaseJson.DefaultTemplate = new BaseJson.JsonByExample.Schema();

            Exception ex1 = null;
            Exception ex2 = null;
            BaseJson.DefaultTemplate.SimpleValue.Bind = "FirstName";

            AutoResetEvent[] autos = new AutoResetEvent[2];
            autos[0] = new AutoResetEvent(false);
            autos[1] = new AutoResetEvent(false);

            ThreadPool.QueueUserWorkItem((state) => {
                try {
                    var json = new BaseJson();
                    var data = new Person() {
                        FirstName = "John"
                    };

                    for (int i = 0; i < 100000; i++) {
                        json.Data = data;
                        var v = json.SimpleValue;
                        v = json.SimpleValue;
                        v = json.SimpleValue;
                        v = json.SimpleValue;
                        v = json.SimpleValue;
                    }
                } catch (Exception ex) {
                    ex1 = ex;
                } finally {
                    autos[0].Set();
                }
            });

            ThreadPool.QueueUserWorkItem((state) => {
                try {
                    var json = new BaseJson();
                    Person data = null;

                    for (int i = 0; i < 100000; i++) {
                        json.Data = data;
                        var v = json.SimpleValue;
                        v = json.SimpleValue;
                        v = json.SimpleValue;
                        v = json.SimpleValue;
                        v = json.SimpleValue;
                    }
                } catch (Exception ex) {
                    ex2 = ex;
                } finally {
                    autos[1].Set();
                }
            });

            WaitHandle.WaitAll(autos);

            if (ex1 != null) {
                ExceptionDispatchInfo.Capture(ex1).Throw();
            }

            if (ex2 != null) {
                ExceptionDispatchInfo.Capture(ex2).Throw();
            }
        }

        [Test]
        public static void TestBoundToCodeBehindWithData() {
            // Unittest for reproducing issue #2542
            // The second call to session.GenerateChangeLog will call the bound property
            // before the Data-property is set, resulting in Data being null when it should
            // not be.

            Session session = new Session();
            simplewithcodebehind simple = new simplewithcodebehind();
            simple.Data = new TestData() { Name = "Apapapa" };
            simple.Session = session;

            simple.ChangeLog.Generate(true);
            simple.ChangeLog.Generate(true);
        }

        [Test]
        public static void TestConversionToAndFromEnumAndString() {
            dynamic json = new Json();
            var dataObj = new ObjWithEnum() { TestEnum = TestEnum.Second };

            json.TestEnum = ""; // Creating property

            json.Data = dataObj;
            Assert.AreEqual("Second", json.TestEnum);

            json.TestEnum = "Third";
            Assert.AreEqual(TestEnum.Third, dataObj.TestEnum);

            Assert.Throws<ArgumentException>(() => { json.TestEnum = "NonExisting"; });
        }

        [Test]
        public static void TestSettingDataObjectOnSingleValue() {
            Json json = new Json();
            json.Template = new TString();
            json.Data = new Person();
        }
    }
}
