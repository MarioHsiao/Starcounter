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

        /// <summary>
        /// Verifies that the current cached binding can be used with the current dataobject. 
        /// If the dataobject is of another type than the cached binding specifies it will be recreated
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="dataType">The type of the dataobject.</param>
        /// <param name="bindingName">The name of the property in the dataobject to create (if needed) the binding for.</param>
        /// <returns>An cached or created instance of <see cref="DataValueBinding{IEnumerable}"/></returns>
        internal static DataValueBinding<IEnumerable> VerifyOrCreateBinding(TObjArr template, Type dataType, string bindingName) {
            var binding = template.dataBinding;
            if (VerifyBinding(binding, dataType, template))
                return binding;

            var pInfo = GetPropertyForBinding(dataType, bindingName, template);
            binding = new DataValueBinding<IEnumerable>(template, pInfo);
            template.dataBinding = binding;
            return binding;
        }

        /// <summary>
        /// Verifies that the current cached binding can be used with the current dataobject. 
        /// If the dataobject is of another type than the cached binding specifies it will be recreated
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="dataType">The type of the dataobject.</param>
        /// <param name="bindingName">The name of the property in the dataobject to create (if needed) the binding for.</param>
        /// <returns>An cached or created instance of <see cref="DataValueBinding{IBindable}"/></returns>
        internal static DataValueBinding<IBindable> VerifyOrCreateBinding(TObj template, Type dataType, string bindingName) {
            var binding = template.dataBinding;
            if (VerifyBinding(binding, dataType, template))
                return binding;

            var pInfo = GetPropertyForBinding(dataType, bindingName, template);
            binding = new DataValueBinding<IBindable>(template, pInfo);
            template.dataBinding = binding;
            return binding;
        }

        /// <summary>
        /// Verifies that the current cached binding can be used with the current dataobject. 
        /// If the dataobject is of another type than the cached binding specifies it will be recreated
        /// </summary>
        /// <typeparam name="TVal">The primitive instance type of the template.</typeparam>
        /// <param name="template">The template.</param>
        /// <param name="dataType">The type of the dataobject.</param>
        /// <param name="bindingName">The name of the property in the dataobject to create (if needed) the binding for.</param>
        /// <returns>An cached or created instance of <see cref="DataValueBinding{TVal}"/></returns></returns>
        internal static DataValueBinding<TVal> VerifyOrCreateBinding<TVal>(TValue<TVal> template, Type dataType, string bindingName) {
            var binding = template.dataBinding;
            if (VerifyBinding(binding, dataType, template))
                return binding;

            var pInfo = GetPropertyForBinding(dataType, bindingName, template);
            binding = new DataValueBinding<TVal>(template, pInfo);
            template.dataBinding = binding;
            return binding;
        }

        /// <summary>
        /// Returns the property with the specified name from the data type. If not found an exception 
        /// is thrown.
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="bindingName"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        private static MemberInfo GetPropertyForBinding(Type dataType, string bindingName, Template template) {
            var pInfo = ReflectionHelper.FindPropertyOrField(dataType, bindingName);
            if (pInfo == null) {
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
        private static bool VerifyBinding(DataValueBinding binding, Type dataType, Template template) {
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
