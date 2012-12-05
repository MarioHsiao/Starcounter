﻿using System;using System.Collections.Generic;using System.Dynamic;using System.Linq.Expressions;using System.Reflection;using System.Linq;#if CLIENTusing Starcounter.Client.Template;namespace Starcounter.Client {#elseusing Starcounter.Templates;
using System.Diagnostics;namespace Starcounter {#endif    public partial class App : IDynamicMetaObjectProvider {        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(            Expression parameter) {            return new DynamicPropertyMetaObject(parameter, this);        }        private class DynamicPropertyMetaObject : DynamicMetaObject {            internal DynamicPropertyMetaObject(                System.Linq.Expressions.Expression parameter,                App value)                : base(parameter, BindingRestrictions.Empty, value) {            }            public override DynamicMetaObject BindGetMember(GetMemberBinder binder) {                PropertyInfo pi = this.RuntimeType.GetProperty(binder.Name);                if (pi != null)                    return base.BindGetMember(binder);                App app = (App)Value;                Property templ = (Property)(app.Template.Properties[binder.Name]);//                Column c = Column.LookupColumn(binder.Name);                Debug.WriteLine("DynamicEntity Binding Get " + binder.Name);                MethodInfo method;                if (templ is ListingProperty) {                    // The GetMethod does not deal with generic signatures causing an exception                    // for the ambiguous methods GetValue<T>( AppListTemplate x ) and GetValue( AppListTemplate x).                    // We need to call GetMethods instead (probably slower).                    // See http://stackoverflow.com/questions/11566613/how-do-i-distinguish-between-generic-and-non-generic-signatures-using-getmethod                    var mis = LimitType.GetMethods().Where( m => {                        var paris = m.GetParameters();                        if (paris.Length == 1) {                            var pari = paris[0];                            var tt = templ.GetType();                            return ( pari.ParameterType == tt || pari.ParameterType.IsSubclassOf(tt) ) && !m.IsGenericMethod;                            // Do we need to check instance of                        }                        return false;                    });                    method = mis.First();                }                else {                    method = LimitType.GetMethod("GetValue", new Type[] { templ.GetType() });                }                /* (DynamicDurableProxy)this.Get(); */                Expression call = Expression.Call(Expression.Convert(this.Expression, this.LimitType), method, Expression.Constant(templ) );                // Expression wrapped = Expression.Block(call); // , Expression.New(typeof(object)));                Expression wrapped = Expression.Convert( call, binder.ReturnType );                return new DynamicMetaObject(wrapped, BindingRestrictions.GetTypeRestriction(Expression, LimitType));            }            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value) {                PropertyInfo pi = this.RuntimeType.GetProperty(binder.Name);                if (pi != null)                    return base.BindSetMember(binder, value);                App app = (App)Value;                Property templ = (Property)(app.Template.Properties[binder.Name]);                MethodInfo method = LimitType.GetMethod("SetValue", new Type[] { templ.GetType(), value.LimitType });                if (method == null)                    throw new Exception(String.Format("No Set(uint,uint,{0}) method found when binding.", value.LimitType.Name));                //            Expression call = Expression.Call( Expression.Convert(this.Expression, this.LimitType), method, Expression.Constant(columnId), Expression.Constant(columnIndex), Expression.Convert(value.Expression, value.LimitType) );                Expression call = Expression.Call(Expression.Convert(this.Expression, this.LimitType), method, Expression.Constant(templ), Expression.Convert(value.Expression, templ.InstanceType));                Expression wrapped = Expression.Block(call, Expression.Convert(value.Expression, typeof(object)));                return new DynamicMetaObject(wrapped, BindingRestrictions.GetTypeRestriction(Expression, LimitType));            }        }    }}