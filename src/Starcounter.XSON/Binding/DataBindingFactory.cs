using System;
using System.Collections;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using Starcounter.Advanced;
using Starcounter.Internal;
using Starcounter.Logging;
using Starcounter.Templates;
using Starcounter.Internal.XSON;
using System.Collections.Generic;

namespace Starcounter.Internal.XSON {
    /// <summary>
    /// Internal class responsible for creating and verifying code that reads and writes values to and from
    /// dataobjects. Used in typed json to create binding between json and data.
    /// </summary>
    internal class DataBindingFactory {
        internal static bool ThrowExceptionOnBindindRecreation = false;
        private static LogSource logSource = new LogSource("json");
        private static string warning = "The existing databinding for '{0}' was created for another type of dataobject. Binding needs to be recreated.";
        private static string propNotFound = "Property '{2}' was not found in type '{3}' (or baseclass). Json property: '{0}.{1}'.";

		internal static bool VerifyOrCreateBinding(TValue template, object data) {
			bool throwExceptionOnBindingFailure;
			string bindingName;
			Bound bound = template.Bound;

			if (bound == Bound.No)
				return false;

			if (template.invalidateBinding) {
				template.dataBinding = null;
				template.invalidateBinding = false;
			}

			Type dataType = data.GetType();
			// TODO: 
			// Rewrite this code. When Auto is set and the property is not found we don't want to search for it every time.
			// The current implementation creates an empty binding that is only used to verify that the datatype and name is
			// the same, but returns false from this method which is confusing.
			if (VerifyBinding(template.dataBinding, dataType, template)) {
				return !template.dataBinding.IsDummyBinding;
			}

			if (bound == Bound.Yes) {
				throwExceptionOnBindingFailure = true;
				bindingName = template.Bind;
				if (bindingName == null)
					bindingName = template.PropertyName;
			} else {
				throwExceptionOnBindingFailure = false;
				bindingName = template.PropertyName;
			}

//			var pInfo = GetMemberForBinding(dataType, bindingName, template, throwExceptionOnBindingFailure);
			var bInfo = GetBindingPath(dataType, data, bindingName, template, throwExceptionOnBindingFailure);
			if (bInfo.Member != null) {
				var @switch = new Dictionary<Type, Func<DataValueBinding>> {
					 { typeof(byte), () => { return new DataValueBinding<TLong>(template, bInfo); }},
					 { typeof(UInt16), () => { return new DataValueBinding<TLong>(template, bInfo); }},
					 { typeof(Int16), () => { return new DataValueBinding<TLong>(template, bInfo); }},
					 { typeof(UInt32), () => { return new DataValueBinding<TLong>(template, bInfo); }},
					 { typeof(Int32), () => { return new DataValueBinding<TLong>(template, bInfo); }},
					 { typeof(UInt64), () => { return new DataValueBinding<TLong>(template, bInfo); }},
					 { typeof(Int64), () => { return new DataValueBinding<TLong>(template, bInfo); }},
					 { typeof(float), () => { return new DataValueBinding<TLong>(template, bInfo); }},
					 { typeof(double), () => { return new DataValueBinding<TDouble>(template, bInfo); }},
					 { typeof(decimal), () => { return new DataValueBinding<TDecimal>(template, bInfo); }},
					 { typeof(bool), () => { return new DataValueBinding<TBool>(template, bInfo); }},
					 { typeof(string), () => { return new DataValueBinding<TString>(template, bInfo); }}
					 };
				template.dataBinding = @switch[template.InstanceType]();
				return true;
			} else {
				template.dataBinding = new AutoValueBinding(template, dataType);
				return false;
			}
		}

