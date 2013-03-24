
/**
 * Scadmin service module
 */

var myApp = angular.module('scadmin', ['scadminServices', 'ui', 'ui.bootstrap','$strap.directives', 'uiHandsontable'], function ($routeProvider, $locationProvider) {

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
function MainCtrl($scope, $http) {
}


/**
 * Databases Controller
 */
function DatabasesCtrl($scope, Database) {
    console.log("DatabasesCtrl");

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
    console.log("DatabaseCtrl");

    $scope.message = null;

    // Get a database
    $scope.database = Database.get({ databaseId: $routeParams.databaseId }, function (database, headers) {

        // Get location from the response header
        $scope.location = headers('Location');
        console.log("DatabaseCtrl.Location:" + $scope.location);

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
function LogCtrl($scope, $http, Log, patchService) {

    console.log("LogCtrl");

    // Get a database
    $scope.log = Log.query(function (log, headers) {

        // Get location from the response header
        $scope.location = headers('Location');
        console.log("LogCtr.Location:" + $scope.location);

        // Observe the model
        var observer = jsonpatch.observe($scope.log, function (patches) {
            console.log("jsonpatch.observe triggerd");
            patchService.applyPatch($scope.log, $scope.location, patches);
        });

    });

    // User clicked the "Refresh" button
    $scope.btnClick_refresh = function () {
        console.log("btnClick_start");
        $scope.log.RefreshList$ = !$scope.log.RefreshList$;
    }


}


/**
 * Sql Controller
 */
function SqlCtrl($scope, Sql, Database, patchService, SqlQuery, $dialog) {

    console.log("SqlCtrl");

    $scope.selectedDatabase = null;
    $scope.message = null;
    $scope.sqlquery = "select m from systable m";

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
        $scope.message = "Can not retrive a list of databases";
    });


    // User clicked the "Execute" button
    $scope.btnClick_execute = function () {

        $scope.message = null;

        SqlQuery.send({ databaseName: $scope.selectedDatabase.DatabaseName }, $scope.sqlquery, function (result, headers) {

            $scope.Columns = result.columns;
            $scope.Rows = result.rows;

            if (result.sqlexception != null) {
                // Show message

                //$scope.sqlexception.BeginPosition
                //$scope.sqlexception.EndPosition
                //$scope.sqlexception.ErrorMessage
                //$scope.sqlexception.HelpLink
                //$scope.sqlexception.Message
                //$scope.sqlexception.Query
                //$scope.sqlexception.ScErrorCode
                //$scope.sqlexception.Token

                $scope.message = result.sqlexception.Message;

                // Show modal window with the error message
                if (result.sqlexception.ErrorMessage != null) {
                    var title = 'Starcounter internal error';
                    var msg = result.sqlexception.ErrorMessage + "<br><br>" + result.sqlexception.HelpLink;
                    var btns = [{ result: 'ok', label: 'OK', cssClass: 'btn-primary' }];
                    $dialog.messageBox(title, msg, btns).open();
                }

            }

            if (result.exception != null) {
                // Show modal error window (this error is an internal starcounter error)
                //$scope.exception.helplink
                //$scope.exception.message

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
                    data: { header: "Starcounter Error", message: result.exception.message, helplink: result.exception.helplink, stacktrace: "" }
                };

                var d = $dialog.dialog($scope.opts);
                d.open().then(function (result) {
                    if (result) {
                        alert('dialog closed with result: ' + result);
                    }
                });

            }



        }, function (response) {
            $scope.message = "Can not connect to the database " + $scope.selectedDatabase.DatabaseName;
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

    $scope.hasMessage = function () {
        return $scope.message != null && $scope.message.length > 0;
    }

}

// the dialog is injected in the specified controller
function DialogController($scope, dialog) {

    //console.log("Test");

    $scope.header = dialog.options.data.header;
    $scope.message = dialog.options.data.message;
    $scope.helplink = dialog.options.data.helplink;
    $scope.stacktrace = dialog.options.data.stacktrace;

    $scope.close = function (result) {
        dialog.close(result);
    };
}