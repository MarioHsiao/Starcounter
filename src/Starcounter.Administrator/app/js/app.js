/**
 * ----------------------------------------------------------------------------
 * Starcounter Administrator module
 * ----------------------------------------------------------------------------
 */
var adminModule = angular.module('scadmin', ['ngRoute', 'ui.bootstrap', 'uiHandsontable', 'ui', 'ui.config','ngSanitize','ui.select'], function ($routeProvider) {

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
        templateUrl: '/app/partials/applications-store',
        controller: 'AppStoreCtrl',
        resolve: {
            setDatabase: function (HostModelService, $route) {
                return HostModelService.setDatabase($route.current.params.name);
            }
        }
    });

    $routeProvider.when('/databases/:name/sql', {
        templateUrl: '/app/partials/database-sql.html',
        controller: 'SqlCtrl',
        resolve: {
            setDatabase: function (HostModelService, $route) {
                return HostModelService.setDatabase($route.current.params.name);
            }
        }
    });

    $routeProvider.when('/databases/:name/executabe-start', {
        templateUrl: '/app/partials/executabe-start.html',
        controller: 'ExecutableStartCtrl',
        resolve: {
            setDatabase: function (HostModelService, $route) {
                return HostModelService.setDatabase($route.current.params.name);
            }
        }
    });

    $routeProvider.when('/databases/:name/settings', {
        templateUrl: '/app/partials/database-settings.html',
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

    $routeProvider.when('/database-new', {
        templateUrl: '/app/partials/database-new.html',
        controller: 'DatabaseNewCtrl',
        resolve: {
            assureHostModel: function (HostModelService, $route) {
                return HostModelService.waitForModel();
            }
        }
    });

    $routeProvider.when('/messages', {
        templateUrl: '/app/partials/messages.html',
        controller: 'MessagesCtrl',
        resolve: {
            assureHostModel: function (HostModelService, $route) {
                return HostModelService.waitForModel();
            }
        }
    });

    $routeProvider.when('/server/log', {
        templateUrl: '/app/partials/server-log.html',
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
        templateUrl: '/app/partials/server-network.html',
        controller: 'NetworkCtrl',
        resolve: {
            assureHostModel: function (HostModelService, $route) {
                return HostModelService.waitForModel();
            }
        }
    });

    $routeProvider.when('/server/settings', {
        templateUrl: '/app/partials/Server-settings.html',
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
adminModule.controller('NavbarController', ['$scope', '$rootScope', '$location', '$log', 'NoticeFactory', 'SubmenuService', 'MessageService', 'HostModelService', function ($scope, $rootScope, $location, $log, NoticeFactory, SubmenuService, MessageService, HostModelService) {


    $scope.subMenu = SubmenuService.model;

    $scope.newVersion = null;

    $scope.messages = MessageService.messages;
    $scope.serverModel = HostModelService.serverStatus.obj;


    $scope.data = HostModelService.data;

    //$scope.$watchCollection('serverModel.Databases', function (newCollection, oldCollection) {

    //    if (newCollection == null) return;

    //    for (var i = 0; i < newCollection.length ; i++) {
    //        console.log(">" + newCollection[i].DisplayName);
    //    }


    //});

    //$rootScope.$on('HostModelService.loaded', function (event, data) {
    //    debugger;


    //});

    // TODO: Keep querystate
    $rootScope.queryState = {
        selectedDatabaseName: null,
        sqlQuery: "",
        columns: [],
        rows: []
    }

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

    $scope.isActive = function (viewLocation) {

        return viewLocation === "#" + $location.path();
    };

    //$scope.$on('$routeChangeError', function () {
    //    // handle the error
    //    debugger;
    //});

    $rootScope.$on("$routeChangeError", function (event, current, pervious, refection) {
        // Show Network down..

        if (refection.databaseNotFound) {
            $location.path("/databases");
            return;
        }

        NoticeFactory.ShowNotice({ type: 'danger', msg: "The server is not responding or is not reachable.", helpLink: null });
    });
    // $routeChangeStart
    // $routeChangeSuccess

    $scope.viewmode = null;

    //$scope.$watch("viewmode", function (newValue, oldValue) {

    //    $location.search('view', newValue);
    //});

    $rootScope.$on("$routeChangeSuccess", function (event, current, pervious, refection) {

        var re = new RegExp("(/databases/)(.*?)(/.*?|$)");
        var locationStr = $location.path();
        var r = locationStr.match(re);

        if (r != null && r.length > 2) {
            HostModelService._wantedSelectedDatabaseName = r[2];
        }
        else {
            HostModelService._wantedSelectedDatabaseName = null;
        }

        var databaseName = HostModelService._wantedSelectedDatabaseName;

        SubmenuService.model.menues.length = 0;
        
        if (locationStr == "/databases" || locationStr == "/database-new") {
            //HostModelService.data.selectedDatabase = null;
            SubmenuService.model.menues.push({ "Title": "New database", "Link": "#/database-new" })
            SubmenuService.model.showHome = false;
            return;
        }

        if (locationStr.lastIndexOf("/server", 0) === 0) {
            SubmenuService.model.showHome = false;
            return;
        }

        if (databaseName != null) {
            SubmenuService.model.showHome = true;
            //SubmenuService.subMenu.push({ "Title": databaseName, "Link": "#/databases/" + databaseName + "" })
            //SubmenuService.subMenu.push({ "Title": "home", "Link": "#/databases/" + databaseName + "" })
            SubmenuService.model.menues.push({ "Title": "SQL", "Link": "#/databases/" + databaseName + "/sql" })
            SubmenuService.model.menues.push({ "Title": "AppStore", "Link": "#/databases/" + databaseName + "/appstore" })
            SubmenuService.model.menues.push({ "Title": "Start Executable", "Link": "#/databases/" + databaseName + "/executabe-start" })
        }

    });

    //    $rootScope.$on('$routeChangeStart', function (scope, next, current) {
    //        console.log('Changing from ' + angular.toJson(current) + ' to ' + angular.toJson(next));
    //    });

    // Init
    // Refresh host model
    //HostModelService.refreshHostModel(function () {
    //}, function (messageObject) {
    //    // Error
    //    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
    //});


}]);


/**
 * Updates the position of the horizontal scrollbar on SQL and Log pages
 */
function scrollRefresh() {
    if (!window.virtualScroller) {
        createHorizontalScrollbar();
    }
    var wt = $('.handsontable:eq(0)').handsontable('getInstance').view.wt;
    if (wt.wtScrollbars.horizontal) {
        var width = Handsontable.Dom.outerWidth(wt.wtScrollbars.instance.wtTable.holder.parentNode);
        var scrolledWidth = wt.wtScrollbars.instance.wtViewport.getWorkspaceActualWidth();
        if (scrolledWidth > width) {
            var box = wt.wtScrollbars.horizontal.scrollHandler.getBoundingClientRect();
            if (box.bottom > document.documentElement.clientHeight && wt.wtScrollbars.horizontal.scrollHandler.scrollLeft !== void 0) {
                virtualScroller.DIV.style.display = 'block';
                virtualScroller.DIV.style.top = '';
                virtualScroller.DIV.style.bottom = 0;
                virtualScroller.DIV.style.left = parseInt(box.left, 10) + 1 + 'px';
                virtualScroller.setPositionPx(wt.wtScrollbars.horizontal.windowScrollPosition);
                virtualScroller.setWidth(width);
                virtualScroller.setScrolledWidth(scrolledWidth);
            }
            else {
                virtualScroller.DIV.style.display = 'none';
            }
        }
        else {
            virtualScroller.DIV.style.display = 'none';
        }
    }
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