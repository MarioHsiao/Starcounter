﻿
using System;

namespace Starcounter.Binding
{

    public abstract class BinaryPropertyBinding : PrimitivePropertyBinding
    {

        public override sealed DbTypeCode TypeCode { get { return DbTypeCode.Binary; } }

        protected override sealed Boolean? DoGetBoolean(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Byte? DoGetByte(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed DateTime? DoGetDateTime(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Decimal? DoGetDecimal(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Double? DoGetDouble(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Int16? DoGetInt16(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Int32? DoGetInt32(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Int64? DoGetInt64(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Entity DoGetObject(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed SByte? DoGetSByte(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed Single? DoGetSingle(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed String DoGetString(object obj)
        {
            Binary value;
            value = DoGetBinary(obj);
            if (value.IsNull)
            {
                return null;
            }
            return value.ToString();
        }

        protected override sealed UInt16? DoGetUInt16(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed UInt32? DoGetUInt32(object obj)
        {
            throw ExceptionForInvalidType();
        }

        protected override sealed UInt64? DoGetUInt64(object obj)
        {
            throw ExceptionForInvalidType();
        }

        private Exception ExceptionForInvalidType()
        {
            throw new NotSupportedException(
                "Attempt to access a binary attribute as something other then a binary attribute."
            );
        }
    }

}
