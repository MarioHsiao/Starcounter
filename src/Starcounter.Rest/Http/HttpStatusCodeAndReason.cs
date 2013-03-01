
using System;
using System.Net;

namespace Starcounter.Internal.REST {

    /// <summary>
    /// Provides functionality to allow simple usage and validation of
    /// HTTP/1.1 response status codes and reasons, as defined in section
    /// <see href="http://www.w3.org/Protocols/rfc2616/rfc2616-sec6.html#sec6.1.1">
    /// 6.1.1</see> of said HTTP specification (RFC2616).
    /// </summary>
    public sealed class HttpStatusCodeAndReason {

        /// <summary>
        /// Reason phrase used if no reason was given.
        /// </summary>
        public const string ReasonNotAvailable = "N/A";

        /// <summary>
        /// Gets the response status code.
        /// </summary>
        public readonly int StatusCode;

        /// <summary>
        /// Gets the response reason phrase.
        /// </summary>
        public readonly string ReasonPhrase = ReasonNotAvailable;

        /// <summary>
        /// Gets the recommended reason phrase of the response status code
        /// part of the HTTP/1.1 specification.
        /// </summary>
        /// <param name="code">The response status code.</param>
        /// <returns>The recommended reason phrase for the given code.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when
        /// either the given code is either outside the range of given
        /// status codes (including extension codes), i.e. 0-999; or
        /// when the given code is not one of the codes defined in the
        /// specification.</exception>
        public static string GetRecommendedHttp11ReasonPhrase(int code) {
            return GetRecommendedHttp11ReasonPhrase((HttpStatusCode)code);
        }

        /// <summary>
        /// Gets the recommended reason phrase of the response status code
        /// part of the HTTP/1.1 specification.
        /// </summary>
        /// <param name="code">The response status code, given as a
        /// <see cref="HttpStatusCode"/> instance.</param>
        /// <returns>The recommended reason phrase for the given code.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when
        /// either the given code is either outside the range of given
        /// status codes (including extension codes), i.e. 0-999; or
        /// when the given code is not one of the codes defined in the
        /// specification.</exception>
        public static string GetRecommendedHttp11ReasonPhrase(HttpStatusCode code) {
            // Note: we are a bit sloppy when translating to the reason codes,
            // using the named variables of the given enum. To be fully correct
            // and compatible with the specification, we should split the phrase
            // up into multi-part tokens, separated by spaces, on each camel-case
            // occurance.
            var phrase = Enum.GetName(typeof(HttpStatusCode), code);
            if (phrase == null) {
                RaiseIfInvalidHTTP11CodeRange(code);
                throw new ArgumentOutOfRangeException("code");
            }
            return phrase;
        }

        /// <summary>
        /// Gets the recommended reason phrase of the response status code
        /// part of the HTTP/1.1 specification.
        /// </summary>
        /// <param name="code">The response status code.</param>
        /// <param name="phrase">When this method returns, contains the value
        /// associated with the given code, if the code is recognized; otherwise,
        /// null.</param>
        /// <returns>True if the code was recognized as one of the standard
        /// codes; otherwise, false.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when
        /// the given code is outside the range of given status codes (including
        /// extension codes), i.e. 0-999.</exception>
        public static bool TryGetRecommendedHttp11ReasonPhrase(int code, out string phrase) {
            return TryGetRecommendedHttp11ReasonPhrase((HttpStatusCode)code, out phrase);
        }

