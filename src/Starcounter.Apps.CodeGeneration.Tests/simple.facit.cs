// This is a system generated file. It reflects the Starcounter App Template defined in the file "simple.json"
// DO NOT MODIFY DIRECTLY - CHANGES WILL BE OVERWRITTEN

using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter;
using Starcounter.Internal;
using Starcounter.Templates;

public partial class PlayerApp {
    public static PlayerTApp DefaultTemplate = new PlayerTApp();
    public PlayerApp() { Template = DefaultTemplate; }
    public PlayerApp(PlayerTApp template) { Template = template; }
    public new PlayerTApp Template { get { return (PlayerTApp)base.Template; } set { base.Template = value; } }
    public new PlayerObjMetadata Metadata { get { return (PlayerObjMetadata)base.Metadata; } }
    public PlayerApp.KidApp Kid { get { return GetValue<PlayerApp.KidApp>(Template.Kid); } set { SetValue(Template.Kid, value); } }
    public App Page { get { return GetValue<App>(Template.Page); } set { SetValue(Template.Page, value); } }
    public int PlayerId { get { return GetValue(Template.PlayerId); } set { SetValue(Template.PlayerId, value); } }
    public String FullName { get { return GetValue(Template.FullName); } set { SetValue(Template.FullName, value); } }
    public Arr<PlayerApp.AccountsApp> Accounts { get { return GetValue<PlayerApp.AccountsApp>(Template.Accounts); } set { SetValue<PlayerApp.AccountsApp>(Template.Accounts, value); } }
    public class KidApp : App {
        public static KidTApp DefaultTemplate = new KidTApp();
        public KidApp() { Template = DefaultTemplate; }
        public KidApp(KidTApp template) { Template = template; }
        public new KidTApp Template { get { return (KidTApp)base.Template; } set { base.Template = value; } }
        public new KidObjMetadata Metadata { get { return (KidObjMetadata)base.Metadata; } }
        public new PlayerApp Parent { get { return (PlayerApp)base.Parent; } set { base.Parent = value; } }
        public String Grandkid { get { return GetValue(Template.Grandkid); } set { SetValue(Template.Grandkid, value); } }
        public class KidTApp : TApp {
            public KidTApp()
                : base() {
                InstanceType = typeof(PlayerApp.KidApp);
                ClassName = "KidApp";
                Grandkid = Register<TString>("Grandkid", "Grandkid");
            }
            public override object CreateInstance(Container parent) { return new KidApp(this) { Parent = (PlayerApp)parent }; }
            public TString Grandkid;
        }
        public class KidObjMetadata : ObjMetadata {
            public KidObjMetadata(App app, TApp template) : base(app, template) { }
            public new PlayerApp.KidApp App { get { return (PlayerApp.KidApp)base.App; } }
            public new PlayerApp.KidApp.KidTApp Template { get { return (PlayerApp.KidApp.KidTApp)base.Template; } }
            public StringMetadata Grandkid { get { return __p_Grandkid ?? (__p_Grandkid = new StringMetadata(App, App.Template.Grandkid)); } }
            private StringMetadata __p_Grandkid;
        }
    }
    public class AccountsApp : App {
        public static AccountsTApp DefaultTemplate = new AccountsTApp();
        public AccountsApp() { Template = DefaultTemplate; }
        public AccountsApp(AccountsTApp template) { Template = template; }
        public new AccountsTApp Template { get { return (AccountsTApp)base.Template; } set { base.Template = value; } }
        public new AccountsObjMetadata Metadata { get { return (AccountsObjMetadata)base.Metadata; } }
        public new Arr<PlayerApp.AccountsApp> Parent { get { return (Arr<PlayerApp.AccountsApp>)base.Parent; } set { base.Parent = value; } }
        public int AccountId { get { return GetValue(Template.AccountId); } set { SetValue(Template.AccountId, value); } }
        public int AccountType { get { return GetValue(Template.AccountType); } set { SetValue(Template.AccountType, value); } }
        public Decimal Balance { get { return GetValue(Template.Balance); } set { SetValue(Template.Balance, value); } }
        public class AccountsTApp : TApp {
            public AccountsTApp()
                : base() {
                InstanceType = typeof(PlayerApp.AccountsApp);
                ClassName = "AccountsApp";
                AccountId = Register<TLong>("AccountId", "AccountId");
                AccountType = Register<TLong>("AccountType$", "AccountType", Editable = true);
                Balance = Register<TDecimal>("Balance", "Balance");
            }
            public override object CreateInstance(Container parent) { return new AccountsApp(this) { Parent = (Arr<PlayerApp.AccountsApp>)parent }; }
            public TLong AccountId;
            public TLong AccountType;
            public TDecimal Balance;
        }
        public class AccountsObjMetadata : ObjMetadata {
            public AccountsObjMetadata(App app, TApp template) : base(app, template) { }
            public new PlayerApp.AccountsApp App { get { return (PlayerApp.AccountsApp)base.App; } }
            public new PlayerApp.AccountsApp.AccountsTApp Template { get { return (PlayerApp.AccountsApp.AccountsTApp)base.Template; } }
            public IntMetadata AccountId { get { return __p_AccountId ?? (__p_AccountId = new IntMetadata(App, App.Template.AccountId)); } }
            private IntMetadata __p_AccountId;
            public IntMetadata AccountType { get { return __p_AccountType ?? (__p_AccountType = new IntMetadata(App, App.Template.AccountType)); } }
            private IntMetadata __p_AccountType;
            public DecimalMetadata Balance { get { return __p_Balance ?? (__p_Balance = new DecimalMetadata(App, App.Template.Balance)); } }
            private DecimalMetadata __p_Balance;
        }
    }
    public class PlayerTApp : TApp {
        public PlayerTApp()
            : base() {
            InstanceType = typeof(PlayerApp);
            ClassName = "PlayerApp";
            Kid = Register<PlayerApp.KidApp.KidTApp>("Kid", "Kid");
            Page = Register<TApp>("Page", "Page");
            PlayerId = Register<TLong>("PlayerId", "PlayerId");
            FullName = Register<TString>("FullName$", "FullName", Editable = true);
            Accounts = Register<TArr<PlayerApp.AccountsApp, PlayerApp.AccountsApp.AccountsTApp>>("Accounts", "Accounts");
            Accounts.App = PlayerApp.AccountsApp.DefaultTemplate;
        }
        public override object CreateInstance(Container parent) { return new PlayerApp(this) { Parent = parent }; }
        public PlayerApp.KidApp.KidTApp Kid;
        public TApp Page;
        public TLong PlayerId;
        public TString FullName;
        public TArr<PlayerApp.AccountsApp, PlayerApp.AccountsApp.AccountsTApp> Accounts;
    }
    public class PlayerObjMetadata : ObjMetadata {
        public PlayerObjMetadata(App app, TApp template) : base(app, template) { }
        public new PlayerApp App { get { return (PlayerApp)base.App; } }
        public new PlayerApp.PlayerTApp Template { get { return (PlayerApp.PlayerTApp)base.Template; } }
        public PlayerApp.KidApp.KidObjMetadata Kid { get { return __p_Kid ?? (__p_Kid = new PlayerApp.KidApp.KidObjMetadata(App, App.Template.Kid)); } }
        private PlayerApp.KidApp.KidObjMetadata __p_Kid;
        public ObjMetadata Page { get { return __p_Page ?? (__p_Page = new ObjMetadata(App, App.Template.Page)); } }
        private ObjMetadata __p_Page;
        public IntMetadata PlayerId { get { return __p_PlayerId ?? (__p_PlayerId = new IntMetadata(App, App.Template.PlayerId)); } }
        private IntMetadata __p_PlayerId;
        public StringMetadata FullName { get { return __p_FullName ?? (__p_FullName = new StringMetadata(App, App.Template.FullName)); } }
        private StringMetadata __p_FullName;
        public ArrMetadata<PlayerApp.AccountsApp, PlayerApp.AccountsApp.AccountsTApp> Accounts { get { return __p_Accounts ?? (__p_Accounts = new ArrMetadata<PlayerApp.AccountsApp, PlayerApp.AccountsApp.AccountsTApp>(App, App.Template.Accounts)); } }
        private ArrMetadata<PlayerApp.AccountsApp, PlayerApp.AccountsApp.AccountsTApp> __p_Accounts;
    }
    public static class Json {
        public class Kid : TemplateAttribute {
        }
        public class Accounts : TemplateAttribute {
        }
    }
    public static class Input {
        public static class Kid {
            public class Grandkid : Input<PlayerApp.KidApp, TString, String> {
            }
        }
        public class PlayerId : Input<PlayerApp, TLong, int> {
        }
        public class FullName : Input<PlayerApp, TString, String> {
        }
        public static class Accounts {
            public class AccountId : Input<PlayerApp.AccountsApp, TLong, int> {
            }
            public class AccountType : Input<PlayerApp.AccountsApp, TLong, int> {
            }
            public class Balance : Input<PlayerApp.AccountsApp, TDecimal, Decimal> {
            }
        }
    }
}
