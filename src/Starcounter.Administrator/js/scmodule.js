/**
 * ----------------------------------------------------------------------------
 * sc.sqlquery
 * ----------------------------------------------------------------------------
 */
var sqlQueryModule = angular.module('sc.sqlquery', ['sc.sqlquery.service', 'sc.sqlquery.directive'], function ($routeProvider) {

    $routeProvider.when('/sql', {
        templateUrl: '/partials/sql.html',
        controller: 'SqlQueryCtrl'
    });

}).value('ui.config', {
    codemirror: {
        mode: 'text/x-mysql',
        lineNumbers: true,
        matchBrackets: true
    }
});

/**
 * Sql Query Controller
 */
sqlQueryModule.controller('SqlQueryCtrl', ['$scope', 'SqlQuery', function ($scope, SqlQuery) {

    $scope.alerts.length = 0;
    $scope.isBusy = false;

    $scope.selectedDatabaseName = null;
    $scope.sqlQuery = "";
    $scope.columns = [];
    $scope.rows = [];

    $scope.executeButtonTitle = function () {
        if ($scope.isBusy) {
            return "Executing...";
        }
        else {
            return "Execute";
        }
    }

    $scope.executeQuery = function () {

        $scope.isBusy = true;

        $scope.columns = [];
        $scope.rows = [];

        SqlQuery.send({ name: $scope.selectedDatabaseName }, $scope.sqlQuery, function (response, headers) {

            $scope.isBusy = false;

            // Success
            $scope.columns = response.columns;
            $scope.rows = response.rows;

            // Make all columns readonly
            for (var i = 0; i < $scope.columns.length ; i++) {
                $scope.columns[i].readOnly = true;
            }

            if (response.queryPlan != null) {
                $scope.queryPlan = response.queryPlan.replace(/\r\n/g, "<br>");  // Replace all occurrences of \r\n with the html tag <br>
                $scope.queryPlan = $scope.queryPlan.replace(/\t/g, "&emsp;");  // Replace all occurrences of \t with &emsp;
            }

            if (response.sqlException != null) {
                // Show message
                $scope.alerts.push({ type: 'error', msg: response.sqlException.message, helpLink: response.sqlException.helpLink });
            }

            if (response.exception != null) {
                $scope.showException(response.exception.message, response.exception.helpLink, response.exception.stackTrace);
            }

        }, function (response) {
            // Error
            $scope.isBusy = false;
            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
            }
            else {

                if (response.status == 404) {
                    // 404	Not Found
                    var message = "Could not execute the query on " + $scope.selectedDatabaseName + " database, Caused by a missing or not started database";
                    $scope.alerts.push({ type: 'error', msg: message });

                }
                else {
                    $scope._handleErrorReponse(response);
                }

            }
        });
    }


    // User clicked the "Execute" button
    $scope.btnExecute = function () {
        $scope.alerts.length = 0;
        $scope.executeQuery();
    }

    // Init
    $scope._RefreshDatabases(function () {
        // success
        for (var i = 0 ; i < $scope.databases.length ; i++) {
            if ($scope.databases[i].running == true) {
                $scope.selectedDatabaseName = $scope.databases[0].name;
                break;
            }
        }

    });

}]);


/**
 * sc.sqlquery.service
 */
angular.module('sc.sqlquery.service', ['ngResource'], function ($provide) {

    // OLDWAY TO EXECUT THE SQL QUERY
    //$provide.factory('SqlQuery', function ($resource) {
    //    return $resource('/api/admin/databases/:name/sql', { name: '@name' }, {
    //        send: { method: 'POST', isArray: false }    // We need to override this (the return type is not an array)
    //    });
    //});

    $provide.factory('SqlQuery', function ($resource) {
        return $resource('/__:name/sql', { name: '@name' }, {
            send: { method: 'POST', isArray: false }    // We need to override this (the return type is not an array)
        });
    });


});


/**
 * sc.sqlquery.directive
 */
angular.module('sc.sqlquery.directive', ['ui.config']);

/**
 * ----------------------------------------------------------------------------
 * scadmin
 * ----------------------------------------------------------------------------
 */
var adminModule = angular.module('scadmin', ['sc.sqlquery', 'ui', 'ui.bootstrap', 'uiHandsontable'], function ($routeProvider) {

    // List of all Databases
    $routeProvider.when('/databases', {
        templateUrl: '/partials/databases.html',
        controller: 'DatabasesCtrl'
    });

    // One Database
    $routeProvider.when('/databases/:name', {
        templateUrl: '/partials/database.html',
        controller: 'DatabaseCtrl'
    });

    // Edit database settings
    $routeProvider.when('/databases/:name/edit', {
        templateUrl: '/partials/database_edit.html',
        controller: 'DatabaseEditCtrl'
    });

    // Create database
    $routeProvider.when('/database_create', {
        templateUrl: '/partials/database_create.html',
        controller: 'DatabaseCreateCtrl'
    })

    // List of all Exectuables
    $routeProvider.when('/executables', {
        templateUrl: '/partials/executables.html',
        controller: 'ExecutablesCtrl'
    });

    // List of all Exectuables
    $routeProvider.when('/executable_start', {
        templateUrl: '/partials/executable_start.html',
        controller: 'ExecutableStartCtrl'
    });

    $routeProvider.when('/server_edit', {
        templateUrl: '/partials/server_edit.html',
        controller: 'ServerEditCtrl'
    });

    $routeProvider.when('/log', {
        templateUrl: '/partials/log.html',
        controller: 'LogCtrl'
    });

    $routeProvider.when('/gateway', {
        templateUrl: '/partials/gateway.html',
        controller: 'GatewayCtrl'
    });

    $routeProvider.otherwise({ redirectTo: '/executables' });

    //$locationProvider.html5Mode(true);

});

/**
 * Head Controller
 */
