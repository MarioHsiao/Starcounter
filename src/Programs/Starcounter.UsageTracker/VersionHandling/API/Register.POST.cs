using System;
using Codeplex.Data;
using System.Net;
using StarcounterApplicationWebSocket.VersionHandler;
using System.IO;
using System.Collections.Generic;
using Starcounter.Applications.UsageTracker.VersionHandling.Model;
using System.Net.Mail;
using StarcounterApplicationWebSocket.VersionHandler.Model;

namespace Starcounter.Applications.UsageTrackerApp.API.Versions {
    internal class Register {

        public static void BootStrap(ushort port) {

            Handle.POST(port, "/register", (Request request) => {

                try {
                    IPAddress clientIP = request.ClientIpAddress;
                    String content = request.Body;

                    dynamic incomingJson = DynamicJson.Parse(content);
                    bool bValid = incomingJson.IsDefined("useremail");
                    if (bValid == false) {
                        throw new FormatException("Invalid content format ");
                    }

                    LogWriter.WriteLine(string.Format("NOTICE: User registration [{0}] from ({1})", incomingJson.useremail, clientIP.ToString()));

                    Db.Transaction(() => {
                        RegisteredUser user = new RegisteredUser();
                        user.RegistredDate = DateTime.UtcNow;
                        user.IPAdress = clientIP.ToString();
                        user.Email = incomingJson.useremail;

                        sendEmailEvent(user);
                    });

                    Response response = new Response() {
                        StatusCode = (ushort)HttpStatusCode.NoContent
                    };

                    response["Access-Control-Allow-Origin"] = "*";

                    // TODO: Not sure if this is needed
                    response["Access-Control-Allow-Headers"] = "Origin, X-Requested-With, Content-Type, Accept";

                    return response;

                }
                catch (Exception e) {
                    return Utils.CreateErrorResponse(e);
                }

            });

            Handle.GET(port, "/register", (Request request) => {

                try {

                    Response response = new Response() {
                        StatusCode = (ushort)HttpStatusCode.OK,
                    };

                    response["Access-Control-Allow-Origin"] = "*";
                    response["Access-Control-Allow-Headers"] = "Origin, X-Requested-With, Content-Type, Accept";

                    return response;
                }
                catch (Exception e) {
                    return Utils.CreateErrorResponse(e);
                }

            });

            Handle.CUSTOM(port, "OPTIONS /register", (Request request) => {

                try {

                    Response response = new Response() {
                        StatusCode = (ushort)HttpStatusCode.OK,
                    };

                    response["Access-Control-Allow-Origin"] = "*";
                    response["Access-Control-Allow-Headers"] = "Origin, X-Requested-With, Content-Type, Accept";

                    return response;
                }
                catch (Exception e) {
                    return Utils.CreateErrorResponse(e);
                }

            });
        }


        /// <summary>
        /// Send email to starcounter.com about the user registration
        /// </summary>
        /// <param name="user">Registration user</param>
        public static void sendEmailEvent(RegisteredUser user) {


            try {

                VersionHandlerSettings settings = VersionHandlerSettings.GetSettings();

                EmailSettings emailSettings = ReadEmailConfiguration(settings.EmailSettingsFile);
                MailAddress fromAddress = new MailAddress(emailSettings.from, "Starcounter Registration Form");
                MailAddress toAddress = new MailAddress(emailSettings.to);

                const string subject = "EVENT: Starcounter User Registration";

                string body = string.Format(
                    "Date(UTC): {0}" + Environment.NewLine +
                    "IP: {1}" + Environment.NewLine +
                    "EMAIL: {2}", user.RegistredDate, user.IPAdress, user.Email);

                var smtp = new SmtpClient {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, emailSettings.password)
                };
                using (var message = new MailMessage(fromAddress, toAddress) {
                    Subject = subject,
                    Body = body
                }) {
                    smtp.Send(message);
                }
            }
            catch (Exception e) {
                LogWriter.WriteLine(string.Format("ERROR: Failed to send registration email event. {0}", e.Message));
            }
        }


        /// <summary>
        /// Read email settings from a json file
        /// </summary>
        /// <param name="file">File containing the emailsettings in json format</param>
        /// <returns>EmailSettings</returns>
        public static EmailSettings ReadEmailConfiguration(string file) {

            try {
                string content = System.IO.File.ReadAllText(file);
                EmailSettings emailSettings = new EmailSettings();
                emailSettings.PopulateFromJson(content);
                return emailSettings;
            }
            catch (Exception e) {
                throw new Exception(string.Format("Failed to read email settings {0}. {1}", file, e.Message));
            }
        }


    }
}
