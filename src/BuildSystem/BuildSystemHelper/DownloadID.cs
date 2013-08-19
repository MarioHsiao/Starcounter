using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Web;

namespace BuildSystemHelper
{
    public class DownloadID
    {
        // Length of download key in bytes.
        const Int32 DownloadKeyLengthBytes = 15;

        // Valid chars for the Base32 encoding.
        const String ValidBase32Chars = "QAZ2WSX3" + "EDC4RFV5" + "TGB6YHN7" + "UJM8K9LP";

        /// <summary>
        /// Generates unique download key.
        /// </summary>
        /// <returns></returns>
        public static String GenerateNewUniqueDownloadKey()
        {
            // Generating random number of specified length in bytes.
            Byte[] IDBytes = new Byte[DownloadKeyLengthBytes];
            RNGCryptoServiceProvider randCSP = new RNGCryptoServiceProvider();
            randCSP.GetBytes(IDBytes);

            // Converting whole generated sequence to Base32 string.
            return ToBase32String(IDBytes);
        }
        
        /// <summary>
        /// Converts an array of bytes to a Base32 string.
        /// </summary>
        public static String ToBase32String(Byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(); // Holds the Base32 chars.
            Byte index;
            int hi = 5;
            int currentByte = 0;

            while (currentByte < bytes.Length)
            {
                // Do we need to use the next byte?
                if (hi > 8)
                {
                    // Get the last piece from the current byte, shift it to the right
                    // and increment the byte counter.
                    index = (byte)(bytes[currentByte++] >> (hi - 5));
                    if (currentByte != bytes.Length)
                    {
                        // If we are not at the end, get the first piece from
                        // the next byte, clear it and shift it to the left.
                        index = (byte)(((byte)(bytes[currentByte] << (16 - hi)) >> 3) | index);
                    }

                    hi -= 3;
                }
                else if (hi == 8)
                {
                    index = (byte)(bytes[currentByte++] >> 3);
                    hi -= 3;
                }
                else
                {
                    // Simply get the stuff from the current byte.
                    index = (byte)((byte)(bytes[currentByte] << (8 - hi)) >> 3);
                    hi += 5;
                }

                sb.Append(ValidBase32Chars[index]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts a Base32 string into an array of bytes.
        /// </summary>
        /// <exception cref="System.ArgumentException">
        /// Input string <paramref name="s">s</paramref> contains invalid Base32 characters.
        /// </exception>
        public static Byte[] FromBase32String(String str)
        {
            int numBytes = str.Length * 5 / 8;
            Byte[] bytes = new Byte[numBytes];

            // All UPPERCASE letters.
            str = str.ToUpper();

            int bit_buffer;
            int currentCharIndex;
            int bits_in_buffer;

            if (str.Length < 3)
            {
                bytes[0] = (byte)(ValidBase32Chars.IndexOf(str[0]) | ValidBase32Chars.IndexOf(str[1]) << 5);
                return bytes;
            }

            bit_buffer = (ValidBase32Chars.IndexOf(str[0]) | ValidBase32Chars.IndexOf(str[1]) << 5);
            bits_in_buffer = 10;
            currentCharIndex = 2;
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)bit_buffer;
                bit_buffer >>= 8;
                bits_in_buffer -= 8;
                while (bits_in_buffer < 8 && currentCharIndex < str.Length)
                {
                    bit_buffer |= ValidBase32Chars.IndexOf(str[currentCharIndex++]) << bits_in_buffer;
                    bits_in_buffer += 5;
                }
            }

            return bytes;
        }
    }
}
