

using System;
using NUnit.Framework;
using Starcounter.Templates;
namespace Starcounter.Internal.Application.JsonReader.Tests {
    public class TestJsReader {

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


            AppTemplate actual = TemplateFromJs.CreateFromJs(script2,false);
            Assert.IsInstanceOf(typeof(AppTemplate), actual);
            Assert.IsInstanceOf<StringProperty>(actual.Properties[0]);
            Assert.IsInstanceOf<StringProperty>(actual.Properties[1]);
            Assert.IsInstanceOf<BoolProperty>(actual.Properties[2]);
            Assert.IsInstanceOf<ListingProperty>(actual.Properties[3]);
            Console.WriteLine(actual);
        }

        
      [Test]
      public static void CreateSimpleAdornedTemplateFromJs()
      {
         const string script2 = @"
                      {
                                      FirstName:'Joachim'               .Editable(), 
                       }.Class('TestApp')";

         AppTemplate actual = TemplateFromJs.CreateFromJs(script2, false);
         Assert.IsInstanceOf(typeof(AppTemplate), actual);
         Assert.IsInstanceOf<StringProperty>(actual.Properties[0]);
         Assert.AreEqual(true, ((StringProperty)actual.Properties[0]).Editable);
         Assert.AreEqual("TestApp", actual.ClassName);
         Console.WriteLine(actual);
      }


      [Test]
      public static void CreateComplexTemplateFromJs()
      {
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


         AppTemplate actual = TemplateFromJs.CreateFromJs(script2, false);
         Assert.IsInstanceOf(typeof(AppTemplate), actual);
         Assert.AreEqual("TestApp", actual.ClassName);
         Assert.AreEqual("Test", actual.Namespace);
         Assert.IsInstanceOf<StringProperty>(actual.Properties[0]);
         Assert.IsInstanceOf<StringProperty>(actual.Properties[1]);
         Assert.IsInstanceOf<BoolProperty>(actual.Properties[2]);
         Assert.IsInstanceOf<ListingProperty>(actual.Properties[3]);
         Assert.AreEqual(true, ((StringProperty)actual.Properties[0]).Editable);
         Assert.AreEqual(false, ((StringProperty)actual.Properties[1]).Editable);
         Assert.AreEqual(true, ((BoolProperty)actual.Properties[2]).Editable);
         Console.WriteLine(actual);
      }

      
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
         AppTemplate actual = TemplateFromJs.CreateFromJs(script, false);
         Assert.IsInstanceOf(typeof(AppTemplate), actual);
      }
   }
}
