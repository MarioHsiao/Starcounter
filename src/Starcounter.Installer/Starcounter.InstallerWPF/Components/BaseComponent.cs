using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections;
using Starcounter.Internal;

namespace Starcounter.InstallerWPF.Components
{
    abstract public class BaseComponent : INotifyPropertyChanged
    {

        #region Properties

        public virtual string ComponentIdentifier
        {
            get
            {
                throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED,
                    "The 'ComponentIdentifier' property must be implemented in the class" + this.GetType().Name);
            }
        }

        public virtual string Name
        {
            get
            {
                throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED,
                    "The 'Name' property must be implemented in the class" + this.GetType().Name);
            }
        }

        virtual public bool ShowProperties
        {
            get
            {
                switch (this.Command)
                {
                    default:
                    case ComponentCommand.None:
                        return false;

                    case ComponentCommand.Install:

                        return !this.IsInstalled && this.ExecuteCommand;


                    case ComponentCommand.Uninstall:
                        return false;

                    case ComponentCommand.Update:
                        return false;
                }

            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can be installed by starcounter installer.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance can be installed; otherwise, <c>false</c>.
        /// </value>
        virtual public bool CanBeInstalled
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can be installed by starcounter installer.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance can be installed; otherwise, <c>false</c>.
        /// </value>
        virtual public bool CanBeUnInstalled
        {
            get
            {
                return true;
            }
        }


        /// <summary>
        /// Gets a value indicating whether this instance can be installed without 
        /// any components refering to this instance
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance can be installed; otherwise, <c>false</c>.
        /// </value>
        virtual public bool CanBeInstalledWithoutReferer
        {
            get
            {
                return true;
            }
        }


        /// <summary>
        /// Gets a value indicating whether this instance can be uninstalled without 
        /// any components refering to this instance
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance can be uninstalled; otherwise, <c>false</c>.
        /// </value>
        virtual public bool CanBeUnInstalledWithoutReferer
        {
            get
            {
                return true;
            }
        }

        private bool _MissingDependency;
        public bool MissingDependency
        {
            get
            {
                return this._MissingDependency;
            }
            protected set
            {
                if (this._MissingDependency == value) return;

                this._MissingDependency = value;
                this.OnPropertyChanged("MissingDependency");
                this.OnPropertyChanged("IsExecuteCommandEnabled");
            }
        }

        private string _MissingDependencyText;
        public string MissingDependencyText
        {
            get
            {
                return this._MissingDependencyText;
            }
            protected set
            {
                if (string.Compare(this._MissingDependencyText, value) == 0) return;

                this._MissingDependencyText = value;
                this.OnPropertyChanged("MissingDependencyText");
                this.OnPropertyChanged("Comment");
            }
        }

        virtual public string Comment
        {
            get
            {

                if (!string.IsNullOrEmpty(this.MissingDependencyText))
                {
                    return this.MissingDependencyText;
                }



                switch (this.Command)
                {
                    default:
                    case ComponentCommand.None:
                        break;

                    case ComponentCommand.Install:
                        if (this.IsInstalled)
                        {
                            return "(Already installed)";
                        }

                        if (this.CanBeInstalled == false)
                        {
                            return "(Installation blocked)";
                        }

                        break;

                    case ComponentCommand.Uninstall:

                        if (this.CanBeUnInstalled == false)
                        {
                            return "(Uninstallation blocked)";
                        }

                        if (!this.IsInstalled)
                        {
                            return "(Not installed)";
                        }

                        break;

                    case ComponentCommand.Update:
                        if (!this.IsInstalled)
                        {
                            return "(Not installed)";
                        }
                        break;
                }

                return string.Empty;

            }

        }

        virtual public bool IsExecuteCommandEnabled
        {
            get
            {

                switch (this.Command)
                {
                    default:
                    case ComponentCommand.None:
                        return false;

                    case ComponentCommand.Install:
                        return !this.IsInstalled && this.MissingDependency == false && this.CanBeInstalled;

                    case ComponentCommand.Uninstall:
                        return this.IsInstalled && this.CanBeUnInstalled;

                    case ComponentCommand.Update:
                        return this.IsInstalled && this.MissingDependency == false;
                }


            }
        }

        private bool _IsInstalled;
        public bool IsInstalled
        {
            get
            {
                return this._IsInstalled;
            }
            set
            {
                if (this._IsInstalled == value) return;
                this._IsInstalled = value;
                this.OnPropertyChanged("IsInstalled");
                this.OnPropertyChanged("CanBeInstalled");
                this.OnPropertyChanged("CanBeUnInstalled");
                this.OnPropertyChanged("IsExecuteCommandEnabled");
                this.OnPropertyChanged("IsAvailable");
            }
        }

        private bool _IsAvailable;
        /// <summary>
        /// Gets or sets a value indicating whether this component is available. (after installation)
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this component is available; otherwise, <c>false</c>.
        /// </value>
        public bool IsAvailable
        {
            get
            {

                switch (this.Command)
                {
                    case ComponentCommand.Install:

                        if (!this.IsInstalled)
                        {

                            if (this.CanBeInstalled && this.ExecuteCommand == true)
                            {
                                this._IsAvailable = true;
                            }
                            else
                            {
                                this._IsAvailable = false;
                            }

                        }
                        else
                        {
                            this._IsAvailable = true;
                        }
                        break;
                    case ComponentCommand.None:
                        this._IsAvailable = true;
                        break;
                    case ComponentCommand.Uninstall:
                        this._IsAvailable = true;
                        break;
                    case ComponentCommand.Update:
                        this._IsAvailable = true;
                        break;
                }



                return this._IsAvailable;
            }

        }

        private ComponentCommand _Command = ComponentCommand.None;
        public ComponentCommand Command
        {
            get { return this._Command; }
            set
            {
                if (this._Command == value) return;
                this._Command = value;
                this.OnPropertyChanged("Command");
                this.OnPropertyChanged("IsExecuteCommandEnabled");
                this.OnPropertyChanged("Comment");
                this.OnPropertyChanged("ShowProperties");
                this.OnPropertyChanged("IsAvailable");
            }
        }

        private bool _ExecuteCommand;
        public bool ExecuteCommand
        {
            get
            {
                return this._ExecuteCommand;
            }
            set
            {
                if (this._ExecuteCommand == value) return;
                this._ExecuteCommand = value;
                this.OnPropertyChanged("ExecuteCommand");
                this.OnPropertyChanged("ShowProperties");
                this.OnPropertyChanged("IsAvailable");
            }
        }

        private readonly string[] _Dependencys = new string[] { };

        public virtual string[] Dependencys
        {
            get
            {
                return this._Dependencys;
            }
        }

        private ObservableCollection<BaseComponent> _References;
        public ObservableCollection<BaseComponent> References
        {
            get
            {
                return this._References;
            }
        }

        private ObservableCollection<BaseComponent> _componentCollection;

        #endregion

        public BaseComponent(ObservableCollection<BaseComponent> components)
        {
            if (components == null)
            {
                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS,
                    "Tried to initialize a component without providing the component collection list");
            }

            this.PropertyChanged += new PropertyChangedEventHandler(BaseComponent_PropertyChanged);

            this._References = new ObservableCollection<BaseComponent>();
            this._References.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(_References_CollectionChanged);

            this.InitComponentCollectionHandling(components);

        }

        void _References_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {


            switch (this.Command)
            {
                case ComponentCommand.Install:

                    if (this.CanBeInstalled && !this.IsInstalled && this.CanBeInstalledWithoutReferer == false)
                    {
                        this.ExecuteCommand = this.References.Count != 0;
                    }

                    break;
                case ComponentCommand.None:
                    break;
                case ComponentCommand.Uninstall:

                    if (this.CanBeUnInstalled && this.IsInstalled && this.CanBeUnInstalledWithoutReferer == false)
                    {
                        this.ExecuteCommand = this.References.Count == 0;
                    }

                    break;
                case ComponentCommand.Update:
                    break;
            }



        }


        void BaseComponent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Command"))
            {
                this.SetDefaultValues();
            }
        }

        #region ComponentCollection Handling

        private void InitComponentCollectionHandling(ObservableCollection<BaseComponent> components)
        {
            this._componentCollection = components;

            foreach (BaseComponent component in this._componentCollection)
            {
                component.PropertyChanged += new PropertyChangedEventHandler(component_PropertyChanged);
            }

            this._componentCollection.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(components_CollectionChanged);

            this.CheckDependencys();

        }

        private void components_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (BaseComponent component in e.NewItems)
                {
                    component.PropertyChanged += new PropertyChangedEventHandler(component_PropertyChanged);
                }
            }


            this.CheckDependencys();
        }

        private void component_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("ExecuteCommand"))
            {
                this.RefreshReferenceCounters();
                this.CheckDependencys();
            }
            else if (e.PropertyName.Equals("Command"))
            {
                this.RefreshReferenceCounters();
                this.CheckDependencys();
            }
        }

        #endregion


        private void RefreshReferenceCounters()
        {

            foreach (string identifier in this.Dependencys)
            {
                BaseComponent depComponent = this.GetComponent(identifier);
                if (depComponent == null) continue;


                switch (this.Command)
                {
                    case ComponentCommand.Install:

                        if (this.ExecuteCommand == true || this.IsInstalled)
                        {
                            if (!depComponent.References.Contains(this))
                            {
                                depComponent.References.Add(this);
                            }
                        }
                        else
                        {
                            if (depComponent.References.Contains(this))
                            {
                                depComponent.References.Remove(this);
                            }
                        }

                        break;
                    case ComponentCommand.None:
                        break;
                    case ComponentCommand.Uninstall:

                        if (this.ExecuteCommand == true || !this.IsInstalled)
                        {
                            if (depComponent.References.Contains(this))
                            {
                                depComponent.References.Remove(this);
                            }
                        }
                        else
                        {
                            if (!depComponent.References.Contains(this))
                            {
                                depComponent.References.Add(this);
                            }
                        }

                        break;
                    case ComponentCommand.Update:
                        break;
                }








            }


        }


        private bool CheckDependencys()
        {
            string comment;
            if (this.Command == ComponentCommand.Install)
            {

                // AND
                foreach (string identifier in this.Dependencys)
                {

                    string[] identifiers = identifier.Split('|');

                    if (identifiers.Length > 1)
                    {
                        // OR
                        bool or_result = this.Check_OR_Dependencys(identifiers, out comment);
                        if (or_result == false)
                        {
                            this.MissingDependencyText = "(" + comment + " is not installed)";
                            this.MissingDependency = true;
                            this.ExecuteCommand = false;
                            return false;
                        }
                        continue;
                    }

                    BaseComponent component = this.GetComponent(identifier);
                    if (component == null)
                    {
                        throw new InvalidOperationException(identifier + " is not registered in the installer");
                    }

                    bool isAvailable = this.CheckIfComponentIsAvailable(component);
                    if (isAvailable == false)
                    {
                        this.MissingDependencyText = "(" + component.Name + " is not installed)";
                        this.MissingDependency = true;
                        this.ExecuteCommand = false;
                        return false;
                    }
                }
            }
            else if (this.Command == ComponentCommand.Uninstall)
            {
                if (this.IsInstalled)
                {
                    // AND
                    foreach (string identifier in this.Dependencys)
                    {

                        string[] identifiers = identifier.Split('|');

                        if (identifiers.Length > 1)
                        {
                            // TODO:
                            // OR
                            //bool or_result = this.Check_OR_Dependencys(identifiers, out comment);
                            //if (or_result == false)
                            //{
                            //    this.MissingDependencyText = "(" + comment + " is not installed)";
                            //    this.MissingDependency = true;
                            //    return false;
                            //}
                            continue;
                        }

                        BaseComponent component = this.GetComponent(identifier);
                        if (component == null)
                        {
                            throw new InvalidOperationException(identifier + " is not registered in the installer");
                        }

                        bool isAvailable = this.CheckIfComponentIsAvailable(component);
                        if (isAvailable == false)
                        {
                            this.MissingDependencyText = "(" + component.Name + " is not available)";
                            this.ExecuteCommand = true;
                            this.MissingDependency = true;
                            return false;
                        }
                    }
                }
            }

            this.MissingDependency = false;
            this.MissingDependencyText = string.Empty;
            return true;
        }


        private bool Check_OR_Dependencys(string[] dependencys, out string comment)
        {
            string or_comment_txt = string.Empty;
            bool validation_result = false;

            comment = string.Empty;

            if (this.Command == ComponentCommand.Install)
            {

                foreach (string identifier in dependencys)
                {
                    BaseComponent component = this.GetComponent(identifier);
                    if (component == null)
                    {
                        throw new InvalidOperationException(identifier + " is not registered in the installer");
                    }

                    if (this.CheckIfComponentIsAvailable(component) == false)
                    {
                        if (!string.IsNullOrEmpty(or_comment_txt))
                        {
                            or_comment_txt += " or ";
                        }

                        or_comment_txt += component.Name;
                    }
                    else
                    {
                        validation_result = true;
                    }

                }
            }

            comment = or_comment_txt;

            return validation_result;
        }


        /// <summary>
        /// Checks if component is available.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>true if component is installed or will be installed, otherwize false</returns>
        private bool CheckIfComponentIsAvailable(BaseComponent component)
        {
            //comment = string.Empty;
            //if (component == null)
            //{
            //    this.MissingDependencyText = "(" + identifier + " not installed)";
            //    this.MissingDependency = true;
            //    return false;
            //}

            switch (component.Command)
            {
                case ComponentCommand.Install:


                    if (!component.IsInstalled)
                    {

                        if (component.CanBeInstalled && component.ExecuteCommand == true)
                        {
                        }
                        else
                        {
                            //comment = component.Name;
                            //this.MissingDependencyText = "(" + component.Name + " not installed)";
                            //this.MissingDependency = true;
                            return false;
                        }

                    }

                    ////if (!component.IsInstalled && component.ExecuteCommand == false && component.CanBeInstalled)
                    //if (!component.IsInstalled && !component.CanBeInstalled)
                    //{
                    //    this.MissingDependencyText = "(" + component.Name + " not installed)";
                    //    this.MissingDependency = true;
                    //    return false;
                    //}

                    break;
                case ComponentCommand.None:
                    break;
                case ComponentCommand.Uninstall:

                    if (component.IsInstalled == false || component.ExecuteCommand == true)
                    {
                        return false;
                    }

                    break;
                case ComponentCommand.Update:
                    break;
            }

            return true;
        }

        private BaseComponent GetComponent(string identifier)
        {

            foreach (BaseComponent component in this._componentCollection)
            {

                if (string.Equals(component.ComponentIdentifier, identifier))
                {
                    return component;
                }

            }

            return null;
        }


        protected virtual void SetDefaultValues()
        {
            this.ExecuteCommand = false;
        }

        public virtual IList<DictionaryEntry> GetProperties()
        {
            return null;
        }

        public abstract bool ValidateSettings();

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string fieldName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(fieldName));
            }
        }

        #endregion

    }

}
