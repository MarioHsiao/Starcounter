
using Starcounter.Advanced;
using Starcounter.Templates;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Generic;

namespace Starcounter.Internal.XSON {

    /// <summary>
    /// Baseclass for bindings.
    /// </summary>
    internal abstract class DataValueBinding {
        protected Type dataType;
        private Template template;
        
        internal DataValueBinding(Template template) {
            this.template = template;
        }

        internal Type DataType { get { return dataType; } }
        internal Template Template { get { return template; } }
		internal virtual bool IsDummyBinding { get { return false; } }
    }

	/// <summary>
	/// Empty binding class used to verify the binding (even if the template is not bound)
	/// when template is set to Auto and a check is already made for the property.
	/// </summary>
	internal class AutoValueBinding : DataValueBinding {
		internal AutoValueBinding(Template template, Type dataType) : base(template) {
			this.dataType = dataType;
		}

		internal override bool IsDummyBinding { get { return true; } }
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

		private static ParameterExpression instance = Expression.Parameter(typeof(IBindable));
		private static ParameterExpression value = Expression.Parameter(typeof(TVal));

        private Func<IBindable, TVal> getBinding;
        private Action<IBindable, TVal> setBinding;

        internal DataValueBinding(Template template, BindingInfo bindingInfo)
            : base(template) {
				CompileBinding(bindingInfo);
        }

