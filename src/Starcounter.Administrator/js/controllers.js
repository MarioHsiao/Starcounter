
/**
 * Scadmin service module
 */
var myApp = angular.module('scadmin', ['scadminServices', 'ui', 'ui.bootstrap', 'uiHandsontable'], function ($routeProvider, $locationProvider) {

    $routeProvider.when('/databases/:databaseId', {
        templateUrl: '/partials/database.html',
        controller: DatabaseCtrl,
        resolve: {
            // I will cause a 1 second delay
            delay: function ($q, $timeout) {
                var delay = $q.defer();
                $timeout(delay.resolve, 1000);
                return delay.promise;
            }
        }
    });

    $routeProvider.when('/server', {
        templateUrl: '/partials/server.html',
        controller: ServerCtrl
    });

    $routeProvider.when('/databases', {
        templateUrl: '/partials/databases.html',
        controller: DatabasesCtrl
    });

    $routeProvider.when('/log', {
        templateUrl: '/partials/log.html',
        controller: LogCtrl
    });

    $routeProvider.when('/sql', {
        templateUrl: '/partials/sql.html',
        controller: SqlCtrl
    });
    //    $routeProvider.otherwise({redirectTo: '/index.html'});

    //$locationProvider.html5Mode(true);
});


/**
 * Configuration for CodeMirror (Sql query textbox)
 */
myApp.value('ui.config', {
    codemirror: {
        mode: 'text/x-mysql',
        lineNumbers: true,
        matchBrackets: true
    }
});


/**
 * Main Controller
 */
function MainCtrl($scope, $location) {

    // Handles the active navbar item
    $scope.isActiveUrl = function (path) {
        return $location.path() == path;
    }
}


/**
 * Server Controller
 */
function ServerCtrl($scope, Server) {

    $scope.message = null;

    // Get a database
    $scope.server = Server.get({}, function (server, headers) {
        // Success
    }, function (response) {
        // Error, Can not retrive list of databases
        $scope.message = "Can not retrive the server";
    });

    $scope.hasMessage = function () {
        return $scope.message != null && $scope.message.length > 0;
    }
}


/**
 * Databases Controller
 */
function DatabasesCtrl($scope, Database) {

    $scope.message = null;

    $scope.databases = Database.query(function (databases) {
        $scope.databases = databases.DatabaseList;
    }, function (response) {
        // Error, Can not retrive list of databases
        $scope.message = "Can not retrive a list of databases";
    });

    $scope.hasMessage = function () {
        return $scope.message != null && $scope.message.length > 0;
    }

}


/**
 * Databass Controller
 */
function DatabaseCtrl($scope, $routeParams, Database, patchService) {

    $scope.message = null;

    // Get a database
    $scope.database = Database.get({ databaseId: $routeParams.databaseId }, function (database, headers) {

        // Get location from the response header
        $scope.location = headers('Location');

        // Set the data model to the scope
        //$scope.database = database;

        // Observe the model
        var observer = jsonpatch.observe($scope.database, function (patches) {
            console.log("jsonpatch.observe triggerd");
            patchService.applyPatch($scope.database, $scope.location, patches);
        });

    }, function (response) {
        // Error, Can not retrive list of databases
        $scope.message = "Can not retrive the database";
    });


    // User clicked the "Start" button
    $scope.btnClick_start = function () {
        console.log("btnClick_start");
        $scope.database.Start$ = !$scope.database.Start$;
    }

    // User clicked the "Stop" button
    $scope.btnClick_stop = function () {
        console.log("btnClick_stop");
        $scope.database.Stop$ = !$scope.database.Stop$;
    }

    $scope.hasMessage = function () {
        return $scope.message != null && $scope.message.length > 0;
    }
}


/**
 * Log Controller
 */
function LogCtrl($scope, Log) {

    $scope.message = null;
    $scope.isBusy = false;

    $scope.log = {};
    $scope.log.LogEntries = [];

    $scope.filterModel = {
        debug: false,
        notice: false,
        warning: true,
        error: true
    };

    $scope.$watch('filterModel', function () {
        $scope.refresh();
    }, true);

    $scope.refresh = function () {

        $scope.isBusy = true;

        // Get a database
        Log.query($scope.filterModel, function (log, headers) {
            // Success
            $scope.log = log;
            $scope.isBusy = false;
        }, function (response) {
            // Error, Can not retrive the log
            $scope.isBusy = false;
            $scope.message = "Can not retrive the log";
        });
    }

    $scope.hasMessage = function () {
        return $scope.message != null && $scope.message.length > 0;
    }

    // User clicked the "Refresh" button
    $scope.btnClick_refresh = function () {
        $scope.refresh();
    }


}


/**
 * Sql Controller
 */
