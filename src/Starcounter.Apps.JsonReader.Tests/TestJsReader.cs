// ***********************************************************************
// <copyright file="TestJsReader.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using NUnit.Framework;
using Starcounter.Templates;
using Starcounter.Internal.JsonTemplate;

namespace Starcounter.Internal.JsonTemplate.Tests {
    /// <summary>
    /// Class TestJsReader
    /// </summary>
    public class TestJsReader {
        /// <summary>
        /// Creates the simple template from string.
        /// </summary>
        [Test]
        public static void CreateSimpleTemplateFromString() {
            const string script2 = @"
                                    {
                                      FirstName:'Joachim', 
                                      LastName:'Wester', 
                                      Selected:true,
                                      Emails: [  {
                                            Domain:'starcounter.com',
                                            User:'joachim.wester',
                                            Delete:event
                                         }
                                      ],
                                      Delete: event
                                    }";


            TJson actual = (TJson)TemplateFromJs.CreateFromJs(script2, false);
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
                                      FirstName:'Joachim'               .Editable(), 
                                    }.Class('TestApp')";

            TJson actual = (TJson)TemplateFromJs.CreateFromJs(script2, false);
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
                                            Domain:'starcounter.com'.Unbound(),
                                            User:'joachim.wester'.Bind('UserName'),
                                            Delete: event
                                         }
                                      ],
                                      Delete: event
                                    }.Class('TestApp').Namespace('Test')";


            TJson actual = (TJson)TemplateFromJs.CreateFromJs(script2, false);
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
        Login: event,
        Logout: event,
    },
    MainMenu: {
        Close: event,
        CreateCustomerCompany: event,
        CreateCustomerPerson: event,
        AdminMenu: {
            EditMyProfile: event,
            EditOurOrganisation: event,
            AdministerUsers: event
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
                    MailOrSms: event
                }.Class('ReachInfoApp')]
            }.Class('ContactPersonApp')
            ],
            ReachInfo: [{
                TypeName: 'mobile',
                Address: '12345',
                MailOrSms: event
            }.Class('ReachInfoApp')
            ],
            Selected: false.Unbound(),
            CreateEmail: event,
            ShowCustomer: event,
            CreateOrder: event
        }.Class('CustomerApp')
        ]
    }.Bind('this'),
    HistoryApp: {}.Include('HistoryApp')
}.Class('CrmApp')";
            TJson actual = (TJson)TemplateFromJs.CreateFromJs(script, false);
            Assert.IsInstanceOf(typeof(TJson), actual);
        }
    }
}
