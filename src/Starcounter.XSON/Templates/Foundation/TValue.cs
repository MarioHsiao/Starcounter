using System;

namespace Starcounter.Templates {
	/// <summary>
	/// 
	/// </summary>
	public abstract class TValue : Template {
		private BindingStrategy strategy = BindingStrategy.UseParent;
		private string bind;
		internal Type dataTypeForBinding;

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
						parent = Parent;
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
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="rawValue"></param>
		/// TODO!
		/// Should be changed to accept IntPtr to rawValue using an int size parameter. This way is not fast enough.
		public abstract void ProcessInput(Json obj, Byte[] rawValue);

		/// <summary>
		/// 
		/// </summary>
		internal abstract void InvalidateBoundGetterAndSetter();

		/// <summary>
		/// 
		/// </summary>
		internal abstract bool GenerateBoundGetterAndSetter(Json json);
		internal abstract void GenerateUnboundGetterAndSetter(Json json);

		internal virtual void SetBoundValueAsObject(Json obj, object value) {
			throw new NotSupportedException();
		}

		internal virtual object GetBoundValueAsObject(Json obj) {
			throw new NotSupportedException();
		}

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
		/// <param name="data"></param>
		/// <returns></returns>
		internal bool UseBinding(Json json) {
			BindingStrategy strategy;
			object data;

			data = json.Data;
			strategy = BindingStrategy;
			if (data == null || strategy == BindingStrategy.Unbound)
				return false;

			// TODO:
			// Need to handle Auto bound that should not be bound.
			if (dataTypeForBinding != null && VerifyBinding(data.GetType(), false))
				return true;

			GenerateBoundGetterAndSetter(json);
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dataType"></param>
		/// <returns></returns>
		private bool VerifyBinding(Type dataType, bool throwExceptionOnFail) {
			if (dataType.Equals(dataTypeForBinding) || dataType.IsSubclassOf(dataTypeForBinding))
				return true;

			if (throwExceptionOnFail)
				throw new Exception("TODO!");
			//                throw new Exception(string.Format(warning, DataBindingFactory.GetParentClassName(this) + "." + this.TemplateName));
			//			logSource.LogWarning(string.Format(warning, GetParentClassName(template) + "." + template.TemplateName));          
			return false;
		}
	}
}