		/// <summary>
		/// Verifies and caches an binding on the template.
		/// </summary>
        /// <param name="template">The template.</param>
        /// <param name="dataType">The type of the dataobject.</param>
		/// <returns>True if a binding could be created and cached on the template, false otherwise.</returns>
        internal static bool VerifyOrCreateBinding(TObjArr template, object data) {
			bool throwExceptionOnBindingFailure;
			string bindingName;
			Bound bound = template.Bound;

			if (bound == Bound.No)
				return false;

			if (template.invalidateBinding) {
				template.dataBinding = null;
				template.invalidateBinding = false;
			}

			Type dataType = data.GetType();
			// TODO: 
			// Rewrite this code. When Auto is set and the property is not found we don't want to search for it every time.
			// The current implementation creates an empty binding that is only used to verify that the datatype and name is
			// the same, but returns false from this method which is confusing.
			if (VerifyBinding(template.dataBinding, dataType, template)) {
				return !template.dataBinding.IsDummyBinding;
			}

			if (bound == Bound.Yes) {
				throwExceptionOnBindingFailure = true;
				bindingName = template.Bind;
				if (bindingName == null)
					bindingName = template.PropertyName;
			} else {
				throwExceptionOnBindingFailure = false;
				bindingName = template.PropertyName;
			}

            var bInfo = GetBindingPath(dataType, data, bindingName, template, throwExceptionOnBindingFailure);
			if (bInfo.Member != null) {
				template.dataBinding = new DataValueBinding<IEnumerable>(template, bInfo);
				return true;
			} else {
				template.dataBinding = new AutoValueBinding(template, dataType);
				return false;
			}
        }

		/// <summary>
		/// Verifies and caches an binding on the template.
		/// </summary>
        /// <param name="template">The template.</param>
        /// <param name="dataType">The type of the dataobject.</param>
		/// <returns>True if a binding could be created and cached on the template, false otherwise.</returns>
        internal static bool VerifyOrCreateBinding(TObject template, object data) {
			bool throwExceptionOnBindingFailure;
			string bindingName;
			Bound bound = template.Bound;

			if (bound == Bound.No)
				return false;

			if (template.invalidateBinding) {
				template.dataBinding = null;
				template.invalidateBinding = false;
			}

			Type dataType = data.GetType();

			// TODO: 
			// Rewrite this code. When Auto is set and the property is not found we don't want to search for it every time.
			// The current implementation creates an empty binding that is only used to verify that the datatype and name is
			// the same, but returns false from this method which is confusing.
			if (VerifyBinding(template.dataBinding, dataType, template)) {
				return !template.dataBinding.IsDummyBinding;
			}

			if (bound == Bound.Yes) {
				throwExceptionOnBindingFailure = true;
				bindingName = template.Bind;
				if (bindingName == null)
					bindingName = template.PropertyName;
			} else {
				throwExceptionOnBindingFailure = false;
				bindingName = template.PropertyName;
			}

            var bInfo = GetBindingPath(dataType, data, bindingName, template, throwExceptionOnBindingFailure);
			if (bInfo.Member != null) {
				template.dataBinding = new DataValueBinding<IBindable>(template, bInfo);
				return true;
			} else {
				template.dataBinding = new AutoValueBinding(template, dataType);
				return false;
			}
        }

		/// <summary>
		/// Verifies and caches an binding on the template.
		/// </summary>
        /// <typeparam name="TVal">The primitive instance type of the template.</typeparam>
        /// <param name="template">The template.</param>
        /// <param name="dataType">The type of the dataobject.</param>
		/// <returns>True if a binding could be created and cached on the template, false otherwise.</returns>
        internal static bool VerifyOrCreateBinding<TVal>(Property<TVal> template, object data) {
			bool throwExceptionOnBindingFailure;
			string bindingName;
			Bound bound = template.Bound;

			if (bound == Bound.No)
				return false;

			if (template.invalidateBinding) {
				template.dataBinding = null;
				template.invalidateBinding = false;
			}

			Type dataType = data.GetType();
			// TODO: 
			// Rewrite this code. When Auto is set and the property is not found we don't want to search for it every time.
			// The current implementation creates an empty binding that is only used to verify that the datatype and name is
			// the same, but returns false from this method which is confusing.
			if (VerifyBinding(template.dataBinding, dataType, template)) {
				return !template.dataBinding.IsDummyBinding;
			}

			if (bound == Bound.Yes) {
				throwExceptionOnBindingFailure = true;
				bindingName = template.Bind;
				if (bindingName == null)
					bindingName = template.PropertyName;
			} else {
				throwExceptionOnBindingFailure = false;
				bindingName = template.PropertyName;
			}

            var bInfo = GetBindingPath(dataType, data, bindingName, template, throwExceptionOnBindingFailure);
			if (bInfo.Member != null) {
				template.dataBinding = new DataValueBinding<TVal>(template, bInfo);
				return true;
			} else {
				template.dataBinding = new AutoValueBinding(template, dataType);
				return false;
			}
        }

