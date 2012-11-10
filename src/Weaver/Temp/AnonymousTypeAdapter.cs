// ***********************************************************************
// <copyright file="AnonymousTypeAdapter.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Interface IAnonymousType
    /// </summary>
    public interface IAnonymousType {
        /// <summary>
        /// Gets the underlying object.
        /// </summary>
        /// <value>The underlying object.</value>
        IObjectView UnderlyingObject {
            get;
        }
    }

    /// <summary>
    /// Class AnonymousTypeAdapter
    /// </summary>
    public sealed class AnonymousTypeAdapter {
        /// <summary>
        /// The indices
        /// </summary>
        private readonly int[] indices;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnonymousTypeAdapter" /> class.
        /// </summary>
        /// <param name="indices">The indices.</param>
        public AnonymousTypeAdapter(int[] indices) {
            this.indices = indices;
        }

        /// <summary>
        /// Gets the property.
        /// </summary>
        /// <param name="objectView">The object view.</param>
        /// <param name="index">The index.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object GetProperty(IObjectView objectView, int index, Type targetType) {
            int resolvedIndex = ResolveIndex(index);
            // Determine whether the type is nullable and get the underlying type.
            Type valueType;
            bool targetIsNullable;
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                valueType = targetType.GetGenericArguments()[0];
                targetIsNullable = true;
            } else {
                valueType = targetType;
                targetIsNullable = false;
            }
            switch (Type.GetTypeCode(valueType)) {
                case TypeCode.Boolean:
                    return GetValue(objectView.GetBoolean(resolvedIndex), targetIsNullable);
                case TypeCode.Byte:
                    return GetValue(objectView.GetByte(resolvedIndex), targetIsNullable);
                case TypeCode.DateTime:
                    return GetValue(objectView.GetDateTime(resolvedIndex), targetIsNullable);
                case TypeCode.Decimal:
                    return GetValue(objectView.GetDecimal(resolvedIndex), targetIsNullable);
                case TypeCode.Double:
                    return GetValue(objectView.GetDouble(resolvedIndex), targetIsNullable);
                case TypeCode.Int16:
                    return GetValue(objectView.GetInt16(resolvedIndex), targetIsNullable);
                case TypeCode.Int32:
                    return GetValue(objectView.GetInt32(resolvedIndex), targetIsNullable);
                case TypeCode.Int64:
                    return GetValue(objectView.GetInt64(resolvedIndex), targetIsNullable);
                case TypeCode.UInt16:
                    return GetValue(objectView.GetUInt16(resolvedIndex), targetIsNullable);
                case TypeCode.UInt32:
                    return GetValue(objectView.GetUInt32(resolvedIndex), targetIsNullable);
                case TypeCode.UInt64:
                    return GetValue(objectView.GetUInt64(resolvedIndex), targetIsNullable);
                case TypeCode.SByte:
                    return GetValue(objectView.GetSByte(resolvedIndex), targetIsNullable);
                case TypeCode.Single:
                    return GetValue(objectView.GetSingle(resolvedIndex), targetIsNullable);
                case TypeCode.String:
                    return objectView.GetString(resolvedIndex);
                default:
                    if (valueType == typeof(Binary)) {
                        return GetValue(objectView.GetBinary(resolvedIndex), targetIsNullable);
                    }
                    if (typeof(IObjectView).IsAssignableFrom(valueType)) {
                        return objectView.GetObject(resolvedIndex);
                    }
                    // TODO: Process anonymous types here.
                    throw new NotImplementedException(string.Format("Unexpected value type: {0}.", valueType));
            }
        }

        /// <summary>
        /// Resolves the index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>System.Int32.</returns>
        public int ResolveIndex(int index) {
            return indices[index];
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <param name="targetIsNullable">if set to <c>true</c> [target is nullable].</param>
        /// <returns>System.Object.</returns>
        private static object GetValue<T>(T? value, bool targetIsNullable) where T : struct {
            return targetIsNullable ? (object)value : value.Value;
        }
    }
}
