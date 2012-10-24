// ***********************************************************************
// <copyright file="simple.facit.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

// This is a system generated file. It reflects the Starcounter App Template defined in the file "simple.json"
// DO NOT MODIFY DIRECTLY - CHANGES WILL BE OVERWRITTEN

using System;
using System.Collections.Generic;
using Starcounter;
using Starcounter.Internal;
using Starcounter.Templates;

/// <summary>
/// Class PlayerApp
/// </summary>
public partial class PlayerApp {
    /// <summary>
    /// The default template
    /// </summary>
    public static PlayerAppTemplate DefaultTemplate = new PlayerAppTemplate();
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerApp" /> class.
    /// </summary>
    public PlayerApp() {
        Template = DefaultTemplate;
    }
    /// <summary>
    /// Gets or sets the template.
    /// </summary>
    /// <value>The template.</value>
    public new PlayerAppTemplate Template { get { return (PlayerAppTemplate)base.Template; } set { base.Template = value; } }
    /// <summary>
    /// Gets the metadata.
    /// </summary>
    /// <value>The metadata.</value>
    public new PlayerAppMetadata Metadata { get { return (PlayerAppMetadata)base.Metadata; } }
    /// <summary>
    /// Gets or sets the kid.
    /// </summary>
    /// <value>The kid.</value>
    public PlayerApp.KidApp Kid { get { return GetValue<PlayerApp.KidApp>(Template.Kid); } set { SetValue(Template.Kid, value); } }
    /// <summary>
    /// Gets or sets the page.
    /// </summary>
    /// <value>The page.</value>
    public App Page { get { return GetValue<App>(Template.Page); } set { SetValue(Template.Page, value); } }
    /// <summary>
    /// Gets or sets the player id.
    /// </summary>
    /// <value>The player id.</value>
    public int PlayerId { get { return GetValue(Template.PlayerId); } set { SetValue(Template.PlayerId, value); } }
    /// <summary>
    /// Gets or sets the full name.
    /// </summary>
    /// <value>The full name.</value>
    public String FullName { get { return GetValue(Template.FullName); } set { SetValue(Template.FullName, value); } }
    /// <summary>
    /// Gets or sets the accounts.
    /// </summary>
    /// <value>The accounts.</value>
    public Listing<PlayerApp.AccountsApp> Accounts { get { return GetValue<PlayerApp.AccountsApp>(Template.Accounts); } set { SetValue<PlayerApp.AccountsApp>(Template.Accounts, value); } }
    /// <summary>
    /// Class KidApp
    /// </summary>
    public class KidApp : App {
        /// <summary>
        /// The default template
        /// </summary>
        public static KidAppTemplate DefaultTemplate = new KidAppTemplate();
        /// <summary>
        /// Initializes a new instance of the <see cref="KidApp" /> class.
        /// </summary>
        public KidApp() {
            Template = DefaultTemplate;
        }
        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        public new KidAppTemplate Template { get { return (KidAppTemplate)base.Template; } set { base.Template = value; } }
        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <value>The metadata.</value>
        public new KidAppMetadata Metadata { get { return (KidAppMetadata)base.Metadata; } }
        /// <summary>
        /// Gets or sets the grandkid.
        /// </summary>
        /// <value>The grandkid.</value>
        public String Grandkid { get { return GetValue(Template.Grandkid); } set { SetValue(Template.Grandkid, value); } }
        /// <summary>
        /// Class KidAppTemplate
        /// </summary>
        public class KidAppTemplate : AppTemplate {
            /// <summary>
            /// Initializes a new instance of the <see cref="KidAppTemplate" /> class.
            /// </summary>
            public KidAppTemplate()
                : base() {
                InstanceType = typeof(PlayerApp.KidApp);
                ClassName = "KidApp";
                Grandkid = Register<StringProperty>("Grandkid");
            }
            /// <summary>
            /// The grandkid
            /// </summary>
            public StringProperty Grandkid;
        }
        /// <summary>
        /// Class KidAppMetadata
        /// </summary>
        public class KidAppMetadata : AppMetadata {
            /// <summary>
            /// Initializes a new instance of the <see cref="KidAppMetadata" /> class.
            /// </summary>
            /// <param name="app">The app.</param>
            /// <param name="template">The template.</param>
            public KidAppMetadata(App app, AppTemplate template) : base(app, template) { }
            /// <summary>
            /// Gets the app.
            /// </summary>
            /// <value>The app.</value>
            public new PlayerApp.KidApp App { get { return (PlayerApp.KidApp)base.App; } }
            /// <summary>
            /// Gets the template.
            /// </summary>
            /// <value>The template.</value>
            public new PlayerApp.KidApp.KidAppTemplate Template { get { return (PlayerApp.KidApp.KidAppTemplate)base.Template; } }
            /// <summary>
            /// Gets the grandkid.
            /// </summary>
            /// <value>The grandkid.</value>
            public StringMetadata Grandkid { get { return __p_Grandkid ?? (__p_Grandkid = new StringMetadata(App, App.Template.Grandkid)); } } private StringMetadata __p_Grandkid;
        }
    }
    /// <summary>
    /// Class AccountsApp
    /// </summary>
    public class AccountsApp : App {
        /// <summary>
        /// The default template
        /// </summary>
        public static AccountsAppTemplate DefaultTemplate = new AccountsAppTemplate();
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountsApp" /> class.
        /// </summary>
        public AccountsApp() {
            Template = DefaultTemplate;
        }
        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        public new AccountsAppTemplate Template { get { return (AccountsAppTemplate)base.Template; } set { base.Template = value; } }
        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <value>The metadata.</value>
        public new AccountsAppMetadata Metadata { get { return (AccountsAppMetadata)base.Metadata; } }
        /// <summary>
        /// Gets or sets the account id.
        /// </summary>
        /// <value>The account id.</value>
        public int AccountId { get { return GetValue(Template.AccountId); } set { SetValue(Template.AccountId, value); } }
        /// <summary>
        /// Gets or sets the type of the account.
        /// </summary>
        /// <value>The type of the account.</value>
        public int AccountType { get { return GetValue(Template.AccountType); } set { SetValue(Template.AccountType, value); } }
        /// <summary>
        /// Gets or sets the balance.
        /// </summary>
        /// <value>The balance.</value>
        public Decimal Balance { get { return GetValue(Template.Balance); } set { SetValue(Template.Balance, value); } }
        /// <summary>
        /// Class AccountsAppTemplate
        /// </summary>
        public class AccountsAppTemplate : AppTemplate {
            /// <summary>
            /// Initializes a new instance of the <see cref="AccountsAppTemplate" /> class.
            /// </summary>
            public AccountsAppTemplate()
                : base() {
                InstanceType = typeof(PlayerApp.AccountsApp);
                ClassName = "AccountsApp";
                AccountId = Register<IntProperty>("AccountId");
                AccountType = Register<IntProperty>("AccountType", Editable = true);
                Balance = Register<DecimalProperty>("Balance");
            }
            /// <summary>
            /// The account id
            /// </summary>
            public IntProperty AccountId;
            /// <summary>
            /// The account type
            /// </summary>
            public IntProperty AccountType;
            /// <summary>
            /// The balance
            /// </summary>
            public DecimalProperty Balance;
        }
        /// <summary>
        /// Class AccountsAppMetadata
        /// </summary>
        public class AccountsAppMetadata : AppMetadata {
            /// <summary>
            /// Initializes a new instance of the <see cref="AccountsAppMetadata" /> class.
            /// </summary>
            /// <param name="app">The app.</param>
            /// <param name="template">The template.</param>
            public AccountsAppMetadata(App app, AppTemplate template) : base(app, template) { }
            /// <summary>
            /// Gets the app.
            /// </summary>
            /// <value>The app.</value>
            public new PlayerApp.AccountsApp App { get { return (PlayerApp.AccountsApp)base.App; } }
            /// <summary>
            /// Gets the template.
            /// </summary>
            /// <value>The template.</value>
            public new PlayerApp.AccountsApp.AccountsAppTemplate Template { get { return (PlayerApp.AccountsApp.AccountsAppTemplate)base.Template; } }
            /// <summary>
            /// Gets the account id.
            /// </summary>
            /// <value>The account id.</value>
            public IntMetadata AccountId { get { return __p_AccountId ?? (__p_AccountId = new IntMetadata(App, App.Template.AccountId)); } } private IntMetadata __p_AccountId;
            /// <summary>
            /// Gets the type of the account.
            /// </summary>
            /// <value>The type of the account.</value>
            public IntMetadata AccountType { get { return __p_AccountType ?? (__p_AccountType = new IntMetadata(App, App.Template.AccountType)); } } private IntMetadata __p_AccountType;
            /// <summary>
            /// Gets the balance.
            /// </summary>
            /// <value>The balance.</value>
            public DecimalMetadata Balance { get { return __p_Balance ?? (__p_Balance = new DecimalMetadata(App, App.Template.Balance)); } } private DecimalMetadata __p_Balance;
        }
    }
    /// <summary>
    /// Class PlayerAppTemplate
    /// </summary>
    public class PlayerAppTemplate : AppTemplate {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerAppTemplate" /> class.
        /// </summary>
        public PlayerAppTemplate()
            : base() {
            InstanceType = typeof(PlayerApp);
            ClassName = "PlayerApp";
            Kid = Register<PlayerApp.KidApp.KidAppTemplate>("Kid");
            Page = Register<AppTemplate>("Page");
            PlayerId = Register<IntProperty>("PlayerId");
            FullName = Register<StringProperty>("FullName", Editable = true);
            Accounts = Register<ListingProperty<PlayerApp.AccountsApp, PlayerApp.AccountsApp.AccountsAppTemplate>>("Accounts");
        }
        /// <summary>
        /// The kid
        /// </summary>
        public PlayerApp.KidApp.KidAppTemplate Kid;
        /// <summary>
        /// The page
        /// </summary>
        public AppTemplate Page;
        /// <summary>
        /// The player id
        /// </summary>
        public IntProperty PlayerId;
        /// <summary>
        /// The full name
        /// </summary>
        public StringProperty FullName;
        /// <summary>
        /// The accounts
        /// </summary>
        public ListingProperty<PlayerApp.AccountsApp, PlayerApp.AccountsApp.AccountsAppTemplate> Accounts;
    }
    /// <summary>
    /// Class PlayerAppMetadata
    /// </summary>
    public class PlayerAppMetadata : AppMetadata {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerAppMetadata" /> class.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="template">The template.</param>
        public PlayerAppMetadata(App app, AppTemplate template) : base(app, template) { }
        /// <summary>
        /// Gets the app.
        /// </summary>
        /// <value>The app.</value>
        public new PlayerApp App { get { return (PlayerApp)base.App; } }
        /// <summary>
        /// Gets the template.
        /// </summary>
        /// <value>The template.</value>
        public new PlayerApp.PlayerAppTemplate Template { get { return (PlayerApp.PlayerAppTemplate)base.Template; } }
        /// <summary>
        /// Gets the kid.
        /// </summary>
        /// <value>The kid.</value>
        public PlayerApp.KidApp.KidAppMetadata Kid { get { return __p_Kid ?? (__p_Kid = new PlayerApp.KidApp.KidAppMetadata(App, App.Template.Kid)); } } private PlayerApp.KidApp.KidAppMetadata __p_Kid;
        /// <summary>
        /// Gets the page.
        /// </summary>
        /// <value>The page.</value>
        public AppMetadata Page { get { return __p_Page ?? (__p_Page = new AppMetadata(App, App.Template.Page)); } } private AppMetadata __p_Page;
        /// <summary>
        /// Gets the player id.
        /// </summary>
        /// <value>The player id.</value>
        public IntMetadata PlayerId { get { return __p_PlayerId ?? (__p_PlayerId = new IntMetadata(App, App.Template.PlayerId)); } } private IntMetadata __p_PlayerId;
        /// <summary>
        /// Gets the full name.
        /// </summary>
        /// <value>The full name.</value>
        public StringMetadata FullName { get { return __p_FullName ?? (__p_FullName = new StringMetadata(App, App.Template.FullName)); } } private StringMetadata __p_FullName;
        /// <summary>
        /// Gets the accounts.
        /// </summary>
        /// <value>The accounts.</value>
        public ListingMetadata<PlayerApp.AccountsApp, PlayerApp.AccountsApp.AccountsAppTemplate> Accounts { get { return __p_Accounts ?? (__p_Accounts = new ListingMetadata<PlayerApp.AccountsApp, PlayerApp.AccountsApp.AccountsAppTemplate>(App, App.Template.Accounts)); } } private ListingMetadata<PlayerApp.AccountsApp, PlayerApp.AccountsApp.AccountsAppTemplate> __p_Accounts;
    }
    /// <summary>
    /// Class Json
    /// </summary>
    public static class Json {
        /// <summary>
        /// Class Kid
        /// </summary>
        public class Kid : TemplateAttribute {
        }
        /// <summary>
        /// Class Accounts
        /// </summary>
        public class Accounts : TemplateAttribute {
        }
    }
    /// <summary>
    /// Class Input
    /// </summary>
    public static class Input {
        /// <summary>
        /// Class Kid
        /// </summary>
        public static class Kid {
            /// <summary>
            /// Class Grandkid
            /// </summary>
            public class Grandkid : Input<PlayerApp.KidApp, StringProperty, String> {
            }
        }
        /// <summary>
        /// Class PlayerId
        /// </summary>
        public class PlayerId : Input<PlayerApp, IntProperty, int> {
        }
        /// <summary>
        /// Class FullName
        /// </summary>
        public class FullName : Input<PlayerApp, StringProperty, String> {
        }
        /// <summary>
        /// Class Accounts
        /// </summary>
        public static class Accounts {
            /// <summary>
            /// Class AccountId
            /// </summary>
            public class AccountId : Input<PlayerApp.AccountsApp, IntProperty, int> {
            }
            /// <summary>
            /// Class AccountType
            /// </summary>
            public class AccountType : Input<PlayerApp.AccountsApp, IntProperty, int> {
            }
            /// <summary>
            /// Class Balance
            /// </summary>
            public class Balance : Input<PlayerApp.AccountsApp, DecimalProperty, Decimal> {
            }
        }
    }
}