adminModule.controller('HeadCtrl', ['$scope', '$http', '$location', '$dialog', '$rootScope', function ($scope, $http, $location, $dialog, $rootScope) {

    $scope.alerts = [];
    $scope.jobs = [];           // { message:"default" }
    $scope.engines = [];        // { uri:"http://localhost:8181/api/engines/default", name:"default" }
    $scope.databases = [];      // { "uri":"http://headsutv19:8181/api/databases/default", name:"default", running:true, engineUri:"http://headsutv19:8181/api/engines/default" }
    $scope.executables = [];    // { path:"c:\tmp\some.exe", databaseName:"default" }

    $rootScope.$on("$routeChangeError", function (event, current, pervious, refection) {
        $scope.showNetworkDownError();
    });

    // Handles the active navbar item
    $scope.isActiveUrl = function (path) {
        return $location.path() == path;
    }

    // Close alert box
    $scope.closeAlert = function (index) {
        $scope.alerts.splice(index, 1);
    };

    // Show Exception dialog
    $scope.showException = function (message, helpLink, stackTrace) {

        $scope.opts = {
            backdrop: true,
            keyboard: true,
            backdropClick: true,
            templateUrl: "partials/error.html",
            controller: 'DialogCtrl',
            data: { header: "Internal Server Error", message: message, stackTrace: stackTrace, helpLink: helpLink }
        };

        var d = $dialog.dialog($scope.opts);
        d.open();
    }

    // Show Client Error dialog
    $scope.showClientError = function (message, helpLink, stackTrace) {
        $scope.showError("Client Error", message, helpLink, stackTrace);
    }

    // Show Server Error dialog
    $scope.showServerError = function (message, helpLink, stackTrace) {
        $scope.showError("Server Error", message, helpLink, stackTrace);
    }

    // Show Network down..
    $scope.showNetworkDownError = function (helpLink) {
        $scope.alerts.push({ type: 'error', msg: "The server is not responding or is not reachable.", helpLink: helpLink });
    }

    // Show Exception dialog
    $scope.showError = function (title, message, helpLink, stackTrace) {

        $scope.opts = {
            backdrop: true,
            keyboard: true,
            backdropClick: true,
            templateUrl: "partials/error.html",
            controller: 'DialogCtrl',
            data: { header: title, message: message, stackTrace: stackTrace, helpLink: helpLink }
        };

        var d = $dialog.dialog($scope.opts);
        d.open();
    }

    $scope.addJob = function (job) {
        $scope.jobs.push(job);
        return job;
    }

    $scope.removeJob = function (job) {

        // Remove job
        var index = $scope.jobs.indexOf(job);
        if (index != -1) {
            $scope.jobs.splice(index, 1);
        }
        return job;
    }

    // Get Execuables from all engines
    $scope._GetExecutables = function (readyCallback) {

        var executableList = [];

        $scope.engines.length = 0;

        $http.get('/api/engines').then(function (response) {
            // success handler
            if (response.data.hasOwnProperty("Engines") == true) {
                var engineList = response.data.Engines;

                var countDown = engineList.length;

                // Get Each engine
                for (var i = 0; i < engineList.length ; i++) {

                    var engine = engineList[i];
                    $scope.engines.push({ name: engine.Name, uri: engine.Uri });

                    $scope._GetEngineExecutables(engine.Name, function (list) {
                        countDown--;

                        for (var n = 0 ; n < list.length; n++) {
                            executableList.push(list[n]);
                        }

                        if (countDown == 0 && readyCallback != null) {
                            readyCallback(executableList);
                        }
                    });

                }

                if (engineList.length == 0 && readyCallback != null) {
                    readyCallback(executableList);
                }

            }
            else {
                $scope.showClientError("Unknown server response", null, "._GetExecutables() " + JSON.stringify(response.data));
                if (readyCallback != null) {
                    readyCallback(executables);
                }
            }

        }, function (response) {
            // error handler
            $scope._handleErrorReponse(response);
            if (readyCallback != null) {
                readyCallback(executableList);
            }
        });


    }

    // Get Running executables in one engine
    $scope._GetEngineExecutables = function (name, readyCallback) {

        var executables = [];

        $http.get('/api/engines/' + name).then(function (response) {
            // success handler

            if (response.data.hasOwnProperty("Executables") == true &&
                response.data.Executables.hasOwnProperty("Executing")) {

                var executablList = response.data.Executables.Executing;
                for (var n = 0 ; n < executablList.length ; n++) {
                    var executable = executablList[n];
                    executables.push({ path: executable.Path, databaseName: name });
                }

                if (readyCallback != null) {
                    readyCallback(executables);
                }

            }
            else {
                $scope.showClientError("Unknown server response", null, "._GetEngineExecutables() " + JSON.stringify(response.data));
            }


        }, function (response) {
            // error handler

            if (response.status == 404) {
                // No engine = no executables
            } else {
                $scope._handleErrorReponse(response);
            }

            if (readyCallback != null) {
                readyCallback(executables);
            }

        });

    }

    // Get the status of one database
    $scope._GetDatabaseStatus = function (name, readyCallback) {

        $http.get('/api/engines/' + name).then(function (response) {
            // success handler

            if (response.data.hasOwnProperty("DatabaseProcess") == true) {

                dbStatus = { uri: response.data.DatabaseProcess.Uri, running: response.data.DatabaseProcess.Running };

                if (readyCallback != null) {
                    readyCallback(dbStatus);
                }

            }
            else {
                $scope.showClientError("Unknown server response", null, "._GetEngineExecutables() " + JSON.stringify(response.data));
            }


        }, function (response) {
            // error handler

            if (response.status == 404) {
                // Database is not running
            }
            else {
                $scope._handleErrorReponse(response);
            }

            if (readyCallback != null) {
                readyCallback(null);
            }

        });

    }

    // Gets an database with status
    $scope._GetDatabase = function (name, readyCallback) {

        $http.get('/api/databases/' + name).then(function (response) {
            // success handler

            if (response.hasOwnProperty("data") == true && response.data.hasOwnProperty("Engine") == true) {

                var remoteDatabase = response.data;

                var relativeEngineUri = toRelativePath(remoteDatabase.Engine.Uri);

                var database = { name: remoteDatabase.Name, uri: remoteDatabase.Uri, running: false, engineUri: relativeEngineUri };

                // GET Database status
                $scope._GetDatabaseStatus(remoteDatabase.Name, function (status) {
                    // Ready
                    if (status != null) {
                        database.running = status.running;
                        database.uri = status.uri;
                    }

                    if (readyCallback != null) {
                        readyCallback(database);
                    }

                });

            }
            else {
                $scope.showClientError("Unknown server response", null, "._GetDatabase() " + JSON.stringify(response.data));
            }


        }, function (response) {
            // error handler
            $scope._handleErrorReponse(response);
            if (readyCallback != null) {
                readyCallback(null);
            }

        });

    }

    // Get all databases
    $scope._GetDatabases = function (readyCallback) {

        var databaseList = [];


        $http.get('/api/databases').then(function (response) {
            // success handler
            if (response.data.hasOwnProperty("Databases") == true) {
                var remoteDatabaseList = response.data.Databases;

                var countDown = remoteDatabaseList.length;

                // Get Each database
                for (var i = 0; i < remoteDatabaseList.length ; i++) {

                    $scope._GetDatabase(remoteDatabaseList[i].Name, function (database) {
                        countDown--;

                        databaseList.push(database);

                        if (countDown == 0 && readyCallback != null) {
                            readyCallback(databaseList);
                        }
                    });

                }

                if (remoteDatabaseList.length == 0 && readyCallback != null) {
                    readyCallback(databaseList);
                }

            }
            else {
                $scope.showClientError("Unknown server response", null, "._GetDatabases() " + JSON.stringify(response.data));
                if (readyCallback != null) {
                    readyCallback(databaseList);
                }
            }

        }, function (response) {
            // error handler
            $scope._handleErrorReponse(response);
            if (readyCallback != null) {
                readyCallback(databaseList);
            }
        });


    }

    // Gets an database with status
    $scope._GetServerSettings = function (name, successCallback, errorCallback) {

        $http.get('/api/admin/servers/' + name + '/settings').then(function (response) {
            // success handler

            if (response.hasOwnProperty("data") == true && response.data.hasOwnProperty("settings") == true) {

                if (successCallback != null) {
                    successCallback(response.data.settings);
                }
            }
            else {
                $scope.showClientError("Unknown server response", null, "._GetServerSettings() " + JSON.stringify(response.data));
            }

        }, function (response) {
            // error handler
            $scope._handleErrorReponse(response);
            if (errorCallback != null) {
                errorCallback(null);
            }

        });

    }

    // Verify database properties
    $scope._VerifyServerProperties = function (properties, successCallback, errorCallback) {

        $http.post('/api/admin/verify/serverproperties', properties).then(function (response) {
            // success handler

            if (response.hasOwnProperty("data") == true && response.data.hasOwnProperty("validationErrors") == true) {

                if (successCallback != null) {
                    successCallback(response.data.validationErrors);
                }

            }
            else {
                $scope.showClientError("Unknown server response", null, "._VerifyServerProperties() " + JSON.stringify(response));
                if (errorCallback != null) {
                    errorCallback();
                }
            }

        }, function (response) {
            // error handler
            $scope._handleErrorReponse(response);

            if (errorCallback != null) {
                errorCallback();
            }

        });

    }

    // Save database information
    $scope._SaveServerSettings = function (server, successCallback, errorCallback) {

        $http.put('/api/admin/servers/' + server.name + '/settings', server).then(function (response) {
            // success handler

            if (response.data.hasOwnProperty("message") == true) {
                $scope.alerts.push({ type: 'success', msg: response.data.message });
            }

            if (response.data.hasOwnProperty("settings") == true) {
                if (successCallback != null) {
                    successCallback(response.data.settings);
                }
            }
            else {
                $scope.showClientError("Unknown server response", null, "._SaveServerSettings() " + JSON.stringify(response.data));
                if (errorCallback != null) {
                    errorCallback();
                }
            }

        }, function (response) {
            // error handler
            $scope._handleErrorReponse(response);
            if (errorCallback != null) {
                errorCallback();
            }
        });

    }

    // Handle errors (Show user dialog/message)
    $scope._handleErrorReponse = function (response) {

        // error handler
        if (response instanceof SyntaxError) {
            $scope.showClientError(response.message, null, response.stack);
        }
        else if (response.status == 404) {
            // 404 Not Found

            if (response.data.hasOwnProperty("Text") == true) {
                $scope.showServerError(response.data.Text, response.data.Helplink);
            }
            else if (response.data.hasOwnProperty("message") == true) {
                $scope.showServerError(response.data.message, response.data.helpLink, response.data.stackTrace);
            }
            else {

                if (response.data == "") {
                    $scope.showServerError("404 Not found", null, null);
                }
                else {
                    $scope.showServerError(response.data, null, null);
                }
            }

        }
        else if (response.status == 405) {
            // 405 Method Not Allowed
            if (response.data.hasOwnProperty("Allow") == true) {
                $scope.showClientError("Allowed methods are " + response.data.Allow, response.data.Helplink);
            }
            else {
                $scope.showClientError(response.data, null, null);
            }

        }
        else if (response.status == 409) {
            // 409 Conflict
            if (response.data.hasOwnProperty("Text") == true) {
                $scope.showServerError(response.data.Text, response.data.Helplink);
            }
            else {
                $scope.showServerError(response.data, null, null);
            }

        }
        else if (response.status == 500) {

            if (response.data.hasOwnProperty("exception") == true) {
                $scope.showServerError(response.data.exception.message, response.data.exception.helpLink, response.data.exception.stackTrace);
            }
            else {
                if (response.data.hasOwnProperty("message") == true) {
                    $scope.showServerError(response.data.message, response.data.helpLink, response.data.stackTrace);
                }
                else if (response.data.hasOwnProperty("Text") == true) {
                    $scope.showServerError(response.data.Text, response.data.Helplink);
                }
                else {
                    $scope.showServerError(response.data, null, null);
                }
            }

        }
        else if (response.status == 0) {
            $scope.showNetworkDownError();
        }
        else {
            $scope.showClientError("Unhandled http statuscode " + response.status, null, null);
        }

    }

    $scope._RefreshExecutables = function (readyCallback) {

        $scope._GetExecutables(function (executables) {

            $scope.executables.length = 0;
            $scope.executables = executables;

            if (readyCallback != null) {
                readyCallback();
            }
        });

    }

    $scope._RefreshDatabases = function (readyCallback) {

        $scope._GetDatabases(function (databases) {

            $scope.databases.length = 0;
            $scope.databases = databases;

            if (readyCallback != null) {
                readyCallback();
            }
        });


    }

    // Stop the executable's engine code host (All executables running in the database will be stopped)
    $scope._StopAllExecutables = function (engine, readyCallback) {

        $http.delete('/api/engines/' + engine.name + '/host').then(function (response) {
            // success handler

            if (response.hasOwnProperty("data") == true) {

                if (readyCallback != null) {
                    readyCallback();
                }
            }
            else {
                $scope.showClientError("Unknown server response", null, "._StopAllExecutables() " + JSON.stringify(response.data));
            }


        }, function (response) {
            // error handler
            $scope._handleErrorReponse(response);
            if (readyCallback != null) {
                readyCallback();
            }

        });


    }

    // Start engine if it's not running
    $scope._StartEngine = function (engineData, successCallback, errorCallback) {

        $http.post('/api/engines', engineData).then(function (response) {
            // success handler

            // TODO: Refresh engines..?

            if (response.hasOwnProperty("data") == true) {
                if (successCallback != null) {
                    successCallback();
                }
            }
            else {
                $scope.showClientError("Unknown server response", null, "._StartEngine() " + JSON.stringify(response));
                if (errorCallback != null) {
                    errorCallback();
                }
            }

        }, function (response) {
            // error handler
            $scope._handleErrorReponse(response);
            if (errorCallback != null) {
                errorCallback();
            }

        });

    }

    // Start Executable
    $scope._StartExecutable = function (databaseName, data, successCallback, errorCallback) {

        var bodyData = { Path: data.file, StartedBy: data.startedBy, Arguments: data.arguments };

        $http.post('/api/engines/' + databaseName + '/executables', bodyData).then(function (response) {
            // success handler

            // TODO: Refresh engines..?

            if (response.hasOwnProperty("data") == true) {

                $scope.alerts.push({ type: 'success', msg: response.data.Description }); // TODO Move to successcallback

                if (successCallback != null) {
                    successCallback();
                }
            }
            else {
                $scope.showClientError("Unknown server response", null, "._StartExecutable() " + JSON.stringify(response));
                if (errorCallback != null) {
                    errorCallback();
                }
            }

        }, function (response) {
            // error handler

            if (response.status == 409) {
                // 409 Conflict (Already running)
                if (response.data.hasOwnProperty("Text") == true) {
                    $scope.alerts.push({ type: 'info', msg: response.data.Text, helpLink: response.data.Helplink }); // TODO Move to successcallback
                }
                else {
                    $scope._handleErrorReponse(response);
                }
            }
            else if (response.status == 422) {
                // 422 Unprocessable Entity (WebDAV; RFC 4918)
                if (response.data.hasOwnProperty("Text") == true) {
                    $scope.alerts.push({ type: 'error', msg: response.data.Text, helpLink: response.data.Helplink });
                }
                else {
                    $scope._handleErrorReponse(response);
                }
            }
            else {
                $scope._handleErrorReponse(response);
            }

            if (errorCallback != null) {
                errorCallback();
            }

        });
    }

    // Start database
    $scope._startDatabase = function (database, successCallback, errorCallback) {

        $http.post('/api/engines', { Name: database.name }).then(function (response) {
            // success handler

            if (response.hasOwnProperty("data") == true) {

                $scope._RefreshDatabases(function () {
                    if (successCallback != null) {
                        successCallback();
                    }
                });

            }
            else {
                $scope.showClientError("Unknown server response", null, "._startDatabase() " + JSON.stringify(response));
                if (errorCallback != null) {
                    errorCallback();
                }
            }

        }, function (response) {
            // error handler
            if (response.status == 422) {
                // 409 Conflict
                if (response.data.hasOwnProperty("Text") == true) {
                    $scope.alerts.push({ type: 'error', msg: response.data.Text, helpLink: response.data.HelpLink });
                }
                else {
                    $scope._handleErrorReponse(response);
                }

            }
            else {
                $scope._handleErrorReponse(response);
            }

            if (errorCallback != null) {
                errorCallback();
            }

        });

    }

    // Stop database
    $scope._stopDatabase = function (database, successCallback, errorCallback) {

        $http.delete(database.engineUri, { Name: name }).then(function (response) {
            // success handler

            if (response.hasOwnProperty("data") == true) {

                $scope._RefreshDatabases(function () {
                    if (successCallback != null) {
                        successCallback();
                    }
                });

            }
            else {
                $scope.showClientError("Unknown server response", null, "._stoptDatabase() " + JSON.stringify(response));
                if (errorCallback != null) {
                    errorCallback();
                }
            }

        }, function (response) {
            // error handler
            if (response.status == 422) {
                // 409 Conflict
                if (response.data.hasOwnProperty("Text") == true) {
                    $scope.alerts.push({ type: 'error', msg: response.data.Text, helpLink: response.data.HelpLink });
                }
                else {
                    $scope._handleErrorReponse(response);
                }

            }
            else {
                $scope._handleErrorReponse(response);
            }

            if (errorCallback != null) {
                errorCallback();
            }

        });

    }

    // Verify database properties
    $scope._VerifyDatabaseProperties = function (properties, successCallback, errorCallback) {

        $http.post('/api/admin/verify/databaseproperties', properties).then(function (response) {
            // success handler

            if (response.hasOwnProperty("data") == true && response.data.hasOwnProperty("validationErrors") == true) {

                if (successCallback != null) {
                    successCallback(response.data.validationErrors);
                }

            }
            else {
                $scope.showClientError("Unknown server response", null, "._VerifyDatabaseProperties() " + JSON.stringify(response));
                if (errorCallback != null) {
                    errorCallback();
                }
            }

        }, function (response) {
            // error handler
            $scope._handleErrorReponse(response);

            if (errorCallback != null) {
                errorCallback();
            }

        });

    }

    // Save database information
    $scope._SaveDatabaseSettings = function (database, successCallback, errorCallback) {

        $http.put('/api/admin/databases/' + database.name + '/settings', database).then(function (response) {
            // success handler

            if (response.data.hasOwnProperty("message") == true) {
                $scope.alerts.push({ type: 'success', msg: response.data.message });
            }

            if (response.data.hasOwnProperty("settings") == true) {
                if (successCallback != null) {
                    successCallback(response.data.settings);
                }
            }
            else {
                $scope.showClientError("Unknown server response", null, "._SaveDatabaseSettings() " + JSON.stringify(response.data));
                if (errorCallback != null) {
                    errorCallback();
                }
            }

        }, function (response) {
            // error handler
            $scope._handleErrorReponse(response);
            if (errorCallback != null) {
                errorCallback();
            }
        });



    }

    // Gets an database with status
    $scope._GetDefaultDatabaseSettings = function (successCallback, errorCallback) {

        $http.get('/api/admin/settings/database').then(function (response) {
            // success handler

            if (response.hasOwnProperty("data") == true && response.data.hasOwnProperty("settings") == true) {

                if (successCallback != null) {
                    successCallback(response.data.settings);
                }
            }
            else {
                $scope.showClientError("Unknown server response", null, "._GetDefaultDatabaseSettings() " + JSON.stringify(response.data));
            }

        }, function (response) {
            // error handler
            $scope._handleErrorReponse(response);
            if (errorCallback != null) {
                errorCallback(null);
            }

        });

    }

    // Gets an database with status
    $scope._GetDatabaseSettings = function (name, successCallback, errorCallback) {

        $http.get('/api/admin/databases/' + name + '/settings').then(function (response) {
            // success handler

            if (response.hasOwnProperty("data") == true && response.data.hasOwnProperty("settings") == true) {

                if (successCallback != null) {
                    successCallback(response.data.settings);
                }
            }
            else {
                $scope.showClientError("Unknown server response", null, "._GetDatabaseSettings() " + JSON.stringify(response.data));
            }

        }, function (response) {
            // error handler
            $scope._handleErrorReponse(response);
            if (errorCallback != null) {
                errorCallback(null);
            }

        });

    }


    // Create database properties
    //$scope._CreateDatabaseNEWAPI = function (properties, successCallback, errorCallback) {

    //    var data = { Name: properties.name };

    //    $http.post('/api/databases', data).then(function (response) {
    //        // success handler
    //        if (successCallback != null) {
    //            successCallback(response.data.validationErrors);
    //        }

    //    }, function (response) {
    //        // error handler

    //        if (response.status == 422) {
    //            // 422 Unprocessable Entity (WebDAV; RFC 4918)
    //            // The request was well-formed but was unable to be followed due to semantic errors
    //            if (response.data.hasOwnProperty("Text") == true) {
    //                $scope.alerts.push({ type: 'error', msg: response.data.Text, helpLink: response.data.HelpLink });
    //            }
    //            else {
    //                $scope._handleErrorReponse(response);
    //            }

    //        }
    //        else {
    //            $scope._handleErrorReponse(response);
    //        }

    //        if (errorCallback != null) {
    //            errorCallback();
    //        }

    //    });

    //}

    // Verify database properties
    $scope._CreateDatabase = function (settings, successCallback, errorCallback) {

        $http.post('/api/admin/databases/' + settings.name + '/createdatabase', settings).then(function (response) {
            // success handler

            if (response.data.hasOwnProperty("errors") == true) {
                for (var i = 0; i < response.data.errors.length; i++) {
                    $scope.alerts.push({ type: 'error', msg: response.data.errors[i].message, helpLink: response.data.errors[i].helplink });
                }
                if (errorCallback != null) {
                    errorCallback();
                }

            }
            else {
                if (successCallback != null) {
                    successCallback();
                }

            }

        }, function (response) {
            // error handler

            if (response.status == 422) {
                // 422 Unprocessable Entity (WebDAV; RFC 4918)
                // The request was well-formed but was unable to be followed due to semantic errors
                if (response.data.hasOwnProperty("Text") == true) {
                    $scope.alerts.push({ type: 'error', msg: response.data.Text, helpLink: response.data.HelpLink });
                }
                else {
                    $scope._handleErrorReponse(response);
                }

            }
            else {
                $scope._handleErrorReponse(response);
            }

            if (errorCallback != null) {
                errorCallback();
            }

        });

    }


    // Get console output for one database
    $scope._GetConsole = function (database, successCallback, errorCallback) {


        // OLDWAY TO GET THE CONSOLE
        //$http.get('/api/admin/databases/' + database.name + '/console').then(function (response) {
        $http.get('/__' + database.name + '/console').then(function (response) {
            // success handler

            if (response.hasOwnProperty("data") == true && response.data.hasOwnProperty("console") == true) {

                if (successCallback != null) {
                    successCallback(response.data.console);
                }

            }
            else {
                $scope.showClientError("Unknown server response", null, "._GetConsole() " + JSON.stringify(response));
                if (errorCallback != null) {
                    errorCallback();
                }
            }

        }, function (response) {
            // error handler

            if (response.status == 404) {
                // 404	Not Found
                var message = "Could not retrive the console output from the " + database.name + " database, Caused by a missing/not started database or there is no Executable running in the database";
                $scope.alerts.push({ type: 'error', msg: message });
            }
            else {
                $scope._handleErrorReponse(response);
            }

            if (errorCallback != null) {
                errorCallback();
            }

        });

    }

}]);


