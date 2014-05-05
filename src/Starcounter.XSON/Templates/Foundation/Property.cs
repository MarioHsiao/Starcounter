﻿// ***********************************************************************
// <copyright file="ValueTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Starcounter.XSON;

namespace Starcounter.Templates {
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">The primitive system type of this property.</typeparam>
    public abstract class Property<T> : TValue {
		public Action<Json, T> Setter;
		public Func<Json, T> Getter;
		internal Action<Json, T> BoundSetter;
		internal Func<Json, T> BoundGetter;
		internal Action<Json, T> UnboundSetter;
		internal Func<Json, T>  UnboundGetter;  
        private Func<Json, Property<T>, T, Input<T>> _inputEventCreator;
        private Action<Json, Input<T>> _inputHandler;

		public Property() {
			Getter = BoundOrUnboundGet;
			Setter = BoundOrUnboundSet;
		}

		private T BoundOrUnboundGet(Json parent) {
			if (UseBinding(parent))
				return BoundGetter(parent);
			return UnboundGetter(parent);
		}

		private void BoundOrUnboundSet(Json parent, T value) {
			if (UseBinding(parent)) {
				if (BoundSetter != null)
					BoundSetter(parent, value);
			} else
				UnboundSetter(parent, value);

			if (parent.HasBeenSent)
				parent.MarkAsReplaced(TemplateIndex);

			parent.CallHasChanged(this);
		}

		/// <summary>
		/// Sets the getter and setter delegates for unbound values to the submitted delegates.
		/// </summary>
		/// <param name="getter"></param>
		/// <param name="setter"></param>
		public void SetCustomAccessors(Func<Json, T> getter, 
									   Action<Json, T> setter,
									   bool overwriteExisting = true) {
			bool overwrite = (overwriteExisting || !hasCustomAccessors);

			if (BindingStrategy == BindingStrategy.Unbound) {
				if (overwrite || Getter == null)
					Getter = getter;
				if (overwrite || Setter == null)
					Setter = setter;
			}

			if (overwrite || UnboundGetter == null) {
				UnboundGetter = getter;
#if DEBUG
				DebugUnboundGetter = "<custom>";
#endif
			}

			if (overwrite || UnboundSetter == null) {
				UnboundSetter = setter;
#if DEBUG
				DebugUnboundSetter = "<custom>";
#endif
			}

			hasCustomAccessors = true;
		}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="parent"></param>
        //internal override void SetDefaultValue(Json parent) {
        //    UnboundSetter(parent, (T)CreateInstance(parent));
        //}

		/// <summary>
		/// 
		/// </summary>
		internal override void InvalidateBoundGetterAndSetter() {
			BoundGetter = null;
			BoundSetter = null;
			base.InvalidateBoundGetterAndSetter();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		internal override bool GenerateBoundGetterAndSetter(Json parent) {
			TemplateDelegateGenerator.GenerateBoundDelegates<T>(this, parent);
			return (BoundGetter != null);
		}

		/// <summary>
		/// 
		/// </summary>
		internal override void GenerateUnboundGetterAndSetter() {
			if (UnboundGetter == null)
				TemplateDelegateGenerator.GenerateUnboundDelegates<T>(this, false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		/// <returns></returns>
		internal override object GetUnboundValueAsObject(Json parent) {
			return UnboundGetter(parent);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		/// <returns></returns>
		internal override object GetValueAsObject(Json parent) {
			return Getter(parent);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="value"></param>
		internal override void SetValueAsObject(Json parent, object value) {
			Setter(parent, (T)value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		internal override void Checkpoint(Json parent) {
			if (UseBinding(parent))
				UnboundSetter(parent, BoundGetter(parent));
			base.Checkpoint(parent);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="addToChangeLog"></param>
		internal override void CheckAndSetBoundValue(Json parent, bool addToChangeLog) {
			if (UseBinding(parent)) {
				T boundValue = BoundGetter(parent);
				T oldValue = UnboundGetter(parent);

				// Since all values except string are valuetypes (and cannot be null),
				// the default implementation does no nullchecks. This method is overriden
				// in TString where we check for null as well.
				if (!boundValue.Equals(oldValue)) {
					UnboundSetter(parent, boundValue);
					if (addToChangeLog)
						parent.Session.UpdateValue(parent, this);
				}
			}
		}

		internal override string ValueToJsonString(Json parent) {
		    return Getter(parent).ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="from"></param>
		internal override void CopyValueDelegates(Template toTemplate) {
			var p = toTemplate as Property<T>;
			if (p != null) {
				p.UnboundGetter = UnboundGetter;
				p.UnboundSetter = UnboundSetter;
				p.hasCustomAccessors = hasCustomAccessors;

#if DEBUG
				p.DebugUnboundGetter = DebugUnboundGetter;
				p.DebugUnboundSetter = DebugUnboundSetter;
#endif
			}
		}

        /// <summary>
        /// Adds an inputhandler to this property.
        /// </summary>
        /// <param name="createInputEvent"></param>
        /// <param name="handler"></param>
        public void AddHandler(Func<Json, Property<T>, T, Input<T>> createInputEvent,
                               Action<Json, Input<T>> handler) {
            _inputEventCreator = createInputEvent;
            _inputHandler = handler;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="value"></param>
        public void ProcessInput(Json parent, T value) {
            Input<T> input = null;

            if (_inputEventCreator != null)
                input = _inputEventCreator.Invoke(parent, this, value);

            if (input != null && _inputHandler != null) {
                input.OldValue = Getter(parent);
                _inputHandler.Invoke(parent, input);

                if (!input.Cancelled) {
                    Debug.WriteLine("Setting value after custom handler: " + input.Value);
                    Setter(parent, input.Value);
                } else {
                    Debug.WriteLine("Handler cancelled: " + value);
                }
            } else {
                if (BasedOn == null) {
                    Debug.WriteLine("Setting value after no handler: " + value);
                    Setter(parent, value);
                } else {
                    // This is an inherited template with no inputhandler, lets 
                    // see if the base-template has a registered handler.
                    ((Property<T>)BasedOn).ProcessInput(parent, value);
                }
            }
        }

        internal void ProcessInput(Json parent, Input<T> existingInput) {
            Input<T> input = null;

            if (_inputEventCreator != null)
                input = _inputEventCreator.Invoke(parent, this, existingInput.Value);

            if (input != null && _inputHandler != null) {
                input.OldValue = existingInput.OldValue;
                _inputHandler.Invoke(parent, input);

                if (!input.Cancelled) {
                    existingInput.Value = input.Value;
                } else {
                    existingInput.Cancel();
                }
            } 
        }
	}

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class PrimitiveProperty<T> : Property<T> {
		public override bool IsPrimitive {
			get { return true; }
		}
	}
}
