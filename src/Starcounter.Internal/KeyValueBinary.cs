// ***********************************************************************
// <copyright file="KeyValueBinary.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Starcounter.Internal
{
    /// <summary>
    /// Utility class allowing a simple and fast serialization/deserialization
    /// of key-value pairs, usually given as a <see cref="System.Collections.Generic.Dictionary&lt;TKey, TValue&gt;"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Current limitations:
    /// 1) Keys can not contain delimiter (currently '=').
    /// 2) The maximum size of key+value is KeyValueBinary.PropertyPairMaxSize (998).
    /// 3) If value is NULL, key can not be string.Empty
    /// </para>
    /// <para>
    /// Serialized instances look like:
    /// 000[Key]=[Value]
    /// where
    /// * 000 is the length of the coming key-value pair,
    /// * key is the name of the key,
    /// * '=' is the delimiter,
    /// * and [Value] holds the actual value.
    /// </para>
    /// <para>
    /// Currently, the only error handling in this class is implemented as asserts.
    /// </para>
    /// </remarks>
    public class KeyValueBinary
    {
        /// <summary>
        /// Gets the maximum size of a key-value pair, i.e. the combination of
        /// the name of the key and the actual property value.
        /// </summary>
        public const int PropertyPairMaxSize = 998;

        /// <summary>
        /// Gets the delimiter to use during serialization/deserialization.
        /// </summary>
        public const char Delimiter = '=';

        /// <summary>
        /// Prevent instantiation without using factory methods.
        /// </summary>
        private KeyValueBinary(string serialized)
        {
            this.Value = serialized;
        }

        /// <summary>
        /// The (opaque) value representing the key/value data after
        /// it has been serialized using a factory method, like the
        /// <see cref="FromDictionary"/> method.
        /// </summary>
        public string Value
        {
            get;
            private set;
        }

        /// <summary>
        /// Serializes the given <see cref="System.Collections.Generic.Dictionary&lt;TKey, TValue&gt;"/> to a in instance of
        /// <see cref="KeyValueBinary"/>.
        /// </summary>
        /// <param name="dictionary">The <see cref="System.Collections.Generic.Dictionary&lt;TKey, TValue&gt;"/> to serialize.</param>
        /// <returns>A <see cref="KeyValueBinary"/> holding all items serialized from the 
        /// given <see cref="System.Collections.Generic.Dictionary&lt;TKey, TValue&gt;"/>.</returns>
        public static KeyValueBinary FromDictionary(Dictionary<string, string> dictionary)
        {
            StringBuilder buffer = new StringBuilder();
            foreach (var item in dictionary)
            {
                SerializeKeyValuePair(buffer, item.Key, item.Value);
            }

            return new KeyValueBinary(AppendTerminatorAndConvertToString(buffer));
        }

        /// <summary>
        /// Serializes the given <see cref="T:object[]"/> to a in instance of
        /// <see cref="KeyValueBinary"/>. The actual data that will be serialized
        /// is produced by calling <see cref="object.ToString()"/> on each item
        /// before it is serialized.
        /// </summary>
        /// <remarks>
        /// Individual items in the serialized results will be keyed by their
        /// position in the array, like "0=[value0], 1=[value1]..."
        /// </remarks>
        /// <param name="array">The array to serialize.</param>
        /// <returns>A <see cref="KeyValueBinary"/> holding all items serialized from the 
        /// given <see cref="T:object[]"/>.</returns>
        public static KeyValueBinary FromArray(object[] array)
        {
            return FromArray(array, 0);
        }

        /// <summary>
        /// Serializes the given <see cref="T:object[]"/> to a in instance of
        /// <see cref="KeyValueBinary"/>. The actual data that will be serialized
        /// is produced by calling <see cref="object.ToString()"/> on each item
        /// before it is serialized.
        /// </summary>
        /// <remarks>
        /// Individual items in the serialized results will be keyed by their
        /// position in the array, like "0=[value0], 1=[value1]..."
        /// </remarks>
        /// <param name="array">The array to serialize.</param>
        /// <param name="startIndex">The index to start from when retreiving
        /// the items to serialize from the given array.</param>
        /// <returns>A <see cref="KeyValueBinary"/> holding all items serialized from the 
        /// given <see cref="T:object[]"/>.</returns>
        public static KeyValueBinary FromArray(object[] array, int startIndex)
        {
            StringBuilder buffer = new StringBuilder();
            int items = array == null ? 0 : array.Length;

            for (int i = startIndex; i < items; i++)
            {
                SerializeKeyValuePair(buffer, i.ToString(), array[i].ToString());
            }

            return new KeyValueBinary(AppendTerminatorAndConvertToString(buffer));
        }

        /// <summary>
        /// Creates a dictionary from a byte array, previosuly serialized using the
        /// <see cref="FromDictionary"/> method.
        /// </summary>
        /// <param name="binary">The data to deserialized</param>
        /// <returns>A <see cref="System.Collections.Generic.Dictionary&lt;TKey, TValue&gt;"/> with all key/value pairs found
        /// in the given binary.</returns>
        public static Dictionary<string, string> ToDictionary(byte[] binary)
        {
            string content = Encoding.UTF8.GetString(binary);
            return ToDictionary(content);
        }

        /// <summary>
        /// Creates a dictionary from a string, previosuly serialized using one
        /// of the factory methods, e.g <see cref="FromDictionary"/>.
        /// </summary>
        /// <param name="content">The string to deserialize</param>
        /// <returns>A <see cref="System.Collections.Generic.Dictionary&lt;TKey, TValue&gt;"/> with all key/value pairs found
        /// in the given string.</returns>
        public static Dictionary<string, string> ToDictionary(string content)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            ParseContent(content, (string key, string value) => dictionary.Add(key, value));
            return dictionary;
        }

        /// <summary>
        /// Creates a string array from a string, previosuly serialized using one
        /// of the factory methods, usually <see cref="FromArray(object[])"/>.
        /// </summary>
        /// <seealso cref="ToArray()"/>
        /// <param name="content">The serialized content to deserialize.</param>
        /// <returns>A <see cref="T:string[]"/> with all values found in the
        /// serialized instance of <see cref="KeyValueBinary"/> represented by
        /// the given <paramref name="content"/>.</returns>
        public static string[] ToArray(string content)
        {
            List<string> array = new List<string>();
            ParseContent(content, (string key, string value) => array.Add(value));
            return array.ToArray();
        }

        /// <summary>
        /// Returns the value of the current <see cref="KeyValueBinary"/>
        /// as an array of bytes.
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            return Encoding.UTF8.GetBytes(this.Value);
        }

        /// <summary>
        /// Returns the value of the current <see cref="KeyValueBinary"/>
        /// as a <see cref="System.Collections.Generic.Dictionary&lt;TKey, TValue&gt;"/>.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> ToDictionary()
        {
            return KeyValueBinary.ToDictionary(this.Value);
        }

        /// <summary>
        /// Creates a string array from the current instance, previosuly serialized
        /// using one of the factory methods, usually <see cref="FromArray(object[])"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:string[]"/> with all values found in the current serialized
        /// instance of <see cref="KeyValueBinary"/>. Keys are ignored.
        /// </returns>
        public string[] ToArray()
        {
            return KeyValueBinary.ToArray(this.Value);
        }

        /// <summary>
        /// Parses the content.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="receiver">The receiver.</param>
        static void ParseContent(string content, Action<string, string> receiver)
        {
            int contentLength;

            string next = content;
            contentLength = int.Parse(next.Substring(0, 3));
            while (contentLength > 0)
            {
                string keyValuePair = next.Substring(3, contentLength);
                int indexOfEqualSign = keyValuePair.IndexOf('=');
                string key;

                if (indexOfEqualSign == -1)
                {
                    key = keyValuePair;
                    Trace.Assert(key.Length == contentLength);
                    receiver(key, null);
                }
                else
                {
                    key = keyValuePair.Substring(0, indexOfEqualSign);
                    int lengthOfPropertyValue = keyValuePair.Length - (key.Length + 1);
                    receiver(key, keyValuePair.Substring(indexOfEqualSign + 1, lengthOfPropertyValue));
                }

                next = next.Substring(3 + contentLength);
                contentLength = int.Parse(next.Substring(0, 3));
            }
        }

        /// <summary>
        /// Serializes the key value pair.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        static void SerializeKeyValuePair(StringBuilder builder, string key, string value)
        {
            Trace.Assert(!key.Contains(KeyValueBinary.Delimiter));
            if (value == null)
            {
                Trace.Assert(!key.Equals(string.Empty));
                Trace.Assert(key.Length < 999);
                builder.AppendFormat("{0}{1}", key.Length.ToString("D3"), key);
            }
            else
            {
                int lengthOfContent = key.Length + value.Length + 1;
                Trace.Assert(lengthOfContent < 1000);
                builder.AppendFormat("{0}{1}={2}", lengthOfContent.ToString("D3"), key, value);
            }
        }

        /// <summary>
        /// Appends the terminator and convert to string.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns>System.String.</returns>
        static string AppendTerminatorAndConvertToString(StringBuilder buffer)
        {
            buffer.Append("000");
            return buffer.ToString();
        }
    }
}
