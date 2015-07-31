using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal.Tests {
    [TestFixture]
    class UriMatcherGenBug {
        [Test]
        public static void TestUriMatcherGenBug() {

            Handle.PATCH("/__default/{?}", (Session p0) => { return 200; });

            Handle.GET("/__default/{?}", (Session p0) => { return 200; });

            Handle.GET("/__default/wsupgrade/{?}", (Session p0) => { return 200; });

            Handle.GET("/so2/{?}/{?}", (String p0, String p1) => { return 200; });

            Handle.GET("/db2/{?}/{?}", (String p0, String p1) => { return 200; });

            Handle.GET("/polyjuice/htmlmerger2?{?}", (String p0) => { return 200; });

            Handle.GET("/launcher/launchpad/polyjuice/htmlmerger?{?}", (String p0) => { return 200; });

            Handle.POST("/launcher/juicytilessetup?{?}", (String p0) => { return 200; });

            Handle.GET("/launcher/juicytilessetup?{?}", (String p0) => { return 200; });

            Handle.DELETE("/launcher/juicytilessetup?{?}", (String p0) => { return 200; });

            Handle.GET("/launcher/generatestyles/{?}", (String p0) => { return 200; });

            Handle.GET("/launcher", () => { return 200; });

            Handle.GET("/launcher/dashboard", () => { return 200; });

            Handle.GET("/launcher/search?query={?}", (String p0) => { return 200; });

            Handle.GET("/", () => { return 200; });

            Handle.GET("/useradmin/admin/settings", () => { return 200; });

            Handle.GET("/useradmin/accessdenied", () => { return 200; });

            Handle.GET("/useradmin/admin/createuser", () => { return 200; });

            Handle.GET("/useradmin/admin/users", () => { return 200; });

            Handle.GET("/useradmin/admin/users/{?}", (String p0) => { return 200; });

            Handle.GET("/useradmin/admin/_users/{?}", (String p0) => { return 200; });

            Handle.GET("/useradmin/user/resetpassword?{?}", (String p0) => { return 200; });

            Handle.GET("/useradmin/admin/createusergroup", () => { return 200; });

            Handle.GET("/useradmin/admin/usergroups", () => { return 200; });

            Handle.GET("/useradmin/admin/usergroups/{?}", (String p0) => { return 200; });

            Handle.GET("/useradmin", () => { return 200; });

            Handle.GET("/useradmin/app-name", () => { return 200; });

            Handle.GET("/useradmin/app-icon", () => { return 200; });

            Handle.GET("/useradmin/menu", () => { return 200; });

            Handle.GET("/useradmin/search/{?}", (String p0) => { return 200; });

            Handle.GET("/polyjuice/menu", () => { return 200; });

            Handle.GET("/polyjuice/app-name", () => { return 200; });

            Handle.GET("/polyjuice/app-icon", () => { return 200; });

            Handle.GET("/polyjuice/search?query={?}", (String p0) => { return 200; });

            Handle.GET("/signin/user", () => { return 200; });

            Handle.GET("/signin/signin/{?}/{?}", (String p0, String p1) => { return 200; });

            Handle.GET("/signin/signout", () => { return 200; });

            Handle.GET("/signin/signinuser", () => { return 200; });

            Handle.GET("/signin/signinuser?{?}", (String p0) => { return 200; });

            Handle.GET("/polyjuice/user", () => { return 200; });

            Handle.GET("/adfenix/import-shit", () => { return 200; });

            Handle.GET("/adfenix/index", () => { return 200; });

            Handle.GET("/adfenix/logout", () => { return 200; });

            Handle.GET("/adfenix/externallogin/{?}/{?}", (String p0, String p1) => { return 200; });

            Handle.GET("/adfenix/master", () => { return 200; });

            Handle.GET("/adfenix/publicationowner/any/view/all", () => { return 200; });

            Handle.GET("/adfenix/publicationowner/any/view/{?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/settings", () => { return 200; });

            Handle.GET("/adfenix/vertical/view/any", () => { return 200; });

            Handle.GET("/adfenix/city/view/all", () => { return 200; });

            Handle.GET("/adfenix/menu", () => { return 200; });

            Handle.GET("/adfenix", () => { return 200; });

            Handle.GET("/adfenix/teststartapps", () => { return 200; });

            Handle.GET("/adfenix/create_filebyid", () => { return 200; });

            Handle.GET("/adfenix/create_fblist", () => { return 200; });

            Handle.GET("/adfenix/add-admin-user-to-group-once", () => { return 200; });

            Handle.GET("/adfenix/testsegments", () => { return 200; });

            Handle.GET("/adfenix/testfortnox?url={?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/test-send-email", () => { return 200; });

            Handle.GET("/adfenix/check-realtors", () => { return 200; });

            Handle.GET("/adfenix/realtors-to-file", () => { return 200; });

            Handle.GET("/adfenix/sample-data-boligtipset", () => { return 200; });

            Handle.GET("/adfenix/delete-odd-stuff", () => { return 200; });

            Handle.GET("/adfenix/check-insta-example", () => { return 200; });

            Handle.GET("/adfenix/check-campaign-stats", () => { return 200; });

            Handle.GET("/adfenix/check-popularity?domain={?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/check-if-live", () => { return 200; });

            Handle.GET("/adfenix/add-advertiser", () => { return 200; });

            Handle.GET("/adfenix/sync-users", () => { return 200; });

            Handle.GET("/adfenix/update-index", () => { return 200; });

            Handle.GET("/adfenix/create-homebasedrule", () => { return 200; });

            Handle.GET("/adfenix/test-homebasedrule", () => { return 200; });

            Handle.GET("/adfenix/sample-data", () => { return 200; });

            Handle.GET("/adfenix/init-data", () => { return 200; });

            Handle.GET("/adfenix/sync-advertisers/{?}/{?}/{?}", (String p0, String p1, String p2) => { return 200; });

            Handle.GET("/adfenix/new-host-token/{?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/initiate-adfenix-interface/{?}/{?}", (String p0, String p1) => { return 200; });

            Handle.GET("/adfenix/api/simulator?{?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/api/tracker?{?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/api/network/{?}/{?}", (String p0, String p1) => { return 200; });

            Handle.PUT("/adfenix/add/keywordsegmentationrule", () => { return 200; });

            Handle.PUT("/adfenix/add/classifiedbroadsegmentationrule", () => { return 200; });

            Handle.POST("/adfenix/add/homesegmentationrule", () => { return 200; });

            Handle.GET("/adfenix/add/homesegmentationrule/{?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/api/addcampaign?{?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/api/add/campaign?{?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/campaign/order/{?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/campaign/listingbased/create/{?}/{?}", (String p0, String p1) => { return 200; });

            Handle.GET("/adfenix/campaign/broadbased/create/new", () => { return 200; });

            Handle.GET("/adfenix/campaign/vehiclebased/create/new", () => { return 200; });

            Handle.GET("/adfenix/campaign/homebased/create/new", () => { return 200; });

            Handle.GET("/adfenix/campaign/{?}/view/all", (String p0) => { return 200; });

            Handle.GET("/adfenix/campaign/facebook/edit/{?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/campaign/facebook/create/new", () => { return 200; });

            Handle.GET("/adfenix/campaign/facebook/approve/yes/{?}", (Int32 p0) => { return 200; });

            Handle.GET("/adfenix/campaign/facebook/approve/no/{?}", (Int32 p0) => { return 200; });

            Handle.GET("/adfenix/segmentationrule/any/view/all", () => { return 200; });

            Handle.GET("/adfenix/segrule/any/view/{?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/segmentationrule/any/view/{?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/segmentationrule/simpleclassifiedbroadsegmentationrule/create/new", () => { return 200; });

            Handle.GET("/adfenix/segmentationrule/simplekeywordsegmentationrule/create/new", () => { return 200; });

            Handle.GET("/adfenix/segmentationrule/simplehomesegmentationrule/create/new", () => { return 200; });

            Handle.GET("/adfenix/segmentationrule/simplejobsegmentationrule/create/new", () => { return 200; });

            Handle.GET("/adfenix/segmentationrule/simplecompanysegmentationrule/create/new", () => { return 200; });

            Handle.GET("/adfenix/get-orator-conversations/{?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/get-website-adincome-estimate?monthlyvisitors={?}&monthlyincome={?}&monthyladvertiser={?}", (Decimal p0, Decimal p1, Int32 p2) => { return 200; });

            Handle.GET("/adfenix/get-website-sales-estimate?monthlyvisitors={?}&customers={?}&income={?}&industry={?}", (Decimal p0, Decimal p1, Decimal p2, String p3) => { return 200; });

            Handle.GET("/adfenix/get-service-leads?company={?}&audience={?}", (String p0, String p1) => { return 200; });

            Handle.GET("/adfenix/orator-check-from-file/{?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/delete-webpagevisit/{?}", (Int32 p0) => { return 200; });

            Handle.GET("/adfenix/reset-listing-stats?domain={?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/boligtipset/search?query={?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/boligtipset/get?id={?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/boligtipset/comment?id={?}&message={?}&access_token={?}&source={?}", (String p0, String p1, String p2, String p3) => { return 200; });

            Handle.GET("/adfenix/boligtipset/like?id={?}&access_token={?}&source={?}", (String p0, String p1, String p2) => { return 200; });

            Handle.GET("/adfenix/boligtipset/share?id={?}&access_token={?}&source={?}", (String p0, String p1, String p2) => { return 200; });

            Handle.GET("/adfenix/boligtipset/eiendom/{?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/boligtipset/initiate", () => { return 200; });

            Handle.GET("/adfenix/facebook/check-image?url={?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/hemnet-start/{?}", (Int32 p0) => { return 200; });

            Handle.GET("/adfenix/campaignhemnet/order?propertyid={?}&week={?}", (String p0, String p1) => { return 200; });

            Handle.GET("/adfenix/campaign/classified/info?uniqueid={?}&token={?}&domain={?}", (String p0, String p1, String p2) => { return 200; });

            Handle.GET("/adfenix/campaign/classified/report?uniqueid={?}&token={?}&domain={?}", (String p0, String p1, String p2) => { return 200; });

            Handle.POST("/adfenix/campaign/classified/create?token={?}&domain={?}", (String p0, String p1) => { return 200; });

            Handle.GET("/adfenix/campaign/hemnet/start?uniqueid={?}&organization={?}&broker={?}&contactemail={?}", (String p0, String p1, String p2, String p3) => { return 200; });

            Handle.GET("/adfenix/campaignbohjem/start?url={?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/update-area-relations/{?}", (Int32 p0) => { return 200; });

            Handle.GET("/adfenix/get-started", () => { return 200; });

            Handle.POST("/adfenix/api/facebooklookalike", () => { return 200; });

            Handle.POST("/adfenix/api/servletendpoints?operation={?}", (String p0) => { return 200; });

            Handle.GET("/adfenix/api/servletendpoints?operation={?}", (String p0) => { return 200; });

            Response resp = Self.GET("/launcher");
            Assert.IsTrue(resp.IsSuccessStatusCode);

        }
    }
}