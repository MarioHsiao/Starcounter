
using Starcounter.Binding;
using System;

namespace Starcounter.Advanced {

    /// <summary>
    /// Causes Starcounter to notify the implementing database
    /// class every time a database attribute is updated.
    /// </summary>
    public interface ISetValueCallback {
        /// <summary>
        /// Invoked by Starcounter when a database attribute is
        /// being assigned.
        /// </summary>
        /// <param name="attributeName">The name of the attriubyte
        /// that was assigned.</param>
        /// <param name="value">The value assigned to the given
        /// database attribute.</param>
        void OnValueSet(string attributeName, object value);

        /// <summary>
        /// Invoked when a binary database column is written.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <param name="value">The value written.</param>
        void OnWriteBinary(string name, Binary value);

        /// <summary>
        /// Invoked when a boolean database column is written.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <param name="value">The value written.</param>
        void OnWriteBoolean(string name, bool? value);

        /// <summary>
        /// Invoked when a DateTime database column is written.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <param name="value">The value written.</param>
        void OnWriteDateTime(string name, DateTime? value);

        /// <summary>
        /// Invoked when a decimal database column is written.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <param name="value">The value written.</param>
        void OnWriteDecimal(string name, Decimal? value);

        /// <summary>
        /// Invoked when a double database column is written.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <param name="value">The value written.</param>
        void OnWriteDouble(string name, Double? value);

        /// <summary>
        /// Invoked when a signed integer database column is written.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <param name="value">The value written.</param>
        void OnWriteInteger(string name, Int64? value);

        /// <summary>
        /// Invoked when an object reference database column is written.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <param name="value">The value written.</param>
        void OnWriteObject(string name, IObjectProxy value);

        /// <summary>
        /// Invoked when a single/float database column is written.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <param name="value">The value written.</param>
        void OnWriteSingle(string name, Single? value);

        /// <summary>
        /// Invoked when a string database column is written.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <param name="value">The value written.</param>
        void OnWriteString(string name, string value);

        /// <summary>
        /// Invoked when a TimeSpan database column is written.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <param name="value">The value written.</param>
        void OnWriteTimeSpan(string name, TimeSpan? value);

        /// <summary>
        /// Invoked when an unsigned integer database column is written.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <param name="value">The value written.</param>
        void OnWriteUnsignedInteger(string name, UInt64? value);
    }
}