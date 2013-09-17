// ***********************************************************************
// <copyright file="Obj.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Advanced;
using Starcounter.Templates;
using System;
using Starcounter.Internal;
using Starcounter.Templates.Interfaces;
using System.Runtime.CompilerServices;

namespace Starcounter {
    /// <summary>
    /// A Json object represents tree of values that can be serialized to or from a 
    /// JSON string.
    /// 
    /// A value can be a primitive such as numbers, strings and booleans or they can be other Json objects.or
    /// arrays of values.
    /// 
    /// The objects mimics the types of trees inducable from the JSON string format.
    /// The difference from the Json induced object tree in Javascript is
    /// foremost that Obj supports multiple numeric types, time and higher precision numerics.
    /// 
    /// While JSON is a text based notation format, the Json class is a materialized
    /// tree of arrays, objects and primitive values than can be serialized 
    /// to and from the corresponding JSON text.
    /// 
    /// The types supported are:
    ///
    /// Object			    (can contain properties of any supported type)
    /// List			    (typed array/list/vector of any supported type),
    /// null            
    /// Time 			    (datetime)
    /// Boolean
    /// String 			    (variable length Unicode string),
    /// Integer 		    (variable length up to 64 bit, signed)
    /// Unsigned Integer	(variable length up to 64 bit, unsigned)
    /// Decimal			    (base-10 floating point up to 64 bit),
    /// Float			    (base-2 floating point up to 64 bit)
    /// </summary>
    /// <remarks>
    /// The current implementation has a few shortcommings. Currently Json only supports arrays of objects.
    /// Also, all objects in the array must use the same Schema.
    /// 
    /// In the release version of Starcounter, Obj objects trees will be optimized for storage in "blobs" rather than on
    /// the garbage collected heap. This is such that stateful sessions can employ them without causing unnecessary system
    /// stress.
    //
    /// A Json object can be data bound to a database object such as its bound properties
    /// merely reflect the values of the database objects.
    /// </remarks>
    public partial class Json {

        /// <summary>
        /// Base classes to be derived by Json-by-example classes.
        /// </summary>
        public static class JsonByExample {
            /// <summary>
            /// Used by to support inheritance when using Json-by-example compiler
            /// </summary>
            /// <typeparam name="JsonType">The Json instances described by this schema</typeparam>
            public class Schema : Starcounter.Templates.TObject {
            }

            /// <summary>
            /// Used by to support inheritance when using Json-by-example compiler
            /// </summary>
            /// <typeparam name="JsonType">The Json instances described by this schema</typeparam>
            public class Metadata<SchemaType, JsonType> : Starcounter.Templates.ObjMetadata<SchemaType, JsonType>
                where SchemaType : Starcounter.Templates.TObject
                where JsonType : Json {

                public Metadata(JsonType app, SchemaType template) : base(app, template) { }
            }
        }

        /// <summary>
        /// Static constructor to automatically initialize XSON.
        /// </summary>
        static Json() {
            // TODO! Whay is this and why is it needed?
            HelperFunctions.LoadNonGACDependencies();
            //    XSON.CodeGeneration.Initializer.InitializeXSON();
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="Obj" /> class.
        /// </summary>
        public Json()
            : base() {
            _cacheIndexInArr = -1;
            _transaction = null;
            //LogChanges = false;
            if (_Template == null) {
                Template = GetDefaultTemplate();
            }
        }

        /// <summary>
        /// Returns True if current Obj is within the given tree.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Boolean HasThisRoot(Json treeRoot) {
            Json r = this;
            while (r.Parent != null)
                r = r.Parent;
            Json root = (Json)r;

            if (treeRoot == root)
                return true;

            return false;
        }




        protected virtual Template GetDefaultTemplate() {
            return null;
        }

        /// <summary>
        /// Refreshes the specified property of this Obj.
        /// </summary>
        /// <param name="property">The property</param>
        public void Refresh(Template property) {
            if (property is TObjArr) {
                TObjArr apa = (TObjArr)property;
                this.Set(apa, this.GetBound(apa));
            }
            else if (property is TObject) {
                var at = (TObject)property;
                IBindable v = this.GetBound(at);
                this.Set(at, v);
            }
            else {
                TValue p = property as TValue;
                if (p != null) {
                    HasChanged(p);
                }
            }
        }

        /// <summary>
        /// Is overridden by Puppet to log changes.
        /// </summary>
        /// <remarks>
        /// The puppet needs to log all changes as they will need to be sent to the client (the client keeps a mirrored view model).
        /// See MVC/MVVM (TODO! REF!). See Puppets (TODO REF)
        /// </remarks>
        /// <param name="property">The property that has changed in this Obj</param>
        protected virtual void HasChanged(TValue property) {
            //throw new Exception();
            //var s = Session;
            //if (s!=null)
            //    Session.UpdateValue(this, property);
        }

        //        /// <summary>
        //        /// The template defining the schema (properties) of this Obj.
        //        /// </summary>
        //        /// <value>The template</value>
        //        public new Schema Template {
        //            get { return (Schema)base.Template; }
        //            set { base.Template = value; }
        //        }

        /// <summary>
        /// Here you can set properties for each property in this Obj (such as Editable, Visible and Enabled).
        /// The changes only affect this instance.
        /// If you which to change properties for the template, use the Template property instead.
        /// </summary>
        /// <value>The metadata.</value>
        /// <remarks>It is much less expensive to set this kind of metadata for the
        /// entire template (for example to mark a property for all Obj instances as Editable).</remarks>
        public ObjMetadata<TObject, Json> Metadata {
            get {
                return _Metadata;
            }
        }

        //		/// <summary>
        //		/// If set true and a ChangeLog is set on the current thread, all 
        //		/// changes done to this Obj will be logged.
        //		/// </summary>
        //		public bool LogChanges { get; set; }

        //        public virtual void ProcessInput<V>(TValue<V> template, V value) {
        //        }


    }
}
