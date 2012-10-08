
using System;
using Starcounter.Templates.Interfaces;
using System.ComponentModel;
using Starcounter.Templates;
using Starcounter.Internal.REST;

#if CLIENT
using Starcounter.Client.Template;

namespace Starcounter.Client {
#else

namespace Starcounter {
#endif

    /// <summary>
    /// An App is a live view model object controlled by your C# application code. 
    /// It is mirrored between the server and the client in an MVVM or MVC application.
    /// App objects can be used to drive MVVM views or other such model driven clients.
    /// </summary>
    /// <remarks>
    /// An App object is modelled to simulate a JSON object. The App object can have properties such as strings, booleans
    /// arrays and other app objects. In this way you can build JSON like trees. These trees are then bound to your GUI.
    /// Whenever you change properties in the tree, such as changing values or adding and removing elements in the arrays,
    /// the UI gets updated. Likewise, when the user clicks or writes text inside your UI, the App view model tree gets updated.
    /// This is a very efficient way to connect a user interface to your application logic and will result in clean, simple
    /// and easy to understand and maintain code. This model view controller pattern (MVC) pattern is sometimes referred to as
    /// MVVM (model view view-model) or MDV (model driven views).    /// An App is a view model object (the VM in the MVVM pattern) and the controller of said view model (the C in the MVC pattern).
    /// If your JSON object adds an event (like a command when a button is clicked),
    /// your C# code will be called. If you make a property editable, changes by the user will change App object (and an event will be triggered
    /// in case you which to validate the change). 
    /// An App is a view model object (the VM in the MVVM pattern) and the controller of said view model (the C in the MVC pattern).
    /// If your JSON object adds an event (like a command when a button is clicked),
    /// your C# code will be called. If you make a property editable, changes by the user will change App object (and an event will be triggered
    /// in case you which to validate the change). 
    /// </remarks>
    public partial class App : AppNode
#if IAPP
        , IApp
#endif
    {
       public App() : base() 
       {
           _cacheIndexInList = -1;
       }

       public App(Entity data) : this() {
          Data = data;
       }

       public static HttpRestServer StaticResources;

        static internal void TriggerTypeInitialization() {
            // Calling a static method will trigger type initialization.
            // This is important to detect if the EXE module is running out of process.
            // (so that it can be stopped and restarted inside the database process).
            // Called when the When class is initialized.
        }

        public Boolean IsSerialized { get; internal set; }

        internal Int32 _cacheIndexInList;

        internal override void FillIndexPath(int[] path, int pos)
        {
            if (Parent != null)
            {
                if (Parent is Listing)
                {
                    if (_cacheIndexInList == -1)
                    {
                        _cacheIndexInList = ((Listing)Parent).IndexOf(this);
                    }
                    path[pos] = _cacheIndexInList;
                }
                else
                {
                    path[pos] = Template.Index;
                }
                Parent.FillIndexPath(path, pos - 1);
            }
        }

        private Entity _Data;
        public Entity Data {
            get {
                return _Data;
            }
            set {
                _Data = value;
                OnData();
            }
        }

        protected virtual void Init() {
        }

        protected virtual void OnData() {
        }

        internal void CallInit() {
            Init();
        }

//        public void Input( Input input ) {
//        }
        
        public virtual void Commit() {
        }

        public void Refresh(Template model) {
        }

        public virtual void Abort() {
        }

        public static implicit operator App(string str) {
            return new App() { Media = str };
        }

        /// <summary>
        /// The template defining the properties of this App.
        /// </summary>
        public new AppTemplate Template 
        { 
            get { return (AppTemplate)base.Template; } 
            set { base.Template = value; }
        }

        /// <summary>
        /// Implementation field used to cache the Properties property.
        /// </summary>
        private AppMetadata _Metadata = null;

        /// <summary>
        /// Here you can set properties for each property in this App (such as Editable, Visible and Enabled).
        /// The changes only affect this instance.
        /// If you which to change properties for the template, use the Template property instead.  
        /// </summary>
        /// <remarks>
        /// It is much less expensive to set this kind of metadata for the
        /// entire template (for example to mark a property for all App instances as Editable).
        /// </remarks>
        public AppMetadata Metadata {
            get {
                return _Metadata;
            }
        }

//        /// <summary>
//        /// Use this method to override the default communication from the client.
//        /// </summary>
//        /// <remarks>
//        /// Requests can use the WebSockets or HTTP protocol
//        /// </remarks>
//        /// <param name="request">Can be used to retrieve the data of the request</param>
//        /// <returns>The raw response</returns>
//        public virtual byte[] HandleRawRequest(HttpRequest request) {
//            return null;
//        }

        /// <summary>
        /// If the view lives in this .NET application domain, this property can be used to reference it.
        /// For Starcounter serverside App objects, this property is often a string that is used to identifify
        /// a specific view. For web applications, the string is often a reference to the .html file.
        /// </summary>
        public Media Media { get; set; }

        public string View { get; set; }

#if !CLIENT
        /// <summary>
        /// For convenience, the static SQL function can be called from either the App class,
        /// the Entity class or the Db class. The implementations are identical.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="pars"></param>
        /// <returns></returns>
        public static SqlResult SQL(string str, params object[] pars) {
            return Db.SQL(str,pars);
        }

        public static SqlResult2<T> SQL<T>(string str, params object[] pars) where T:Entity {
            return null;
        }

        public static void Transaction(Action action) {
            Db.Transaction(action);
        }

        public static SqlResult SlowSQL(string str, params object[] pars) {
            return Db.SlowSQL(str, pars);
        }

#endif

        public void Delete() { }

        /// <summary>
        /// Removes this App from its parent.
        /// </summary>
        public void Close() { }

        public void Show() {
        }
    }
}
