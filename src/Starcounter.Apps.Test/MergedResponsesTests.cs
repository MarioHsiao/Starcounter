using NUnit.Framework;
using Starcounter.Advanced;
using Starcounter.Internal.Web;
using Starcounter.Rest;
using Starcounter.Templates;
using System;
using System.Collections.Generic;
using System.Text;
namespace Starcounter.Internal.Test
{
    /// <summary>
    /// Tests merged responses behavior.
    /// </summary>
    [TestFixture]
    public class MergedResponsesTests
    {
        [TestFixtureTearDown]
        public static void AfterTest(){
            UriHandlersManager.ResetUriHandlersManagers();
            Response.ResponsesMergerRoutine_ = null;
        }

        [Test]
        public void Test1()
        {
            Response.ResponsesMergerRoutine_ = (Request req, Response resp, List<Response> responses) => {

                StringBuilder sb = new StringBuilder();

                switch (req.PreferredMimeType) {
                    case MimeType.Unspecified:
                    case MimeType.Text_Html:
                    {
                        sb.Append(
@"
<!doctype html>
<head>
    <title>Launcher</title>
    <script src=""/vendor/json-patch-duplex.js""></script>
    <script src=""/vendor/puppet.js""></script>
    <script src=""/vendor/platform/platform.js""></script>

    <link rel=""import"" href=""/vendor/x-html.html"">

    <link rel=""stylesheet"" href=""/css/style.css"">
</head>

<body>
    <template id=""root"" bind>");

                        for (Int32 i = 0; i < responses.Count; i++)
                        {
                            sb.Append("<template bind=\"{{" + responses[i].AppName + "}}\">\n");
                            sb.Append(responses[i].Body);
                            sb.Append("</template>\n");
                        }

                        sb.Append(
@"
    </template>
    <script>
        var puppet = new Puppet(null, function (obj) {
            document.getElementById(""root"").model = obj;
        });
    </script>
</body>");
                        break;
                    }

                    case MimeType.Application_Json:
                    {
                        sb.Append("{");
                        Int32 n = responses.Count;
                        for (Int32 i = 0; i < n; i++)
                        {
                            sb.Append("\"" + responses[i].AppName + "\":");
                            sb.Append(responses[i].Body);
                            if (i < n - 1)
                                sb.Append(",");
                        }
                        sb.Append("}");

                        break;
                    }

                    default:
                        throw new ArgumentException("Request is of unsuitable MIME type to merge responses!");
                }

                Response mergedResp = new Response() {
                    Body = sb.ToString()
                };

                return mergedResp;
            };

            StarcounterEnvironment.AppName = "App1";

            Handle.GET("/apps", () =>
            {
                var eTemplate = new TObject();
                eTemplate.Add<TString>("firstName");
                eTemplate.Add<TString>("html");

                dynamic e = new Json();
                e.Template = eTemplate;
                e.firstName = "MyApp1!!!";
                e.html = "<article><input value=\"{{firstName}}!!\"></article>";

                return e;
            });

            StarcounterEnvironment.AppName = "App2";

            Handle.GET("/apps", () =>
            {
                var sTemplate = new TObject();
                sTemplate.Add<TString>("title");
                sTemplate.Add<TString>("html");

                dynamic s = new Json();
                s.Template = sTemplate;
                s.title = "MyApp2!!!";
                s.html = "<div><button>{{title}}</button><div>";

                return s;
            });

            Response mergedResponse;
            X.GET("/apps", out mergedResponse);
        }
    }
}