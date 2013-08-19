// ***********************************************************************
// <copyright file="JsonPointer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


namespace Starcounter.Internal
{
    /// <summary>
    /// Class JsonPointer
    /// </summary>
    internal class JsonPointer : IEnumerator<String>
    {
        /// <summary>
        /// The _buffer
        /// </summary>
        private Byte[] _buffer;
        /// <summary>
        /// The _pointer
        /// </summary>
        private Byte[] _pointer;
        /// <summary>
        /// The _current token
        /// </summary>
        private String _currentToken;
        /// <summary>
        /// The _offset
        /// </summary>
        private Int32 _offset;
        /// <summary>
        /// The _buffer pos
        /// </summary>
        private Int32 _bufferPos;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonPointer" /> class.
        /// </summary>
        /// <param name="pointer">The pointer.</param>
        internal JsonPointer(Byte[] pointer)
        {
            _pointer = pointer;
            _currentToken = null;
            _offset = 0;
            _bufferPos = -1;
            _buffer = new Byte[pointer.Length];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonPointer" /> class.
        /// </summary>
        /// <param name="pointer">The pointer.</param>
        internal JsonPointer(String pointer) 
            : this(Encoding.UTF8.GetBytes(pointer))
        {
        }

        /// <summary>
        /// Finds the next token.
        /// </summary>
        /// <returns>Boolean.</returns>
        private Boolean FindNextToken()
        {
            Boolean afterFirst;
            Byte current;

            _currentToken = null;
            if (_offset == _pointer.Length)
            {
                _bufferPos = -1;
                return false;
            }

            afterFirst = false;
            _bufferPos = 0;

            while (_offset < _pointer.Length)
            {
                current = _pointer[_offset];
                if (current == '/') // Start or end of token.
                {
                    if (afterFirst)
                        break;

                    _offset++;
                    afterFirst = true;
                }
                else if (current == '~')
                {
                    DecodeTildeEscapeCharacter();
                }
                else if (current == '\\')
                {
                    DecodeEscapeSequence();
                }
                else
                {
                    _buffer[_bufferPos++] = current;
                    _offset++;
                }
            }

//            _currentToken = Encoding.UTF8.GetString(_buffer, 0, _bufferPos);
            return true;
        }

        /// <summary>
        /// Decodes the tilde escape character.
        /// </summary>
        /// <exception cref="System.Exception">Unexpected ecsape sequence. End of pointer reached.</exception>
        private void DecodeTildeEscapeCharacter()
        {
            Byte current;
            _offset++;
            if (_offset >= _pointer.Length)
                throw new Exception("Unexpected ecsape sequence. End of pointer reached.");

            current = _pointer[_offset++];
            if (current == '0')
                _buffer[_bufferPos++] = (Byte)'~';
            else if (current == '1')
                _buffer[_bufferPos++] = (Byte)'/';
            else
                throw new Exception("Unexpected token. Illegal escape character.");
        }

        /// <summary>
        /// Decodes the escape sequence.
        /// </summary>
        /// <exception cref="System.Exception">Unexpected token. End of pointer reached.</exception>
        private void DecodeEscapeSequence()
        {
            Byte current;
            Int32 unicodeValue;

            _offset++;
            if (_offset >= _pointer.Length)
                throw new Exception("Unexpected token. End of pointer reached.");

            current = _pointer[_offset++];
            if (current == 'u') // Four digit unicode escape sequence.
            {
                if ((_offset + 4) >= _pointer.Length)
                    throw new Exception("Unexpected token. End of pointer reached.");

                unicodeValue = 0;
                for (Int32 i = 0; i < 4; i++)
                {
                    unicodeValue = (unicodeValue * 10) + (_pointer[_offset] - '0');
                    _offset++;
                }

                // TODO: 
                // This might not be correct. Check that we can cast it directly.
                _buffer[_bufferPos++] = (Byte)unicodeValue;
            }
            else
                throw new Exception("Unexpected escape sequence.");
        }

        /// <summary>
        /// Gets the current as int.
        /// </summary>
        /// <value>The current as int.</value>
        /// <exception cref="System.Exception">The current token is not a number.</exception>
        public Int32 CurrentAsInt
        {
            get
            {
                Int32 value;
                Byte current;

                if (_bufferPos == -1) return -1;

                value = 0;
                for (Int32 i = 0; i < _bufferPos; i++)
                {
                    current = _buffer[i];
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
        public String Current
        {
            get 
            {
                if ((_bufferPos != -1) && (_currentToken == null))
                    _currentToken = Encoding.UTF8.GetString(_buffer, 0, _bufferPos);
                return _currentToken;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <value>The current.</value>
        /// <returns>The element in the collection at the current position of the enumerator.</returns>
        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        public bool MoveNext()
        {
            return FindNextToken();
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset()
        {
            _currentToken = null;
            _offset = 0;
            _bufferPos = -1;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return Encoding.UTF8.GetString(_pointer);
        }
    }
}
