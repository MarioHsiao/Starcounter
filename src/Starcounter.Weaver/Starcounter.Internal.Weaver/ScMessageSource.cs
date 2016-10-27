// ***********************************************************************
// <copyright file="ScMessageSource.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Resources;
using PostSharp;
using PostSharp.Extensibility;
using System;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Defines a message source for Starcounter-related weaver messages.
    /// </summary>
    public class ScMessageSource : IMessageDispenser {
        /// <summary>
        /// The name of the Starcounter weaver message source.
        /// </summary>
        public const string WeaverMessageSource = "Starcounter.Weaver";

#if false
        /// <summary>
        /// A message source for Starcounter-specific stuff.
        /// </summary>
        /// <remarks>
        /// This source will be removed when the weaver has been rewritten.
        /// It is backed by a resource manager, where the new source (from
        /// PostSharp 2.x), is backed by our own error system.
        /// </remarks>
        public static readonly MessageSource Instance = new MessageSource(
            WeaverMessageSource,
            new ResourceManager("Weaver.Messages",
                                typeof(ScMessageSource).Assembly));
#endif

        /// <summary>
        /// Redirect the legacy "Instance" to the new "Source" until we've
        /// had time to fixup all weaver-related messages. This will work,
        /// but it will give just codes back, never proper error messages.
        /// </summary>
        public static MessageSource Instance {
            get {
                return ScMessageSource.Source;
            }
        }

        /// <summary>
        /// The new message source, replacing <see cref="Instance"/>, to be
        /// used from PostSharp 2.x.
        /// </summary>
        public static readonly MessageSource Source = new MessageSource(
            WeaverMessageSource,
            new ScMessageSource()
            );

        /// <summary>
        /// Legacy support for how we previously wrote errors (using
        /// Instance.Write).
        /// </summary>
        /// <param name="severity">The severity</param>
        /// <param name="id">An error id</param>
        /// <param name="parameters">Parameters</param>
        public static void Write(SeverityType severity, string id, object[] parameters) {
#pragma warning disable 618
            Instance.Write(severity, id, parameters);
#pragma warning restore 618
        }

        /// <summary>
        /// Utility method that writes a weaver error message based on a
        /// well-known Starcounter error code, using the new message source.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="errorCode"></param>
        public static void WriteError(MessageLocation location, uint errorCode) {
            InternalWriteMessage(location, SeverityType.Error, errorCode, null, null);
        }

        /// <summary>
        /// Utility method that writes a weaver error message based on a
        /// well-known Starcounter error code, using the new message source.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="errorCode"></param>
        /// <param name="postfix"></param>
        public static void WriteError(MessageLocation location, uint errorCode, string postfix) {
            InternalWriteMessage(location, SeverityType.Error, errorCode, postfix, null);
        }

        /// <summary>
        /// Utility method that writes a weaver error message based on a
        /// well-known Starcounter error code, using the new message source.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="errorCode"></param>
        /// <param name="postfix"></param>
        /// <param name="exception"></param>
        public static void WriteError(MessageLocation location, uint errorCode, string postfix, Exception exception) {
            InternalWriteMessage(location, SeverityType.Error, errorCode, postfix, exception);
        }

        /// <summary>
        /// Utility method that writes a weaver error message based on a
        /// well-known Starcounter error code, using the new message source.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="errorCode"></param>
        /// <param name="exception"></param>
        public static void WriteError(MessageLocation location, uint errorCode, Exception exception) {
            InternalWriteMessage(location, SeverityType.Error, errorCode, null, exception);
        }

        /// <summary>
        /// Utility method that writes a weaver error message based on a
        /// well-known Starcounter error code, using the new message source.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="errorCode"></param>
        /// <param name="postfix"></param>
        /// <param name="exception"></param>
        /// <param name="messageArguments"></param>
        public static void WriteError(
            MessageLocation location,
            uint errorCode,
            string postfix,
            Exception exception,
            params object[] messageArguments) {
            InternalWriteMessage(location, SeverityType.Error, errorCode, postfix, exception, messageArguments);
        }

        private static void InternalWriteMessage(
            MessageLocation location,
            SeverityType severity,
            uint errorCode,
            string postfix,
            Exception innerException,
            params object[] messageArguments) {
            Message message;
            ErrorMessage error;

            if (location == null)
                location = MessageLocation.Unknown;

            error = messageArguments == null || messageArguments.Length == 0
                ? ErrorCode.ToMessage(errorCode, postfix)
                : ErrorCode.ToMessageWithArguments(errorCode, postfix, messageArguments);

            // We construct the full and final message object
            // "by hand", not counting on the IMessageDispenser
            // implementation to do it for us, since it doesn't
            // support some features we rely on in our error
            // infrastructure, such as the notion of a postfix
            // message (created on the fly).

            message = new Message(
                location,
                SeverityType.Error,
                error.DecoratedCode,
                error.ToString(),
                error.Helplink,
                ScMessageSource.WeaverMessageSource,
                innerException
                );

            // Write the message to the source and ultimately to
            // the sink.

            ScMessageSource.Source.Write(message);
        }

        #region IMessageDispenser Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetMessage(string key) {
            // We currently really don't implement anything other than
            // error handling through the new message source (Source),
            // and we expect code to use one of the static WriteError
            // utility methods to write the error. Since WriteError will
            // create the full Message object, this method "should"
            // never be called, and hence we just return the key as the
            // message in case some code whould call it by accident.

            uint errorCode = WeaverUtilities.WeaverMessageToErrorCode(key);
            if (errorCode == 0) {
                
                // Test.
                // We should ultimately factor out all messages from the
                // embedded resource stream and utilize our own error system
                // (i.e. "Grey"), extending it with support for warnings and
                // information messages too.
                // TODO:
                //if (key.Equals("SCINF01")) {
                //    return "Analyzing the module {0}.";
                //}


                return key;
            }

            return ErrorCode.ToMessage(errorCode);
        }

        #endregion
    }
}