using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using BuildSystemHelper;
using System.Security.Cryptography;
using System.Web;

namespace BuildSystemHelper
{
    public class DownloadID
    {
        public Byte[] IDBytes = null;
        public String IDFullBase32 = "000000000000000000000000";
        public String IDTailBase64 = "0000000";
        public UInt32 IDTailDecimal = 0;

        // Length of tail identifier in bytes.
        const UInt32 TailLengthBytes = 4;

        // Valid chars for the Base32 encoding.
        const String validBase32Chars = "QAZ2WSX3" + "EDC4RFV5" + "TGB6YHN7" + "UJM8K9LP";

        /// <summary>
        /// Generates unique DownloadID.
        /// </summary>
        /// <param name="ftpConfigName">Configuation name for FTP.</param>
        /// <param name="lengthInBytes">Length of identifier in bytes.</param>
        /// <param name="pathToHistoryFile">Path on to histoy file on FTP.</param>
        public UInt32 Generate(String ftpConfigName, UInt32 lengthInBytes, String pathToHistoryFile)
        {
            // Determines number of Base32 string characters for DownloadID.
            UInt32 base32CharactersNum = (lengthInBytes * 8) / 5;

            TextWriter errorOut = Console.Error;
            errorOut.WriteLine("Generating unique DownloadID, different from all previous generations...");

            // Reading history file with previous generations.
            String[] previousRandomNums = BuildSystem.GetFTPFileAllLines(ftpConfigName, pathToHistoryFile);

            // Calculating how many entries we already have.
            UInt32 previousRandomNumCount = 0;

            // Checking if any history exists.
            if (previousRandomNums != null)
            {
                foreach (String previousString in previousRandomNums)
                {
                    // Checking if length of each entry is correct.
                    if (previousString.Length == base32CharactersNum)
                        previousRandomNumCount++;
                }
            }
            errorOut.WriteLine("Found previous DownloadID entries: " + previousRandomNumCount);

            // Generating random number of specified length in bytes.
            IDBytes = new Byte[lengthInBytes];
            RNGCryptoServiceProvider randCSP = new RNGCryptoServiceProvider();
            randCSP.GetBytes(IDBytes);

            // Going through already existing list of unique download IDs and checking for last part uniqueness.
            if (previousRandomNums != null)
            {
                while (true)
                {
                    Boolean needToRegenerate = false;

                    // Going through already existing list of unique download IDs and checking for last part uniqueness.
                    foreach (String previousString in previousRandomNums)
                    {
                        if (previousString.Length == base32CharactersNum)
                        {
                            // Converting the old random string to Base32.
                            Byte[] previousRand = FromBase32String(previousString);

                            // Going through tail bytes.
                            UInt32 i = 0;
                            for (i = lengthInBytes - TailLengthBytes; i < lengthInBytes; i++)
                            {
                                if (IDBytes[i] != previousRand[i])
                                    break;
                            }

                            // Checking if postfix sequences are the same.
                            if (i == lengthInBytes)
                            {
                                // We already have at least one same postfix.
                                needToRegenerate = true;
                                break;
                            }
                        }
                    }

                    // Checking if we need to regenerate the random sequence.
                    if (!needToRegenerate)
                        break;

                    // Re-generating the random sequence.
                    randCSP.GetBytes(IDBytes);
                }
            }

            // Converting postfix byte array to Base64 string.
            Byte[] tailBytes = new Byte[TailLengthBytes];
            for (Int32 i = 0; i < TailLengthBytes; i++)
                tailBytes[i] = IDBytes[lengthInBytes - i - 1];

            // Converting whole generated sequence to Base32 string.
            IDFullBase32 = ToBase32String(IDBytes);

            // Adding converted postfix bytes in Base64 format.
            IDTailBase64 = HttpServerUtility.UrlTokenEncode(tailBytes);
            IDTailDecimal = BitConverter.ToUInt32(tailBytes, 0);

            errorOut.WriteLine("Successfully generated new DownloadID: { " + IDFullBase32 + ", " + IDTailBase64 + ", " + IDTailDecimal + " }.");

            return previousRandomNumCount;
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

                sb.Append(validBase32Chars[index]);
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
                bytes[0] = (byte)(validBase32Chars.IndexOf(str[0]) | validBase32Chars.IndexOf(str[1]) << 5);
                return bytes;
            }

            bit_buffer = (validBase32Chars.IndexOf(str[0]) | validBase32Chars.IndexOf(str[1]) << 5);
            bits_in_buffer = 10;
            currentCharIndex = 2;
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)bit_buffer;
                bit_buffer >>= 8;
                bits_in_buffer -= 8;
                while (bits_in_buffer < 8 && currentCharIndex < str.Length)
                {
                    bit_buffer |= validBase32Chars.IndexOf(str[currentCharIndex++]) << bits_in_buffer;
                    bits_in_buffer += 5;
                }
            }

            return bytes;
        }
    }
}
