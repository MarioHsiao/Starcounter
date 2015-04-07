// ***********************************************************************
// <copyright file="TestJsReader.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using NUnit.Framework;
using Starcounter.Templates;
using Starcounter.Internal.JsonTemplate;
using TJson = Starcounter.Templates.TObject;

namespace Starcounter.Internal.JsonTemplate.Tests {
    /// <summary>
    ///
    /// </summary>
    public class TestJsReader {
        /// <summary>
        /// 
        /// </summary>
        [Test]
        public static void DefaultValueTest() {
            string script = @"{StrVal: ""default"",BoolVal: true,DblVal: 2e4,DecVal: 23.4,LongVal: 99}";

            TObject schema = TObject.CreateFromJson(script);

            Assert.AreEqual("default", ((TString)schema.Properties[0]).DefaultValue);
            Assert.AreEqual(true, ((TBool)schema.Properties[1]).DefaultValue);
            Assert.AreEqual(20000d, ((TDouble)schema.Properties[2]).DefaultValue);
            Assert.AreEqual(23.4m, ((TDecimal)schema.Properties[3]).DefaultValue);
            Assert.AreEqual(99, ((TLong)schema.Properties[4]).DefaultValue);
        }

        /// <summary>
        /// Creates the simple template from string.
        /// </summary>
        [Test]
        public static void CreateSimpleTemplateFromString() {
            const string script2 = @"{
                                      FirstName:'Joachim', 
                                      LastName:'Wester', 
                                      Selected:true,
                                      Emails: [  {
                                            Domain:'starcounter.com',
                                            User:'joachim.wester',
                                            Delete:null
                                         }
                                      ],
                                      Delete: null
                                    }";


            var actual = TJson.CreateFromJson(script2);
            Assert.IsInstanceOf(typeof(TJson), actual);
            Assert.IsInstanceOf<TString>(actual.Properties[0]);
            Assert.IsInstanceOf<TString>(actual.Properties[1]);
            Assert.IsInstanceOf<TBool>(actual.Properties[2]);
            Assert.IsInstanceOf<TObjArr>(actual.Properties[3]);
            Console.WriteLine(actual);
        }

        /// <summary>
        /// Creates the simple adorned template from js.
        /// </summary>
        [Test]
        public static void CreateSimpleAdornedTemplateFromJs() {
            const string script2 = @"
                                    {
                                      FirstName:'Joachim'.Editable(), 
                                    }.Class('TestApp')";

            var actual = TJson.CreateFromJson(script2);
            Assert.IsInstanceOf(typeof(TJson), actual);
            Assert.IsInstanceOf<TString>(actual.Properties[0]);
            Assert.AreEqual(true, ((TString)actual.Properties[0]).Editable);
            Assert.AreEqual("TestApp", actual.ClassName);
            Console.WriteLine(actual);
        }

        /// <summary>
        /// Creates the complex template from js.
        /// </summary>
        [Test]
        public static void CreateComplexTemplateFromJs() {
            const string script2 = @"
                                    {
                                      FirstName:'Joachim'               .Editable(), 
                                      LastName:'Wester'                 .Editable(false), 
                                      Selected:true                     .Editable(false).Editable(true),
                                      Emails: [  {
                                            Domain:'starcounter.com',
                                            User:'joachim.wester'.Bind('UserName'),
                                            Delete: null
                                         }
                                      ],
                                      Delete: null
                                    }.Class('TestApp').Namespace('Test')";


            var actual = TJson.CreateFromMarkup<Json, TJson>("json", script2, null);
            Assert.IsInstanceOf(typeof(TJson), actual);
            Assert.AreEqual("TestApp", actual.ClassName);
            Assert.AreEqual("Test", actual.Namespace);
            Assert.IsInstanceOf<TString>(actual.Properties[0]);
            Assert.IsInstanceOf<TString>(actual.Properties[1]);
            Assert.IsInstanceOf<TBool>(actual.Properties[2]);
            Assert.IsInstanceOf<TObjArr>(actual.Properties[3]);
            Assert.AreEqual(true, ((TString)actual.Properties[0]).Editable);
            Assert.AreEqual(false, ((TString)actual.Properties[1]).Editable);
            Assert.AreEqual(true, ((TBool)actual.Properties[2]).Editable);
            Console.WriteLine(actual);
        }


