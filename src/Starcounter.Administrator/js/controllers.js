
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

    $routeProvider.when('/create_database', {
        templateUrl: '/partials/create_database.html',
        controller: CreateDatabaseCtrl
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
function MainCtrl($scope, $location, $rootScope, $dialog) {

    $rootScope.alerts = [];


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
            controller: 'DialogController',
            data: { header: "Internal Server Error", message: message, stackTrace: stackTrace, helpLink: helpLink }
        };

        var d = $dialog.dialog($scope.opts);
        d.open();
    }

}


/**
 * Server Controller
 */
function ServerCtrl($scope, $dialog, Server, $http) {

    $scope.isBusy = false;

    // Retrive server information
    $scope.getServer = function () {

        Server.get({}, function (server) {
            // Success
            $scope.server = server;
        }, function (response) {
            // Error, Can not retrive list of databases
            var message = "Can not retrive the server properties";

            // 500 Internal Server Error
            if (response.status === 500) {
                $scope.showException(message, null, response.data);
            }
            else {
                $scope.alerts.push({ type: 'error', msg: message });
            }
        });

    }

    // User clicked the "Refresh" button
    $scope.btnClick_refresh_gwateway_stats = function () {
        $scope.getGatewayStats();
    }

    // Get Gateway information
    $scope.getGatewayStats = function () {

        $scope.alerts.length = 0;
        $scope.isBusy = true;

        $http({ method: 'GET', url: '/gwstats', headers: { 'Accept': 'text/html,text/plain,*/*' } }).
          success(function (data, status, headers, config) {
              $scope.isBusy = false;
              $scope.gwStats = data;
          }).
          error(function (data, status, headers, config) {
              $scope.isBusy = false;
              $scope.gwStats = "";

              var message = "Can not retrive the gateway statistics";

              // 500 Internal Server Error
              if (response.status === 500) {
                  $scope.showException(message, null, response.data);
              }
              else {
                  $scope.alerts.push({ type: 'error', msg: message });
              }
          });
    }


    $scope.getServer();

    $scope.getGatewayStats();

}


/**
 * Databases Controller
 */
function DatabasesCtrl($scope, $dialog, Database) {

    // Retrive all databases
    $scope.getDatabases = function () {

        Database.query(function (databases) {
            $scope.databases = databases.DatabaseList;
        }, function (response) {
            // Error, Can not retrive list of databases
            var message = "Can not retrive the database list";

            // 500 Internal Server Error
            if (response.status === 500) {
                $scope.showException(message, null, response.data);
            }
            else {
                $scope.alerts.push({ type: 'error', msg: message });
            }
        });
    }

    $scope.getDatabases();

}


/**
 * Database Controller
 */
function DatabaseCtrl($scope, $routeParams, $dialog, $http, Database, Console, patchService) {

    $scope.apps = [];

    // Retrive the console output for a specific database
    $scope.getConsole = function (databaseName) {

        var message = "Can not retrive the console output from the database.";
        $scope.isBusy = true;
        Console.get({ databaseName: databaseName }, function (result) {
            // Success
            $scope.isBusy = false;

            $scope.console = result.console.replace(/\r\n/g, "<br>");  // Replace all occurrences of \r\n with the html tag <br>
            $("#console").scrollTop($("#console")[0].scrollHeight); // TODO: Do this in the next cycle?
            if (result.exception != null) {
                // Show Exception
                $scope.showException(result.exception.message, result.exception.helpLink, result.exception.stackTrace);
            }

        }, function (response) {
            // Error, Can not retrive the console output
            $scope.isBusy = false;

            // 500 Internal Server Error
            if (response.status === 500) {
                $scope.showException(message, null, response.data);
            }
            else if (response.status === 503) {
                // ServiceUnavailable
                $scope.alerts.push({ type: 'error', msg: message + ", " + "service is unavailable" });
            }
            else {
                // One cause can be that the database is not started.
                $scope.alerts.push({ type: 'error', msg: message + ", Status code:" + response.status + ". " + "Is the database running?" });
            }

        });
    }

    // Retrive database information
    $scope.getDatabase = function (databaseId) {
        $scope.isBusy = true;

        // Get a database
        Database.get({ databaseId: databaseId }, function (database, headers) {
            $scope.isBusy = false;
            $scope.database = database;
            // Get location from the response header
            //$scope.location = headers('Location');

//            $scope.getConsole(database.DatabaseName);
            // Observe the model
            /*
                    var observer = jsonpatch.observe($scope.database, function (patches) {
                        console.log("jsonpatch.observe triggerd");
                        patchService.applyPatch($scope.database, $scope.location, patches);
                    });
            */
        }, function (response) {
            // Error, Can not retrive list of databases
            $scope.isBusy = false;
            var message = "Can not retrive the database information";

            // 500 Internal Server Error
            if (response.status === 500) {
                $scope.showException(message, null, response.data);
            }
            else {
                $scope.alerts.push({ type: 'error', msg: message });
            }

        });

    }

    // User clicked the "Refresh" button
    $scope.btnClick_refresh_console = function () {
        $scope.alerts.length = 0;
        $scope.getConsole($scope.database.DatabaseName);
    }

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

    // Handsontable (fixed the height)
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



    // Retrive database information
    $scope.getDatabase($routeParams.databaseId);

}


