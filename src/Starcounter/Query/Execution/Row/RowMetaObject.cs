// ***********************************************************************
// <copyright file="RowMetaObject.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Dynamic;
using System.Linq.Expressions;

namespace Starcounter.Query.Execution
{
    internal class RowMetaObject : DynamicMetaObject
    {
        internal RowMetaObject(Expression parameter, Row value)
            : base(parameter, BindingRestrictions.Empty, value)
        { }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            // Method call in the containing class.
            string methodName = "GetValue";

            // One parameter.
            Expression[] parameters = new Expression[]
            {
                Expression.Constant(binder.Name)
            };

            DynamicMetaObject getValue = new DynamicMetaObject(
                Expression.Call(Expression.Convert(Expression, LimitType), typeof(Row).GetMethod(methodName), parameters),
                BindingRestrictions.GetTypeRestriction(Expression, LimitType));

            return getValue;
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            throw new NotSupportedException();
        }
    }
}
