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

namespace Starcounter.XSON {
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
        private static PropertyInfo GetPropertyForBinding(Type dataType, string bindingName, Template template) {
            var pInfo = dataType.GetProperty(bindingName, BindingFlags.Instance | BindingFlags.Public);
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
                className = ((TObjArr)template.Parent).App.ClassName;
            }

            if (className == null)
                className = "<noname>";
            return className;
        }
    }

    /// <summary>
    /// Baseclass for bindings.
    /// </summary>
    internal abstract class DataValueBinding {
        protected Type dataType;
        private Template template;
        private PropertyInfo property;

        internal DataValueBinding(Template template, PropertyInfo property) {
            this.template = template;
            this.property = property;
        }

        internal Type DataType { get { return dataType; } }
        internal Template Template { get { return template; } }
        internal PropertyInfo Property { get { return property; } }
    }

    /// <summary>
    /// Generic class for bindings to a specific type. Creates code that gets and sets the
    /// values to and from dataobject.
    /// </summary>
    /// <typeparam name="TVal"></typeparam>
    internal class DataValueBinding<TVal> : DataValueBinding {
        private static MethodInfo dateTimeToStringInfo = typeof(DateTime).GetMethod("ToString", new Type[] { typeof(string) });
        private static MethodInfo dateTimeParseInfo = typeof(DateTime).GetMethod("Parse", new Type[] { typeof(string) });
        private static string propNotCompatible = "Incompatible types for binding. Json property '{0}.{1}' ({2}), data property '{3}.{4}' ({5}).";

        private Func<IBindable, TVal> getBinding;
        private Action<IBindable, TVal> setBinding;
        

        internal DataValueBinding(Template template, PropertyInfo bindToProperty) 
            : base(template, bindToProperty) {
            MethodInfo methodInfo;
            this.dataType = bindToProperty.DeclaringType;
            
            methodInfo = bindToProperty.GetGetMethod();
            if (methodInfo != null) {
                // We need to check if this is an abstract or virtual method.
                // In that case we want to create the binding for the type that 
                // declares it in first hand.
                if (methodInfo.IsVirtual) {
                    methodInfo = methodInfo.GetBaseDefinition();
                    dataType = methodInfo.DeclaringType;
                }
                CreateGetBinding(methodInfo);
            }

            methodInfo = bindToProperty.GetSetMethod();
            if (methodInfo != null) {
                if (methodInfo.IsVirtual) {
                    methodInfo = methodInfo.GetBaseDefinition();
                }
                CreateSetBinding(methodInfo);
            }
        }

        private void CreateGetBinding(MethodInfo getMethod) {
            ParameterExpression instance = Expression.Parameter(typeof(IBindable));
            Expression cast = Expression.Convert(instance, dataType);
            Expression call = Expression.Call(cast, getMethod);

            if (!getMethod.ReturnType.Equals(typeof(TVal))) {
                call = AddTypeConversionIfPossible(call, getMethod.ReturnType, typeof(TVal));
            }

            var lambda = Expression.Lambda<Func<IBindable, TVal>>(call, instance);
            getBinding = lambda.Compile();
        }

        private void CreateSetBinding(MethodInfo setMethod) {
            ParameterExpression instance = Expression.Parameter(typeof(IBindable));
            ParameterExpression value = Expression.Parameter(typeof(TVal));

            Expression cast = Expression.TypeAs(instance, dataType);

            Expression setValue = value;
            Type valueType = setMethod.GetParameters()[0].ParameterType;
            if (!valueType.Equals(typeof(TVal))) {
                setValue = AddTypeConversionIfPossible(value, typeof(TVal), valueType);
            }
            Expression call = Expression.Call(cast, setMethod, setValue);

            var lambda = Expression.Lambda<Action<IBindable, TVal>>(call, instance, value);
            setBinding = lambda.Compile();
        }

        private Expression AddTypeConversionIfPossible(Expression expr, Type from, Type to) {
            Expression newExpr = null;

            if (to.Equals(typeof(string))) {
                if (from.Equals(typeof(DateTime))) {
                    // TODO:
                    // What format should be used and how much information?
                    newExpr = Expression.Call(expr, dateTimeToStringInfo, Expression.Constant("u")); // "u": datetime universal sortable format.
                }
            } else if (to.Equals(typeof(DateTime))) {
                if (from.Equals(typeof(string))) {
                    newExpr = Expression.Call(null, dateTimeParseInfo, expr);
                }
            } 
                
            if (newExpr == null) {
                try {
                    newExpr = Expression.Convert(expr, to);
                } catch (Exception ex) {
                    throw ErrorCode.ToException(Error.SCERRCREATEDATABINDINGFORJSON, 
                                                ex,
                                                string.Format(propNotCompatible, 
                                                              DataBindingFactory.GetParentClassName(Template),
                                                              Template.TemplateName,
                                                              Template.JsonType,
                                                              dataType.FullName,
                                                              Property.Name,
                                                              Property.PropertyType.FullName));
                }
            }
            return newExpr;
        }

        internal bool HasGetBinding(){
            return (getBinding != null);
        }

        internal bool HasSetBinding(){
            return (setBinding != null);
        }

        internal TVal Get(IBindable data) {
            if (getBinding != null) {
                return getBinding(data);
            }
            return default(TVal);
        }

        internal void Set(IBindable data, TVal value) {
            if (setBinding != null) {
                setBinding(data, value);
            }
        }
    }
}