/**
 * Log Controller
 */
function LogCtrl($scope, $dialog, Log) {

    $scope.isBusy = false;

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

        $scope.isBusy = true;
        $scope.alerts.length = 0;
        // Get a database
        Log.query($scope.filterModel, function (log) {
            // Success
            $scope.isBusy = false;
            $scope.log = log;
        }, function (response) {
            // Error, Can not retrive the log
            $scope.isBusy = false;

            var message = "Can not retrive the log";

            // 500 Internal Server Error
            if (response.status === 500) {
                $scope.showException(message, null, response.data);
            }
            else {
                $scope.alerts.push({ type: 'error', msg: message });
            }


        });
    }

    // User clicked the "Refresh" button
    $scope.btnClick_refresh = function () {
        $scope.getLog();
    }

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




}


/**
 * Sql Controller
 */
function SqlCtrl($scope, Sql, Database, patchService, SqlQuery, $dialog) {

    $scope.selectedDatabase = null;
    $scope.sqlQuery = "";
    $scope.columns = [];
    $scope.rows = [];

    $scope.isBusy = false;
    $scope.executeButtonTitle = function () {
        if ($scope.isBusy) {
            return "Executing...";
        }
        else {
            return "Execute";
        }
    }

    $scope.getDatabases = function () {

        $scope.alerts.length = 0;
        $scope.selectedDatabase = null;

        // Retrive database list
        Database.query(function (databases, headers) {
            $scope.databases = databases.DatabaseList;
            if ($scope.databases.length > 0) {
                $scope.selectedDatabase = $scope.databases[0];
            }

        }, function (response) {
            // Error, Can not retrive a list of databases
            var message = "Can not retrive the database list";

            // 500 Internal Server Error
            if (response.status === 500) {
                $scope.showException(message, null, response.data);
            }
            else {
                $scope.alerts.push({ type: 'error', msg: message });
            }

        });
    }


    $scope.executeQuery = function () {

        $scope.isBusy = true;
        $scope.alerts.length = 0;

        $scope.columns = [];
        $scope.rows = [];

        SqlQuery.send({ databaseName: $scope.selectedDatabase.DatabaseName }, $scope.sqlQuery, function (result, headers) {

            $scope.isBusy = false;

            // Success
            $scope.columns = result.columns;
            $scope.rows = result.rows;

            if (result.sqlException != null) {
                // Show message
                //$scope.sqlException.beginPosition
                //$scope.sqlException.endPosition
                //$scope.sqlException.helpLink
                //$scope.sqlException.message
                //$scope.sqlException.query
                //$scope.sqlException.scErrorCode
                //$scope.sqlException.token
                //$scope.sqlException.stackTrace

                $scope.alerts.push({ type: 'error', msg: result.sqlException.message, helpLink: result.sqlException.helpLink });
            }

            if (result.exception != null) {
                $scope.showException(result.exception.message, result.exception.helpLink, result.exception.stackTrace);
            }

        }, function (response) {
            // Error
            $scope.isBusy = false;

            var message = "Can not connect to server";

            // 500 Internal Server Error
            if (response.status === 500) {
                $scope.showException(message, null, response.data);
            }
            else if (response.status === 503) {
                // ServiceUnavailable
                $scope.alerts.push({ type: 'error', msg: message + ", " + "service is unavailable" });
            }
            else {
                $scope.alerts.push({ type: 'error', msg: message });
            }

        });
    }

    // User clicked the "Execute" button
    $scope.btnClick_execute = function () {
        $scope.executeQuery();
    }

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

    $scope.getDatabases();

}


