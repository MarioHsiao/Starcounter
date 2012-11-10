// ***********************************************************************
// <copyright file="HashHelper.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Starcounter.Internal.Weaver {

    /// <summary>
    /// Class HashHelper
    /// </summary>
    internal static class HashHelper {

        /// <summary>
        /// Computes the hash.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        public static string ComputeHash(string path) {
            HashAlgorithm hashAlgorithm = MD5.Create();
            // MD5 collisions have been proved "easily" reproduced, maybe we should turn
            // to something like SHA-2 (SHA-256/224) where collisions not yet has been
            // found?
            //
            // hashAlgorithm = SHA256.Create();
            using (FileStream inputStream = File.OpenRead(path)) {
                MemoryStream outputStream = new MemoryStream();
                using (CryptoStream cryptoStream = new CryptoStream(outputStream, hashAlgorithm, CryptoStreamMode.Write)) {
                    const int bufferLen = 4096;
                    byte[] buffer = new byte[bufferLen];
                    int read;
                    while ((read = inputStream.Read(buffer, 0, bufferLen)) > 0) {
                        cryptoStream.Write(buffer, 0, read);
                    }
                    cryptoStream.FlushFinalBlock();
                }
                byte[] hash = hashAlgorithm.Hash;
                StringBuilder stringBuilder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) {
                    stringBuilder.AppendFormat("{0:x2}", hash[i]);
                }
                return stringBuilder.ToString();
            }
        }
    }
}
