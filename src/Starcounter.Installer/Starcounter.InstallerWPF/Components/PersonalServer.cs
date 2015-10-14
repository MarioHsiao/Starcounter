﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Starcounter.InstallerEngine;
using System.Collections;
using Starcounter.Internal;
using System.Globalization;
using System.Windows.Controls;
using Starcounter.InstallerWPF.Rules;

namespace Starcounter.InstallerWPF.Components {
    public class PersonalServer : BaseComponent {

        public const string Identifier = "PersonalServer";

        public override string ComponentIdentifier {
            get {
                return PersonalServer.Identifier;
            }
        }

        public override string Name {
            get {
                return "Server";
            }
        }

        //private bool _SendUsageAndCrashReports;
        //public bool SendUsageAndCrashReports {
        //    get {
        //        return this._SendUsageAndCrashReports;
        //    }
        //    set {
        //        if (this._SendUsageAndCrashReports == value) return;
        //        this._SendUsageAndCrashReports = value;
        //        this.OnPropertyChanged("SendUsageAndCrashReports");
        //    }
        //}

        private readonly string[] _Dependencys = new string[] { InstallationBase.Identifier };
        public override string[] Dependencys {
            get {
                return this._Dependencys;
            }
        }

        protected override void SetDefaultValues() {
            base.SetDefaultValues();

#if !SIMULATE_CLEAN_INSTALLATION
            this.IsInstalled = MainWindow.InstalledComponents[(int)ComponentsCheck.Components.PersonalServer];
#endif

            //this.SendUsageAndCrashReports = true;

            this.Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ConstantsBank.SCProductName);

            this.DefaultUserHttpPort = StarcounterConstants.NetworkPorts.DefaultPersonalServerUserHttpPort;
            this.DefaultSystemHttpPort = StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort;
            this.DefaultAggregationPort = StarcounterConstants.NetworkPorts.DefaultPersonalServerAggregationPort;
            this.DefaultPrologSqlProcessPort = StarcounterConstants.NetworkPorts.DefaultPersonalPrologSqlProcessPort;

            switch (this.Command) {
                case ComponentCommand.Install:
                    this.ExecuteCommand = !this.IsInstalled;
                    break;
                case ComponentCommand.None:
                    this.ExecuteCommand = false;
                    break;
                case ComponentCommand.Uninstall:
                    this.ExecuteCommand = false;
                    break;
                case ComponentCommand.Update:
                    this.ExecuteCommand = false;
                    break;
            }
        }

        public bool HasPath {
            get {
                return !string.IsNullOrEmpty(this.Path);
            }
        }

        private string _Path;
        public string Path {
            get {
                return this._Path;
            }
            set {
                if (string.Compare(this._Path, value) == 0) return;
                this._Path = value;
                this.OnPropertyChanged("Path");
            }
        }

        private UInt16 _DefaultUserHttpPort;
        public UInt16 DefaultUserHttpPort {
            get {
                return _DefaultUserHttpPort;
            }

            set {
                if (_DefaultUserHttpPort == value)
                    return;

                this._DefaultUserHttpPort = value;
                this.OnPropertyChanged("DefaultUserHttpPort");
            }
        }

        private UInt16 _DefaultSystemHttpPort;
        public UInt16 DefaultSystemHttpPort {
            get {
                return _DefaultSystemHttpPort;
            }

            set {
                if (_DefaultSystemHttpPort == value)
                    return;

                this._DefaultSystemHttpPort = value;
                this.OnPropertyChanged("DefaultSystemHttpPort");
            }
        }

        private UInt16 _DefaultAggregationPort;
        public UInt16 DefaultAggregationPort {
            get {
                return _DefaultAggregationPort;
            }

            set {
                if (_DefaultAggregationPort == value)
                    return;

                this._DefaultAggregationPort = value;
                this.OnPropertyChanged("DefaultAggregationPort");
            }
        }

        private UInt16 _DefaultPrologSqlProcessPort;
        public UInt16 DefaultPrologSqlProcessPort {
            get {
                return _DefaultPrologSqlProcessPort;
            }

            set {
                if (_DefaultPrologSqlProcessPort == value)
                    return;

                this._DefaultPrologSqlProcessPort = value;
                this.OnPropertyChanged("DefaultPrologSqlProcessPort");
            }
        }

        public PersonalServer(ObservableCollection<BaseComponent> components)
            : base(components) {
        }

        public override IList<System.Collections.DictionaryEntry> GetProperties() {

            List<DictionaryEntry> properties = new List<DictionaryEntry>();

            properties.Add(new DictionaryEntry("Path", this.Path));

            return properties;

        }

        public override bool ValidateSettings() {

            IsLocalPathRule pathRule = new Rules.IsLocalPathRule();
            if (pathRule.Validate(this.Path, CultureInfo.CurrentCulture).IsValid == false) return false;

            DatabaseRepositoryFolderRule r = new DatabaseRepositoryFolderRule();
            r.UseWarning = true;
            r.CheckEmptyString = true;
            if (r.Validate(this.Path, CultureInfo.CurrentCulture).IsValid == false) return false;

            MainWindow win = System.Windows.Application.Current.MainWindow as MainWindow;
            InstallationBase installationBaseComponent = win.Configuration.Components[InstallationBase.Identifier] as InstallationBase;

            DuplicatPathCheckRule r2 = new DuplicatPathCheckRule();
            r2.Type = DuplicatPathCheckRule.SelfType.SystemServerPath;
            r2.InstallationPath = installationBaseComponent.Path;
            if (r2.Validate(this.Path, CultureInfo.CurrentCulture).IsValid == false) return false;

            PortRule pr = new PortRule();
            if (pr.Validate(this.DefaultUserHttpPort, CultureInfo.CurrentCulture).IsValid == false) return false;

            PortRule pr2 = new PortRule();
            pr2.CheckIfAvailable = true;
            if (pr2.Validate(this.DefaultSystemHttpPort, CultureInfo.CurrentCulture).IsValid == false) return false;

            return true;
        }
    }
}
