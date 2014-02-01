'use strict';

var adminApp = angular.module('admin', ['ngResource']);

adminApp.config(function ($routeProvider) {

    $routeProvider.
        when('/debug', {
            controller: 'DebugCtrl as debugCtrl',
            templateUrl: '/partials/debug.html'
        }).
        when('/overview', {
            controller: 'OverviewCtrl as overviewCtrl',
            templateUrl: '/partials/overview.html'
        }).
        when('/products/:id', {
            controller: 'ProductCtrl as model',
            templateUrl: '/partials/Product.html'
        });

    $routeProvider.otherwise({ redirectTo: '/overview' });

});



adminApp.controller('MasterCtrl', function ($location) {

    this.test = function () {
        console.log("Simulate Button clicked");
    }

    // Handles the active navbar item
    this.isActiveUrl = function (path) {
        return $location.path() == path;
    }

});

adminApp.controller('DebugCtrl', function ($http) {

    var a = this;
    this.refresh = function () {
        this.refreshInstallations();
        this.refreshInstallerStart();
        this.refreshInstallerExecuting();
        this.refreshInstallerFinish();
        this.refreshInstallerEnd();
        this.refreshInstallerAbort();
        this.refreshUsage();
    }

    this.installationsDelete = function (item) {

        $http({ method: 'DELETE', url: '/admin/installations/' + item.id }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {

              var arr = a.installation;
              var i = arr.indexOf(item);
              arr.splice(i, 1);

          }).
          error(function (response, status, headers, config) {
              console.log("Delete Installations - ERROR - Unhandled http statuscode " + status);
          });

    }

    this.installerStartDelete = function (item) {

        $http({ method: 'DELETE', url: '/admin/installerstart/' + item.id }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {

              var arr = a.installerStart;
              var i = arr.indexOf(item);
              arr.splice(i, 1);

          }).
          error(function (response, status, headers, config) {
              console.log("Delete InstallerStart - ERROR - Unhandled http statuscode " + status);
          });
    }

    this.installerExecutingDelete = function (item) {

        $http({ method: 'DELETE', url: '/admin/installerexecuting/' + item.id }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {

              var arr = a.installerExecuting;
              var i = arr.indexOf(item);
              arr.splice(i, 1);

          }).
          error(function (response, status, headers, config) {
              console.log("Delete InstallerExecuting - ERROR - Unhandled http statuscode " + status);
          });

    }

    this.installerFinishDelete = function (item) {

        $http({ method: 'DELETE', url: '/admin/installerfinish/' + item.id }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {

              var arr = a.installerFinish;
              var i = arr.indexOf(item);
              arr.splice(i, 1);

          }).
          error(function (response, status, headers, config) {
              console.log("Delete InstallerFinish - ERROR - Unhandled http statuscode " + status);
          });

    }

    this.installerEndDelete = function (item) {

        $http({ method: 'DELETE', url: '/admin/installerend/' + item.id }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {

              var arr = a.installerEnd;
              var i = arr.indexOf(item);
              arr.splice(i, 1);

          }).
          error(function (response, status, headers, config) {
              console.log("Delete InstallerEnd - ERROR - Unhandled http statuscode " + status);
          });

    }

    this.installerAbortDelete = function (item) {

        $http({ method: 'DELETE', url: '/admin/installerabort/' + item.id }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {

              var arr = a.installerAbort;
              var i = arr.indexOf(item);
              arr.splice(i, 1);

          }).
          error(function (response, status, headers, config) {
              console.log("Delete InstallerAbort - ERROR - Unhandled http statuscode " + status);
          });

    }

    this.usageDelete = function (item) {

        $http({ method: 'DELETE', url: '/admin/usage/' + item.id }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {

              var arr = a.usage;
              var i = arr.indexOf(item);
              arr.splice(i, 1);
          }).
          error(function (response, status, headers, config) {
              console.log("Delete Usage - ERROR - Unhandled http statuscode " + status);
          });

    }

    this.refreshInstallations = function () {

        $http({ method: 'GET', url: '/admin/installations' }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {
              a.installation = response.installation;
          }).
          error(function (response, status, headers, config) {
              console.log("Refresh Installations() - ERROR - Unhandled http statuscode " + status);
          });
    }

    this.refreshInstallerStart = function () {
  

        $http({ method: 'GET', url: '/admin/installerstart' }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {
              a.installerStart = response.start;
          }).
          error(function (response, status, headers, config) {
              console.log("Refresh InstallerStart() - ERROR - Unhandled http statuscode " + status);
          });
    }

    this.refreshInstallerExecuting = function () {

        $http({ method: 'GET', url: '/admin/installerexecuting' }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {
              a.installerExecuting = response.executing;
          }).
          error(function (response, status, headers, config) {
              console.log("Refresh InstallerExecuting() - ERROR - Unhandled http statuscode " + status);
          });
    }

    this.refreshInstallerFinish = function () {

        $http({ method: 'GET', url: '/admin/installerfinish' }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {
              a.installerFinish = response.finish;
          }).
          error(function (response, status, headers, config) {
              console.log("Refresh InstallerFinish() - ERROR - Unhandled http statuscode " + status);
          });
    }

    this.refreshInstallerEnd = function () {

        $http({ method: 'GET', url: '/admin/installerend' }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {
              a.installerEnd = response.end;
          }).
          error(function (response, status, headers, config) {
              console.log("Refresh InstallerEnd() - ERROR - Unhandled http statuscode " + status);
          });
    }

    this.refreshInstallerAbort = function () {

        $http({ method: 'GET', url: '/admin/installerabort' }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {
              a.installerAbort = response.abort;
          }).
          error(function (response, status, headers, config) {
              console.log("Refresh InstallerAbort() - ERROR - Unhandled http statuscode " + status);
          });
    }

    this.refreshUsage = function () {

        $http({ method: 'GET', url: '/admin/usage' }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {
              a.usage = response.usage;
          }).
          error(function (response, status, headers, config) {
              console.log("Refresh InstallerUsage() - ERROR - Unhandled http statuscode " + status);
          });
    }

    this.refresh();

});

adminApp.controller('OverviewCtrl', function ($http) {

    this.chartData = null;
    var a = this;

    this.refresh = function () {
        this.refreshOverview();
    }



    this.refreshOverview = function () {

        $http({ method: 'GET', url: '/admin/overview' }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {
              a.overview = response.overview;

              var columns = "chxl=1:|Abort|End|Finish|Executing|Start";
              var values = "chd=t:" + response.overview.start + "," + response.overview.executing + "," + response.overview.finish + "," + response.overview.end + "," + response.overview.abort + "";

              var maxValue = Math.max(response.overview.start, response.overview.executing);
              maxValue = Math.max(maxValue, response.overview.finish);
              maxValue = Math.max(maxValue, response.overview.end);
              maxValue = Math.max(maxValue, response.overview.abort);

              var size = "&chs=440x220";

              a.chartData = "//chart.googleapis.com/chart?" + columns + "&chxr=0,0," + maxValue + "&chxs=1,676767,11.5,0,l,676767&chxt=x,y&chbh=a" + size + "&cht=bhs&chco=4D89F9&chds=0," + maxValue + "&" + values + "";



          }).
          error(function (response, status, headers, config) {
              console.log("Refresh Overview() - ERROR - Unhandled http statuscode " + status);
          });
    }

    this.refreshOverview();




});

/**
 * Master Controller
 */
function MasterCtrl($location) {

    // Handles the active navbar item
    this.isActiveUrl = function (path) {
        return $location.path() == path;
    }


}