        /// <summary>
        /// Creates the even more complex template from js.
        /// </summary>
        [Test]
        public static void CreateEvenMoreComplexTemplateFromJs() {
            const string script =
               @"{
    UserFullName: 'Joachim Wester',
    Login: {
        UserName: ''.Editable(),
        Password: ''.Editable(),
        Login: null,
        Logout: null,
    },
    MainMenu: {
        Close: null,
        CreateCustomerCompany: null,
        CreateCustomerPerson: null,
        AdminMenu: {
            EditMyProfile: null,
            EditOurOrganisation: null,
            AdministerUsers: null
        }
    },
    Search: {
        SearchText: ''.OnUpdate('DoSearch'),
        ShowCompanies: true,
        ShowPersons: true,
        ShowCustomers: true,
        ShowUsers: true,
        ShowContactPersons: true,
        ShowOurOrganisation: true,
        ShowReachInfo: true,
        Results: [{
            Role: 'Customer',
            FullName: 'Google',
            Contacts: [{
                FirstName: 'Larry',
                LastName: 'Page',
                ReachInfo: [{
                    TypeName: 'email',
                    Address: 'larry@google.com',
                    MailOrSms: null
                }.Class('ReachInfoApp')]
            }.Class('ContactPersonApp')
            ],
            ReachInfo: [{
                TypeName: 'mobile',
                Address: '12345',
                MailOrSms: null
            }.Class('ReachInfoApp')
            ],
            Selected: false,
            CreateEmail: null,
            ShowCustomer: null,
            CreateOrder: null
        }.Class('CustomerApp')
        ]
    }.Bind('this'),
    HistoryApp: {}.Include('HistoryApp')
}.Class('CrmApp')";
            var actual = TJson.CreateFromMarkup<Json, TJson>("json", script, null);
            Assert.IsInstanceOf(typeof(TJson), actual);
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public static void TestCreateTemplateFromPrimitiveJson() {
            // string
            string json = @"""value""";
            TValue schema = TObject.CreateFromMarkup<Json, TValue>("JSON", json, null);
            Assert.IsInstanceOf<TString>(schema);
            Assert.AreEqual("value", ((TString)schema).DefaultValue);

            // integer
            json = @"13";
            schema = TObject.CreateFromMarkup<Json, TValue>("JSON", json, null);
            Assert.IsInstanceOf<TLong>(schema);
            Assert.AreEqual(13, ((TLong)schema).DefaultValue);

            // boolean
            json = @"true";
            schema = TObject.CreateFromMarkup<Json, TValue>("JSON", json, null);
            Assert.IsInstanceOf<TBool>(schema);
            Assert.AreEqual(true, ((TBool)schema).DefaultValue);

            // double
            json = @"1e3";
            schema = TObject.CreateFromMarkup<Json, TValue>("JSON", json, null);
            Assert.IsInstanceOf<TDouble>(schema);
            Assert.AreEqual(1000.0d, ((TDouble)schema).DefaultValue);

            // decimal
            json = @"1.2354";
            schema = TObject.CreateFromMarkup<Json, TValue>("JSON", json, null);
            Assert.IsInstanceOf<TDecimal>(schema);
            Assert.AreEqual(1.2354m, ((TDecimal)schema).DefaultValue);
        }

        /// <summary>
        /// 
        /// </summary>
//        [Test]
        public static void TestCreateTemplateFromNamedPrimitive() {
            // TODO:
            // Test disabled. Not supported in parser. Needs to be fixed there.

            string json = @"""namedmember"":""value""";
            TValue schema = TObject.CreateFromMarkup<Json, TValue>("JSON", json, null);

            Assert.IsInstanceOf<TString>(schema);
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public static void TestCreateTemplateFromPrimitiveArrayJson() {
            TValue elementType;

            // string
            string json = @"[""value""]";
            TValue schema = TObject.CreateFromMarkup<Json, TValue>("JSON", json, null);
            Assert.IsInstanceOf<TObjArr>(schema);
            elementType = ((TObjArr)schema).ElementType;
            Assert.IsInstanceOf<TString>(elementType);
            Assert.AreEqual("value", ((TString)elementType).DefaultValue);

            // integer
            json = @"[13]";
            schema = TObject.CreateFromMarkup<Json, TValue>("JSON", json, null);
            Assert.IsInstanceOf<TObjArr>(schema);
            elementType = ((TObjArr)schema).ElementType;
            Assert.IsInstanceOf<TLong>(elementType);
            Assert.AreEqual(13, ((TLong)elementType).DefaultValue);

            // boolean
            json = @"[true]";
            schema = TObject.CreateFromMarkup<Json, TValue>("JSON", json, null);
            Assert.IsInstanceOf<TObjArr>(schema);
            elementType = ((TObjArr)schema).ElementType;
            Assert.IsInstanceOf<TBool>(elementType);
            Assert.AreEqual(true, ((TBool)elementType).DefaultValue);

            // double
            json = @"[1e3]";
            schema = TObject.CreateFromMarkup<Json, TValue>("JSON", json, null);
            Assert.IsInstanceOf<TObjArr>(schema);
            elementType = ((TObjArr)schema).ElementType;
            Assert.IsInstanceOf<TDouble>(elementType);
            Assert.AreEqual(1000.0d, ((TDouble)elementType).DefaultValue);

            // decimal
            json = @"[1.2354]";
            schema = TObject.CreateFromMarkup<Json, TValue>("JSON", json, null);
            Assert.IsInstanceOf<TObjArr>(schema);
            elementType = ((TObjArr)schema).ElementType;
            Assert.IsInstanceOf<TDecimal>(elementType);
            Assert.AreEqual(1.2354m, ((TDecimal)elementType).DefaultValue);
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public static void TestCreateTemplateWithPrimitiveArray() {
            string json = @"{""Items"":[1]}";
            TValue schema = TObject.CreateFromMarkup<Json, TValue>("JSON", json, null);
            Assert.IsInstanceOf<TObject>(schema);

            var tarr = (TObjArr)((TObject)schema).Properties[0];

            Assert.IsInstanceOf<TLong>(tarr.ElementType);
        }
    }
}
