using System;
using Starcounter;

namespace StarcounterAdministrator
{
    public class UriRegistrator : App
    {
        public static void Main(String[] args)
        {
            Bootstrapper.Bootstrap();

            GET("/", () =>
            {
                return Root.CreateFakeRootPage();
            });

            GET("/test", () =>
            {
                return ScAdmin.CreateAdminPage();
            });

            GET("/about", () =>
            {
                return "Starcounter Administrator Application";
            });
        }
    }
}