        /// <summary>
        /// Returns the property with the specified name from the data type. If not found an exception 
        /// is thrown.
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="bindingName"></param>
        /// <param name="template"></param>
		/// <param name="throwException"></param>
        /// <returns></returns>
        private static BindingInfo GetBindingPath(Type dataType, object data, string bindingName, Template template, bool throwException) {
			int index;
			int offset;
			string partName;
			Type partType;
			BindingInfo binfo;
			MemberInfo memberInfo = null;
			List<MemberInfo> memberPath = null;
			object currentValue = data;

			index = bindingName.IndexOf('.');
			if (index == -1) {
				memberInfo = GetMemberForBinding(dataType, bindingName, template, throwException);
			} else {
				offset = 0;
				partType = dataType;
				while (offset != -1) {
					if (memberPath == null)
						memberPath = new List<MemberInfo>();

					if (memberInfo != null)
						memberPath.Add(memberInfo);

					if (index == -1) {
						partName = bindingName.Substring(offset);
						offset = -1;
					} else {
						partName = bindingName.Substring(offset, index - offset);
						offset = index + 1;
						index = bindingName.IndexOf('.', offset);
					}

					memberInfo = GetMemberForBinding(partType, partName, template, throwException);
					if (memberInfo == null) {
						memberPath = null;
						break;
					}

					partType = null;
					if (memberInfo is PropertyInfo) {
						if (currentValue != null){
							currentValue = ((PropertyInfo)memberInfo).GetValue(currentValue);
							if (currentValue != null)
								partType = currentValue.GetType();
						}

						if (partType == null)
							partType = ((PropertyInfo)memberInfo).PropertyType;
					} else {
						if (currentValue != null) {
							currentValue = ((FieldInfo)memberInfo).GetValue(currentValue);
							if (currentValue != null)
								partType = currentValue.GetType();
						}

						if (partType == null)
							partType = ((FieldInfo)memberInfo).FieldType;
					}
				}
			}

			binfo.Member = memberInfo;
			binfo.Path = memberPath;
            return binfo;
        }

		/// <summary>
		/// Returns the property with the specified name from the data type.
		/// </summary>
		/// <param name="dataType"></param>
		/// <param name="bindingName"></param>
		/// <param name="template"></param>
		/// <param name="throwException"></param>
		/// <returns></returns>
		private static MemberInfo GetMemberForBinding(Type dataType, string bindingName, Template template, bool throwException) {
			var pInfo = ReflectionHelper.FindPropertyOrField(dataType, bindingName);
			if (pInfo == null && throwException) {
				throw ErrorCode.ToException(Error.SCERRCREATEDATABINDINGFORJSON,
											string.Format(propNotFound,
														  GetParentClassName(template),
														  template.TemplateName,
														  bindingName,
														  dataType.FullName
										   ));
			}
			return pInfo;
		}

        /// <summary>
        /// Verifies that the existing binding can be used for the current datatype and template.
        /// </summary>
        /// <param name="binding"></param>
        /// <param name="dataType"></param>
        /// <param name="template"></param>
        /// <returns>True if the binding can be used, false if not or binding is null</returns>
        private static bool VerifyBinding(DataValueBinding binding, Type dataType, TValue template) {
            if (binding != null) {
                if (dataType.Equals(binding.DataType) || dataType.IsSubclassOf(binding.DataType)) {
                    return true;
                }

                if (ThrowExceptionOnBindindRecreation)
                    throw new Exception(string.Format(warning, GetParentClassName(template) + "." + template.TemplateName));

                logSource.LogWarning(string.Format(warning, GetParentClassName(template) + "." + template.TemplateName));
            }
            return false;
        }

        /// <summary>
        /// Used when exception is raised to get the correct classname for the template.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        internal static string GetParentClassName(Template template) {
            string className = null;
            if (template.Parent is TObject)
                className = ((TObject)template.Parent).ClassName;
            else if (template.Parent is TObjArr) {
				var elementType = ((TObjArr)template.Parent).ElementType;
				if (elementType != null)
					className = elementType.ClassName;
            }

            if (className == null)
                className = "<noname>";
            return className;
        }
    }

}
