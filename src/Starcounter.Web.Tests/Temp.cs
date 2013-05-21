// ***********************************************************************
// <copyright file="Temp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Internal.Uri;
using System.Text;
using System.Collections.Generic;
using System;
using Starcounter.Internal;

namespace __urimatcher__ {

    /// <summary>
    /// Class GeneratedRequestProcessor
    /// </summary>
    public class GeneratedRequestProcessor : TopLevelRequestProcessor {
        /// <summary>
        /// The sub0
        /// </summary>
        public Sub0_Processor Sub0 = new Sub0_Processor();
        /// <summary>
        /// The V u0
        /// </summary>
        public byte[] VU0 = new byte[] { (byte)'G', (byte)'E', (byte)'T', (byte)' ', (byte)'/', (byte)'p', (byte)'l', (byte)'a', (byte)'y', (byte)'e', (byte)'r', (byte)'s', (byte)'/', (byte)'{', (byte)'x', (byte)'}' };
        /// <summary>
        /// The sub1
        /// </summary>
        public RequestProcessor Sub1 = new Sub1_Processor();
        /// <summary>
        /// The V u1
        /// </summary>
        public byte[] VU1 = new byte[] { (byte)'G', (byte)'E', (byte)'T', (byte)' ', (byte)'/', (byte)'d', (byte)'a', (byte)'s', (byte)'h', (byte)'b', (byte)'o', (byte)'a', (byte)'r', (byte)'d', (byte)'/', (byte)'{', (byte)'x', (byte)'}' };
        /// <summary>
        /// The sub2
        /// </summary>
        public RequestProcessor Sub2 = new Sub2_Processor();
        /// <summary>
        /// The V u2
        /// </summary>
        public byte[] VU2 = new byte[] { (byte)'G', (byte)'E', (byte)'T', (byte)' ', (byte)'/', (byte)'p', (byte)'l', (byte)'a', (byte)'y', (byte)'e', (byte)'r', (byte)'s', (byte)'?', (byte)'f', (byte)'=', (byte)'{', (byte)'x', (byte)'}' };
        /// <summary>
        /// The sub3
        /// </summary>
        public RequestProcessor Sub3 = new Sub3_Processor();
        /// <summary>
        /// The V u3
        /// </summary>
        public byte[] VU3 = new byte[] { (byte)'P', (byte)'U', (byte)'T', (byte)' ', (byte)'/', (byte)'p', (byte)'l', (byte)'a', (byte)'y', (byte)'e', (byte)'r', (byte)'s', (byte)'/', (byte)'{', (byte)'x', (byte)'}' };
        /// <summary>
        /// The sub4
        /// </summary>
        public RequestProcessor Sub4 = new Sub4_Processor();
        /// <summary>
        /// The V u4
        /// </summary>
        public byte[] VU4 = new byte[] { (byte)'P', (byte)'O', (byte)'S', (byte)'T', (byte)' ', (byte)'/', (byte)'t', (byte)'r', (byte)'a', (byte)'n', (byte)'s', (byte)'f', (byte)'e', (byte)'r', (byte)'?', (byte)'f', (byte)'=', (byte)'{', (byte)'x', (byte)'}', (byte)'&', (byte)'t', (byte)'=', (byte)'{', (byte)'x', (byte)'}', (byte)'&', (byte)'x', (byte)'=', (byte)'{', (byte)'x', (byte)'}' };
        /// <summary>
        /// The sub5
        /// </summary>
        public RequestProcessor Sub5 = new Sub5_Processor();
        /// <summary>
        /// The V u5
        /// </summary>
        public byte[] VU5 = new byte[] { (byte)'P', (byte)'O', (byte)'S', (byte)'T', (byte)' ', (byte)'/', (byte)'d', (byte)'e', (byte)'p', (byte)'o', (byte)'s', (byte)'i', (byte)'t', (byte)'?', (byte)'a', (byte)'=', (byte)'{', (byte)'x', (byte)'}', (byte)'&', (byte)'x', (byte)'=', (byte)'{', (byte)'x', (byte)'}' };
        /// <summary>
        /// The sub6
        /// </summary>
        public RequestProcessor Sub6 = new Sub6_Processor();
        /// <summary>
        /// The V u6
        /// </summary>
        public byte[] VU6 = new byte[] { (byte)'D', (byte)'E', (byte)'L', (byte)'E', (byte)'T', (byte)'E', (byte)' ', (byte)'/', (byte)'a', (byte)'l', (byte)'l' };

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedRequestProcessor" /> class.
        /// </summary>
        public GeneratedRequestProcessor() {
            Registrations["GET /players/{x}"] = Sub0;
            Registrations["GET /dashboard/{x}"] = (SingleRequestProcessorBase)Sub1;
            Registrations["GET /players?f={x}"] = (SingleRequestProcessorBase)Sub2;
            Registrations["PUT /players/{x}"] = (SingleRequestProcessorBase)Sub3;
            Registrations["POST /transfer?f={x}&t={x}&x={x}"] = (SingleRequestProcessorBase)Sub4;
            Registrations["POST /deposit?a={x}&x={x}"] = (SingleRequestProcessorBase)Sub5;
            Registrations["DELETE /all"] = (SingleRequestProcessorBase)Sub6;
        }

