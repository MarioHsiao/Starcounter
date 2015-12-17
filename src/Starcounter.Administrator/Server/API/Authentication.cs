using Starcounter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.API {
    public class Authentication {

        internal static string StarcounterServerApiRealm = "StarcounterServerApi";

        /// <summary>
        /// Authenticate request
        /// Uses Basic Authorization headers 
        /// </summary>
        /// <param name="request">Incoming request</param>
        /// <param name="response">alternative response or null if return value is true</param>
        /// <returns>True if request is authenticated otherwise false</returns>
        static public bool Authenticate(RestSettings settings, Request request, out Response response) {

            response = null;

            try {

                if (settings.Enabled) {

                    if (string.IsNullOrEmpty(settings.Username) && string.IsNullOrEmpty(settings.Password)) {
                        return true;
                    }

                    string authorizationHeader = request["Authorization"];

                    if (!string.IsNullOrEmpty(authorizationHeader)) {

                        string[] credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authorizationHeader.Substring(6))).Split(':');
                        if (credentials.Length != 2) {
                            throw new ArgumentException("Invalid authorization request header.");
                        }

                        string userName = credentials[0];
                        string password = credentials[1];

                        if (userName == settings.Username && password == settings.Password) {
                            return true;
                        }
                    }
                }

                response = new Response();
                response.Body = "Unauthorized.";
                response.StatusCode = (ushort)System.Net.HttpStatusCode.Unauthorized;
                response["WWW-Authenticate"] = "Basic realm=\"" + StarcounterServerApiRealm + "\"";
                //                response["locacion"] = "/index.html#/login/";
                return false;
            }
            catch (Exception e) {

                response = new Response() { StatusCode = (ushort)System.Net.HttpStatusCode.BadRequest, Body = e.Message };
                return false;
            }
        }
    }
}
