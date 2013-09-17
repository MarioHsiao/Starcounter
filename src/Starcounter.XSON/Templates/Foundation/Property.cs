// ***********************************************************************
// <copyright file="ValueTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.Advanced.XSON;
using Starcounter.Advanced;
using Starcounter.Internal.XSON;
using Starcounter.Internal;

namespace Starcounter {

    public abstract class PrimitiveProperty<T> : Property<T> {
        public override bool IsPrimitive {
            get { return true; }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">The primitive system type of this property.</typeparam>
    public abstract class Property<T> : TValue {
        public Func<Json, Property<T>, T, Input<T>> CustomInputEventCreator = null;
        public List<Action<Json, Input<T>>> CustomInputHandlers = new List<Action<Json, Input<T>>>();

        internal override bool UseBinding(IBindable data) {
			if (data == null)
				return false;
            return DataBindingFactory.VerifyOrCreateBinding<T>(this, data.GetType());
        }

        /// <summary>
        /// Adds an inputhandler to this property.
        /// </summary>
        /// <param name="createInputEvent"></param>
        /// <param name="handler"></param>
        public void AddHandler(
            Func<Json, Property<T>, T, Input<T>> createInputEvent = null,
            Action<Json, Input<T>> handler = null) {
            this.CustomInputEventCreator = createInputEvent;
            this.CustomInputHandlers.Add(handler);
        }

        internal override object GetBoundValueAsObject(Json obj) {
            return obj.GetBound<T>(this);
        }

        internal override void SetBoundValueAsObject(Json obj, object value) {
            obj.SetBound<T>(this, (T)value);
        }
	}

    /// <summary>
    /// Class Property
    /// </summary>
    public abstract class TValue : Template {
        internal Bound _Bound = Bound.UseParent;
        private string _Bind;
		internal bool invalidateBinding;

        internal DataValueBinding dataBinding;

//        protected TValue() {
//            IsEnumerable = true;
//        }

        /// <summary>
        /// Gets a value indicating whether this instance has instance value on client.
        /// </summary>
        /// <value><c>true</c> if this instance has instance value on client; otherwise, <c>false</c>.</value>
        public override bool HasInstanceValueOnClient {
            get { return true; }
        }

		internal virtual bool UseBinding(IBindable data) {
			if (data == null) return false;
            return DataBindingFactory.VerifyOrCreateBinding(this, data.GetType());
        }

        /// <summary>
        /// Gets or sets the name of the property this template is bound to.
        /// </summary>
        /// <value>The name of the property to bind.</value>
        public string Bind {
            get { return _Bind; }
            set {
                _Bind = value;
                var b = !string.IsNullOrEmpty(_Bind);
                if (b) {
                    _Bound = Bound.Yes;
                }
                else {
                    _Bound = Bound.No;
                }
				invalidateBinding = true;
            }
        }

//        /// <summary>
//        /// System defined properties such as Html and HtmlContent are
//        /// not visible when you query the Object tempate for properties.
//        /// This is important such that these properies stay out of harms
//        /// way for messages where they are not used. See also IsHiddenIfNull.
//        /// System defined properties such as Html and HtmlContent are
//        /// not materialized when serializing JSON unless they are set to a
//        /// value other than null.
//        /// </summary>
//        public bool IsEnumerable { get; set; }

        /// <summary>
        /// Gets a value indicating whether this template is bound.
        /// </summary>
        /// <value><c>true</c> if bound; otherwise, <c>false</c>.</value>
        public Bound Bound { 
			get {
				if (_Bound == Templates.Bound.UseParent) {
					var parent = Parent;
					if (parent == null)
						return Bound.No;

					while (!(parent is TObject))
						parent = Parent;
					return ((TObject)parent).BindChildren;
				}
				
				return _Bound; 
			} 
			set {
				_Bound = value;

				// After we set the value we retrieve it again just to get the correct
				// binding to use in case we read the value from the parent.
				var real = Bound;
				if (real == Templates.Bound.No)
					_Bind = null;
				else
					_Bind = PropertyName;
				invalidateBinding = true;
			} 
		}

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// TODO!
        /// Should be changed to accept IntPtr to rawValue using an int size parameter. This way is not fast enough.
        /// </remarks>
        /// <param name="obj"></param>
        /// <param name="rawValue"></param>
        public abstract void ProcessInput(Json obj, Byte[] rawValue);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toTemplate"></param>
        public override void CopyTo(Template toTemplate) {
            base.CopyTo(toTemplate);
            ((TValue)toTemplate).Bind = Bind;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal virtual object GetBoundValueAsObject(Json obj) {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        internal virtual void SetBoundValueAsObject(Json obj, object value) {
            throw new NotSupportedException();
        }



    }
}
