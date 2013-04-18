
/**
 * Scadmin service module
 */
var myApp = angular.module('scadmin', ['scadminServices', 'ui', 'ui.bootstrap', 'uiHandsontable'], function ($routeProvider, $locationProvider) {


    $routeProvider.when('/main', {
        templateUrl: '/partials/main.html',
        controller: MainCtrl
    });

    $routeProvider.when('/server', {
        templateUrl: '/partials/server.html',
        controller: ServerCtrl
    });

    $routeProvider.when('/server/edit', {
        templateUrl: '/partials/edit_server.html',
        controller: ServerCtrl
    });


    $routeProvider.when('/databases', {
        templateUrl: '/partials/databases.html',
        controller: DatabasesCtrl
    });

    $routeProvider.when('/databases/:name', {
        templateUrl: '/partials/database.html',
        controller: DatabaseCtrl
    });

    $routeProvider.when('/databases/:name/edit', {
        templateUrl: '/partials/edit_database.html',
        controller: DatabaseCtrl
    });


    $routeProvider.when('/create_database', {
        templateUrl: '/partials/create_database.html',
        controller: CreateDatabaseCtrl
    })

    $routeProvider.when('/apps', {
        templateUrl: '/partials/apps.html',
        controller: AppsCtrl
    });

    $routeProvider.when('/gateway', {
        templateUrl: '/partials/gateway.html',
        controller: GatewayCtrl
    });


    $routeProvider.when('/log', {
        templateUrl: '/partials/log.html',
        controller: LogCtrl
    });

    $routeProvider.when('/sql', {
        templateUrl: '/partials/sql.html',
        controller: SqlCtrl
    });


    $routeProvider.otherwise({ redirectTo: '/main' });

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
 * App Controller
 */
function HeadCtrl($scope, $location, $rootScope, $http, $dialog, App, Database, DbWorkaround) {

    $rootScope.alerts = [];
    $rootScope.databases = [];
    $rootScope.apps = [];

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

    // Retrive all databases
    $rootScope.getDatabases = function () {

        Database.query(function (result) {
            $scope.databases = result.databases;

            if (result.exception != null) {
                $scope.showException(result.exception.message, result.exception.helpLink, result.exception.stackTrace);
            }

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

    // Start database
    $rootScope.startDatabase = function (name) {

        $scope.alerts.length = 0;
        $scope.alerts.push({ type: 'info', msg: "Not implemented" });
        return;

        DbWorkaround.start({ name: name }, "some payload", function (response) { // TODO
            //$scope.databases = result.databases;
            $scope.alerts.push({ type: 'info', msg: "response:" + response });

            var commandStarted = true;

            if (response.exception != null) {
                commandStarted = false;
                $scope.showException(response.exception.message, response.exception.helpLink, response.exception.stackTrace);
            }

            // Start listening/polling for end of command 
            if (commandStarted) {
                $scope.isBusy = true;
                $rootScope.pollCommand(response.commandId);
            }



        }, function (response) {
            // Error, Can not retrive list of databases
            var message = "Can not start database";

            // 500 Internal Server Error
            if (response.status === 500) {
                $scope.showException(message, null, response.data);
            }
            else {
                $scope.alerts.push({ type: 'error', msg: message });
            }
        });
    }


    // Stop database
    $rootScope.stopDatabase = function (name) {

        $scope.alerts.length = 0;
        $scope.alerts.push({ type: 'info', msg: "Not implemented" });
        return;

        DbWorkaround.stop({ name: name }, "some payload", function (response) { // TODO
            //$scope.databases = result.databases;
            $scope.alerts.push({ type: 'info', msg: "response:" + response });

            var commandStarted = true;

            if (response.exception != null) {
                commandStarted = false;
                $scope.showException(response.exception.message, response.exception.helpLink, response.exception.stackTrace);
            }

            // Start listening/polling for end of command 
            if (commandStarted) {
                $scope.isBusy = true;
                $rootScope.pollCommand(response.commandId);
            }


        }, function (response) {
            // Error, Can not retrive list of databases
            var message = "Can not stop database";

            // 500 Internal Server Error
            if (response.status === 500) {
                $scope.showException(message, null, response.data);
            }
            else {
                $scope.alerts.push({ type: 'error', msg: message });
            }
        });
    }


    $rootScope.pollCommand = function (id) {

        var pollFrequency = 100 // 500ms
        var pollTimeout = 60000; // 60 Seconds

        (function poll() {

            $.ajax({
                url: "/command/" + id,

                success: function (response) {

                    $scope.status = response.message;
                    $scope.progressText = response.progressText;

                    console.log("Message:" + response.message);
                    console.log("progressText:" + response.progressText);

                    if (response.exception != null) {
                        $scope.isBusy = false;
                        $scope.showException(response.exception.message, response.exception.helpLink, response.exception.stackTrace);
                    } else if (response.isCompleted) {
                        $scope.isBusy = false;
                        $scope.status = "";
                        // Check for errors
                        if (response.errors.length > 0) {
                            for (var i = 0; i < response.errors.length ; i++) {
                                $scope.alerts.push({ type: 'error', msg: response.errors[i].message, helpLink: response.errors[i].helpLink });
                            }
                        }
                        else {
                            console.log("Command done");

                            // Command finished
                            $scope.alerts.push({ type: 'success', msg: "Command done" });
                        }
                    }
                    else {
                        setTimeout(function () { poll(); }, pollFrequency); // wait 3 seconds than call ajax request again
                    }
                },

                error: function (xhr, textStatus, thrownError) {
                    $scope.isBusy = false;
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



    }


    // Retrive all databases
    $rootScope.getApps = function () {

        App.query(function (result) {
            $scope.apps = result.apps;

            if (result.exception != null) {
                $scope.showException(result.exception.message, result.exception.helpLink, result.exception.stackTrace);
            }

        }, function (response) {
            // Error, Can not retrive list of databases
            var message = "Can not retrive the list of applications";

            // 500 Internal Server Error
            if (response.status === 500) {
                $scope.showException(message, null, response.data);
            }
            else {
                $scope.alerts.push({ type: 'error', msg: message });
            }
        });
    }

    // Get Gateway information
    $rootScope.getGatewayStats = function () {

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

              var message = "Can not retrive the gateway statistics.";
              message += ", " + data;
              if (status != null) {
                  message += " " + "Status code:" + status;
              }

              // 500 Internal Server Error
              if (status === 500) {
                  $scope.showException(message, null, null);
              }
              else {
                  $scope.alerts.push({ type: 'error', msg: message });
              }
          });
    }


    // Load databases
    $rootScope.getDatabases();

    $rootScope.getApps();

}


/**
 * Main Controller
 */
function MainCtrl($scope, Database) {

    $scope.alerts.length = 0;


    $scope.btnClick_refreshDatabases = function () {
        $scope.getDatabases();
    }

    $scope.btnClick_refreshApps = function () {
        $scope.getApps();
    }


    $scope.btnClick_restart = function () {
        $scope.alerts.length = 0;
        $scope.alerts.push({ type: 'info', msg: "Not implemented" });
    }

    $scope.btnClick_start = function (database) {
        $scope.startDatabase(database.name);
    }

    $scope.btnClick_stop = function (database) {
        $scope.stopDatabase(database.name);
    }

    $scope.btnClick_delete = function (database) {
        $scope.alerts.length = 0;
        $scope.alerts.push({ type: 'info', msg: "Not implemented:" });
    }
}


/**
 * Gateway Controller
 */
function GatewayCtrl($scope, $dialog) {

    $scope.alerts.length = 0;

    // User clicked the "Refresh" button
    $scope.btnClick_refresh_gwateway_stats = function () {
        $scope.getGatewayStats();
    }

    $scope.getGatewayStats();

}


/**
 * Apps Controller
 */
function AppsCtrl($scope, $location, $rootScope, $dialog) {

    $scope.alerts.length = 0;

    $scope.btnClick_refreshApps = function () {
        $scope.getApps();
    }
}


/**
 * Server Controller
 */
function ServerCtrl($scope, $dialog, Server, $http) {

    $scope.isBusy = true;
    $scope.alerts.length = 0;

    // Retrive server information
    $scope.getServer = function () {

        Server.get(function (response) {
            $scope.isBusy = false;
            // Success
            $scope.server = response.server;

            if (response.server == null) {
                $scope.alerts.push({ type: 'error', msg: "Can not retrive server information" });
            }

            if (response.exception != null) {
                $scope.showException(response.exception.message, response.exception.helpLink, response.exception.stackTrace);
            }


        }, function (response) {
            $scope.isBusy = false;
            // Error, Can not retrive list of databases
            var message = "Can not retrive the server information.";
            if (response.status != null) {
                message += " " + "Status code:" + response.status;
            }

            // 500 Internal Server Error
            if (response.status === 500) {
                $scope.showException(message, null, response.data);
            }
            else {
                $scope.alerts.push({ type: 'error', msg: message });
            }
        });

    }

    // Retrive database information
    $scope.saveSettings = function () {
        $scope.isBusy = true;

        // Save server settings
        Server.save($scope.server, function (response, headers) {
            $scope.isBusy = false;

            if (response.message != null) {
                $scope.alerts.push({ type: 'success', msg: response.message });
            }

            if (response.validationErrors != null && response.validationErrors.length > 0) {

                for (var i = 0; i < response.validationErrors.length; i++) {
                    $scope.alerts.push({ type: 'error', msg: response.validationErrors[i].message });
                    $scope.myForm[response.validationErrors[i].property].$setValidity("validationError", false);
                    var id = response.validationErrors[i].property;
                    var unregister = $scope.$watch("server." + response.validationErrors[i].property, function (newValue, oldValue) {
                        if (newValue == oldValue) return;
                        $scope.myForm[id].$setValidity("validationError", true);
                        unregister();
                    }, false);

                }
            } else if (response.server == null) {
                $scope.alerts.push({ type: 'error', msg: "Can not save configuration to server " + $scope.server.name });
            }

            if (response.exception != null) {
                $scope.showException(response.exception.message, response.exception.helpLink, response.exception.stackTrace);
            }

            if (response.server != null && response.exception == null) {
                // Success
                $scope.server = response.server;
                $scope.myForm.$setPristine();
            }

        }, function (response) {

            $scope.isBusy = false;
            var message = "Can not save configuration to server " + $scope.server.name + ".";
            if (response.status != null) {
                message += " " + "Status code:" + response.status;
            }

            // 500 Internal Server Error
            if (response.status === 500) {
                $scope.showException(message, null, response.data);
            }
            else {
                $scope.alerts.push({ type: 'error', msg: message });
            }

        });

    }

    $scope.btnClick_saveSettings = function () {
        $scope.alerts.length = 0;
        $scope.saveSettings();
    }

    $scope.getServer();

}


/**
 * Databases Controller
 */
function DatabasesCtrl($scope, $dialog) {
    $scope.alerts.length = 0;


    $scope.btnClick_refreshDatabases = function () {
        $scope.getDatabases();
    }


    $scope.btnClick_restart = function (database) {
        $scope.alerts.length = 0;
        $scope.alerts.push({ type: 'info', msg: "Not implemented" });
    }

    $scope.btnClick_start = function (database) {
        $scope.startDatabase(database.name);
    }

    $scope.btnClick_stop = function (database) {
        $scope.stopDatabase(database.name);
    }

    $scope.btnClick_delete = function (database) {
        $scope.alerts.length = 0;
        $scope.alerts.push({ type: 'info', msg: "Not implemented:" });
    }

}


/**
 * Database Controller
 */
function DatabaseCtrl($scope, $location, $routeParams, $dialog, $http, Database, Console, patchService) {

    //$scope.selectedDatabaseId = $routeParams.databaseId;
    $scope.apps = [];
    $scope.alerts.length = 0;

    //    $scope.email = "bad@domain.co";
    //    $scope.site = "dd";
    //    console.log("DatabaseCtrl");
    // Watch for changes in the filer
    //$scope.$watch('selectedDatabaseId', function () {

    //    console.log("selectedDatabaseId:" + $scope.selectedDatabaseId);
    //    angular.forEach($scope.databases,
    //        function (item, key) {
    //            if ($scope.selectedDatabaseId === item.id) {
    //                $scope.getDatabase(item.id);
    //                // forEach dosent support 'break' :-(
    //            }
    //        });

    //}, true);

    // Retrive the console output for a specific database
    $scope.getConsole = function (name) {

        var message = "Can not retrive the console output from the database.";
        $scope.isBusy = true;
        Console.get({ name: name }, function (response) {
            // Success
            $scope.isBusy = false;

            $scope.console = response.console.replace(/\r\n/g, "<br>");  // Replace all occurrences of \r\n with the html tag <br>
            $("#console").scrollTop($("#console")[0].scrollHeight); // TODO: Do this in the next cycle?
            if (response.exception != null) {
                // Show Exception
                $scope.showException(response.exception.message, response.exception.helpLink, response.exception.stackTrace);
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
    $scope.getDatabase = function (name) {
        $scope.isBusy = true;

        // Get a database
        Database.get({ name: name }, function (result, headers) {
            $scope.isBusy = false;
            $scope.database = result.database;

            if (result.database == null) {
                $scope.alerts.push({ type: 'error', msg: "Can not retrive database information" });
            }

            if (result.exception != null) {
                $scope.showException(result.exception.message, result.exception.helpLink, result.exception.stackTrace);
            }


            // Get location from the response header
            //$scope.location = headers('Location');
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
            var message = "Can not retrive the database information.";
            if (response.status != null) {
                message += " " + "Status code:" + response.status;
            }

            // 500 Internal Server Error
            if (response.status === 500) {
                $scope.showException(message, null, response.data);
            }
            else {
                $scope.alerts.push({ type: 'error', msg: message });
            }

        });

    }

    // Save database information
    $scope.saveSettings = function (name) {
        $scope.isBusy = true;

        // Get a database
        Database.save({ name: name }, $scope.database, function (result, headers) {
            $scope.isBusy = false;

            if (result.message != null) {
                $scope.alerts.push({ type: 'success', msg: result.message });
            }

            if (result.validationErrors != null && result.validationErrors.length > 0) {

                for (var i = 0; i < result.validationErrors.length; i++) {
                    $scope.alerts.push({ type: 'error', msg: result.validationErrors[i].message });
                    $scope.myForm[result.validationErrors[i].property].$setValidity("validationError", false);
                    var id = result.validationErrors[i].property;
                    var unregister = $scope.$watch("database." + result.validationErrors[i].property, function (newValue, oldValue) {
                        if (newValue == oldValue) return;
                        $scope.myForm[id].$setValidity("validationError", true);
                        unregister();
                    }, false);

                }
            } else if (result.database == null) {
                $scope.alerts.push({ type: 'error', msg: "Can not save configuration to database " + $scope.database.name });
            }

            if (result.exception != null) {
                $scope.showException(result.exception.message, result.exception.helpLink, result.exception.stackTrace);
            }

            if (result.database != null && result.exception == null) {
                // Success
                $scope.database = result.database;
                $scope.myForm.$setPristine();
            }

        }, function (response) {
            // Error, Can not retrive list of databases
            $scope.isBusy = false;
            var message = "Can not save configuration to database " + $scope.database.name + ".";
            if (response.status != null) {
                message += " " + "Status code:" + response.status;
            }

            // 500 Internal Server Error
            if (response.status === 500) {
                $scope.showException(message, null, response.data);
            }
            else {
                $scope.alerts.push({ type: 'error', msg: message });
            }

        });

    }

    // Form validation
    //$scope.blackList = ['bad@domain.com', 'verybad@domain.com'];
    //$scope.notBlackListed = function (value) {

    //    return $scope.blackList.indexOf(value) === -1;

    //};

    //$scope.test2Validate = function (value) {

    //    return true;

    //};

    //$scope.testValidate = function (value) {

    //    if (value == null) return false;
    //    return value.length === 2;

    //};




    //$scope.notBlackListed = function (value) {
    //    var blacklist = ['bad@domain.com', 'verybad@domain.com'];
    //    return blacklist.indexOf(value) === -1;
    //}



    // User clicked the "Refresh" button
    $scope.btnClick_refreshConsole = function () {
        $scope.alerts.length = 0;
        $scope.getConsole($scope.database.name);
    }

    $scope.btnClick_saveSettings = function () {
        $scope.alerts.length = 0;
        $scope.saveSettings($scope.database.name);
    }


    // User clicked the "Start" button
    $scope.btnClick_start = function () {
        $scope.startDatabase($scope.database.name);
    }

    // User clicked the "Stop" button
    $scope.btnClick_stop = function () {
        $scope.stopDatabase($scope.database.name);
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
    $scope.getDatabase($routeParams.name);

}


/**
 * Log Controller
 */
function LogCtrl($scope, $dialog, Log) {

    $scope.isBusy = false;
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
            if (response.status != null) {
                message += " " + "Status code:" + response.status;
            }

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

    $scope.isBusy = false;
    $scope.alerts.length = 0;

    $scope.selectedDatabase = null;
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

    //$scope.getDatabases = function () {

    //    $scope.alerts.length = 0;
    //    $scope.selectedDatabase = null;

    //    // Retrive database list
    //    Database.query(function (databases, headers) {
    //        $scope.databases = databases.DatabaseList;
    //        if ($scope.databases.length > 0) {
    //            $scope.selectedDatabase = $scope.databases[0];
    //        }

    //    }, function (response) {
    //        // Error, Can not retrive a list of databases
    //        var message = "Can not retrive the database list";

    //        // 500 Internal Server Error
    //        if (response.status === 500) {
    //            $scope.showException(message, null, response.data);
    //        }
    //        else {
    //            $scope.alerts.push({ type: 'error', msg: message });
    //        }

    //    });
    //}


    $scope.executeQuery = function () {

        $scope.isBusy = true;
        $scope.alerts.length = 0;

        $scope.columns = [];
        $scope.rows = [];

        SqlQuery.send({ name: $scope.selectedDatabase.name }, $scope.sqlQuery, function (response, headers) {

            $scope.isBusy = false;

            // Success
            $scope.columns = response.columns;
            $scope.rows = response.rows;

            if (response.sqlException != null) {
                // Show message
                //$scope.sqlException.beginPosition
                //$scope.sqlException.endPosition
                //$scope.sqlException.helpLink
                //$scope.sqlException.message
                //$scope.sqlException.query
                //$scope.sqlException.scErrorCode
                //$scope.sqlException.token
                //$scope.sqlException.stackTrace

                $scope.alerts.push({ type: 'error', msg: response.sqlException.message, helpLink: response.sqlException.helpLink });
            }

            if (response.exception != null) {
                $scope.showException(response.exception.message, response.exception.helpLink, response.exception.stackTrace);
            }

        }, function (response) {
            // Error
            $scope.isBusy = false;

            var message = "Can not connect to server.";
            if (response.status != null) {
                message += " " + "Status code:" + response.status;
            }
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
        var border = 12 + 60;
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

    //$scope.getDatabases();

    if ($scope.databases.length > 0) {
        $scope.selectedDatabase = $scope.databases[0];
    }

}


/**
 * Edit Database Controller
 */
function CreateDatabaseCtrl($scope, Settings, Database, CommandStatus, $dialog, $rootScope, $location) {

    $scope.isBusy = false;
    $scope.alerts.length = 0;

    $scope.getDefaultSettings = function () {

        $scope.isBusy = true;
        $scope.alerts.length = 0;

        // Retrive database list
        Settings.query({ type: "database" }, function (response) {
            $scope.isBusy = false;
            response.tempDirectory = response.tempDirectory.replace("[DatabaseName]", response.databaseName);
            response.imageDirectory = response.imageDirectory.replace("[DatabaseName]", response.databaseName);
            response.transactionLogDirectory = response.transactionLogDirectory.replace("[DatabaseName]", response.databaseName);

            $scope.settings = response;
        }, function (response) {
            // Error, Can not retrive a list of databases
            $scope.isBusy = false;
            var message = "Can not retrive database detault settings.";
            if (response.status != null) {
                message += " " + "Status code:" + response.status;
            }

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

        Database.create({},$scope.settings, function (response) {

            $scope.isBusy = false;
            var commandStarted = true;

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
                commandStarted = false;
            }

            if (response.exception != null) {
                commandStarted = false;
                $scope.showException(response.exception.message, response.exception.helpLink, response.exception.stackTrace);
            }

            if (commandStarted) {
                $scope.isBusy = true;

                (function poll() {

                    $.ajax({
                        url: "/command/" + response.commandId,

                        success: function (response) {

                            $scope.status = response.message;

                            if (response.exception != null) {
                                $scope.isBusy = false;
                                $scope.showException(response.exception.message, response.exception.helpLink, response.exception.stackTrace);
                            } else if (response.isCompleted) {
                                $scope.isBusy = false;
                                $scope.status = "";
                                // Check for errors
                                if (response.errors.length > 0) {
                                    for (var i = 0; i < response.errors.length ; i++) {
                                        $scope.alerts.push({ type: 'error', msg: response.errors[i].message, helpLink: response.errors[i].helpLink });
                                    }
                                }
                                else {
                                    $scope.alerts.push({ type: 'success', msg: "Database " + $scope.settings.name + " was successfully created." });
                                    //$location.path("/databases");
                                    $scope.settings = null;
                                }
                            }
                            else {
                                setTimeout(function () { poll(); }, pollFrequency); // wait 3 seconds than call ajax request again
                            }
                        },

                        error: function (xhr, textStatus, thrownError) {
                            $scope.isBusy = false;
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

            }
        }, function (response) {
            // Error, Can not create database
            $scope.isBusy = false;
            var message = "Can not create database.";
            if (response.status != null) {
                message += " " + "Status code:" + response.status;
            }

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