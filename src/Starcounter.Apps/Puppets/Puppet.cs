
using HttpStructs;
using Newtonsoft.Json;
using Starcounter.Advanced;
using Starcounter.Templates;
using System;
using System.ComponentModel;
using System.Text;

namespace Starcounter {


    /// <summary>
    /// See Puppet TODO! REF 
    /// </summary>
    public class Puppet : Puppet<NullData> {


        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String" /> to <see cref="Puppet" />.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Puppet(string str) {
            return new Puppet() { Media = str };
        }
    }

    /// <summary>
    /// A Puppet is a live view model object controlled by your C# application code.
    /// It is mirrored between the server and the client in an MVVM or MVC application.
    /// App objects can be used to drive MVVM views or other such model driven clients.
    /// </summary>
    /// <remarks>An App object is modelled to simulate a JSON object. The App object can have properties such as strings, booleans
    /// arrays and other app objects. In this way you can build JSON like trees. These trees are then bound to your GUI.
    /// Whenever you change properties in the tree, such as changing values or adding and removing elements in the arrays,
    /// the UI gets updated. Likewise, when the user clicks or writes text inside your UI, the App view model tree gets updated.
    /// This is a very efficient way to connect a user interface to your application logic and will result in clean, simple
    /// and easy to understand and maintain code. This model view controller pattern (MVC) pattern is sometimes referred to as
    /// MVVM (model view view-model) or MDV (model driven views).    An App is a view model object (the VM in the MVVM pattern) and the controller of said view model (the C in the MVC pattern).
    /// If your JSON object adds an event (like a command when a button is clicked),
    /// your C# code will be called. If you make a property editable, changes by the user will change App object (and an event will be triggered
    /// in case you which to validate the change).
    /// An App is a view model object (the VM in the MVVM pattern) and the controller of said view model (the C in the MVC pattern).
    /// If your JSON object adds an event (like a command when a button is clicked),
    /// your C# code will be called. If you make a property editable, changes by the user will change App object (and an event will be triggered
    /// in case you which to validate the change).
    /// </remarks>
    public partial class Puppet<T> : Obj<T> where T : IBindable {

        /// <summary>
        /// 
        /// </summary>
        public Puppet() : base() {
                   ViewModelId = -1;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Delete() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes this Obj from its parent.
        /// </summary>
        public void Close() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// If the view lives in this .NET application domain, this property can be used to reference it.
        /// For Starcounter serverside App objects, this property is often a string that is used to identifify
        /// a specific view. For web applications, the string is often a reference to the .html file.
        /// </summary>
        /// <value>The media.</value>
        public Media Media { get; set; }

        /// <summary>
        /// Gets or sets the view.
        /// </summary>
        /// <value>The view.</value>
        public string View { get; set; }


        /// <summary>
        /// Returns the id of this app or -1 if not used.
        /// </summary>
        internal int ViewModelId { get; set; }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String" /> to <see cref="Puppet" />.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Puppet<T>(string str) {
            return new Puppet<T>() { Media = str };
        }

        /// <summary>
        /// Logs the change such that it can be mirrored to the client
        /// </summary>
        /// <param name="property">The property that changed</param>
        protected override void HasChanged(TValue property) {
            ChangeLog.UpdateValue(this, property);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="addComma"></param>
        /// <returns></returns>
        protected override int InsertAdditionalJsonProperties(StringBuilder sb, bool addComma) {

            int t = 0;

            if (ViewModelId != -1) {
                if (addComma)
                    sb.Append(',');
                sb.Append("\"View-Model\":");
                sb.Append(ViewModelId);
                t++;
                addComma = true;
            }

            if (Media.Content != null) {
                if (addComma)
                    sb.Append(',');
                //                if (includeViewContent == IncludeView.Always ) {
                sb.Append("__vc:");
                //return StaticFileServer.GET(relativeUri, request);
                sb.Append(JsonConvert.SerializeObject(Encoding.UTF8.GetString(Media.Content.Uncompressed)));
                //                }
                //                else {
                //                   sb.Append("__vf:");
                //                   sb.Append(JsonConvert.SerializeObject(Media.Content.FilePath.ToString()));
                //                }
                t++;
                addComma = true;
            }
            else {
                //                var view = View ?? templ.PropertyName;

                if (View != null) {
                    if (addComma)
                        sb.Append(',');
                    t++;
                    addComma = true;

#if EMBEDHTML
                    if (false) { // includeViewContent == IncludeView.Always ) { // TODO! JOCKE!!
                        sb.Append("__vc:");
                        var req = HttpRequest.RawGET("/" + View);
                        var res = Puppet.REST.RawRequest( req );
                        if (res == null) {
                            res = StarcounterBase.Fileserver.Handle(new HttpRequest(req));
                        }
                        if (res is HttpResponse) {
                            var response = res as HttpResponse;
                            byte[] body = response.Uncompressed;
                            var html = Encoding.UTF8.GetString(body, response._UncompressedBodyOffset, response._UncompressedBodyLength);
                            sb.Append(JsonConvert.SerializeObject(html));
                        }
                        else {
                            throw new NotImplementedException();
                        }
                    }
#endif
                    //else {
                    //    sb.Append("__vf:");
                    //    sb.Append(JsonConvert.SerializeObject(Media.Content.FilePath.ToString()));
                    //}
                }

            }

            return t;
        }


        /// <summary>
        /// When elements are added to an array, this should be logged such that
        /// the client is updated.
        /// </summary>
        /// <param name="property">The array property of this Puppet</param>
        /// <param name="elementIndex">The added element index</param>
        public override void HasAddedElement(TObjArr property, int elementIndex) {
            ChangeLog.AddItemInList(this, (TObjArr)property, elementIndex);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="elementIndex"></param>
        public override void HasRemovedElement(TObjArr property, int elementIndex) {
            ChangeLog.RemoveItemInList(this, property, elementIndex );
        }


        /// <summary>
        /// Returns true if this puppet have been sent to the client.
        /// </summary>
        public Boolean IsSentExternally { get; internal set; }

        /// <summary>
        /// Commits this instance.
        /// </summary>
        public virtual void Commit() {
            if (_transaction != null) {
                _transaction.Commit();
            }
        }

        /// <summary>
        /// Aborts this instance.
        /// </summary>
        public virtual void Abort() {
            if (_transaction != null) {
                _transaction.Rollback();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        private Transaction _transaction;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        protected override void InternalSetData(IBindable data) {
            if (Transaction == null) {
                Transaction = Transaction._current;
            }
            base.InternalSetData(data);
        }


        /// <summary>
        /// Gets the closest transaction for this app looking up in the tree.
        /// Sets this transaction.
        /// </summary>
        public new Transaction Transaction {
            get {
                if (_transaction != null)
                    return _transaction;

                Obj parent = GetNearestObjParent();
                if (parent != null)
                    return ((Puppet)parent).Transaction;

                return null;
            }
            set {
                if (_transaction != null) {
                    throw new Exception("An transaction is already set for this App. Changing transaction is not allowed.");
                }
                _transaction = value;
            }
        }

        /// <summary>
        /// Returns the transaction that is set on this app. Does NOT
        /// look in parents.
        /// </summary>
        internal Transaction TransactionOnThisApp {
            get { return _transaction; }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>Action.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Action Get(TTrigger property) {
#if QUICKTUPLE
            return _Values[property.Index];
#else
            throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Set(TTrigger property, Action value) {
#if QUICKTUPLE
            _Values[property.Index] = value;
#else
            throw new NotImplementedException();
#endif
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>Action.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Puppet Get(TPuppet property) {
            return Get<Puppet>(property);
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Set(TPuppet property, Puppet value) {
            Set((TObj)property, value);
        }
    }
}
