/**
 * ----------------------------------------------------------------------------
 * Starcounter Administrator module
 * ----------------------------------------------------------------------------
 */
var adminModule = angular.module('scadmin', ['ngRoute', 'ui.bootstrap', 'ngHandsontable', 'ui', 'ui.config', 'ngSanitize', 'ui.select'], function ($routeProvider) {

    $routeProvider.when('/databases', {
        templateUrl: '/app/partials/databases.html',
        controller: 'DatabasesCtrl',
        resolve: {
            waitForModel: function (HostModelService, $route) {
                return HostModelService.waitForModel();
            }
        }
    });

    $routeProvider.when('/databases/:name', {
        templateUrl: '/app/partials/database.html',
        resolve: {
            setDatabase: function (HostModelService, $route) {
                return HostModelService.setDatabase($route.current.params.name);
            }
            //          test: HostModelService.waitForModel("test")
            //assureHostModel: function (HostModelService, $route) {
            //    return HostModelService.init($route.current.params.name);
            //},
            //assureHostModel: function (HostModelService, $route) {
            //    return HostModelService.waitForModel();
            //}

        },
        controller: 'DatabaseCtrl',
    });

    $routeProvider.when('/databases/:name/appstore', {
        templateUrl: '/app/partials/databaseAppstore.html',
        controller: 'AppStoreCtrl',
        resolve: {
            setDatabase: function (HostModelService, $route) {
                return HostModelService.setDatabase($route.current.params.name);
            }
        }
    });

    $routeProvider.when('/databases/:name/sql', {
        templateUrl: '/app/partials/databaseSql.html',
        controller: 'SqlCtrl',
        resolve: {
            setDatabase: function (HostModelService, $route) {
                return HostModelService.setDatabase($route.current.params.name);
            }
        }
    });

    $routeProvider.when('/databases/:name/executabeStart', {
        templateUrl: '/app/partials/executabeStart.html',
        controller: 'ExecutableStartCtrl',
        resolve: {
            setDatabase: function (HostModelService, $route) {
                return HostModelService.setDatabase($route.current.params.name);
            }
        }
    });

    $routeProvider.when('/databases/:name/settings', {
        templateUrl: '/app/partials/databaseSettings.html',
        controller: 'DatabaseSettingsCtrl',
        resolve: {
            setDatabase: function (HostModelService, $route) {
                return HostModelService.setDatabase($route.current.params.name);
            }
        }
    });

    $routeProvider.when('/databases/:name/applications/:appid', {
        templateUrl: '/app/partials/application.html',
        controller: 'ApplicationCtrl',
        controllerAs: 'applicationCtrl1',
        resolve: {
            setDatabase: function (HostModelService, $route) {
                return HostModelService.setDatabase($route.current.params.name);
            }
        }
    });

    $routeProvider.when('/databaseNew', {
        templateUrl: '/app/partials/databaseNew.html',
        controller: 'DatabaseNewCtrl',
        resolve: {
            assureHostModel: function (HostModelService, $route) {
                return HostModelService.waitForModel();
            }
        }
    });

    $routeProvider.when('/server/log', {
        templateUrl: '/app/partials/serverLog.html',
        controller: 'LogCtrl',
        reloadOnSearch: false,
        redirectTo: function (routeParams, path, search) {
            if (jQuery.isEmptyObject(search)) {
                // Set default search filter
                return "/server/log?debug=false&notice=false&warning&error&source=&maxitems=30";
            }
            return;
        }
        //resolve: {
        //    redirect: function ($route, $location) {
        //        if (jQuery.isEmptyObject($location.search())) {
        //            // Set default search filter
        //            $location.search({ "debug": false, "notice": false, "warning": true, "error": true, "source": "", "maxitems": 30 });
        //        }
        //    }
        //}
    });

    $routeProvider.when('/server/network', {
        templateUrl: '/app/partials/serverNetwork.html',
        controller: 'NetworkCtrl',
        resolve: {
            assureHostModel: function (HostModelService, $route) {
                return HostModelService.waitForModel();
            }
        }
    });

    $routeProvider.when('/server/settings', {
        templateUrl: '/app/partials/ServerSettings.html',
        controller: 'ServerSettingsCtrl',
        resolve: {
            assureHostModel: function (HostModelService, $route) {
                return HostModelService.waitForModel();
            }
        }
    });

    $routeProvider.otherwise({ redirectTo: '/databases' });

}).value('ui.config', {
    codemirror: {
        mode: 'text/x-mysql',
        lineNumbers: true,
        matchBrackets: true
    }
});

adminModule.loadData = function ($q, $timeout) {
    var defer = $q.defer();
    $timeout(function () {
        defer.resolve();
        console.log("loadData");
    }, 2000);
    return defer.promise;
};

adminModule.prepData = function ($q, $timeout) {
    var defer = $q.defer();
    $timeout(function () {
        defer.resolve();
        console.log("prepData");
    }, 2000);
    return defer.promise;
};