/**
 * Executables Controller
 */
adminModule.controller('ExecutablesCtrl', ['$scope', '$routeParams', '$dialog', function ($scope, $routeParams, $dialog) {

    $scope.alerts.length = 0;

    $scope.stopAllExecutables = function (engine) {
        var job = $scope.addJob({ message: "Stopping all executables running in database " + engine.name });
        $scope._StopAllExecutables(engine, function () {

            $scope._RefreshExecutables(function () {
                // Done
                $scope.isBusy = false;

                // Remove job
                $scope.removeJob(job);

                // Refresh database statuses
                $scope._RefreshDatabases();


            });
        });
    }

    $scope.btnStopAllExecutable = function (engine) {

        $scope.alerts.length = 0;

        var title = 'Stop all running executable';
        var msg = 'Do you want to stop all executable running in database ' + engine.name;
        var btns = [{ result: 0, label: 'Stop', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];

        $dialog.messageBox(title, msg, btns)
          .open()
          .then(function (result) {
              if (result == 0) {
                  $scope.stopAllExecutables(engine);
              }
          });
    }

    // Init
    $scope.isBusy = true;
    $scope._RefreshExecutables(function () {
        // Ready
        $scope.isBusy = false;

    });


}]);


/**
 * Executable Start Controller
 */
adminModule.controller('ExecutableStartCtrl', ['$scope', '$routeParams', '$location', function ($scope, $routeParams, $location) {

    $scope.alerts.length = 0;
    $scope.selectedDatabaseName = null;

    $scope.file = "";
    $scope.recentExecutables = [];

    $scope.prepareExecutable = function (job, engineName, successCallback, errorCallback) {

        var engineData = { Name: engineName, NoDb: false, LogSteps: false };    // TODO: get NoDb and LogSteps from argumens

        job.message = "Starting Executable in database " + engineName;

        $scope._StartEngine(engineData, function () {
            // Success 

            // TODO:
            var startedBy = "unknown-user@unknown-computer (via webadmin)";
            var arguments = []

            var data = { engineName: engineName, file: $scope.file, startedBy: startedBy, arguments: arguments };

            $scope._StartExecutable(engineName, data, function (message) {
                // success
                if (successCallback != null) {
                    successCallback();
                }

            }, function () {
                // Error
                if (errorCallback != null) {
                    errorCallback();
                }
            });


        }, function () {
            // Error 
            if (errorCallback != null) {
                errorCallback();
            }

        });

    }

    $scope.btn_startExecutable = function () {

        // Clear any previous alerts
        $scope.alerts.length = 0;

        var job = $scope.addJob({ message: "Starting Executable" });

        $scope.prepareExecutable(job, $scope.selectedDatabaseName, function () {
            // success

            // Remember successfully started executables
            $scope.rememberRecentFile($scope.file);

            $scope._RefreshDatabases();
            $scope._RefreshExecutables();

            // Remove job
            $scope.removeJob(job);

        }, function () {
            // Error 
            // Remove job
            $scope.removeJob(job);
        });
    }

    $scope.btn_SetCurrent = function (file) {
        $scope.file = file.name;
    }

    $scope.rememberRecentFile = function (filename) {

        var maxItems = 5;
        // Check if file is already 'rememberd'
        for (var i = 0; i < $scope.recentExecutables.length ; i++) {

            // File already rememberd
            if (filename == $scope.recentExecutables[i].name) {
                return;
            }

        }
        $scope.recentExecutables.unshift({ name: filename });

        var toMany = $scope.recentExecutables.length - maxItems;

        if (toMany > 0) {
            $scope.recentExecutables.splice(maxItems, toMany);
        }

        localStorage.recentExecutables = JSON.stringify($scope.recentExecutables);
    }

    $scope.getRecentExecutables = function () {
        if (typeof (Storage) !== "undefined") {
            if (localStorage.recentExecutables != null) {
                $scope.recentExecutables = JSON.parse(localStorage.recentExecutables);
            }
        }
        else {
            // No web storage support..
        }
    }

    // Init
    $scope.getRecentExecutables();

    $scope._RefreshDatabases(function () {
        if ($scope.databases.length > 0) {
            $scope.selectedDatabaseName = $scope.databases[0].name;
        }
    });

}]);


/**
 * Databases Controller
 */
adminModule.controller('DatabasesCtrl', ['$scope', '$dialog', '$http', function ($scope, $dialog, $http) {

    $scope.alerts.length = 0;

    $scope.startDatabase = function (database) {

        var job = $scope.addJob({ message: "Starting database " + database.name });

        $scope._startDatabase(database, function () {
            // Success
            $scope.removeJob(job);
            $scope.alerts.push({ type: 'success', msg: "Database " + database.name + " was started" });
        }, function () {
            // Error
            $scope.removeJob(job);
        });

    }

    $scope.stopDatabase = function (database) {

        var job = $scope.addJob({ message: "Stopping database " + database.name });

        $scope._stopDatabase(database, function () {
            // Success
            $scope.removeJob(job);
            $scope.alerts.push({ type: 'success', msg: "Database " + database.name + " was stopped" });
        }, function () {
            // Error
            $scope.removeJob(job);
        });

    }

    $scope.btnDeleteDatabase = function (database) {

        $scope.alerts.length = 0;

        var title = 'Stop engine';
        var msg = 'Do you want to delete the database ' + database.name;
        var btns = [{ result: 0, label: 'Delete', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];


        $dialog.messageBox(title, msg, btns)
          .open()
          .then(function (result) {
              if (result == 0) {
                  $scope.deleteDatabase(database);
              }
          });


    }

    $scope.btnStopDatabase = function (database) {

        $scope.alerts.length = 0;

        var title = 'Stop database';
        var msg = 'Do you want to stop the database ' + database.name;
        var btns = [{ result: 0, label: 'Stop', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];

        $dialog.messageBox(title, msg, btns)
          .open()
          .then(function (result) {
              if (result == 0) {
                  $scope.stopDatabase(database);
              }
          });


    }

    $scope.btnStartDatabase = function (database) {

        $scope.alerts.length = 0;

        var title = 'Start database';
        var msg = 'Do you want to start the database ' + database.name;
        var btns = [{ result: 0, label: 'Start', cssClass: 'btn-success' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];

        $dialog.messageBox(title, msg, btns)
          .open()
          .then(function (result) {
              if (result == 0) {
                  $scope.startDatabase(database);
              }
          });


    }

    // Init
    $scope._RefreshDatabases();

}]);


/**
 * Database Controller
 */
adminModule.controller('DatabaseCtrl', ['$scope', '$routeParams', function ($scope, $routeParams) {

    $scope.alerts.length = 0;
    $scope.isExecutableRunning = false;

    // Retrive the console output for a specific database
    $scope.getConsole = function (database) {

        $scope._GetConsole(database, function (text) {
            // Success

            if (text == null) {
                $scope.console = "";;
            }
            else {
                $scope.console = text.replace(/\r\n/g, "<br>");
            }

            $("#console").scrollTop($("#console")[0].scrollHeight); // TODO: Do this in the next cycle?

        }, function () {
            // Error
        });

    }

    // User clicked the "Refresh" button
    $scope.btnClick_refreshConsole = function () {
        $scope.alerts.length = 0;

        $scope.getConsole($scope.database);

    }

    $scope.checkIfRunningExecutables = function (database, successCallback) {

        $scope.isExecutableRunning = false;

        $scope.refreshEnginesList(function () {
            // Success

            var engine = $scope.getEngine(database.name);
            if (engine != null) {

                $scope.getEngineExecutableList(engine, function (executable) {
                    // Success

                    $scope.isExecutableRunning = executable.length > 0;

                    if (successCallback != null) {
                        successCallback();
                    }

                }, function () {
                    // Error
                });

            }
            else {
                // Error can get engine, TODO: Refresh engine list and retry
            }



        }, function () {
            // Error
            // could not retrive the engineslist
        });

    }

    $scope.tryGetConsole = function (name) {

        $scope.isExecutableRunning = false;

        // Init
        $scope.getDatabaseWithConfiguration(name, function (database) {
            // Success

            $scope.database = database;
            $scope.refreshDatabaseProcessStatus(database, function () {
                // Success
                if (database.running) {

                    // TODO: Check if there is any executables running in the database.
                    $scope.checkIfRunningExecutables(database, function () {
                        // Success
                        $scope.getConsole(database.name);
                    });

                }
            }, function () {
                // Error
            });


        }, function () {
            // Error
        });


    }

    // Init
    $scope.isBusy = true;
    $scope._RefreshDatabases(function () {
        // Ready
        for (var i = 0 ; i < $scope.databases.length ; i++) {
            if ($scope.databases[i].name == $routeParams.name) {
                $scope.database = $scope.databases[i];
                break;

            }
        }
        $scope.isBusy = false;

    });

    // Console fixe the height.
    var $window = $(window);
    $scope.winHeight = $window.height();
    $scope.winWidth = $window.width();
    $window.resize(function () {
        $scope.winHeight = $window.height();
        $scope.winWidth = $window.width();
        $scope.$apply();
    });

    $scope.style = function () {
        return {
            'height': ($scope.calcHeight()) + 'px',
            'width': + '100%'
        };
    }

    $scope.calcHeight = function () {
        var border = 12;
        var topOffset = $("#console").offset().top;
        var height = $scope.winHeight - topOffset - 2 * border;
        if (height < 150) {
            return 150;
        }
        return height;
    };

    $scope.calcWidth = function () {
        var border = 12;
        var leftOffset = $("#console").offset().left;
        var width = $scope.winWidth - leftOffset - 2 * border;
        if (width < 150) {
            return 150;
        }
        return width;
    };

}]);


/**
 * Database Edit Controller
 */
adminModule.controller('DatabaseEditCtrl', ['$scope', '$routeParams', '$http', function ($scope, $routeParams, $http) {

    $scope.alerts.length = 0;
    $scope.settings = null;

    // Refresh database settings
    $scope.refreshSettings = function () {

        $scope._GetDatabaseSettings($routeParams.name, function (settings) {
            // Success
            $scope.settings = settings;
            $scope.myForm.$setPristine(); // This disent work, the <select> breaks the pristine state :-(

        }, function () {
            // Error
            $scope.settings = null;
        });

    }

    // Save database settings
    $scope.saveSettings = function (name) {
        $scope.isBusy = true;

        $http({ method: 'PUT', url: ' /api/admin/databases/databases/' + $scope.database.name, data: $scope.database }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {
              $scope.isBusy = false;

              if (status == 200) {
                  // 200 Ok
                  $scope.isBusy = true;

                  if (response.message != null) {
                      $scope.alerts.push({ type: 'success', msg: response.message });
                  }

                  $scope.database = response.database;

                  if (response.database == null) {
                      $scope.showException("Invalid response, database property was null", null, ".saveSettings()");
                  }
                  else {
                      $scope.myForm.$setPristine();
                  }

              }
              else {
                  $scope.showException("Unhandled http statuscode " + status, null, ".saveSettings()");
              }

          }).
          error(function (response, status, headers, config) {
              $scope.isBusy = false;

              if (status == 403) {
                  // 403 Forbidden (Validation Error)
                  if (response.validationErrors != null && response.validationErrors.length > 0) {

                      for (var i = 0; i < response.validationErrors.length; i++) {
                          $scope.alerts.push({ type: 'error', msg: response.validationErrors[i].message });
                          $scope.myForm[response.validationErrors[i].property].$setValidity("validationError", false);
                          var id = response.validationErrors[i].property;
                          var unregister = $scope.$watch("settings." + response.validationErrors[i].property, function (newValue, oldValue) {
                              if (newValue == oldValue) return;
                              $scope.myForm[id].$setValidity("validationError", true);
                              unregister();
                          }, false);

                      }
                  }
                  else {
                      $scope.showException("The return code '403 Forbidden' did not return any validation error fields.", null, null);
                  }

              }
              else if (status == 500) {
                  // 500 Internal Server Error
                  $scope.showException(response.message, response.helpLink, response.stackTrace);
              }
              else {
                  $scope.showException("Unhandled http statuscode " + status, null, ".createDatabase()");
              }

          });
    }

    $scope.btn_SaveSettings = function () {
        $scope.alerts.length = 0;

        var job = $scope.addJob({ message: "Verifying properties" });

        $scope._VerifyDatabaseProperties($scope.settings, function (validationErrors) {
            // Validation done

            job.message = "Saving settings " + $scope.settings.name;

            if (validationErrors.length == 0) {
                // No validation errors, goahead creating database

                $scope._SaveDatabaseSettings($scope.settings, function () {
                    // Success
                    $scope.removeJob(job);
                    $scope.myForm.$setPristine();
                }, function () {
                    $scope.removeJob(job);
                    // Error
                });
            }
            else {

                $scope.removeJob(job);

                // Show errors on screen
                for (var i = 0; i < validationErrors.length; i++) {
                    $scope.alerts.push({ type: 'error', msg: validationErrors[i].message });
                    $scope.myForm[validationErrors[i].property].$setValidity("validationError", false);
                    var id = validationErrors[i].property;
                    var unregister = $scope.$watch("settings." + validationErrors[i].property, function (newValue, oldValue) {
                        if (newValue == oldValue) return;
                        $scope.myForm[id].$setValidity("validationError", true);
                        unregister();
                    }, false);

                }

            }


        }, function () {
            //
            $scope.removeJob(job);

        });


        //$scope.createDatabase();
    }

    $scope.btn_ResetSettings = function () {
        $scope.alerts.length = 0;
        $scope.refreshSettings();
    }

    // Init
    $scope.refreshSettings();


}]);


/**
 * Database Create Controller
 */
adminModule.controller('DatabaseCreateCtrl', ['$scope', '$http', function ($scope, $http) {

    $scope.isBusy = false;
    $scope.alerts.length = 0;

    // Refresh database settings
    $scope.refreshSettings = function () {

        $scope._GetDefaultDatabaseSettings(function (settings) {
            // Success

            settings.tempDirectory = settings.tempDirectory.replace("[DatabaseName]", settings.name);
            settings.imageDirectory = settings.imageDirectory.replace("[DatabaseName]", settings.name);
            settings.transactionLogDirectory = settings.transactionLogDirectory.replace("[DatabaseName]", settings.name);
            settings.dumpDirectory = settings.dumpDirectory.replace("[DatabaseName]", settings.name);

            $scope.settings = settings;
            $scope.myForm.$setPristine(); // This disent work, the <select> breaks the pristine state :-(

        }, function () {
            // Error
            $scope.settings = null;
        });

    }

    $scope.btn_CreateDatabase = function () {

        $scope.alerts.length = 0;
        var job = $scope.addJob({ message: "Verifying properties" });

        $scope._VerifyDatabaseProperties($scope.settings, function (validationErrors) {
            // Validation done

            job.message = "Creating database " + $scope.settings.name;

            if (validationErrors.length == 0) {
                // No validation errors, goahead creating database

                $scope._CreateDatabase($scope.settings, function () {
                    // Success
                    $scope.removeJob(job);
                    $scope.alerts.push({ type: 'success', msg: "Database " + $scope.settings.name + " was created." });

                }, function () {
                    $scope.removeJob(job);
                    // Error
                });
            }
            else {

                $scope.removeJob(job);

                // Show errors on screen
                for (var i = 0; i < validationErrors.length; i++) {
                    //$scope.alerts.push({ type: 'error', msg: validationErrors[i].message });
                    $scope.myForm[validationErrors[i].property].$setValidity("validationError", false);
                    var id = validationErrors[i].property;
                    var unregister = $scope.$watch("settings." + validationErrors[i].property, function (newValue, oldValue) {
                        if (newValue == oldValue) return;
                        $scope.myForm[id].$setValidity("validationError", true);
                        unregister();
                    }, false);

                }


            }


        }, function () {
            //
            $scope.removeJob(job);

        });


        //$scope.createDatabase();
    }

    $scope.btn_ResetSettings = function () {

        $scope.alerts.length = 0;
        $scope.refreshSettings();

    }

    // init
    $scope.refreshSettings();



}]);


/**
 * Server Edit Controller
 */
adminModule.controller('ServerEditCtrl', ['$scope', '$http', function ($scope, $http) {

    $scope.alerts.length = 0;
    $scope.settings = null;

    $scope.refreshSettings = function () {

        $scope._GetServerSettings("personal", function (settings) {
            // Success
            $scope.settings = settings;
            $scope.myForm.$setPristine(); // This disent work, the <select> breaks the pristine state :-(

        }, function () {
            // Error
            $scope.settings = null;
        });

    }

    $scope.btn_SaveSettings = function () {

        $scope.alerts.length = 0;

        var job = $scope.addJob({ message: "Verifying properties" });

        $scope._VerifyServerProperties($scope.settings, function (validationErrors) {
            // Validation done

            job.message = "Saving settings " + $scope.settings.name;

            if (validationErrors.length == 0) {
                // No validation errors, goahead creating database

                $scope._SaveServerSettings($scope.settings, function () {
                    // Success
                    $scope.removeJob(job);
                    $scope.myForm.$setPristine();
                }, function () {
                    // Error
                    $scope.removeJob(job);
                });


            }
            else {

                $scope.removeJob(job);

                // Show errors on screen
                for (var i = 0; i < validationErrors.length; i++) {
                    $scope.alerts.push({ type: 'error', msg: validationErrors[i].message });
                    $scope.myForm[validationErrors[i].property].$setValidity("validationError", false);
                    var id = validationErrors[i].property;
                    var unregister = $scope.$watch("settings." + validationErrors[i].property, function (newValue, oldValue) {
                        if (newValue == oldValue) return;
                        $scope.myForm[id].$setValidity("validationError", true);
                        unregister();
                    }, false);

                }


            }


        }, function () {
            //
            $scope.removeJob(job);

        });

    }

    $scope.btn_ResetSettings = function () {
        $scope.alerts.length = 0;
        $scope.refreshSettings();
    }

    // Init
    $scope.refreshSettings();
}]);


/**
 * Gateway Controller
 */
adminModule.controller('GatewayCtrl', ['$scope', '$http', function ($scope, $http) {

    $scope.alerts.length = 0;

    // Get Gateway information
    $scope.refreshGatewayStats = function () {

        $http.get('/gwstats').then(function (response) {
            // success handler
            $scope.gwStats = response.data;

        }, function (response) {
            // error handler
            $scope.gwStats = "";
            $scope._handleErrorReponse(response);
        });

    }

    // User clicked the "Refresh" button
    $scope.btnRefresh = function () {
        $scope.alerts.length = 0;
        $scope.refreshGatewayStats();
    }

    // Init
    $scope.refreshGatewayStats();

}]);


/**
 * Log Controller
 */
adminModule.controller('LogCtrl', ['$scope', '$http', function ($scope, $http) {

    $scope.alerts.length = 0;
    $scope.log = {};
    $scope.log.LogEntries = [];

    $scope.filterModel = {
        debug: false,
        notice: false,
        warning: true,
        error: true
    };

    // Watch for changes in the filer
    $scope.$watch('filterModel', function () {
        $scope.getLog();
    }, true);

    // Retrive log information
    $scope.getLog = function () {

        $http.get('/api/admin/log').then(function (response) {
            // success handler
            $scope.log = response.data;

        }, function (response) {
            // error handler
            $scope.log = "";
            $scope._handleErrorReponse(response);
        });

    }

    $scope.btnRefresh = function () {
        $scope.alerts.length = 0;
        $scope.getLog();
    }

    // Init 
    // Handsontable (fixed the height)
    var $window = $(window);
    var winHeight = $window.height();
    var winWidth = $window.width();
    $window.resize(function () {
        winHeight = $window.height();
        winWidth = $window.width();
    });

    $scope.calcHeight = function () {
        var border = 12;
        var topOffset = $("#handsontable").offset().top;
        var height = winHeight - topOffset - 2 * border;
        if (height < 50) {
            return 50;
        }
        return height;
    };

    $scope.calcWidth = function () {
        var border = 12;
        var leftOffset = $("#handsontable").offset().left;
        var width = winWidth - leftOffset - 2 * border;
        if (width < 50) {
            return 50;
        }
        return width;
    };

}]);


/**
 * Dialog Controller
 */
adminModule.controller('DialogCtrl', ['$scope', 'dialog', function ($scope, dialog) {

    $scope.header = dialog.options.data.header;
    $scope.message = dialog.options.data.message;
    $scope.helpLink = dialog.options.data.helpLink;
    $scope.stackTrace = dialog.options.data.stackTrace;

    $scope.close = function (result) {
        dialog.close(result);
    };
   
}]);

/**
 * Retrives the relative path of an url
 * Example:
 * Input: http://localhost:8080/foo/bar?123
 * Output: /foo/bar
 */
function toRelativePath(url) {
    var a = document.createElement('a');
    a.href = url;
    return a.pathname;
}
