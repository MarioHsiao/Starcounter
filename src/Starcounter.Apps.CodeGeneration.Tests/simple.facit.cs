// This is a system generated file. It reflects the Starcounter App Template defined in the file "simple.json"
// DO NOT MODIFY DIRECTLY - CHANGES WILL BE OVERWRITTEN

using System;
using System.Collections.Generic;
using Starcounter;
using Starcounter.Internal;
using Starcounter.Templates;

public partial class PlayerApp {
    public static PlayerAppTemplate DefaultTemplate = new PlayerAppTemplate();
    public PlayerApp() {
        Template = DefaultTemplate;
    }
    public new PlayerAppTemplate Template { get { return (PlayerAppTemplate)base.Template; } set { base.Template = value; } }
    public new PlayerAppMetadata Metadata { get { return (PlayerAppMetadata)base.Metadata; } }
    public PlayerApp.KidApp Kid { get { return GetValue<PlayerApp.KidApp>(Template.Kid); } set { SetValue(Template.Kid, value); } }
    public App Page { get { return GetValue<App>(Template.Page); } set { SetValue(Template.Page, value); } }
    public int PlayerId { get { return GetValue(Template.PlayerId); } set { SetValue(Template.PlayerId, value); } }
    public String FullName { get { return GetValue(Template.FullName); } set { SetValue(Template.FullName, value); } }
    public Listing<PlayerApp.AccountsApp> Accounts { get { return GetValue<PlayerApp.AccountsApp>(Template.Accounts); } set { SetValue<PlayerApp.AccountsApp>(Template.Accounts, value); } }
    public class KidApp : App {
        public static KidAppTemplate DefaultTemplate = new KidAppTemplate();
        public KidApp() {
            Template = DefaultTemplate;
        }
        public new KidAppTemplate Template { get { return (KidAppTemplate)base.Template; } set { base.Template = value; } }
        public new KidAppMetadata Metadata { get { return (KidAppMetadata)base.Metadata; } }
        public String Grandkid { get { return GetValue(Template.Grandkid); } set { SetValue(Template.Grandkid, value); } }
        public class KidAppTemplate : AppTemplate {
            public KidAppTemplate()
                : base() {
                InstanceType = typeof(PlayerApp.KidApp);
                ClassName = "KidApp";
                Grandkid = Register<StringProperty>("Grandkid");
            }
            public override object CreateInstance(AppNode parent) { return new KidApp() { Parent = parent }; }
            public StringProperty Grandkid;
        }
        public class KidAppMetadata : AppMetadata {
            public KidAppMetadata(App app, AppTemplate template) : base(app, template) { }
            public new PlayerApp.KidApp App { get { return (PlayerApp.KidApp)base.App; } }
            public new PlayerApp.KidApp.KidAppTemplate Template { get { return (PlayerApp.KidApp.KidAppTemplate)base.Template; } }
            public StringMetadata Grandkid { get { return __p_Grandkid ?? (__p_Grandkid = new StringMetadata(App, App.Template.Grandkid)); } } private StringMetadata __p_Grandkid;
        }
    }
    public class AccountsApp : App {
        public static AccountsAppTemplate DefaultTemplate = new AccountsAppTemplate();
        public AccountsApp() {
            Template = DefaultTemplate;
        }
        public new AccountsAppTemplate Template { get { return (AccountsAppTemplate)base.Template; } set { base.Template = value; } }
        public new AccountsAppMetadata Metadata { get { return (AccountsAppMetadata)base.Metadata; } }
        public int AccountId { get { return GetValue(Template.AccountId); } set { SetValue(Template.AccountId, value); } }
        public int AccountType { get { return GetValue(Template.AccountType); } set { SetValue(Template.AccountType, value); } }
        public Decimal Balance { get { return GetValue(Template.Balance); } set { SetValue(Template.Balance, value); } }
        public class AccountsAppTemplate : AppTemplate {
            public AccountsAppTemplate()
                : base() {
                InstanceType = typeof(PlayerApp.AccountsApp);
                ClassName = "AccountsApp";
                AccountId = Register<IntProperty>("AccountId");
                AccountType = Register<IntProperty>("AccountType", Editable = true);
                Balance = Register<DecimalProperty>("Balance");
            }
            public IntProperty AccountId;
            public IntProperty AccountType;
            public DecimalProperty Balance;
        }
        public class AccountsAppMetadata : AppMetadata {
            public AccountsAppMetadata(App app, AppTemplate template) : base(app, template) { }
            public new PlayerApp.AccountsApp App { get { return (PlayerApp.AccountsApp)base.App; } }
            public new PlayerApp.AccountsApp.AccountsAppTemplate Template { get { return (PlayerApp.AccountsApp.AccountsAppTemplate)base.Template; } }
            public IntMetadata AccountId { get { return __p_AccountId ?? (__p_AccountId = new IntMetadata(App, App.Template.AccountId)); } } private IntMetadata __p_AccountId;
            public IntMetadata AccountType { get { return __p_AccountType ?? (__p_AccountType = new IntMetadata(App, App.Template.AccountType)); } } private IntMetadata __p_AccountType;
            public DecimalMetadata Balance { get { return __p_Balance ?? (__p_Balance = new DecimalMetadata(App, App.Template.Balance)); } } private DecimalMetadata __p_Balance;
        }
    }
    public class PlayerAppTemplate : AppTemplate {
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
        public PlayerApp.KidApp.KidAppTemplate Kid;
        public AppTemplate Page;
        public IntProperty PlayerId;
        public StringProperty FullName;
        public ListingProperty<PlayerApp.AccountsApp, PlayerApp.AccountsApp.AccountsAppTemplate> Accounts;
    }
    public class PlayerAppMetadata : AppMetadata {
        public PlayerAppMetadata(App app, AppTemplate template) : base(app, template) { }
        public new PlayerApp App { get { return (PlayerApp)base.App; } }
        public new PlayerApp.PlayerAppTemplate Template { get { return (PlayerApp.PlayerAppTemplate)base.Template; } }
        public PlayerApp.KidApp.KidAppMetadata Kid { get { return __p_Kid ?? (__p_Kid = new PlayerApp.KidApp.KidAppMetadata(App, App.Template.Kid)); } } private PlayerApp.KidApp.KidAppMetadata __p_Kid;
        public AppMetadata Page { get { return __p_Page ?? (__p_Page = new AppMetadata(App, App.Template.Page)); } } private AppMetadata __p_Page;
        public IntMetadata PlayerId { get { return __p_PlayerId ?? (__p_PlayerId = new IntMetadata(App, App.Template.PlayerId)); } } private IntMetadata __p_PlayerId;
        public StringMetadata FullName { get { return __p_FullName ?? (__p_FullName = new StringMetadata(App, App.Template.FullName)); } } private StringMetadata __p_FullName;
        public ListingMetadata<PlayerApp.AccountsApp, PlayerApp.AccountsApp.AccountsAppTemplate> Accounts { get { return __p_Accounts ?? (__p_Accounts = new ListingMetadata<PlayerApp.AccountsApp, PlayerApp.AccountsApp.AccountsAppTemplate>(App, App.Template.Accounts)); } } private ListingMetadata<PlayerApp.AccountsApp, PlayerApp.AccountsApp.AccountsAppTemplate> __p_Accounts;
    }
    public static class Json {
        public class Kid : TemplateAttribute {
        }
        public class Accounts : TemplateAttribute {
        }
    }
    public static class Input {
        public static class Kid {
            public class Grandkid : Input<PlayerApp.KidApp, StringProperty, String> {
            }
        }
        public class PlayerId : Input<PlayerApp, IntProperty, int> {
        }
        public class FullName : Input<PlayerApp, StringProperty, String> {
        }
        public static class Accounts {
            public class AccountId : Input<PlayerApp.AccountsApp, IntProperty, int> {
            }
            public class AccountType : Input<PlayerApp.AccountsApp, IntProperty, int> {
            }
            public class Balance : Input<PlayerApp.AccountsApp, DecimalProperty, Decimal> {
            }
        }
    }
}
