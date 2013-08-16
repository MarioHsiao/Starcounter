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

		internal static bool VerifyOrCreateBinding(TValue template, Type dataType) {
			bool throwExceptionOnBindingFailure;
			string bindingName;

			if (template.Bound == Bound.No)
				return false;

			if (template.invalidateBinding) {
				template.dataBinding = null;
				template.invalidateBinding = false;
			}

			if (VerifyBinding(template.dataBinding, dataType, template))
				return true;

			if (template.Bound == Bound.Yes) {
				throwExceptionOnBindingFailure = true;
				bindingName = template.Bind;
			} else {
				throwExceptionOnBindingFailure = false;
				bindingName = template.PropertyName;
			}

			var pInfo = GetPropertyForBinding(dataType, bindingName, template, throwExceptionOnBindingFailure);
			if (pInfo != null) {
				var @switch = new Dictionary<Type, Func<DataValueBinding>> {
 					 { typeof(byte), () => { return new DataValueBinding<TLong>(template, pInfo); }},
 					 { typeof(UInt16), () => { return new DataValueBinding<TLong>(template, pInfo); }},
 					 { typeof(Int16), () => { return new DataValueBinding<TLong>(template, pInfo); }},
 					 { typeof(UInt32), () => { return new DataValueBinding<TLong>(template, pInfo); }},
 					 { typeof(Int32), () => { return new DataValueBinding<TLong>(template, pInfo); }},
 					 { typeof(UInt64), () => { return new DataValueBinding<TLong>(template, pInfo); }},
 					 { typeof(Int64), () => { return new DataValueBinding<TLong>(template, pInfo); }},
 					 { typeof(float), () => { return new DataValueBinding<TLong>(template, pInfo); }},
 					 { typeof(double), () => { return new DataValueBinding<TDouble>(template, pInfo); }},
 					 { typeof(decimal), () => { return new DataValueBinding<TDecimal>(template, pInfo); }},
 					 { typeof(bool), () => { return new DataValueBinding<TBool>(template, pInfo); }},
 					 { typeof(string), () => { return new DataValueBinding<TString>(template, pInfo); }}
 					 };
				template.dataBinding = @switch[template.InstanceType]();
				return true;
			} else {
				template.dataBinding = null;
				return false;
			}
		}

		/// <summary>
		/// Verifies and caches an binding on the template.
		/// </summary>
        /// <param name="template">The template.</param>
        /// <param name="dataType">The type of the dataobject.</param>
		/// <returns>True if a binding could be created and cached on the template, false otherwise.</returns>
        internal static bool VerifyOrCreateBinding(TObjArr template, Type dataType) {
			bool throwExceptionOnBindingFailure;
			string bindingName;

			if (template.Bound == Bound.No)
				return false;

			if (template.invalidateBinding) {
				template.dataBinding = null;
				template.invalidateBinding = false;
			}

            if (VerifyBinding(template.dataBinding, dataType, template))
                return true;

			if (template.Bound == Bound.Yes) {
				throwExceptionOnBindingFailure = true;
				bindingName = template.Bind;
			} else {
				throwExceptionOnBindingFailure = false;
				bindingName = template.PropertyName;
			}

            var pInfo = GetPropertyForBinding(dataType, bindingName, template, throwExceptionOnBindingFailure);
			if (pInfo != null) {
				template.dataBinding = new DataValueBinding<IEnumerable>(template, pInfo);
				return true;
			} else {
				template.dataBinding = null;
				return false;
			}
        }

		/// <summary>
		/// Verifies and caches an binding on the template.
		/// </summary>
        /// <param name="template">The template.</param>
        /// <param name="dataType">The type of the dataobject.</param>
		/// <returns>True if a binding could be created and cached on the template, false otherwise.</returns>
        internal static bool VerifyOrCreateBinding(TObj template, Type dataType) {
			bool throwExceptionOnBindingFailure;
			string bindingName;

			if (template.Bound == Bound.No)
				return false;

			if (template.invalidateBinding) {
				template.dataBinding = null;
				template.invalidateBinding = false;
			}

            if (VerifyBinding(template.dataBinding, dataType, template))
                return true;

			if (template.Bound == Bound.Yes) {
				throwExceptionOnBindingFailure = true;
				bindingName = template.Bind;
			} else {
				throwExceptionOnBindingFailure = false;
				bindingName = template.PropertyName;
			}

            var pInfo = GetPropertyForBinding(dataType, bindingName, template, throwExceptionOnBindingFailure);
			if (pInfo != null) {
				template.dataBinding = new DataValueBinding<IBindable>(template, pInfo);
				return true;
			} else {
				template.dataBinding = null;
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
        internal static bool VerifyOrCreateBinding<TVal>(TValue<TVal> template, Type dataType) {
			bool throwExceptionOnBindingFailure;
			string bindingName;

			if (template.Bound == Bound.No)
				return false;

			if (template.invalidateBinding) {
				template.dataBinding = null;
				template.invalidateBinding = false;
			}

			if (VerifyBinding(template.dataBinding, dataType, template))
                return true;

			if (template.Bound == Bound.Yes) {
				throwExceptionOnBindingFailure = true;
				bindingName = template.Bind;
			} else {
				throwExceptionOnBindingFailure = false;
				bindingName = template.PropertyName;
			}

            var pInfo = GetPropertyForBinding(dataType, bindingName, template, throwExceptionOnBindingFailure);
			if (pInfo != null) {
				template.dataBinding = new DataValueBinding<TVal>(template, pInfo);
				return true;
			} else {
				template.dataBinding = null;
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
        private static MemberInfo GetPropertyForBinding(Type dataType, string bindingName, Template template, bool throwException) {
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
            if (template.Parent is TObj)
                className = ((TObj)template.Parent).ClassName;
            else if (template.Parent is TObjArr) {
                className = ((TObjArr)template.Parent).ElementType.ClassName;
            }

            if (className == null)
                className = "<noname>";
            return className;
        }
    }

}
