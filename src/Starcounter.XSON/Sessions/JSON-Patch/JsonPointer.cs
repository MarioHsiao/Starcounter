// ***********************************************************************
// <copyright file="JsonPointer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Advanced.XSON;
using Starcounter.Templates;

namespace Starcounter.XSON {
    /// <summary>
    /// Class representing a jsonpointer as described in http://tools.ietf.org/html/rfc6901
    /// </summary>
    public class JsonPointer : IEnumerator<string> {
        private byte[] buffer;
        private byte[] pointer;
        private int offset;
        private int bufferPos;
        private string currentToken;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonPointer" /> class.
        /// </summary>
        /// <param name="pointer">The pointer.</param>
        public JsonPointer(byte[] pointer) {
            this.pointer = pointer;
            this.currentToken = null;
            this.offset = 0;
            this.bufferPos = -1;
            this.buffer = new byte[pointer.Length];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonPointer" /> class.
        /// </summary>
        /// <param name="pointer">The pointer.</param>
        public JsonPointer(string pointer)
            : this(Encoding.UTF8.GetBytes(pointer)) {
        }

        /// <summary>
        /// Finds the next token.
        /// </summary>
        /// <returns>Boolean.</returns>
        private Boolean FindNextToken() {
            Boolean afterFirst;
            Byte current;

            currentToken = null;
            if (offset == pointer.Length) {
                bufferPos = -1;
                return false;
            }

            afterFirst = false;
            bufferPos = 0;

            while (offset < pointer.Length) {
                current = pointer[offset];
                if (current == '/') { // Start or end of token.
                    if (afterFirst) break;

                    offset++;
                    afterFirst = true;
                } else if (current == '~') {
                    DecodeTildeEscapeCharacter();
                } else if (current == '\\') {
                    DecodeEscapeSequence();
                } else {
                    buffer[bufferPos++] = current;
                    offset++;
                }
            }
            return true;
        }

        /// <summary>
        /// Decodes the tilde escape character.
        /// </summary>
        /// <exception cref="System.Exception">Unexpected ecsape sequence. End of pointer reached.</exception>
        private void DecodeTildeEscapeCharacter() {
            byte current;
            offset++;
            if (offset >= pointer.Length)
                throw new Exception("Unexpected ecsape sequence. End of pointer reached.");

            current = pointer[offset++];
            if (current == '0')
                buffer[bufferPos++] = (byte)'~';
            else if (current == '1')
                buffer[bufferPos++] = (byte)'/';
            else
                throw new Exception("Unexpected token. Illegal escape character.");
        }

        /// <summary>
        /// Decodes the escape sequence.
        /// </summary>
        /// <exception cref="System.Exception">Unexpected token. End of pointer reached.</exception>
        private void DecodeEscapeSequence() {
            byte current;
            int unicodeValue;

            offset++;
            if (offset >= pointer.Length)
                throw new Exception("Unexpected token. End of pointer reached.");

            current = pointer[offset++];
            if (current == 'u') { // Four digit unicode escape sequence.
                if ((offset + 4) >= pointer.Length)
                    throw new Exception("Unexpected token. End of pointer reached.");

                unicodeValue = 0;
                for (int i = 0; i < 4; i++) {
                    unicodeValue = (unicodeValue * 10) + (pointer[offset] - '0');
                    offset++;
                }

                // TODO: 
                // This might not be correct. Check that we can cast it directly.
                buffer[bufferPos++] = (byte)unicodeValue;
            } else {
                throw new Exception("Unexpected escape sequence.");
            }
        }

        /// <summary>
        /// Gets the current as int.
        /// </summary>
        /// <value>The current as int.</value>
        /// <exception cref="System.Exception">The current token is not a number.</exception>
        public int CurrentAsInt {
            get {
                int value;
                byte current;

                if (bufferPos == -1) return -1;

                value = 0;
                for (int i = 0; i < bufferPos; i++) {
                    current = buffer[i];
                    if ((current >= '0') && (current <= '9'))
                        value = (value * 10) + (current - '0');
                    else
                        throw new Exception("The current token is not a number.");
                }
                return value;
            }
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <value>The current.</value>
        /// <returns>The element in the collection at the current position of the enumerator.</returns>
        public string Current {
            get {
                if ((bufferPos != -1) && (currentToken == null))
                    currentToken = Encoding.UTF8.GetString(buffer, 0, bufferPos);
                return currentToken;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <value>The current.</value>
        /// <returns>The element in the collection at the current position of the enumerator.</returns>
        object System.Collections.IEnumerator.Current {
            get { return Current; }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        public bool MoveNext() {
            return FindNextToken();
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset() {
            currentToken = null;
            offset = 0;
            bufferPos = -1;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString() {
            return Encoding.UTF8.GetString(pointer, 0, pointer.Length);
        }
    }
}
