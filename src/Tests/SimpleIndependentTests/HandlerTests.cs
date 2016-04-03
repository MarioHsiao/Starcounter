using Starcounter;
using System;
using Starcounter.Metadata;
using Starcounter.Internal;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

class HandlerDeletionTests {

    public static void TestGetHandlerDeletion() {

        String s;
        Response r;

        s = Self.GET<String>("/handler2");
        Assert.IsTrue(null == s);

        s = Self.GET<String>("/handler1");
        Assert.IsTrue(null == s);

        Handle.GET("/handler1", () => {
            return "/handler1";
        });

        s = Self.GET<String>("/handler1");
        Assert.IsTrue("/handler1" == s);

        r = Http.GET("http://localhost:" + StarcounterEnvironment.Default.UserHttpPort + "/handler1");
        Assert.IsTrue("/handler1" == r.Body);

        Handle.GET("/handler2", () => {
            return "/handler2";
        });
        s = Self.GET<String>("/handler2");
        Assert.IsTrue("/handler2" == s);

        r = Http.GET("http://localhost:" + StarcounterEnvironment.Default.UserHttpPort + "/handler2");
        Assert.IsTrue("/handler2" == r.Body);

        Handle.UnregisterHttpHandler("GET", "/handler1");

        s = Self.GET<String>("/handler1");
        Assert.IsTrue(null == s);

        r = Http.GET("http://localhost:" + StarcounterEnvironment.Default.UserHttpPort + "/handler1");
        Assert.IsTrue(404 == r.StatusCode);

        s = Self.GET<String>("/handler2");
        Assert.IsTrue("/handler2" == s);

        r = Http.GET("http://localhost:" + StarcounterEnvironment.Default.UserHttpPort + "/handler2");
        Assert.IsTrue("/handler2" == r.Body);

        Handle.UnregisterHttpHandler("GET", "/handler2");

        s = Self.GET<String>("/handler2");
        Assert.IsTrue(null == s);

        s = Self.GET<String>("/handler1");
        Assert.IsTrue(null == s);

        r = Http.GET("http://localhost:" + StarcounterEnvironment.Default.UserHttpPort + "/handler1");
        Assert.IsTrue(404 == r.StatusCode);

        r = Http.GET("http://localhost:" + StarcounterEnvironment.Default.UserHttpPort + "/handler2");
        Assert.IsTrue(404 == r.StatusCode);
    }

    public static void TestPostHandlerDeletion() {

        Response r;

        r = Self.POST("/handler2", "Body!");
        Assert.IsTrue(null == r);

        r = Self.POST("/handler1", "Body!");
        Assert.IsTrue(null == r);

        Handle.POST("/handler1", () => {
            return "/handler1";
        });

        r = Self.POST("/handler1", "Body!");
        Assert.IsTrue("/handler1" == r.Body);

        r = Http.POST("http://localhost:" + StarcounterEnvironment.Default.UserHttpPort + "/handler1", "Body!", null);
        Assert.IsTrue("/handler1" == r.Body);

        Handle.POST("/handler2", () => {
            return "/handler2";
        });

        r = Self.POST("/handler2", "Body!");
        Assert.IsTrue("/handler2" == r.Body);

        r = Http.POST("http://localhost:" + StarcounterEnvironment.Default.UserHttpPort + "/handler2", "Body!", null);
        Assert.IsTrue("/handler2" == r.Body);

        Handle.UnregisterHttpHandler("POST", "/handler1");

        r = Self.POST("/handler1", "Body!");
        Assert.IsTrue(null == r);

        r = Http.POST("http://localhost:" + StarcounterEnvironment.Default.UserHttpPort + "/handler1", "Body!", null);
        Assert.IsTrue(404 == r.StatusCode);

        r = Self.POST("/handler2", "Body!");
        Assert.IsTrue("/handler2" == r.Body);

        r = Http.POST("http://localhost:" + StarcounterEnvironment.Default.UserHttpPort + "/handler2", "Body!", null);
        Assert.IsTrue("/handler2" == r.Body);

        Handle.UnregisterHttpHandler("POST", "/handler2");

        r = Self.POST("/handler2", "Body!");
        Assert.IsTrue(null == r);

        r = Self.POST("/handler1", "Body!");
        Assert.IsTrue(null == r);

        r = Http.POST("http://localhost:" + StarcounterEnvironment.Default.UserHttpPort + "/handler1", "Body!", null);
        Assert.IsTrue(404 == r.StatusCode);

        r = Http.POST("http://localhost:" + StarcounterEnvironment.Default.UserHttpPort + "/handler2", "Body!", null);
        Assert.IsTrue(404 == r.StatusCode);
    }

    public static Int32 Run() {

        Console.WriteLine("Starting handler deletion tests...");

        TestGetHandlerDeletion();
        TestPostHandlerDeletion();

        TestGetHandlerDeletion();
        TestPostHandlerDeletion();

        TestPostHandlerDeletion();
        TestGetHandlerDeletion();

        Console.WriteLine("Finished handler deletion tests...");

        return 0;
    }
}