        /// <summary>
        /// Gets the recommended reason phrase of the response status code
        /// part of the HTTP/1.1 specification.
        /// </summary>
        /// <param name="code">The response status code, given as a
        /// <see cref="HttpStatusCode"/> instance.</param>
        /// <param name="phrase">When this method returns, contains the value
        /// associated with the given code, if the code is recognized; otherwise,
        /// null.</param>
        /// <returns>True if the code was recognized as one of the standard
        /// codes; otherwise, false.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when
        /// the given code is outside the range of given status codes (including
        /// extension codes), i.e. 0-999.</exception>
        public static bool TryGetRecommendedHttp11ReasonPhrase(HttpStatusCode code, out string phrase) {
            // Note: we are a bit sloppy when translating to the reason codes,
            // using the named variables of the given enum. To be fully correct
            // and compatible with the specification, we should split the phrase
            // up into multi-part tokens, separated by spaces, on each camel-case
            // occurance.
            phrase = Enum.GetName(typeof(HttpStatusCode), code);
            if (phrase == null) {
                RaiseIfInvalidHTTP11CodeRange(code);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Formats a string confirming to the HTTP/1.1 response status line
        /// tokens 2 and 3 from the given code and reason.
        /// </summary>
        /// <param name="code">The status code.</param>
        /// <param name="reason">The reason phrase.</param>
        /// <returns>A string formatted as described in the summary.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when
        /// the given code is outside the range of given status codes (including
        /// extension codes), i.e. 0-999.</exception>
        public static string ToStatusLineFormat(int code, string reason) {
            RaiseIfInvalidHTTP11CodeRange(code);
            return ToStatusLineFormatNoValidate(code, reason);
        }

        /// <summary>
        /// Formats a string confirming to the HTTP/1.1 response status line
        /// tokens 2 and 3 from the given code and reason. This method does not
        /// check the validness of the specified code.
        /// </summary>
        /// <param name="code">The status code.</param>
        /// <param name="reason">The reason phrase.</param>
        /// <returns>A string formatted as described in the summary.</returns>
        public static string ToStatusLineFormatNoValidate(int code, string reason) {
            return string.Format("{0:000} {1}", code, reason).TrimEnd();
        }

        /// <summary>
        /// Initializes a new <see cref="HttpStatusCodeAndReason"/> with the
        /// given status code. The reason phrase is either set to the recommended
        /// for the given code according to the HTTP/1.1 specification, if the
        /// code is known by said specification. Or, if the code is an extension
        /// to the known codes, to the value of <see cref="ReasonNotAvailable"/>.
        /// </summary>
        /// <param name="code">The status code to use.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when
        /// the given code is outside the range of given status codes (including
        /// extension codes), i.e. 0-999.</exception>
        public HttpStatusCodeAndReason(int code) {
            string phrase;

            if (!TryGetRecommendedHttp11ReasonPhrase(code, out phrase)) {
                phrase = ReasonNotAvailable;
            }

            this.StatusCode = code;
            this.ReasonPhrase = phrase;
        }

        /// <summary>
        /// Initializes a new <see cref="HttpStatusCodeAndReason"/> with the
        /// given status code and the given reason phrase. This constructor
        /// leaves it up to the caller to combine any status code with a custom
        /// reason; recommended reasons of the specification is not considered.
        /// </summary>
        /// <param name="code">The status code to use.</param>
        /// <param name="customReason">The reason phrase to use.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when
        /// the given code is outside the range of given status codes (including
        /// extension codes), i.e. 0-999.</exception>
        public HttpStatusCodeAndReason(int code, string customReason) {
            RaiseIfInvalidHTTP11CodeRange(code);

            // We don't touch the reason unless it is null, in case we
            // use an empty string. We allow any reason mapped to any status code,
            // as specified legal in the HTTP 1.1 specification.

            this.StatusCode = code;
            this.ReasonPhrase = customReason ?? string.Empty;
        }

        /// <summary>
        /// Initializes a new <see cref="HttpStatusCodeAndReason"/> with the
        /// given status code. The reason phrase is either set to the recommended
        /// for the given code according to the HTTP/1.1 specification, if the
        /// code is known by said specification. Or, if the code is an extension
        /// to the known codes, to the value of <see cref="ReasonNotAvailable"/>.
        /// </summary>
        /// <param name="code">The status code to use.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when
        /// the given code is outside the range of given status codes (including
        /// extension codes), i.e. 0-999.</exception>
        public HttpStatusCodeAndReason(HttpStatusCode code) {
            string phrase;

            if (!TryGetRecommendedHttp11ReasonPhrase(code, out phrase)) {
                phrase = ReasonNotAvailable;
            }

            this.StatusCode = (int)code;
            this.ReasonPhrase = phrase;
        }

        /// <summary>
        /// Initializes a new <see cref="HttpStatusCodeAndReason"/> with the
        /// given status code and the given reason phrase. This constructor
        /// leaves it up to the caller to combine any status code with a custom
        /// reason; recommended reasons of the specification is not considered.
        /// </summary>
        /// <param name="code">The status code to use.</param>
        /// <param name="customReason">The reason phrase to use.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when
        /// the given code is outside the range of given status codes (including
        /// extension codes), i.e. 0-999.</exception>
        public HttpStatusCodeAndReason(HttpStatusCode code, string customReason) {
            RaiseIfInvalidHTTP11CodeRange(code);

            // We don't touch the reason unless it is null, in case we
            // use an empty string. We allow any reason mapped to any status code,
            // as specified legal in the HTTP 1.1 specification.

            this.StatusCode = (int)code;
            this.ReasonPhrase = customReason ?? string.Empty;
        }

        /// <summary>
        /// Compares the current instance with the one given for equality.
        /// Two instances are considered valid if the have the same status
        /// code.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>True if <paramref name="obj"/> is considered equal to
        /// the current instance; else false.</returns>
        public override bool Equals(object obj) {
            return base.Equals(obj as HttpStatusCodeAndReason);
        }

        /// <summary>
        /// Compares the current instance with the one given for equality.
        /// Two instances are considered valid if the have the same status
        /// code.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>True if <paramref name="obj"/> is considered equal to
        /// the current instance; else false.</returns>
        public bool Equals(HttpStatusCodeAndReason other) {
            if (other == null) return false;
            return this.StatusCode == other.StatusCode;
        }

        /// <inheritdoc />
        public override int GetHashCode() {
            return this.StatusCode.GetHashCode();
        }

        /// <summary>
        /// Returns the string representation of the current instance, in
        /// a format compliant with the 6.1 and 6.1.1 on RFC2616, HTTP/1.1.
        /// </summary>
        /// <returns>A string representation of the current instance.</returns>
        public override string ToString() {
            return ToStatusLineFormatNoValidate(this.StatusCode, this.ReasonPhrase);
        }

        static void RaiseIfInvalidHTTP11CodeRange(HttpStatusCode code) {
            RaiseIfInvalidHTTP11CodeRange((int)code);
        }

        static void RaiseIfInvalidHTTP11CodeRange(int code) {
            if (code < 0 || code > 999)
                CreateAndRaiseInvalidRangeForCodeParameterException();

        }

        static void CreateAndRaiseInvalidRangeForCodeParameterException() {
            // Create an exception and link to the corresponding section in
            // the HTTP 1.1 specification.
            throw new ArgumentOutOfRangeException(
                "code",
                "HTTP/1.1 allows status codes in the range 0-999 only") {
                    HelpLink = "http://www.w3.org/Protocols/rfc2616/rfc2616-sec6.html#sec6.1.1"
                };
        }
    }
}
