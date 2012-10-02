using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


namespace Starcounter.Internal
{
    internal class JsonPointer : IEnumerator<String>
    {
        private Byte[] _buffer;
        private Byte[] _pointer;
        private String _currentToken;
        private Int32 _offset;
        private Int32 _bufferPos;

        internal JsonPointer(Byte[] pointer)
        {
            _pointer = pointer;
            _currentToken = null;
            _offset = 0;
            _bufferPos = -1;
            _buffer = new Byte[pointer.Length];
        }

        internal JsonPointer(String pointer) 
            : this(Encoding.UTF8.GetBytes(pointer))
        {
        }

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

        public String Current
        {
            get 
            {
                if ((_bufferPos != -1) && (_currentToken == null))
                    _currentToken = Encoding.UTF8.GetString(_buffer, 0, _bufferPos);
                return _currentToken;
            }
        }

        public void Dispose()
        {
        }

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

        public bool MoveNext()
        {
            return FindNextToken();
        }

        public void Reset()
        {
            _currentToken = null;
            _offset = 0;
            _bufferPos = -1;
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(_pointer);
        }
    }
}
