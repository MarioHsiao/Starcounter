using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Starcounter.Errors;
using System.Xml;
using System.IO;
using System.Net;
using System.Web;
using DotNetWikiBot;
using BuildSystemHelper;

namespace WikiErrorPageGenerator
{
    /// <summary>
    /// This program is used to read the ErrorCodes XML and then create wiki pages for each
    /// error to our public wiki using a specific page as a base at the internal wiki.
    /// </summary>
    class Program
    {
        private static Site wiki;
        private static Site internalWiki;
        private static Page template;

        /// <summary>
        /// The entry point for the application.
        /// In order to properly run requires set env vars:
        /// SC_UPDATE_ERROR_PAGES=true
        /// Configuration=Release
        /// SC_RELEASING_BUILD=True
        /// 
        /// The errorcodes.xml is located at Dev\Grey
        /// </summary>
        /// <param name="args">A list of command line arguments.</param>
        public static Int32 Main(string[] args)
        {
            // Catching all possible exceptions.
            try
            {
                // Printing tool welcome message.
                BuildSystem.PrintToolWelcome("Wiki Error Codes Updater");

                // Checking if same executable is already running.
                if (BuildSystem.IsSameExecutableRunning())
                    return 0;

                // Checking if its releasing build.
                if (!BuildSystem.IsReleasingBuild())
                {
                    Console.WriteLine("It is not a releasing build. Quiting.");
                    return 0;
                }

                // Checking if explicit error pages update is needed.
                String updateFlag = Environment.GetEnvironmentVariable("SC_UPDATE_ERROR_PAGES");

                // Checking if its a nightly build.
                if ((updateFlag == null) /*&& (!BuildSystem.NightlyBuild())*/)
                {
                    Console.WriteLine("No SC_UPDATE_ERROR_PAGES flag set. Quiting.");
                    return 0;
                }

                // Checking if arguments supplied.
                if (args.Length <= 0)
                {
                    Console.WriteLine("Please specify full path to errorcodes.xml as a first argument.");
                    return 1;
                }

                // Notification.
                Console.WriteLine("Updating Wiki Error Pages...");

                // Read the XML File
                ErrorFile errorFile = ErrorFileReader.ReadErrorCodes(File.Open(args[0], FileMode.Open, FileAccess.Read));

                // If the errorFile was not read correctly throw an exception
                if (errorFile == null)
                {
                    throw new InvalidOperationException("ErrorCodes file could not be opened.");
                }

                // Login to Public and Internal Wiki.
                Login();

                // Load the base page from internal wiki that we use as a template for all error pages.
                LoadTemplate();

                /* 
                 * For each error code we create the Wiki pages associated with this error.
                 * This includes: 
                 * 1) the main error page: SCERR<c>error.CodeWithFacility</c> which is the number of the error with the type,
                 * 2) the redirect from a page <c>error.CodeWithFacility</c> and,
                 * 3) the redirect from a page with the name of the error called <error.Name>
                 */
                foreach (var code in errorFile.ErrorCodes)
                {
                    CreateWikiPages(code);
                }

                // Notification.
                Console.WriteLine("Update of Wiki Error Pages has been completed successfully!");
            }
            catch (Exception generalException)
            {
                return BuildSystem.LogException(generalException);
            }

            return 0;
        }

        /// <summary>
        /// Login to both Wikis as a StarcounterBot.
        /// </summary>
        private static void Login()
        {
            String username = "StarcounterBot";
            String password = "#sb1301&34#";

            wiki = new Site("http://www.starcounter.com/wiki", username, password);
            internalWiki = new Site("http://www.starcounter.com/internal/wiki", username, password);

            if (wiki==null || internalWiki==null) 
            {
                throw new InvalidOperationException("Login process failed");
            }
        }

        /// <summary>
        /// Loads the base page from internal wiki that we use as a template for all error pages.
        /// </summary>
        private static void LoadTemplate()
        {
            template = new Page(internalWiki, "ErrorPageTemplateUsedByRobot");
            if (template == null)
            {
                throw new InvalidOperationException("Error getting template failed");
            }

            template.Load();
        }

