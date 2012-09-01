
using System;
using System.Dynamic;
using System.Linq.Expressions;

namespace Starcounter.Query.Execution
{
    internal class CompositeMetaObject : DynamicMetaObject
    {
        internal CompositeMetaObject(Expression parameter, CompositeObject value)
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
                Expression.Call(Expression.Convert(Expression, LimitType), typeof(CompositeObject).GetMethod(methodName), parameters),
                BindingRestrictions.GetTypeRestriction(Expression, LimitType));

            return getValue;
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            throw new NotSupportedException();
        }

        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            throw new NotSupportedException();
        }
    }
}