        /// <summary>
        /// Processes the specified fragment.
        /// </summary>
        /// <param name="fragment">The fragment.</param>
        /// <param name="fragmentOffset">The fragment offset.</param>
        /// <param name="invoke">if set to <c>true</c> [invoke].</param>
        /// <param name="request">The request.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="resource">The resource.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Process(byte[] fragment, int fragmentOffset, bool invoke, Starcounter.HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
            switch (fragment[fragmentOffset]) {
                case (byte)'G':
                    fragmentOffset += 5;
                    switch (fragment[fragmentOffset]) {
                        case (byte)'p':
                            fragmentOffset += 7;
                            switch (fragment[fragmentOffset]) {
                                case (byte)'/':
                                    fragmentOffset += 0;
                                    if (Sub0.Process(fragment, fragmentOffset, invoke, request, out handler, out resource))
                                        return true;
                                    break;
                                case (byte)'?':
                                    fragmentOffset += 0;
                                    if (Sub2.Process(fragment, fragmentOffset, invoke, request, out handler, out resource))
                                        return true;
                                    break;
                            }
                            break;
                        case (byte)'d':
                            fragmentOffset += 0;
                            if (Sub1.Process(fragment, fragmentOffset, invoke, request, out handler, out resource))
                                return true;
                            break;
                    }
                    break;
                case (byte)'P':
                    fragmentOffset += 1;
                    switch (fragment[fragmentOffset]) {
                        case (byte)'U':
                            fragmentOffset += 0;
                            if (Sub3.Process(fragment, fragmentOffset, invoke, request, out handler, out resource))
                                return true;
                            break;
                        case (byte)'O':
                            fragmentOffset += 5;
                            switch (fragment[fragmentOffset]) {
                                case (byte)'t':
                                    fragmentOffset += 0;
                                    if (Sub4.Process(fragment, fragmentOffset, invoke, request, out handler, out resource))
                                        return true;
                                    break;
                                case (byte)'d':
                                    fragmentOffset += 0;
                                    if (Sub5.Process(fragment, fragmentOffset, invoke, request, out handler, out resource))
                                        return true;
                                    break;
                            }
                            break;
                    }
                    break;
                case (byte)'D':
                    if (Sub6.Process(fragment, fragmentOffset, invoke, request, out handler, out resource))
                        return true;
                    break;
            }
            handler = null;
            resource = null;
            return false;
        }
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The args.</param>
        public static void Main(string[] args) { // TODO! Remove! Compile as class library
        }
    }

    // Parser for "GET /players/{playerId}"
    /// <summary>
    /// Class Sub0_Processor
    /// </summary>
    public class Sub0_Processor : SingleRequestProcessor<int> {
        //        private static readonly byte[] prefix = new byte[] { (byte)'G', (byte)'E', (byte)'T', (byte)' ', (byte)'/', (byte)'p', (byte)'l', (byte)'a', (byte)'y', (byte)'e', (byte)'r', (byte)'s' , (byte)'/' };
        /// <summary>
        /// Processes the specified fragment.
        /// </summary>
        /// <param name="fragment">The fragment.</param>
        /// <param name="fragmentOffset">The fragment offset.</param>
        /// <param name="invoke">if set to <c>true</c> [invoke].</param>
        /// <param name="request">The request.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="resource">The resource.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Process(byte[] fragment, int fragmentOffset, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
            //            if (!BitsAndBytes.MemCompare(fragment,prefix,fragmentOffset,0,prefix.Length)) {
            //                resource =  null;
            //                handler =  null;
            //                return false;
            //            }
            int i = (int)Utf8Helper.IntFastParseFromAscii(fragment, 13u, fragment.Length - 13);
            if (!invoke)
                resource = null;
            else
                resource = Code.Invoke(i);
            handler = this;
            return true;
        }
    }

    // GET /dashboard/{playerId}
    /// <summary>
    /// Class Sub1_Processor
    /// </summary>
    public class Sub1_Processor : SingleRequestProcessor<int> {
        /// <summary>
        /// Processes the specified fragment.
        /// </summary>
        /// <param name="fragment">The fragment.</param>
        /// <param name="fragmentOffset">The fragment offset.</param>
        /// <param name="invoke">if set to <c>true</c> [invoke].</param>
        /// <param name="request">The request.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="resource">The resource.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Process(byte[] fragment, int fragmentOffset, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
            // GET /dashboard/123
            int i = (int)Utf8Helper.IntFastParseFromAscii(fragment, 15u, fragment.Length - 15);
            if (invoke)
                resource = Code.Invoke(i);
            else
                resource = null;
            handler = this;
            return true;
        }
    }

    /// <summary>
    /// Class Sub2_Processor
    /// </summary>
    public class Sub2_Processor : SingleRequestProcessor<string> {
        //        private static byte[] prefix = new byte[] { (byte)'G', (byte)'E', (byte)'T', (byte)' ', (byte)'/', (byte)'p', (byte)'l', (byte)'a', (byte)'y', (byte)'e', (byte)'r', (byte)'s' , (byte)'?', (byte)'f', (byte)'=' };

        /// <summary>
        /// Processes the specified fragment.
        /// </summary>
        /// <param name="fragment">The fragment.</param>
        /// <param name="fragmentOffset">The fragment offset.</param>
        /// <param name="invoke">if set to <c>true</c> [invoke].</param>
        /// <param name="request">The request.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="resource">The resource.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Process(byte[] fragment, int fragmentOffset, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
            //            if (!BitsAndBytes.MemCompare(fragment,prefix,0,0,prefix.Length)) {
            //               resource =  null;
            //               handler =  null;
            //               return false;
            //            }
            var str = Encoding.UTF8.GetString(fragment, 15, fragment.Length - 15);
            if (!invoke)
                resource = null;
            else
                resource = Code.Invoke(str);
            handler = this;
            return true;
        }
    }

    // GET /players/{playerId}
    /// <summary>
    /// Class Sub3_Processor
    /// </summary>
    public class Sub3_Processor : SingleRequestProcessor<int, Starcounter.HttpRequest> {
        /// <summary>
        /// Processes the specified fragment.
        /// </summary>
        /// <param name="fragment">The fragment.</param>
        /// <param name="fragmentOffset">The fragment offset.</param>
        /// <param name="invoke">if set to <c>true</c> [invoke].</param>
        /// <param name="request">The request.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="resource">The resource.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Process(byte[] fragment,
                    int fragmentOffset,
                    bool invoke,
                    HttpRequest request,
                    out SingleRequestProcessorBase handler,
                    out object resource) {
            int i = (int)Utf8Helper.IntFastParseFromAscii(fragment, 13u, fragment.Length - 13);
            if (invoke)
                resource = Code.Invoke(i, request);
            else
                resource = null;
            handler = this;
            return true;
        }
    }

    // POST /transfer?f={fromAccountId}&t={toAccountId}&x={amount}
    /// <summary>
    /// Class Sub4_Processor
    /// </summary>
    public class Sub4_Processor : SingleRequestProcessor<int, int, int> {
        /// <summary>
        /// Processes the specified fragment.
        /// </summary>
        /// <param name="fragment">The fragment.</param>
        /// <param name="fragmentOffset">The fragment offset.</param>
        /// <param name="invoke">if set to <c>true</c> [invoke].</param>
        /// <param name="request">The request.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="resource">The resource.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Process(byte[] fragment, int fragmentOffset, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
            uint index1, index2;
            int val1, val2, val3;
            byte current;

            // TODO:
            index1 = 17;
            index2 = index1 + 1;
            current = fragment[index2];
            while (current != '&')
                current = fragment[++index2];
            val1 = (int)Utf8Helper.IntFastParseFromAscii(fragment, index1, index2 - index1);

            index1 = index2 + 3;
            index2 = index1 + 1;
            current = fragment[index2];
            while (current != '&')
                current = fragment[++index2];
            val2 = (int)Utf8Helper.IntFastParseFromAscii(fragment, index1, index2 - index1);

            index1 = index2 + 3;
            index2 = (uint)fragment.Length;
            val3 = (int)Utf8Helper.IntFastParseFromAscii(fragment, index1, index2 - index1);

            if (invoke)
                resource = Code.Invoke(val1, val2, val3);
            else
                resource = null;
            handler = this;
            return true;
        }
    }

    // POST /deposit?a={accountId}&x={amount}
    /// <summary>
    /// Class Sub5_Processor
    /// </summary>
    public class Sub5_Processor : SingleRequestProcessor<int, int> {
        /// <summary>
        /// Processes the specified fragment.
        /// </summary>
        /// <param name="fragment">The fragment.</param>
        /// <param name="fragmentOffset">The fragment offset.</param>
        /// <param name="invoke">if set to <c>true</c> [invoke].</param>
        /// <param name="request">The request.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="resource">The resource.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Process(byte[] fragment, int fragmentOffset, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {

            uint index1, index2;
            int val1, val2;
            byte current;

            index1 = 16;
            index2 = index1 + 1;
            current = fragment[index2];
            while (current != '&')
                current = fragment[++index2];
            val1 = (int)Utf8Helper.IntFastParseFromAscii(fragment, index1, index2 - index1);

            index1 = index2 + 3;
            index2 = (uint)fragment.Length;
            val2 = (int)Utf8Helper.IntFastParseFromAscii(fragment, index1, index2 - index1);

            if (invoke)
                resource = Code.Invoke(val1, val2);
            else
                resource = null;
            handler = this;
            return true;
        }
    }

    /// <summary>
    /// Class Sub6_Processor
    /// </summary>
    public class Sub6_Processor : SingleRequestProcessor {
        /// <summary>
        /// Processes the specified fragment.
        /// </summary>
        /// <param name="fragment">The fragment.</param>
        /// <param name="fragmentOffset">The fragment offset.</param>
        /// <param name="invoke">if set to <c>true</c> [invoke].</param>
        /// <param name="request">The request.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="resource">The resource.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Process(byte[] fragment, int fragmentOffset, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
            if (invoke)
                resource = Code.Invoke();
            else
                resource = null;
            handler = this;
            return true;
        }
    }
}

