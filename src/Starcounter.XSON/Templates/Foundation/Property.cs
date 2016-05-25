// ***********************************************************************
// <copyright file="ValueTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics;
using Starcounter.XSON;
using Starcounter.Internal;

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
				parent.MarkAsDirty(TemplateIndex);

			parent.CallHasChanged(this);
		}
        
        protected abstract bool ValueEquals(T value1, T value2);

        /// <summary>
        ///
        /// </summary>
        public T DefaultValue { 
            get; 
            set; 
        }

        internal override void SetDefaultValue(Json parent, bool markAsReplaced = false) {
            if (markAsReplaced) {
                bool hasChanged = !(ValueEquals(UnboundGetter(parent), DefaultValue));
                UnboundSetter(parent, DefaultValue);

                if (hasChanged) {
                    if (parent.HasBeenSent)
                        parent.MarkAsDirty(this.TemplateIndex);
                    parent.CallHasChanged(this);
                }
            } else {
                UnboundSetter(parent, DefaultValue);
            }
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
        /// <returns></returns>
        internal override bool HasBinding() {
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
        /// If the property is bound, it reads the bound value and stores it
        /// using the unbound delegate and marks the property as cached. 
        /// All reads after this will read the from the unbound delegate,
        /// until the cache is resetted when checkpointing.
        /// </summary>
        /// <param name="json"></param>
        internal void SetCachedReads(Json json) {
            // We don't have to check if th property is already cached.
            // That is done when checking if binding should be used.
            if (json.IsTrackingChanges && UseBinding(json) && json.Session.EnableCachedReads) {
                UnboundSetter(json, BoundGetter(json));
                json.MarkAsCached(this.TemplateIndex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        internal override void Checkpoint(Json parent) {
            // We don't have to check if the property is cached.
            // That is done when checking if binding should be used.
            if (UseBinding(parent)) {
                UnboundSetter(parent, BoundGetter(parent));
            }
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
                
                if (!ValueEquals(boundValue, oldValue)) {
                    UnboundSetter(parent, boundValue);
                    if (addToChangeLog)
                        parent.ChangeLog.UpdateValue(parent, this);
                }

                if (parent.Session.EnableCachedReads)
                    parent.MarkAsCached(this.TemplateIndex);
            }
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
        /// Invoking user provided input handler respecting application name.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="input"></param>
        void InvokeHandler(Json parent, Input<T> input) {

            // Setting the application name of the input handler owner.
            String savedAppName = StarcounterEnvironment.AppName;
            try {
                StarcounterEnvironment.AppName = parent.appName;
                _inputHandler.Invoke(parent, input);
            } finally {
                StarcounterEnvironment.AppName = savedAppName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="value"></param>
        public void ProcessInput(Json parent, T value) {
            Input<T> input = null;

            if (_inputEventCreator != null) {
                input = _inputEventCreator.Invoke(parent, this, value);
                input.ValueChanged = false;
            }

            if (input != null && _inputHandler != null) {
                input.OldValue = Getter(parent);

                InvokeHandler(parent, input);

                if (!input.Cancelled) {
                    Debug.WriteLine("Setting value after custom handler: " + input.Value);
                    Setter(parent, input.Value);

                    // Check if incoming value is set without change. If so remove the dirtyflag.
                    if (!input.ValueChanged && this.ValueEquals(input.Value, Getter(parent))) 
                        this.Checkpoint(parent);
                } else {
                    Debug.WriteLine("Handler cancelled: " + value);
                    // Incoming value was cancelled. Mark as dirty so client gets the correct value.
                    parent.MarkAsDirty(this);
                }
            } else {
                if (BasedOn == null) {
                    Debug.WriteLine("Setting value after no handler: " + value);
                    Setter(parent, value);
                    
                    // Check if incoming value is set without change. If so remove the dirtyflag.
                    if (this.ValueEquals(value, Getter(parent)))
                        this.Checkpoint(parent); 
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

                InvokeHandler(parent, input);

                if (!input.Cancelled) {
                    existingInput.Value = input.Value;
                    existingInput.ValueChanged = input.ValueChanged;
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