        /// <summary>
        /// For each error code we create the Wiki pages associated with this error.
        /// This includes:
        /// 1) the main error page: SCERR<c>error.CodeWithFacility</c> which is the number of the error with the type,
        /// 2) the redirect from a page <c>error.CodeWithFacility</c> and,
        /// 3) the redirect from a page with the name of the error called <c>error.Name</c>
        /// </summary>
        /// <param name="code">The error code taken from the XML file</param>
        private static void CreateWikiPages(ErrorCode code)
        {
            string pageName = "SCERR" + code.CodeWithFacility;

            Page p = new Page(wiki, pageName);
            if (p == null)
            {
                throw new InvalidOperationException("Error getting template failed");
            }

            p.Load();

            /* If error page does not exist:
             * Replace the needed text from the base template and then create it
             */
            if (!p.Exists())
            {
                p.text = template.text;
                p.text = p.text.Replace("1234", code.CodeWithFacility.ToString());
                p.text = p.text.Replace("ScErrFictiveError", code.Name);
                p.text = p.text.Replace("An error has occured that is used to describe the structure of an error template page.", code.Description);
                p.text = p.text.Replace("[Insert %Summary text from '''Summary''' section above%]", code.Description);
                
                p.Save("comment: Automatic creation of Error pages", true);

                Console.WriteLine("--> Page {0} created succesfully!", pageName);
            }
            // If error page already exists do nothing, since we don't want the script to mess
            // with already created and maybe modified error pages.
            else
            {
                Console.WriteLine("--> Page {0} already exists. No actions made.", pageName);
            }

            CreateRedirect(code.CodeWithFacility.ToString(), pageName);
            CreateRedirect(code.Name, pageName);
        }

        /// <summary>
        /// We create redirects from one wiki page to another
        /// </summary>
        /// <param name="from">The page that will redirect to the destination</param>
        /// <param name="destination">The page that the redirect will reach</param>
        public static void CreateRedirect(string from, string destination)
        {
            Page rp = new Page(wiki, from);
            if (rp == null)
            {
                throw new InvalidOperationException("Error getting template failed");
            }
            rp.Load();
            if (!rp.Exists())
            {
                rp.text = "#REDIRECT [[" + destination + "]]";
                rp.Save("comment: Automatic creation of redirection to " + destination, true);
                Console.WriteLine("--> Redirect from page {0} to page {1} created succesfully!", from, destination);
            }
            else
            {
                Console.WriteLine("--> Redirect from page {0} to page {1} already exists! No actions made.", from, destination);
            }
        }

        /*
        static void Login()
        {
            String username = "Admin";
            String password = "#wisc1301&34#";

            ApiEdit editor = new ApiEdit("http://www.starcounter.com/wiki/");
            try
            {
                editor.Login(username, password);
                Console.WriteLine("Login Success, Username {0}", editor.User.Name);
            }
            catch (LoginException)
            {
                Console.WriteLine(" failed");
            }
        }
        */

        /*
        static void Login()
        {
            Dictionary<string, string> variable = new Dictionary<string, string>();

            // action=query & prop=info|revisions & intoken=edit & titles=Main%20Page
            variable.Add("format", "xml");
            variable.Add("action", "login");
            variable.Add("lgname", "Admin");
            variable.Add("lgpassword", "#wisc1301&34#");

            String postString = EncodeData(variable);

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] postBytes = ascii.GetBytes(postString);

            HttpWebRequest request;
            request = WebRequest.Create(@"http://www.starcounter.com/wiki/api.php") as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postBytes.Length;

            // add post data to request
            Stream postStream = request.GetRequestStream();
            postStream.Write(postBytes, 0, postBytes.Length);
            postStream.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader readStream = new StreamReader(responseStream, Encoding.UTF8);
            string result = readStream.ReadToEnd();
            Console.WriteLine(result);
        }
        */
        /*
        static String GetToken()
        {
            Dictionary<string, string> variable = new Dictionary<string, string>();

            // action=query & prop=info|revisions & intoken=edit & titles=Main%20Page
            variable.Add("format", "xml");
            variable.Add("action", "query");
            variable.Add("prop", "info|revisions");
            variable.Add("intoken", "edit");
            variable.Add("titles", "Main Page");

            String postString = EncodeData(variable);

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] postBytes = ascii.GetBytes(postString);

            HttpWebRequest request;
            request = WebRequest.Create(@"http://www.starcounter.com/wiki/api.php") as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postBytes.Length;

            // add post data to request
            Stream postStream = request.GetRequestStream();
            postStream.Write(postBytes, 0, postBytes.Length);
            postStream.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader readStream = new StreamReader(responseStream, Encoding.UTF8);
            string result = readStream.ReadToEnd();
            Console.WriteLine(result);
            String token = "";

            return token;
        }

        static String EncodeData(Dictionary<string, string> variable)
        {
            StringBuilder postString = new StringBuilder();
            bool first = true;
            foreach (KeyValuePair<string, string> pair in variable)
            {
                if (first)
                    first = false;
                else
                    postString.Append("&");

                postString.AppendFormat("{0}={1}", pair.Key, HttpUtility.UrlEncode(pair.Value));
            }

            return postString.ToString();
        }*/
    }
}
