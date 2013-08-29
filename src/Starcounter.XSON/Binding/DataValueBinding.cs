
using Starcounter.Advanced;
using Starcounter.Templates;
using System;
using System.Linq.Expressions;
using System.Reflection;
namespace Starcounter.Internal.XSON {

    /// <summary>
    /// Baseclass for bindings.
    /// </summary>
    internal abstract class DataValueBinding {
        protected Type dataType;
        private Template template;
        private MemberInfo property;

        internal DataValueBinding(Template template, MemberInfo property) {
            this.template = template;
            this.property = property;
        }

        internal Type DataType { get { return dataType; } }
        internal Template Template { get { return template; } }
        internal MemberInfo Property { get { return property; } }
    }

	/// <summary>
	/// Empty binding class used to verify the binding (even if the template is not bound)
	/// when template is set to Auto and a check is already made for the property.
	/// </summary>
	internal class AutoValueBinding : DataValueBinding {
		internal AutoValueBinding(Template template, Type dataType) : base(template, null) {
			this.dataType = dataType;
		}
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


        internal DataValueBinding(Template template, MemberInfo bindToMember)
            : base(template, bindToMember) {

                PropertyInfo bindToProperty = bindToMember as PropertyInfo;
                if (bindToProperty != null) {
                    MethodInfo methodInfo;

                    this.dataType = ((PropertyInfo)bindToMember).DeclaringType;
                    methodInfo = bindToProperty.GetGetMethod();
                    if (methodInfo != null) {
                        // We need to check if this is an abstract or virtual method.
                        // In that case we want to create the binding for the type that 
                        // declares it in first hand.
                        if (methodInfo.IsVirtual) {
                            methodInfo = methodInfo.GetBaseDefinition();
                            dataType = methodInfo.DeclaringType;
                        }
                        CreatePropertyGetBinding(methodInfo);
                    }

                    methodInfo = bindToProperty.GetSetMethod();
                    if (methodInfo != null) {
                        if (methodInfo.IsVirtual) {
                            methodInfo = methodInfo.GetBaseDefinition();
                        }
                        CreatePropertySetBinding(methodInfo);
                    }
                }
                else {
                    this.dataType = ((FieldInfo)bindToMember).DeclaringType;

                    CreateFieldGetBinding((FieldInfo)bindToMember);
                    CreateFieldSetBinding((FieldInfo)bindToMember);
                }
        }

        /// <summary>
        /// Generate a delegate (IL code) that accepts an object and
        /// that reads and returns the value of a a specific field 
        /// </summary>
        /// <param name="field">The field to read</param>
        private void CreateFieldGetBinding(FieldInfo field) {
            var instance = Expression.Parameter(typeof(IBindable));
            var cast = Expression.Convert(instance, dataType);
            Expression fieldAccess = Expression.Field(cast, field );

            if (!field.FieldType.Equals(typeof(TVal))) {
                fieldAccess = AddTypeConversionIfPossible(fieldAccess, field.FieldType, typeof(TVal));
            }

            var lambda = Expression.Lambda<Func<IBindable, TVal>>(fieldAccess, instance);
            getBinding = lambda.Compile();
        }

        private void CreateFieldSetBinding(FieldInfo field) {
            var instance = Expression.Parameter(typeof(IBindable));
            var value = Expression.Parameter(typeof(TVal));

            var cast = Expression.TypeAs(instance, dataType);
            Expression fieldAccess = Expression.Field(cast, field);

            Expression setValue = value;
            Type valueType = field.FieldType;
            if (!valueType.Equals(typeof(TVal))) {
                setValue = AddTypeConversionIfPossible(value, typeof(TVal), valueType);
            }
            Expression assign = Expression.Assign(fieldAccess, setValue);

            var lambda = Expression.Lambda<Action<IBindable, TVal>>(assign, instance, value);
            setBinding = lambda.Compile();
        }

        private void CreatePropertyGetBinding(MethodInfo getMethod) {
            ParameterExpression instance = Expression.Parameter(typeof(IBindable));
            Expression cast = Expression.Convert(instance, dataType);
            Expression call = Expression.Call(cast, getMethod);

            if (!getMethod.ReturnType.Equals(typeof(TVal))) {
                call = AddTypeConversionIfPossible(call, getMethod.ReturnType, typeof(TVal));
            }

            var lambda = Expression.Lambda<Func<IBindable, TVal>>(call, instance);
            getBinding = lambda.Compile();
        }

        private void CreatePropertySetBinding(MethodInfo setMethod) {
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
            }
            else if (to.Equals(typeof(DateTime))) {
                if (from.Equals(typeof(string))) {
                    newExpr = Expression.Call(null, dateTimeParseInfo, expr);
                }
            }

            if (newExpr == null) {
                try {
                    newExpr = Expression.Convert(expr, to);
                }
                catch (Exception ex) {
                    string fullName;
                    if (Property is PropertyInfo) {
                        fullName = ((PropertyInfo)Property).PropertyType.FullName;
                    }
                    else {
                        fullName = ((FieldInfo)Property).FieldType.FullName;
                    }
                    throw ErrorCode.ToException(Error.SCERRCREATEDATABINDINGFORJSON,
                                                ex,
                                                string.Format(propNotCompatible,
                                                              DataBindingFactory.GetParentClassName(Template),
                                                              Template.TemplateName,
                                                              Template.JsonType,
                                                              dataType.FullName,
                                                              Property.Name,
                                                              fullName));
                }
            }
            return newExpr;
        }

        internal bool HasGetBinding() {
            return (getBinding != null);
        }

        internal bool HasSetBinding() {
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
