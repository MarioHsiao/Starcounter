using System;
using System.Diagnostics;
using Starcounter.Binding;

//namespace Starcounter.Query.Execution.Variables
namespace Starcounter.Query.Execution
{
    /// <summary>
    /// Class that holds information about a variable of type Type.
    /// </summary>
    internal class TypeVariable : Variable, IVariable, ITypeExpression {
        ITypeBinding value;
        IObjectView dynamicTypeValue;

        internal TypeVariable(Int32 number) {
            this.number = number;
            value = null;
            dynamicTypeValue = null;
        }

        internal TypeVariable(Int32 number, Type value) {
            this.number = number;
            if (value == null)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect value.");
            this.value = Bindings.GetTypeBinding(value.FullName);
            if (this.value == null)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect value.");
        }

        internal TypeVariable(Int32 number, ITypeBinding value) {
            this.number = number;
            if (value == null)
                throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Incorrect value.");
            this.value = value;
        }

        public ITypeBinding Value {
            get {
                return value;
            }
            set {
                this.value = value;
            }
        }

        public override DbTypeCode DbTypeCode {
            get {
                throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "DbTypeCode is not available for Type");
            }
        }
    
        /// <summary>
        /// Appends data of this leaf to the provided filter key.
        /// </summary>
        /// <param name="key">Reference to the filter key to which data should be appended.</param>
        /// <param name="obj">Row for which evaluation should be performed.</param>
        public override void AppendToByteArray(ByteArrayBuilder key, IObjectView obj) {
            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "Append is not available for Type");
        }

        /// <summary>
        /// Sets value to variable in execution enumerator.
        /// </summary>
        public void ProlongValue(IExecutionEnumerator destEnum) {
            destEnum.SetVariable(number, value);
        }

        /// <summary>
        /// Calculates the value of this variable.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>The value of this variable.</returns>
        public ITypeBinding EvaluateToType(IObjectView obj) {
            return value;
        }

        public IObjectView EvaluateToObject(IObjectView obj) {
            return dynamicTypeValue;
        }

        /// <summary>
        /// Examines if the value of this variable is null.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>True, if value is null, otherwise false.</returns>
        public override Boolean EvaluatesToNull(IObjectView obj) {
            return (EvaluateToType(obj) == null);
        }

        /// <summary>
        /// Creates a copy of this variable.
        /// </summary>
        /// <param name="obj">Not used.</param>
        /// <returns>A copy of this variable.</returns>
        public ITypeExpression Instantiate(Row obj) {
            return this;
        }

        public ITypeExpression CloneToType(VariableArray varArray) {
            IVariable variable = varArray.GetElement(number);

            if (variable == null) {
                TypeVariable typeVariable = new TypeVariable(number);
                varArray.SetElement(number, typeVariable);
                return typeVariable;
            }

            if (variable is TypeVariable)
                return variable as TypeVariable;

            throw ErrorCode.ToException(Error.SCERRSQLINTERNALERROR, "Conflicting variables.");
        }

        public override void BuildString(MyStringBuilder stringBuilder, Int32 tabs) {
            stringBuilder.Append(tabs, "TypeVariable(");
            if (value != null)
                stringBuilder.Append(value.Name);
            else
                stringBuilder.Append(Starcounter.Db.NullString);
            stringBuilder.AppendLine(")");
        }

        public override String ToString() {
            if (value != null)
                return value.Name;
            else
                return Starcounter.Db.NullString;
        }

        // String representation of this instruction.
        protected override String CodeAsString() {
            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED);
        }

        // Instruction code value.
        protected override UInt32 InstrCode() {
            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED);
        }

        public override void SetNullValue() {
            value = null;
        }

        public override void SetValue(ITypeBinding newValue) {
            value = newValue;
        }

        public override void SetValue(Type newValue) {
            if (newValue == null)
                this.value = null;
            else {
                this.value = Bindings.GetTypeBinding(newValue.FullName);
                if (this.value == null)
                    throw ErrorCode.ToException(Error.SCERRBADARGUMENTS,
                        "The given type to IS is unknown: " + newValue.FullName);
            }
        }

        public override void SetValue(IObjectView newValue) {
            if (newValue != null) {
                TypeBinding tb = newValue.TypeBinding as TypeBinding;
                Debug.Assert(tb != null);
                if (tb.TypeName == null)
                    throw ErrorCode.ToException(Error.SCERRBADARGUMENTS,
                        "Object without type name defined cannot be used as a type.");
            }
            else
                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS,
                    "Null cannot be used as type.");
            this.dynamicTypeValue = newValue;
            this.value = null;
        }

        // Throws an InvalidCastException if newValue is of an incompatible type.
        public override void SetValue(object newValue) {
            if (newValue is Type)
                SetValue((Type)newValue);
            else if (newValue is ITypeBinding)
                SetValue((ITypeBinding)newValue);
            else if ((newValue is IObjectView)) {
                SetValue(newValue as IObjectView);
            } else
                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS,
"Type of query variable value is expected to be a type, while actual type is " +
newValue.GetType().ToString());

        }

        /// <summary>
        /// Generates compilable code representation of this data structure.
        /// </summary>
        public void GenerateCompilableCode(CodeGenStringGenerator stringGen) {
            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED);
        }

#if DEBUG
        public bool AssertEquals(IValueExpression other) {
            TypeVariable otherNode = other as TypeVariable;
            Debug.Assert(otherNode != null);
            return this.AssertEquals(otherNode);
        }

        internal bool AssertEquals(TypeVariable other) {
            Debug.Assert(other != null);
            if (other == null)
                return false;
            // Check parent
            if (!base.AssertEquals(other))
                return false;
            // Check basic types
            if (this.value == null) {
                Debug.Assert(other.value == null);
                if (other.value != null)
                    return false;
            } else {
                Debug.Assert(this.value.Name == other.value.Name);
                if (this.value != other.value)
                    return false;
            }
            return true;
        }
#endif
    }
}