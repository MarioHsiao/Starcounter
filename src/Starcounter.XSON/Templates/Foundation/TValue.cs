﻿using System;

namespace Starcounter.Templates {
	/// <summary>
	/// 
	/// </summary>
	public abstract class TValue : Template {
		private BindingStrategy strategy = BindingStrategy.UseParent;
		private string bind;
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
						return BindingStrategy.Unbound;

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
		internal abstract void SetDefaultValue(Json parent);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="from"></param>
		internal abstract void CopyValueDelegates(Template toTemplate);

		/// <summary>
		/// 
		/// </summary>
		internal virtual void InvalidateBoundGetterAndSetter() {
			isVerifiedUnbound = false;
			dataTypeForBinding = null;
		}

		/// <summary>
		/// 
		/// </summary>
		internal virtual void OnPropertySet(Json parent) {
			if (parent.HasBeenSent)
				parent.MarkAsReplaced(TemplateIndex);
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
			((TValue)toTemplate).Bind = Bind;
		}

        public abstract string ToJson(Json json);
        public abstract byte[] ToJsonUtf8(Json json);
        public abstract int ToJsonUtf8(Json json, byte[] buffer, int offset);
        public abstract int ToJsonUtf8(Json json, IntPtr ptr, int bufferSize);

        public abstract int EstimateUtf8SizeInBytes(Json json);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		internal bool UseBinding(Json parent) {
			object data;
            if (BindingStrategy == BindingStrategy.Unbound)
				return false;

            if (isVerifiedUnbound && isBoundToParent) {
                if (parent.Data == null)
                    return false;

                // If we have an auto binding and we have checked once but
                // no dataobject was set (i. e the "unbound" points to the parent)
                // we want to reset and check again if we have a dataobject now.
                InvalidateBoundGetterAndSetter();
            }

            if (dataTypeForBinding != null) {
                data = (isBoundToParent) ? parent : parent.Data;
                if (data != null && VerifyBinding(data.GetType()))
                    return !isVerifiedUnbound;

                InvalidateBoundGetterAndSetter();
            }

			return GenerateBoundGetterAndSetter(parent);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dataType"></param>
		/// <returns></returns>
		private bool VerifyBinding(Type dataType) {
			if (dataType.Equals(dataTypeForBinding) || dataType.IsSubclassOf(dataTypeForBinding))
				return true;
			return false;
		}
	}
}
