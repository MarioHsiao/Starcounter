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

namespace Starcounter.XSON.JsonPatch {
    /// <summary>
    /// Class JsonPointer
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
            Byte current;
            offset++;
            if (offset >= pointer.Length)
                throw new Exception("Unexpected ecsape sequence. End of pointer reached.");

            current = pointer[offset++];
            if (current == '0')
                buffer[bufferPos++] = (Byte)'~';
            else if (current == '1')
                buffer[bufferPos++] = (Byte)'/';
            else
                throw new Exception("Unexpected token. Illegal escape character.");
        }

        /// <summary>
        /// Decodes the escape sequence.
        /// </summary>
        /// <exception cref="System.Exception">Unexpected token. End of pointer reached.</exception>
        private void DecodeEscapeSequence() {
            Byte current;
            Int32 unicodeValue;

            offset++;
            if (offset >= pointer.Length)
                throw new Exception("Unexpected token. End of pointer reached.");

            current = pointer[offset++];
            if (current == 'u') { // Four digit unicode escape sequence.
                if ((offset + 4) >= pointer.Length)
                    throw new Exception("Unexpected token. End of pointer reached.");

                unicodeValue = 0;
                for (Int32 i = 0; i < 4; i++) {
                    unicodeValue = (unicodeValue * 10) + (pointer[offset] - '0');
                    offset++;
                }

                // TODO: 
                // This might not be correct. Check that we can cast it directly.
                buffer[bufferPos++] = (Byte)unicodeValue;
            } else {
                throw new Exception("Unexpected escape sequence.");
            }
        }

        /// <summary>
        /// Gets the current as int.
        /// </summary>
        /// <value>The current as int.</value>
        /// <exception cref="System.Exception">The current token is not a number.</exception>
        public Int32 CurrentAsInt {
            get {
                Int32 value;
                Byte current;

                if (bufferPos == -1) return -1;

                value = 0;
                for (Int32 i = 0; i < bufferPos; i++) {
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
        public String Current {
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

        /// <summary>
        /// Evaluates the jsonpointer and retrieves the property it points to 
        /// and the correct jsoninstance for the template starting from the specified root.
        /// </summary>
        /// <param name="mainApp">The root json.</param>
        /// <param name="ptr">The pointer containing the path to the template.</param>
        /// <returns></returns>
        public JsonProperty Evaluate(Json root) {
            Boolean currentIsTApp;
            Boolean nextTokenShouldBeIndex;
            Int32 index;
            Object current = null;
            Json mainApp = root;

            nextTokenShouldBeIndex = false;
            currentIsTApp = false;
            while (this.MoveNext()) {
                // TODO: 
                // Check if this can be improved. Searching for transaction and execute every
                // step in a new action is not the most efficient way.
                mainApp.ExecuteInScope(() => {
                    if (nextTokenShouldBeIndex) {
                        // Previous object was a Set. This token should be an index
                        // to that Set. If not, it's an invalid patch.
                        nextTokenShouldBeIndex = false;
                        index = this.CurrentAsInt;

                        Json list = ((TObjArr)current).Getter(mainApp);
                        current = list._GetAt(index);
                    } else {
                        if (currentIsTApp) {
                            mainApp = ((TObject)current).Getter(mainApp);
                            //                            mainApp.ResumeTransaction(false);
                            currentIsTApp = false;
                        }

                        if (mainApp.IsArray) {
                            throw new NotImplementedException();
                        }

                        Template t = ((TObject)mainApp.Template).Properties.GetExposedTemplateByName(this.Current);
                        if (t == null) {
                            Boolean found = false;
                            if (mainApp.HasStepSiblings()) {
                                foreach (Json j in mainApp.GetStepSiblings()) {
                                    if (j.GetAppName() == this.Current) {
                                        current = j;
                                        found = true;
                                        break;
                                    }
                                }
                            }

                            if (!found) {
                                if (mainApp.GetAppName() == this.Current) {
                                    current = mainApp;
                                } else {
                                    throw new JsonPatchException(
                                        String.Format("Unknown property '{0}' in path.", this.Current),
                                        null
                                    );
                                }
                            }
                        } else {
                            current = t;
                        }
                    }

                    if (current is Json && !(current as Json).IsArray) {
                        mainApp = current as Json;
                    } else if (current is TObject) {
                        currentIsTApp = true;
                    } else if (current is TObjArr) {
                        nextTokenShouldBeIndex = true;
                    } else {
                        // Current token points to a value or an action.
                        // No more tokens should exist. If it does we need to 
                        // return an error.
                        if (this.MoveNext()) {
                            throw new JsonPatchException(
                                        String.Format("Invalid path in patch. Property: '{0}' was not expected", this.Current),
                                        null
                            );
                        }
                    }
                });
            }

            return new JsonProperty(mainApp, current as TValue);
        }

        public static JsonProperty Evaluate(Json root, string pointer) {
            return (new JsonPointer(pointer)).Evaluate(root);
        }

        public static JsonProperty Evaluate(Json root, byte[] pointer) {
            return (new JsonPointer(pointer)).Evaluate(root);
        }
    }
}
