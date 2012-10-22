// ***********************************************************************
// <copyright file="IntExtension.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Internal {
    /// <summary>
    /// Adds methods to converts data to the BigEndian format (not standard x86/x64)
    /// </summary>
    public static class IntExtensions {
        /// <summary>
        /// To the big endian bytes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <returns>System.Byte[][].</returns>
        /// <exception cref="System.InvalidCastException">Cannot be cast to T</exception>
        public static byte[] ToBigEndianBytes<T>(this int source) {
            byte[] bytes;

            var type = typeof(T);
            if (type == typeof(ushort))
                bytes = BitConverter.GetBytes((ushort)source);
            else if (type == typeof(ulong))
                bytes = BitConverter.GetBytes((ulong)source);
            else if (type == typeof(int))
                bytes = BitConverter.GetBytes(source);
            else
                throw new InvalidCastException("Cannot be cast to T");

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        /// <summary>
        /// To the little endian int.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.ArgumentException">Unsupported Size</exception>
        public static int ToLittleEndianInt(this byte[] source) {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(source);

            if (source.Length == 2)
                return BitConverter.ToUInt16(source, 0);

            if (source.Length == 8)
                return (int)BitConverter.ToUInt64(source, 0);

            throw new ArgumentException("Unsupported Size");
        }
    }
}
