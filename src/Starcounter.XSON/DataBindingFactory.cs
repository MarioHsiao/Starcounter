using System;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using Starcounter.Advanced;
using Starcounter.Logging;
using Starcounter.Templates;

namespace Starcounter.XSON {
    internal class DataBindingFactory {
        internal static bool ThrowExceptionOnBindindRecreation = false;
        private static LogSource logSource = new LogSource("json");
        private static string warning = "The existing databinding for '{0}' was created for another type of dataobject. Binding needs to be recreated.";

        internal static DataValueBinding<TVal> VerifyOrCreateBinding<TVal>(TValue template, DataValueBinding<TVal> binding, Type dataType, string bindingName) {
            PropertyInfo pInfo;
            
            if (binding != null) {
                if (dataType.Equals(binding.DataType) || dataType.IsSubclassOf(binding.DataType)) {
                    return binding;
                }

                if (ThrowExceptionOnBindindRecreation)
                    throw new Exception(string.Format(warning, GetParentClassName(template) + "." + template.TemplateName));

                logSource.LogWarning(string.Format(warning, GetParentClassName(template) + "." + template.TemplateName));
            }

            pInfo = dataType.GetProperty(bindingName, BindingFlags.Instance | BindingFlags.Public);
            if (pInfo == null) {
                throw new Exception("Cannot create binding. Property '" + bindingName + "' was not found in type " + dataType.FullName);
            }

            return new DataValueBinding<TVal>(pInfo);
        }

        private static string GetParentClassName(Template template) {
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

    internal class DataValueBinding<TVal> {
        private static MethodInfo dateTimeToStringInfo = typeof(DateTime).GetMethod("ToString", new Type[] { typeof(string) });
        private static MethodInfo dateTimeParseInfo = typeof(DateTime).GetMethod("Parse", new Type[] { typeof(string) });

        private Func<IBindable, TVal> getBinding;
        private Action<IBindable, TVal> setBinding;
        private Type dataType;

        internal DataValueBinding(PropertyInfo bindToProperty) {
            MethodInfo methodInfo;
            dataType = bindToProperty.DeclaringType;

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
                // TODO:
                // Check if typeconversion is possible.
                newExpr = Expression.Convert(expr, to);
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

        internal Type DataType { get { return dataType; } }
    }
}