/**
 * Edit Database Controller
 */
function CreateDatabaseCtrl($scope, Settings, CreateDatabase, CommandStatus, $dialog, $rootScope, $location) {

    $scope.isBusy = false;

    $scope.getDefaultSettings = function () {

        $scope.isBusy = true;
        $scope.alerts.length = 0;

        // Retrive database list
        Settings.query({ type: "database" }, function (defaultSettings) {
            $scope.isBusy = false;

            defaultSettings.tempDirectory = defaultSettings.tempDirectory.replace("[DatabaseName]", defaultSettings.databaseName);
            defaultSettings.imageDirectory = defaultSettings.imageDirectory.replace("[DatabaseName]", defaultSettings.databaseName);
            defaultSettings.transactionLogDirectory = defaultSettings.transactionLogDirectory.replace("[DatabaseName]", defaultSettings.databaseName);

            $scope.settings = defaultSettings;
        }, function (response) {
            // Error, Can not retrive a list of databases
            $scope.isBusy = false;
            var message = "Can not retrive database detault settings";

            // 500 Internal Server Error
            if (response.status === 500) {
                $scope.showException(message, null, response.data);
            }
            else {
                $scope.alerts.push({ type: 'error', msg: message });
            }

        });
    }

    $scope.createDatabase = function () {

        $scope.isBusy = true;
        $scope.alerts.length = 0;
        $scope.status = "Creating database...";
        var pollFrequency = 100 // 500ms
        var pollTimeout = 60000; // 60 Seconds

        CreateDatabase.save({ databaseName: $scope.settings.databaseName }, $scope.settings, function (result) {

            console.log("CommandId:" + result.commandId);

            (function poll() {

                $.ajax({
                    url: "/command/" + result.commandId,

                    success: function (result) {

                        $scope.status = result.message;

                        if (result.exception != null) {
                            $scope.isBusy = false;
                            $scope.showException(result.exception.message, result.exception.helpLink, result.exception.stackTrace);
                        } else if (result.isCompleted) {
                            $scope.isBusy = false;
                            $scope.status = "";
                            // Check for errors
                            if (result.errors.length > 0) {
                                for (var i = 0; i < result.errors.length ; i++) {
                                    $scope.alerts.push({ type: 'error', msg: result.errors[i].message, helpLink: result.errors[i].helpLink });
                                }
                            }
                            else {
                                $scope.alerts.push({ type: 'success', msg: "Database " + $scope.settings.databaseName + " was successfully created." });
                                $location.path("/databases");
                            }
                        }
                        else {
                            setTimeout(function () { poll(); }, pollFrequency); // wait 3 seconds than call ajax request again
                        }
                    },

                    error: function (xhr, textStatus, thrownError) {
                        $scope.alerts.push({ type: 'error', msg: textStatus });
                    },

                    complete: function () {
                        console.log("complete");
                        $scope.$apply();

                    },
                    dataType: "json",
                    timeout: pollTimeout
                });
            })();


        }, function (response) {
            // Error, Can not create database
            $scope.isBusy = false;
            var message = "Can not create database";

            // 500 Internal Server Error
            if (response.status === 500) {
                $scope.showException(message, null, response.data);
            }
            else {
                $scope.alerts.push({ type: 'error', msg: message });
            }

        });
    }


    $scope.btnClick_createDatabase = function () {
        $scope.createDatabase();
    }

    $scope.getDefaultSettings();
}



/**
 * Dialog Controller
 */
function DialogController($scope, dialog) {

    $scope.header = dialog.options.data.header;
    $scope.message = dialog.options.data.message;
    $scope.helpLink = dialog.options.data.helpLink;
    $scope.stackTrace = dialog.options.data.stackTrace;

    $scope.close = function (result) {
        dialog.close(result);
    };
}