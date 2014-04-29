using System;
using Codeplex.Data;
using System.Net;
using StarcounterApplicationWebSocket.VersionHandler;
using System.IO;
using System.Collections.Generic;
using Starcounter.Applications.UsageTracker.VersionHandling.Model;
using System.Net.Mail;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using System.Security.Cryptography;

namespace Starcounter.Applications.UsageTrackerApp.API.Versions {
    internal class Login {

        public static void BootStrap(ushort port) {

            Handle.POST(port, "/login", (Request request) => {
                try {
                    IPAddress clientIP = request.ClientIpAddress;
                    String content = request.Body;

                    dynamic incomingJson = DynamicJson.Parse(content);
                    bool bValid = incomingJson.IsDefined("name") && incomingJson.IsDefined("password");
                    if (bValid == false) {
                        throw new FormatException("Invalid content");
                    }

                    string name = incomingJson.name;
                    string password = incomingJson.password;


                    bool isAuthorized = TryAuthorize(name, password);


                    if (isAuthorized) {
                        LogWriter.WriteLine(string.Format("NOTICE: User logged in {0}", clientIP.ToString()));

                        Response response = new Response();
                        AuthorizeSession(ref response);
                        response.StatusCode = (ushort)System.Net.HttpStatusCode.NoContent;
                        response["Location"] = "/api/versions"; // request["Referer"];
                        return response;


                    }
                    else {
                        LogWriter.WriteLine(string.Format("NOTICE: Wrong login attempt from {0} ({1} / {2})", clientIP.ToString(), name, password));
                        return new Response() { StatusCode = (ushort)HttpStatusCode.Unauthorized };
                    }
                }
                catch (Exception e) {
                    return Utils.CreateErrorResponse(e);
                }

            });


        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private static bool TryAuthorize(string name, string password) {

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(password)) return false;

            try {

                VersionHandlerSettings settings = VersionHandlerSettings.GetSettings();

                Logins logins = LoadLogins(settings.LoginsFile);

                foreach (var item in logins.Items) {
                    if (item.name == name && item.password == password) {
                        return true;
                    }
                }

            }
            catch (Exception e) {
                LogWriter.WriteLine(string.Format("ERROR: Failed to login user. {0}", e.Message));
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static Logins LoadLogins(string file) {

            try {
                string content = System.IO.File.ReadAllText(file);
                Logins logins = new Logins();
                logins.PopulateFromJson(content);
                return logins;
            }
            catch (Exception e) {
                throw new Exception(string.Format("Failed to read logins {0}. {1}", file, e.Message));
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="cookies"></param>
        /// <returns></returns>
        internal static bool IsAuthorized(List<string> cookies) {

            try {
                foreach (string cookie in cookies) {

                    if (cookie.StartsWith("scauthorized=")) {

                        string cookieDate = Login.decode(cookie.Substring(13));

                        DateTime authorizedAt = DateTime.ParseExact(cookieDate, "yyyy-MM-dd HH:mm:dd", null);

                        // Cookie 5min Timeout (TODO: Use Set-Cookie header timeout if we need a timeout
                        if ((DateTime.UtcNow - authorizedAt).TotalMinutes > 5) {
                            return false;
                        }
                        return true;
                    }
                }
            }
            catch (Exception) { }
            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        internal static void AuthorizeSession(ref Response response) {

            string date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:dd");
            string coded = encode(date);

            response.StatusCode = (ushort)HttpStatusCode.OK;
            response["Set-Cookie"] = "scauthorized=" + coded;
        
        }


        private static string encode(string text) {
            byte[] mybyte = System.Text.Encoding.UTF8.GetBytes(text);
            string returntext = System.Convert.ToBase64String(mybyte);
            return returntext;
        }

        private static string decode(string text) {
            byte[] mybyte = System.Convert.FromBase64String(text);
            string returntext = System.Text.Encoding.UTF8.GetString(mybyte);
            return returntext;
        }
    }
}