/**
 * ----------------------------------------------------------------------------
 * Navbar Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('NavbarController', ['$scope', '$rootScope', '$location', '$log', 'NoticeFactory', 'SubmenuService', 'HostModelService', function ($scope, $rootScope, $location, $log, NoticeFactory, SubmenuService, HostModelService) {

    $scope.subMenu = SubmenuService.model;
    $scope.newVersion = null;
    //$scope.serverModel = HostModelService.serverStatus.obj;
    $scope.data = HostModelService.data;

    // Make selected nav pile to be selected 'active'
    $scope.getClass = function (path) {

        var re = new RegExp(path);
        var locationStr = $location.path();
        if (locationStr.match(re)) {
            return true;
        } else {
            return false;
        }
    }


    $scope.$watchCollection('data.model.Databases', function (newNames, oldNames) {

        // Check if selected database exists
        if (newNames == undefined || $scope.data.selectedDatabase == undefined) return;

        for (var i = 0; i < $scope.data.model.Databases.length ; i++) {
            var db = $scope.data.model.Databases[i];

            if ($scope.data.selectedDatabase.ID == db.ID) {
                return;
            }
        }

        // Selected database has been removed.
        if ($scope.data.model.Databases.length > 0) {
            $location.path("/databases/" + $scope.data.model.Databases[0].ID);
        }
        else {
            $scope.data.selectedDatabase = null;
            $location.path("/databases");
        }

    });

    $scope.isActive = function (viewLocation) {

        return viewLocation === "#" + $location.path();
    };

    $rootScope.$on("$routeChangeError", function (event, current, pervious, refection) {
        // Show Network down..

        if (refection.databaseNotFound) {
            $location.path("/databases");
            return;
        }

        NoticeFactory.ShowNotice({ type: 'danger', msg: "The server is not responding or is not reachable.", helpLink: null });
    });

    $scope.viewmode = null;

    $rootScope.$on("$routeChangeSuccess", function (event, current, pervious, refection) {

        var re = new RegExp("(/databases/)(.*?)(/.*?|$)");
        var locationStr = $location.path();
        var r = locationStr.match(re);


        var databaseName = null;

        if (r != null && r.length > 2) {
            databaseName = r[2];
        }

        HostModelService._wantedSelectedDatabaseName = databaseName;

        SubmenuService.model.menues.length = 0;

        if (locationStr == "/databases" || locationStr == "/databaseNew") {
            //HostModelService.data.selectedDatabase = null;
            SubmenuService.model.menues.push({ "Title": "New database", "Link": "#/databaseNew", "Tooltip": "Create a new database" })
            SubmenuService.model.isDatabase = false;
            return;
        }

        if (locationStr.lastIndexOf("/server", 0) === 0) {
            SubmenuService.model.isDatabase = false;
            return;
        }

        if (databaseName != null) {
            SubmenuService.model.isDatabase = true;
            SubmenuService.model.menues.push({ "Title": "SQL", "Link": "#/databases/" + databaseName + "/sql", "Tooltip": "Make SQL Queries" })
            SubmenuService.model.menues.push({ "Title": "AppStore", "Link": "#/databases/" + databaseName + "/appstore", "Tooltip": "Download Applications from the AppStore" })
            SubmenuService.model.menues.push({ "Title": "Start Executable", "Link": "#/databases/" + databaseName + "/executabeStart", "Tooltip" : "Start executable" })
        }

    });
}]);


/**
 * Updates the position of the horizontal scrollbar on SQL and Log pages
 */
function scrollRefresh() {
    if (!window.virtualScroller) {
        createHorizontalScrollbar();
    }
    var hot = angular.element(document.querySelector('#handsontable')).isolateScope().hotInstance;

    if (!hot) {
      return;
    }
    var wt = hot.view.wt;

    // In newest version of Hot we don't have scrollbars
    //if (wt.wtScrollbars.horizontal) {
    //    var width = Handsontable.Dom.outerWidth(wt.wtScrollbars.instance.wtTable.holder.parentNode);
    //    var scrolledWidth = wt.wtScrollbars.instance.wtViewport.getWorkspaceActualWidth();
    //    if (scrolledWidth > width) {
    //        var box = wt.wtScrollbars.horizontal.scrollHandler.getBoundingClientRect();
    //        if (box.bottom > document.documentElement.clientHeight && wt.wtScrollbars.horizontal.scrollHandler.scrollLeft !== void 0) {
    //            virtualScroller.DIV.style.display = 'block';
    //            virtualScroller.DIV.style.top = '';
    //            virtualScroller.DIV.style.bottom = 0;
    //            virtualScroller.DIV.style.left = parseInt(box.left, 10) + 1 + 'px';
    //            virtualScroller.setPositionPx(wt.wtScrollbars.horizontal.windowScrollPosition);
    //            virtualScroller.setWidth(width);
    //            virtualScroller.setScrolledWidth(scrolledWidth);
    //        }
    //        else {
    //            virtualScroller.DIV.style.display = 'none';
    //        }
    //    }
    //    else {
    //        virtualScroller.DIV.style.display = 'none';
    //    }
    //}
}


/**
 * Creates a new instance of VeryNativeScrollbar which is used by SQL and Log pages to scroll horizontally
 */
function createHorizontalScrollbar() {
    window.virtualScroller = new VeryNativeScrollbar();
    virtualScroller.setScrollCallback(function () {
        var hotHolder = document.getElementById('handsontableContainer');
        if (hotHolder) {
            hotHolder.scrollLeft = virtualScroller.getPositionPx();
        }
    });
}