function SqlCtrl($scope, Sql, Database, patchService, SqlQuery, $dialog) {

    $scope.selectedDatabase = null;
    $scope.sqlQuery = "select m from systable m";
    $scope.columns = [];
    $scope.rows = [];
    $scope.alerts = [];

    $scope.isBusy = false;
    $scope.executeButtonTitle = function () {
        if ($scope.isBusy) {
            return "Executing...";
        }
        else {
            return "Execute";
        }
    }

    // Close alert box
    $scope.closeAlert = function (index) {
        $scope.alerts.splice(index, 1);
    };

    // Retrive database list
    $scope.databases = Database.query(function (databases, headers) {
        $scope.databases = databases.DatabaseList;
        if ($scope.databases.length > 0) {
            $scope.selectedDatabase = $scope.databases[0];
        }
        else {
            $scope.selectedDatabase = null;
        }

    }, function (response) {
        // Error, Can not retrive a list of databases
        $scope.alerts.push({ type: 'error', msg: "Can not retrive a list of databases" });
    });

    // User clicked the "Execute" button
    $scope.btnClick_execute = function () {

        $scope.isBusy = true;

        $scope.alerts.length = 0;

        SqlQuery.send({ databaseName: $scope.selectedDatabase.DatabaseName }, $scope.sqlQuery, function (result, headers) {

            $scope.isBusy = false;

            // Success

            $scope.columns = result.columns;
            $scope.rows = result.rows;

            if (result.sqlException != null) {
                // Show message

                //$scope.sqlException.beginPosition
                //$scope.sqlException.endPosition
                //$scope.sqlException.errorMessage
                //$scope.sqlException.helpLink
                //$scope.sqlException.message
                //$scope.sqlException.query
                //$scope.sqlException.scErrorCode
                //$scope.sqlException.token
                //$scope.sqlException.stackTrace

                $scope.alerts.push({ type: 'error', msg: result.sqlException.message });

                // Show modal window with the error message
                if (result.sqlException.errorMessage != null) {
                    var title = 'Starcounter internal error';
                    var msg = result.sqlException.errorMessage + "<br><br>" + result.sqlException.helpLink;
                    var btns = [{ result: 'ok', label: 'OK', cssClass: 'btn-primary' }];
                    $dialog.messageBox(title, msg, btns).open();
                }

            }

            if (result.exception != null) {
                // Show modal error window (this error is an internal starcounter error)
                //$scope.exception.helpLink
                //$scope.exception.message
                //$scope.exception.stackTrace

                //var title = 'Starcounter internal Error';
                //var msg = result.exception.message + "<br><br>" + result.exception.helplink;
                //var btns = [{ result: 'ok', label: 'OK', cssClass: 'btn-primary' }];
                //$dialog.messageBox(title, msg, btns).open();


                // Dialogbox options
                $scope.opts = {
                    backdrop: false,
                    keyboard: true,
                    backdropClick: true,
                    templateUrl: "partials/error.html",
                    controller: 'DialogController',
                    data: { header: "Starcounter Error", message: result.exception.message, helpLink: result.exception.helpLink, stackTrace: "" }
                };

                var d = $dialog.dialog($scope.opts);
                d.open();
            }

        }, function (response) {
            // Error
            $scope.isBusy = false;

            $scope.alerts.push({ type: 'error', msg: "Can not connect to the database " + $scope.selectedDatabase.DatabaseName });
            ////404 or bad
            //if (response.status === 404) {
            //}
        });
    }

    //$scope.openDialog = function (header, message, helplink, stacktrace) {

    //    $scope.opts.data.header = header;
    //    $scope.opts.data.message = message;
    //    $scope.opts.data.helplink = helplink;
    //    $scope.opts.data.stacktrace = stacktrace;

    //    var d = $dialog.dialog($scope.opts);
    //    d.open().then(function (result) {
    //        if (result) {
    //            alert('dialog closed with result: ' + result);
    //        }
    //    });
    //};


    //$scope.openMessageBox = function () {
    //    var title = 'This is a message box';
    //    var msg = 'This is the content of the message box';
    //    var btns = [{ result: 'cancel', label: 'Cancel' }, { result: 'ok', label: 'OK', cssClass: 'btn-primary' }];

    //    $dialog.messageBox(title, msg, btns)
    //      .open()
    //      .then(function (result) {
    //          alert('dialog closed with result: ' + result);
    //      });
    //};

}


/**
 * Dialog Controller
 */
function DialogController($scope, dialog) {

    $scope.header = dialog.options.data.header;
    $scope.message = dialog.options.data.message;
    $scope.helplink = dialog.options.data.helplink;
    $scope.stacktrace = dialog.options.data.stacktrace;

    $scope.close = function (result) {
        dialog.close(result);
    };
}