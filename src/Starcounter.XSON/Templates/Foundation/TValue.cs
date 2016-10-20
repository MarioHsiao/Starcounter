using System;
using Starcounter.Advanced.XSON;
using Module = Starcounter.Internal.XSON.Modules.Starcounter_XSON;

namespace Starcounter.Templates {
    /// <summary>
    /// 
    /// </summary>
    public abstract class TValue : Template {
		private BindingStrategy strategy = BindingStrategy.UseParent;
		private string bind;
        private bool forceGenerateBindings = true;
        protected Type jsonType;
		internal Type dataTypeForBinding;
		internal bool isVerifiedUnbound;
        internal bool isBoundToParent;
		internal bool hasCustomAccessors;

#if DEBUG
		internal string DebugBoundSetter;
		internal string DebugBoundGetter;
		internal string DebugUnboundSetter;
		internal string DebugUnboundGetter;
#endif

		/// <summary>
		/// Gets a value indicating whether this instance has instance value on client.
		/// </summary>
		/// <value><
		/// c>true</c> if this instance has instance value on client; otherwise, <c>false</c>.                         
		/// </value>
		public override bool HasInstanceValueOnClient {
			get { return true; }
		}

		/// <summary>
		/// Gets or sets the name of the property this template is bound to.
		/// </summary>
		/// <value>The name of the property to bind.</value>
		public string Bind {
			get {
				if (bind == null && BindingStrategy != BindingStrategy.Unbound) {
					return this.PropertyName;
				}
				return bind;
			}
			set {
				bind = value;
				var b = !string.IsNullOrEmpty(bind);
				if (b) {
					strategy = BindingStrategy.Bound;
				} else {
					strategy = BindingStrategy.Unbound;
				}
                
                InvalidateBoundGetterAndSetter();
			}
		}

		/// <summary>
		/// Gets a value indicating how this template should handle binding
		/// to a dataobject.
		/// </summary>
		/// <value></value>
		public BindingStrategy BindingStrategy {
			get {
				if (strategy == Templates.BindingStrategy.UseParent) {
					var parent = Parent;
					if (parent == null)
						return BindingStrategy.Auto;

					while (!(parent is TObject))
						parent = parent.Parent;
					return ((TObject)parent).BindChildren;
				}

				return strategy;
			}
			set {				
				strategy = value;

				// After we set the value we retrieve it again just to get the correct
				// binding to use in case we read the value from the parent.
				var real = BindingStrategy;
				if (real == Templates.BindingStrategy.Unbound)
					bind = null;
				else if (bind == null)
					bind = PropertyName;
                
                InvalidateBoundGetterAndSetter();
			}
		}


        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get {
                if (jsonType == null)
                    return DefaultInstanceType;
                return jsonType;
            }
            set { jsonType = value; }
        }

        internal virtual Type DefaultInstanceType {
            get { return typeof(Json); }
        }

		/// <summary>
		/// 
		/// </summary>
		internal abstract bool GenerateBoundGetterAndSetter(Json parent);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="json"></param>
		internal abstract void GenerateUnboundGetterAndSetter();

		/// <summary>
		/// 
		/// </summary>
		internal abstract void CheckAndSetBoundValue(Json parent, bool addToChangeLog);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		/// <returns></returns>
		internal abstract object GetUnboundValueAsObject(Json parent);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		/// <returns></returns>
		internal abstract object GetValueAsObject(Json parent);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="value"></param>
		internal abstract void SetValueAsObject(Json parent, object value);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		internal abstract void SetDefaultValue(Json parent, bool markAsReplaced = false);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="from"></param>
		internal abstract void CopyValueDelegates(Template toTemplate);

        /// <summary>
        /// 
        /// </summary>
        internal virtual void InvalidateBoundGetterAndSetter() {
            isBoundToParent = false;
            isVerifiedUnbound = false;
            dataTypeForBinding = null;
            forceGenerateBindings = true;
        }

		/// <summary>
		/// 
		/// </summary>
		internal virtual void OnPropertySet(Json parent) {
			if (parent.HasBeenSent)
				parent.MarkAsDirty(TemplateIndex);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="json"></param>
		internal virtual void Checkpoint(Json parent) {
			parent.CheckpointAt(TemplateIndex);
		}

        public virtual object CreateInstance(Json parent = null) {
            return new Json() { Template = this, Parent = parent };
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="toTemplate"></param>
		public override void CopyTo(Template toTemplate) {
			base.CopyTo(toTemplate);
			CopyValueDelegates(toTemplate);
			((TValue)toTemplate).bind = bind;
		}
        
        /// <summary>
        /// Returns true if an existing databinding exists, false otherwise.
        /// </summary>
        /// <remarks>
        /// This method tells if a binding exists, not if it should be used.
        /// For example dataobject can be null or bind value changed.
        /// </remarks>
        /// <returns></returns>
        internal abstract bool HasBinding();

        /// <summary>
        /// Checks, verifies and creates the binding.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal bool UseBinding(Json parent) {
            object data;
            Type boundType;

            if (BindingStrategy == BindingStrategy.Unbound || isVerifiedUnbound)
                return false;
            
            // TODO:
            // Workaround for having a property bound to codebehind, but
            // Data-property is always null. Since we want to avoid recreate
            // the binding when Data is null, we need to first check if
            // we are bound to codebehind. 
            // This will not be needed when we change how codebehind-properties
            // work (https://github.com/Starcounter/Starcounter/issues/2964).
            if (forceGenerateBindings) {
                GenerateBoundGetterAndSetter(parent);
                forceGenerateBindings = false;

                if (isVerifiedUnbound)
                    return false;
            }

            if (parent.IsCached(this.TemplateIndex))
                return false;

            if (isBoundToParent)
                return true;

            data = parent.Data;
            if (data == null)
                return false;

            boundType = dataTypeForBinding;
            if (boundType != null) {
                if (VerifyBoundDataType(data.GetType(), boundType))
                    return true;

                if (this.HasBinding())
                    this.SetDefaultValue(parent, true);
                InvalidateBoundGetterAndSetter();
            }
            return GenerateBoundGetterAndSetter(parent);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dataType"></param>
		/// <returns></returns>
		internal bool VerifyBoundDataType(Type dataType, Type boundDataType) {
			if (dataType.Equals(boundDataType) || dataType.IsSubclassOf(boundDataType))
				return true;
			return false;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal TypedJsonSerializer JsonSerializer {
            get {
                return Module.GetJsonSerializer(Module.StandardJsonSerializerId);
            }
        }
	}
}
