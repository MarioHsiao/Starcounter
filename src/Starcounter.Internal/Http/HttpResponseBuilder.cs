//// ***********************************************************************
//// <copyright file="HttpResponseBuilder.cs" company="Starcounter AB">
////     Copyright (c) Starcounter AB.  All rights reserved.
//// </copyright>
//// ***********************************************************************

using System.Collections.Generic;
using System.Text;
using Starcounter.Internal.REST;
using Constants = Starcounter.Internal.StarcounterConstants.NetworkConstants;

namespace Starcounter.Internal.Web {
	/// <summary>
	/// Class HttpResponseBuilder
	/// </summary>
	public class HttpResponseBuilder {
		/// <summary>
		/// Expose a set of factory methods that are slowish prototypes of
		/// HTTP response creation functionality we should consider making
		/// an effort to support as fast.
		/// </summary>
		public static class Slow {
			// TODO: 
			// This class should be removed and Response should be used directly instead so we 
			// have one place where we can optimize the creation and writing of the response, which
			// is response.ConstructFromFields.

			/// <summary>
			/// Build a HTTP/1.1 response using a given status code, a set
			/// of headers and content given in the form of a <see cref="string"/>.
			/// </summary>
			/// <remarks>
			/// Clients should not specify the entity header Content-Type
			/// as part of the <paramref name="headers"/> set. This is set by
			/// the method, and based on the <paramref name="contentEncoding"/>
			/// and <paramref name="contentType"/> parameters respectively.
			/// Specifying a Content-Type as part of the headers collection
			/// yields in an undefined behaviour.
			/// Also a cache directive with no-cache is automatically added to the header
			/// in the current implementation so adding any kind of cache directive in the
			/// header section will yield undefined behaviour.
			/// </remarks>
			/// <param name="code">The status code.</param>
			/// <param name="headers">General-, response and/or entity headers.</param>
			/// <param name="content">The content to include in the response, forming
			/// it's body. If <see langword="null"/> is specified, the response will
			/// be created with no entity/content.</param>
			/// <param name="contentEncoding">Type of encoding to use when turning
			/// the given content to an array of bytes. Ignored if <paramref name="content"/>
			/// is <see langword="null"/>.</param>
			/// <param name="contentType">The content type to indicating the media
			/// type of the given content. If not given, this method assumes
			/// "application/json" as the default.</param>
			/// <returns>A byte array that is the HTTP response message before any
			/// transfer encoding is applied (i.e. it's uncompressed).</returns>
			public static byte[] FromStatusHeadersAndStringContent(
				int code,
				Dictionary<string, string> headers,
				string content,
				Encoding contentEncoding = null,
				string contentType = "application/json"
				) {
				string reason;
				string msgHeader;
				string header;
				bool hasContent;
				byte[] message;
				byte[] entityBody;
				int bodyLength;

				hasContent = !string.IsNullOrEmpty(content);
				bodyLength = 0;
				entityBody = null;
				if (!HttpStatusCodeAndReason.TryGetRecommendedHttp11ReasonPhrase(code, out reason)) {
					reason = HttpStatusCodeAndReason.ReasonNotAvailable;
				}
				msgHeader = "HTTP/1.1 " + HttpStatusCodeAndReason.ToStatusLineFormatNoValidate(code, reason);
				msgHeader += Constants.CRLF;

				if (headers != null) {
					foreach (var pair in headers) {
						header = string.Concat(pair.Key, ":", pair.Value, Constants.CRLF);
						msgHeader += header;
					}
				}
				msgHeader += "Cache-Control: no-cache" + Constants.CRLF;

				if (hasContent) {
					contentEncoding = contentEncoding ?? Encoding.UTF8;
					entityBody = contentEncoding.GetBytes(content);
					bodyLength = entityBody.Length;
					header = string.Concat("Content-Type: ", contentType, ";", contentEncoding.WebName, Constants.CRLF);
					msgHeader += header;
					header = string.Concat("Content-Length: ", bodyLength, Constants.CRLF);
					msgHeader += header;
				} else {
					// Content-Length must always be included!
					header = string.Concat("Content-Length: 0", Constants.CRLF);
					msgHeader += header;
				}

				msgHeader += Constants.CRLF;
				var msgHeaderBytes = Encoding.UTF8.GetBytes(msgHeader);

				message = new byte[msgHeaderBytes.Length + bodyLength];
				msgHeaderBytes.CopyTo(message, 0);
				if (hasContent) {
					entityBody.CopyTo(message, msgHeaderBytes.Length);
				}

				return message;
			}
		}
	}
}

