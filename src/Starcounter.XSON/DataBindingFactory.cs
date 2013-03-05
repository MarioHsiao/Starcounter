using System;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using Starcounter.Advanced;
using Starcounter.Templates;

namespace Starcounter.XSON {
    internal class DataBindingFactory {
        internal static DataValueBinding<TVal> CreateBinding<TVal>(Type dataType, string bindingName) {
            PropertyInfo pInfo;
            pInfo = dataType.GetProperty(bindingName, BindingFlags.Instance | BindingFlags.Public);
            if (pInfo == null) {
                throw new Exception("Cannot create binding. Property '" + bindingName + "' was not found in type " + dataType.FullName);
            }

            return new DataValueBinding<TVal>(pInfo);
        }
    }

    internal class DataValueBinding<TVal> {
        private Func<IBindable, TVal> getBinding;
        private Action<IBindable, TVal> setBinding;
        private Type dataType;

        internal DataValueBinding(PropertyInfo bindToProperty) {
            dataType = bindToProperty.DeclaringType;
            if (bindToProperty.GetGetMethod() != null)
                CreateGetBinding(bindToProperty.GetGetMethod());
            if (bindToProperty.GetSetMethod() != null)
                CreateSetBinding(bindToProperty.GetSetMethod());
        }

        private void CreateGetBinding(MethodInfo getMethod) {
            ParameterExpression instance = Expression.Parameter(typeof(IBindable));
            Expression cast = Expression.Convert(instance, getMethod.DeclaringType);
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

            Expression cast = Expression.TypeAs(instance, setMethod.DeclaringType);

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
            // TODO:
            // Check if typeconversion is possible.
            Expression cast = Expression.Convert(expr, to);
            return cast;
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
