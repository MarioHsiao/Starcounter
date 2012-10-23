// ***********************************************************************
// <copyright file="SqlDebugHelper.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace Sc.Query.Execution
{
    class SqlDebugHelper
    {
        static String logFilePath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "ScDebugLog.txt");
        const Int32 byteLineLength = 64;

        public static void PrintDelimiter(String message)
        {
            String delimiterMessage = Environment.NewLine + "------------------------------------------" + Environment.NewLine + message + Environment.NewLine;
            Console.WriteLine(delimiterMessage);
            File.AppendAllText(logFilePath, delimiterMessage);
        }

        public static void PrintByteBuffer(String dataName, Byte[] buffer, Boolean embLength)
        {
            StringBuilder bufferString = new StringBuilder(1024);
            Int32 lengthBytes = 0;
            if (embLength)
                lengthBytes = BitConverter.ToInt32(buffer, 0);
            else
                lengthBytes = buffer.Length;

            for (Int32 i = 0; i < lengthBytes; i++)
            {
                bufferString.Append(buffer[i] + " ");

                if (((i + 1) % byteLineLength) == 0)
                    bufferString.Append(Environment.NewLine);
            }

            String complete = dataName + ":" + Environment.NewLine + bufferString + Environment.NewLine + Environment.NewLine;
            Console.WriteLine(complete);
            File.AppendAllText(logFilePath, complete);
        }

        public unsafe static void PrintByteBuffer(String dataName, Byte* nativeBuff)
        {
            Int32 bufferLenBytes = (*(Int32*)(nativeBuff));
            Byte[] managedBuff = new Byte[bufferLenBytes];

            Marshal.Copy((IntPtr)nativeBuff, managedBuff, 0, bufferLenBytes);

            PrintByteBuffer(dataName, managedBuff, false);
        }
    }
}
