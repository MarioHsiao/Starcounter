﻿
using Starcounter.Advanced;
using Starcounter.Internal;
using System;

namespace Starcounter.Rest.ExtensionMethods {
    /// <summary>
    /// Provides a set of extension methods for the <see cref="Response"/>
    /// class.
    /// </summary>
    public static class ResponseExtensions {
        static ResponseExtensions() {
            OnUnexpectedResponse = DefaultErrorHandler;
        }

        /// <summary>
        /// The handler invoked when any of the FailIf* methods are
        /// used and the given response fails to meet the expectations.
        /// </summary>
        [ThreadStatic]
        public static Action<Response> OnUnexpectedResponse;

        public static bool IsSuccessOr(this Response response, params int[] codes) {
            if (response.IsSuccessStatusCode)
                return true;

            foreach (var code in codes) {
                if (code == response.StatusCode)
                    return true;
            }

            return false;
        }

        public static bool IsAnyOf(this Response response, params int[] codes) {
            foreach (var code in codes) {
                if (code == response.StatusCode)
                    return true;
            }

            return false;
        }

        public static int FailIfNotSuccess(this Response response) {
            return FailIfNotSuccessOr(response);
        }

        public static int FailIfNotSuccessOr(this Response response, params int[] codes) {
            var pass = response.IsSuccessOr(codes);
            if (!pass) {
                InvokeErrorHandler(response);
            }
            return response.StatusCode;
        }

        public static int FailIfNotIsAnyOf(this Response response, params int[] codes) {
            var pass = response.IsAnyOf(codes);
            if (!pass) {
                InvokeErrorHandler(response);
            }
            return response.StatusCode;
        }

        static void InvokeErrorHandler(Response response) {
            var handler = OnUnexpectedResponse ?? DefaultErrorHandler;
            handler(response);
        }

        static void DefaultErrorHandler(Response response) {
            throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, response.ToString());
        }
    }
}