		private void CompileBinding(BindingInfo bInfo) {
			Expression expr = CreateBinding(bInfo, true);
			if (expr != null) {
				var lambda = Expression.Lambda<Func<IBindable, TVal>>(expr, instance);
				getBinding = lambda.Compile();
			}

			expr = CreateBinding(bInfo, false);
			if (expr != null) {
				var lambda = Expression.Lambda<Action<IBindable, TVal>>(expr, instance, value);
				setBinding = lambda.Compile();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mInfo"></param>
		/// <returns></returns>
		private Type GetMemberReturnType(MemberInfo mInfo) {
			PropertyInfo pInfo = mInfo as PropertyInfo;
			if (pInfo != null)
				return pInfo.PropertyType;
			return ((FieldInfo)mInfo).FieldType;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mInfo"></param>
		/// <returns></returns>
		private Type GetMemberDeclaringType(MemberInfo mInfo) {
			Type type;
			PropertyInfo pInfo = mInfo as PropertyInfo;

			type = mInfo.DeclaringType;
			if (pInfo != null) {
				var method = pInfo.GetGetMethod();
				if (method.IsVirtual) {
					method = method.GetBaseDefinition();
					type = method.DeclaringType;
				}
			}
			return type;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="bInfo"></param>
		/// <param name="createGetBinding"></param>
		/// <returns></returns>
		private Expression CreateBinding(BindingInfo bInfo, bool createGetBinding) {
			BlockExpression block;
			Expression[] callArr;
			Expression bindingExpr = null;
			Expression notNullCheck;
			ParameterExpression lastVar;
			ParameterExpression[] variables;
			Type memberType;

			if (bInfo.Path != null) {
				variables = new ParameterExpression[bInfo.Path.Count];
				callArr = new Expression[bInfo.Path.Count];
				lastVar = instance;

				this.dataType = GetMemberDeclaringType(bInfo.Path[0]);
					
				// Create expressions for each call including local variables to store 
				// the returnvalue from the call.
				for (int i = 0; i < variables.Length; i++) {
					Type type = GetMemberReturnType(bInfo.Path[i]);
					variables[i] = Expression.Variable(type, "localvar" + i);
					bindingExpr = CreateGetMemberBinding(bInfo.Path[i], lastVar, false);
					bindingExpr = Expression.Assign(variables[i], bindingExpr);
					callArr[i] = bindingExpr;
					lastVar = variables[i];
				}

				if (createGetBinding)
					bindingExpr = CreateGetMemberBinding(bInfo.Member, lastVar, true);
				else
					bindingExpr = CreateSetMemberBinding(bInfo.Member, lastVar, true);

				// Create null-check branches for each local stored value
				memberType = GetMemberReturnType(bInfo.Member);
				for (int i = variables.Length - 1; i >= 0; i--) {
					if ((i + 1) < callArr.Length)
						bindingExpr = callArr[i + 1];
					notNullCheck = Expression.NotEqual(variables[i], Expression.Constant(null));

					if (createGetBinding)
						bindingExpr = Expression.Condition(notNullCheck, bindingExpr, Expression.Default(memberType));
					else
						bindingExpr = Expression.IfThen(notNullCheck, bindingExpr);

					block = Expression.Block(new[] { variables[i] }, callArr[i], bindingExpr);
					callArr[i] = block;
				}
				bindingExpr = callArr[0];
			} else {
				this.dataType = GetMemberDeclaringType(bInfo.Member);

				if (createGetBinding)
					bindingExpr = CreateGetMemberBinding(bInfo.Member, instance, true);
				else
					bindingExpr = CreateSetMemberBinding(bInfo.Member, instance, true);
			}
			return bindingExpr;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="member"></param>
		/// <param name="expr"></param>
		/// <returns></returns>
		private Expression CreateGetMemberBinding(MemberInfo member, ParameterExpression variable, bool convertType) {
			MethodInfo methodInfo;
			PropertyInfo bindToProperty = member as PropertyInfo;
			Expression newExpr = null;

			if (bindToProperty != null) {
				methodInfo = bindToProperty.GetGetMethod();
				if (methodInfo != null) {
					// We need to check if this is an abstract or virtual method.
					// In that case we want to create the binding for the type that 
					// declares it in first hand.
					if (methodInfo.IsVirtual) {
						methodInfo = methodInfo.GetBaseDefinition();
					}
					newExpr = CreatePropertyGetBinding(methodInfo, variable, convertType);
				}
			} else {
				var fieldInfo = (FieldInfo)member;
				newExpr = CreateFieldGetBinding(fieldInfo, variable, convertType);
			}
			return newExpr;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="member"></param>
		/// <param name="expr"></param>
		/// <returns></returns>
		private Expression CreateSetMemberBinding(MemberInfo member, ParameterExpression variable, bool convertType) {
			MethodInfo methodInfo;
			PropertyInfo bindToProperty = member as PropertyInfo;
			Expression newExpr = null;

			if (bindToProperty != null) {
				methodInfo = bindToProperty.GetSetMethod();
				if (methodInfo != null) {
					// We need to check if this is an abstract or virtual method. In that case we 
					// want to create the binding for the type that declares it in first hand.
					if (methodInfo.IsVirtual) {
						methodInfo = methodInfo.GetBaseDefinition();
					}
					newExpr = CreatePropertySetBinding(methodInfo, variable, convertType);
				}
			} else {
				var fieldInfo = (FieldInfo)member;
				newExpr = CreateFieldSetBinding(fieldInfo, variable, convertType);
			}
			return newExpr;
		}

        /// <summary>
        /// Generate a delegate (IL code) that accepts an object and
        /// that reads and returns the value of a a specific field 
        /// </summary>
        /// <param name="field">The field to read</param>
		private Expression CreateFieldGetBinding(FieldInfo field, ParameterExpression variable, bool convertType) {
			Expression expr;

			if (variable.Type != field.DeclaringType)
				expr = Expression.Convert(variable, field.DeclaringType);
			else
				expr = variable;

            expr = Expression.Field(expr, field);
            if (convertType && !field.FieldType.Equals(typeof(TVal))) {
                expr = AddTypeConversionIfPossible(expr, field.FieldType, typeof(TVal));
            }
			return expr;
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="field"></param>
		/// <param name="expr"></param>
		/// <param name="convertType"></param>
		/// <returns></returns>
		private Expression CreateFieldSetBinding(FieldInfo field, ParameterExpression variable, bool convertType) {
			Expression expr;
			Type valueType = field.FieldType;

			if (variable.Type != field.DeclaringType)
				expr = Expression.Convert(variable, field.DeclaringType);
			else
				expr = variable;

            expr = Expression.Field(expr, field);

            Expression setValue = value;
			if (convertType && !valueType.Equals(typeof(TVal))) {
                setValue = AddTypeConversionIfPossible(value, typeof(TVal), valueType);
            }
            return Expression.Assign(expr, setValue);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="getMethod"></param>
		/// <param name="expr"></param>
		/// <param name="convertType"></param>
		/// <returns></returns>
        private Expression CreatePropertyGetBinding(MethodInfo getMethod, ParameterExpression variable, bool convertType) {
			Expression expr;

			if (variable.Type != getMethod.DeclaringType) 
				expr = Expression.Convert(variable, getMethod.DeclaringType);
			else 
				expr = variable;

			expr = Expression.Call(expr, getMethod);
			if (convertType && !getMethod.ReturnType.Equals(typeof(TVal))) {
                expr = AddTypeConversionIfPossible(expr, getMethod.ReturnType, typeof(TVal));
            }
			return expr;
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="setMethod"></param>
		/// <param name="expr"></param>
		/// <param name="convertType"></param>
		/// <returns></returns>
		private Expression CreatePropertySetBinding(MethodInfo setMethod, ParameterExpression variable, bool convertType) {
			Expression expr;
			Type valueType = setMethod.GetParameters()[0].ParameterType;

			if (variable.Type != setMethod.DeclaringType)
				expr = Expression.Convert(variable, setMethod.DeclaringType);
			else
				expr = variable;

            Expression setValue = value;
			if (convertType && !valueType.Equals(typeof(TVal))) {
                setValue = AddTypeConversionIfPossible(value, typeof(TVal), valueType);
            }
            return Expression.Call(expr, setMethod, setValue);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="expr"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
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
					throw ErrorCode.ToException(Error.SCERRCREATEDATABINDINGFORJSON,
												ex,
												string.Format(propNotCompatible,
															  DataBindingFactory.GetParentClassName(Template),
															  Template.TemplateName,
															  Template.JsonType,
															  dataType.FullName,
															  from.Name,
															  from.FullName));
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
