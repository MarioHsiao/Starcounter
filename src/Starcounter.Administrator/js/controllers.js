
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
function HeadCtrl($scope, $location, $rootScope, $http, $dialog, App, Database) {

    $rootScope.alerts = [];
    $rootScope.databases = [];
    $rootScope.apps = [];
    $rootScope.isPolling = false;

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
        // TODO: Handle if the server went down, the we can not get the error.html to show the error message..

    }

    // Get a database instance by name
    $rootScope.getDatabaseByName = function (name) {
        for (var i = 0 ; i < $rootScope.databases.length ; i++) {
            if ($rootScope.databases[i].name == name) {
                return $rootScope.databases[i];
            }
        }
        return null;
    }


    // ==== API Calls ====

    // Retrive all databases
    $rootScope.getDatabases = function () {

        Database.query(function (response) {
            $rootScope.databases = response.databases;

            if (response.databases == null) {
                $scope.showException("Invalid response, database property was null", null, ".getDatabase()");
            }
            else {
                // Retrive status
                $rootScope.pollCommands();
            }

        }, function (response) {

            if (response.status == 404) {
                // 404	Not Found
                $scope.alerts.push({ type: 'error', msg: response.data.message, helpLink: response.data.helpLink });
            }
            else if (response.status == 500) {
                // 500 Internal Server Error
                $scope.showException(response.data.message, response.data.helpLink, response.data.stackTrace);
            }
            else {
                $scope.showException("Unhandled http statuscode " + response.status, null, ".getDatabase()");
            }

        });
    }

    // Start database
    $rootScope.startDatabase = function (name) {

        //$scope.alerts.length = 0;
        //$scope.alerts.push({ type: 'info', msg: "Not implemented" });
        //return;

        Database.start({ name: name }, "some payload", function (response) { // TODO
            //$scope.databases = result.databases;
            //$scope.alerts.push({ type: 'info', msg: "response:" + response });

            var commandStarted = true;

            if (response.exception != null) {
                commandStarted = false;
                $scope.showException(response.exception.message, response.exception.helpLink, response.exception.stackTrace);
            }

            // Start listening/polling for end of command 
            if (commandStarted) {
                $rootScope.pollCommands();
                //$scope.isBusy = true;
                //$rootScope.pollCommand(response.commandId);
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

        //$scope.alerts.length = 0;
        //$scope.alerts.push({ type: 'info', msg: "Not implemented" });
        //return;

        Database.stop({ name: name }, "some payload", function (response) { // TODO
            //$scope.databases = result.databases;
            //$scope.alerts.push({ type: 'info', msg: "response:" + response });

            var commandStarted = true;

            if (response.exception != null) {
                commandStarted = false;
                $scope.showException(response.exception.message, response.exception.helpLink, response.exception.stackTrace);
            }


            // Start listening/polling for end of command 
            if (commandStarted) {
                //    $scope.isBusy = true;
                //    $rootScope.pollCommand(response.commandId);
                $rootScope.pollCommands();
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


    $rootScope.getAllServerCommands = function () {

        //console.log("getAllServerCommands");
        $rootScope.stopDatabase("1111mydatabase");
    }

    $rootScope.pollCommands = function () {

        var pollFrequency = 100 // 500ms
        var pollTimeout = 60000; // 60 Seconds

        if ($rootScope.isPolling == true) {
            //console.log("Already polling...");
            return;
        }

        (function poll() {

            $rootScope.isPolling = true;

            $.ajax({
                url: "/adminapi/v1/server/commands",

                success: function (response) {

                    $scope.commands = response.commands;

                    if (response.commands == null) {
                        $scope.showException("Invalid response, commands property was null", null, ".pollCommands()");
                    }
                    else {

                        //console.log("Got commands:" + $scope.commands.length);

                        // Reset status to unknown to all databases, before we retrive the new status.
                        // TODO: 
                        for (var i = 0 ; i < $rootScope.databases.length ; i++) {
                            if ($rootScope.databases[i].hostProcessId > 0) {
                                $rootScope.databases[i].status = "Running";
                            }
                            else {
                                $rootScope.databases[i].status = ".";
                            }
                        }

                        var ignoreList = new Array();

                        for (var i = 0; i < $scope.commands.length ; i++) {

                            // TODO: Update database status texts etc..
                            var database = $rootScope.getDatabaseByName($scope.commands[i].name);
                            if (database == null) continue;

                            //ignoreList[ignoreList.length] = database;

                            database.status = $scope.commands[i].description;
                            database.hostProcessId = $scope.commands[i].hostProcessId;

                            //console.log("Command, description:" + $scope.commands[i].description);
                            //console.log("Command, database name:" + $scope.commands[i].name);
                            //console.log("Command, progressText:" + $scope.commands[i].progressText);
                            //console.log("Command, status:" + $scope.commands[i].status);
                        }

                        if ($scope.commands.length == 0) {
                            // No more running database commands on the server
                            //console.log("No more running database commands on the server, polling stopped.");
                            $rootScope.isPolling = false;
                        }
                        else {
                            // Start new poll
                            setTimeout(function () { poll(); }, pollFrequency); // wait 3 seconds than call ajax request again
                        }
                    }

                    //if (response.exception != null) {
                    //    $scope.isBusy = false;
                    //    $scope.showException(response.exception.message, response.exception.helpLink, response.exception.stackTrace);
                    //}


                },

                error: function (xhr, error, thrownError) {
                    $rootScope.isPolling = false;
                    //                    console.log("error-polling xhr:" + xhr);
                    //                    console.log("error-polling error:" + error);
                    //                    console.log("error-polling thrownError:" + thrownError);

                    var response = JSON.parse(xhr.responseText);

                    if (xhr.status == 404) {
                        // 404	Not Found
                        $scope.alerts.push({ type: 'error', msg: response.message, helpLink: response.helpLink });
                    }
                    else if (xhr.status == 500) {
                        // 500 Internal Server Error
                        $scope.showException(response.message, response.helpLink, response.stackTrace);
                    }
                    else {
                        $scope.showException("Unhandled http error " + textStatus, null, ".pollCommand()");
                    }

                },

                complete: function () {
                    //console.log("polling commands complete");
                    $scope.$apply();

                },
                dataType: "json",
                timeout: pollTimeout
            });
        })();



    }

    $rootScope.pollCommand = function (id) {

        var pollFrequency = 100 // 500ms
        var pollTimeout = 60000; // 60 Seconds

        (function poll() {

            $.ajax({
                url: "/adminapi/v1/server/commands/" + id,

                success: function (response) {

                    if (response.command == null) {
                        $scope.showException("Invalid response, command property was null", null, ".pollCommands()");
                    }
                    else {
                    }

                    //console.log("Message:" + response.command.message);
                    //console.log("progressText:" + response.command.progressText);

                    if (response.command.isCompleted) {

                        // Check for errors
                        if (response.command.errors.length > 0) {
                            for (var i = 0; i < response.command.errors.length ; i++) {
                                $scope.alerts.push({ type: 'error', msg: response.command.errors[i].message, helpLink: response.command.errors[i].helpLink });
                            }
                        }
                        else {
                            //console.log("Command done");

                            // Command finished
                            $scope.alerts.push({ type: 'success', msg: "Command done" });
                        }
                    }
                    else {
                        setTimeout(function () { poll(); }, pollFrequency); // wait 3 seconds than call ajax request again
                    }
                },

                error: function (xhr, textStatus, thrownError) {

                    var response = JSON.parse(xhr.responseText);

                    if (xhr.status == 404) {
                        // 404	Not Found
                        $scope.alerts.push({ type: 'error', msg: response.message, helpLink: response.helpLink });
                    }
                    else if (xhr.status == 500) {
                        // 500 Internal Server Error
                        $scope.showException(response.message, response.helpLink, response.stackTrace);
                    }
                    else {
                        $scope.showException("Unhandled http error " + textStatus, null, ".pollCommand()");
                    }


                },

                complete: function () {
                    //console.log("complete");
                    $scope.$apply();

                },
                dataType: "json",
                timeout: pollTimeout
            });
        })();



    }


    // Retrive all databases
    $rootScope.getApps = function () {

        App.query(function (response) {
            $rootScope.apps = response.apps;

            if (response.apps == null) {
                $scope.showException("Invalid response, apps property was null", null, ".getDatabase()");
            }
            else {
                // Retrive status
                $rootScope.pollCommands();
            }

        }, function (response) {

            if (response.status == 404) {
                // 404	Not Found
                $scope.alerts.push({ type: 'error', msg: response.data.message, helpLink: response.data.helpLink });
            }
            else if (response.status == 500) {
                // 500 Internal Server Error
                $scope.showException(response.data.message, response.data.helpLink, response.data.stackTrace);
            }
            else {
                $scope.showException("Unhandled http statuscode " + response.status, null, ".getApps()");
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
              $rootScope.gwStats = data;
          }).
          error(function (data, status, headers, config) {
              $scope.isBusy = false;
              $rootScope.gwStats = "";

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


    $scope.btnClick_database_restart = function () {
        $scope.alerts.length = 0;
        $scope.alerts.push({ type: 'info', msg: "Not implemented" });
    }

    $scope.btnClick_database_start = function (database) {
        $scope.startDatabase(database.name);
    }

    $scope.btnClick_database_stop = function (database) {
        $scope.stopDatabase(database.name);
    }

    $scope.btnClick_database_delete = function (database) {
        $scope.alerts.length = 0;
        $scope.alerts.push({ type: 'info', msg: "Not implemented:" });
    }

    $scope.btnClick_app_restart = function (app) {
        $scope.alerts.push({ type: 'info', msg: "Not implemented" });
    }

    $scope.btnClick_app_start = function (app) {
        $scope.alerts.push({ type: 'info', msg: "Not implemented" });
    }

    $scope.btnClick_app_stop = function (app) {
        $scope.alerts.push({ type: 'info', msg: "Not implemented" });
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
            $scope.server = response.server;

            if (response.server == null) {
                $scope.showException("Invalid response, server property was null", null, ".getServer()");
            }

        }, function (response) {
            $scope.isBusy = false;

            if (response.status == 404) {
                // 404	Not Found
                $scope.alerts.push({ type: 'error', msg: response.data.message, helpLink: response.data.helpLink });
            }
            else if (response.status == 500) {
                // 500 Internal Server Error
                $scope.showException(response.data.message, response.data.helpLink, response.data.stackTrace);
            }
            else {
                $scope.showException("Unhandled http statuscode " + response.status, null, ".getDatabase()");
            }

        });

    }


    // Save database information
    $scope.saveSettings = function (name) {
        $scope.isBusy = true;

        $http({ method: 'PUT', url: '/adminapi/v1/server', data: $scope.server }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {
              $scope.isBusy = false;

              if (status == 200) {
                  // 200 Ok
                  $scope.isBusy = true;

                  // TODO: Maybe we dont need a 'message' property (look this over)
                  if (response.message != null) {
                      $scope.alerts.push({ type: 'success', msg: response.message });
                  }

                  if (response.server == null) {
                      $scope.showException("Invalid response, server property was null", null, ".saveSettings()");
                  }
                  else {
                      $scope.server = response.server;
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
                  $scope.showException("Unhandled http statuscode " + status, null, ".saveSettings()");
              }

          });c






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
function DatabaseCtrl($scope, $location, $routeParams, $dialog, $http, Database, Console) {

    //$scope.selectedDatabaseId = $routeParams.databaseId;
    $scope.apps = [];
    $scope.alerts.length = 0;

    // Retrive the console output for a specific database
    $scope.getConsole = function (name) {

        $scope.isBusy = true;
        Console.get({ name: name }, function (response) {
            // Success
            $scope.isBusy = false;

            if (response.console == null) {
                $scope.showException("Invalid response, console property was null", null, ".getConsole()");
            }
            else {
                $scope.console = response.console.replace(/\r\n/g, "<br>");  // Replace all occurrences of \r\n with the html tag <br>
                $("#console").scrollTop($("#console")[0].scrollHeight); // TODO: Do this in the next cycle?
            }

        }, function (response) {
            $scope.isBusy = false;

            if (response.status == 404) {
                // 404	Not Found
                $scope.alerts.push({ type: 'error', msg: response.data.message, helpLink: response.data.helpLink });
            }
            else if (response.status == 500) {
                // 500 Internal Server Error
                $scope.showException(response.data.message, response.data.helpLink, response.data.stackTrace);
            }
            else {
                $scope.showException("Unhandled http statuscode " + response.status, null, ".getConsole()");
            }

        });
    }

    // Retrive database information
    $scope.getDatabase = function (name) {
        $scope.isBusy = true;

        // Get a database
        Database.get({ name: name }, function (response, headers) {

            $scope.isBusy = false;
            $scope.database = response.database;

            if (response.database == null) {
                $scope.showException("Invalid response, database property was null", null, ".getDatabase()");
            }

        }, function (response) {
            $scope.isBusy = false;

            if (response.status == 404) {
                // 404	Not Found
                $scope.alerts.push({ type: 'error', msg: response.data.message, helpLink: response.data.helpLink });
            }
            else if (response.status == 500) {
                // 500 Internal Server Error
                $scope.showException(response.data.message, response.data.helpLink, response.data.stackTrace);
            }
            else {
                $scope.showException("Unhandled http statuscode " + response.status, null, ".getDatabase()");
            }

        });

    }

    // Save database information
    $scope.saveSettings = function (name) {
        $scope.isBusy = true;

        $http({ method: 'PUT', url: '/adminapi/v1/databases/' + $scope.database.name, data: $scope.database }).

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
    //$scope.getDatabase($routeParams.name);
    $scope.getDatabase($routeParams.name);

    //    $scope.database = $scope.getDatabaseByName($routeParams.name);
    // If the database was not found in the database list, try to get the database with a GET
    //    if ($scope.database == null) {
    //    }
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
        Log.query($scope.filterModel, function (response) {
            // Success
            $scope.isBusy = false;
            $scope.log = response;

            if (response == null) {
                $scope.showException("Invalid response, log property was null", null, ".saveSettings()");
            }


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
function SqlCtrl($scope, Database, SqlQuery, $dialog) {

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
            if (response.queryPlan != null) {
                $scope.queryPlan = response.queryPlan.replace(/\r\n/g, "<br>");  // Replace all occurrences of \r\n with the html tag <br>
                $scope.queryPlan = $scope.queryPlan.replace(/\t/g, "&emsp;");  // Replace all occurrences of \t with &emsp;
            }

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


            if (response.status == 404) {
                // 404	Not Found
                $scope.alerts.push({ type: 'error', msg: response.data.message, helpLink: response.data.helpLink });
            }
            else if (response.status == 500) {
                // 500 Internal Server Error
                $scope.showException(response.data.message, response.data.helpLink, response.data.stackTrace);
            }
            else if (response.status == 503) {
                // 503 ServiceUnavailable
                $scope.alerts.push({ type: 'error', msg: response.data.message, helpLink: response.data.helpLink });
            }
            else {
                $scope.showException("Unhandled http statuscode " + response.status, null, ".getDatabase()");
            }


            //var message = "Can not connect to server.";
            //if (response.status != null) {
            //    message += " " + "Status code:" + response.status;
            //}
            //// 500 Internal Server Error
            //if (response.status === 500) {
            //    $scope.showException(message, null, response.data);
            //}
            //else if (response.status === 503) {
            //    // ServiceUnavailable
            //    $scope.alerts.push({ type: 'error', msg: message + ", " + "service is unavailable" });
            //}
            //else {
            //    $scope.alerts.push({ type: 'error', msg: message });
            //}

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


    if ($scope.databases.length > 0) {
        $scope.selectedDatabase = $scope.databases[0];
    }

}


/**
 * Edit Database Controller
 */
function CreateDatabaseCtrl($scope, $http, Settings, Database, CommandStatus, $dialog, $rootScope, $location) {

    $scope.isBusy = false;
    $scope.alerts.length = 0;

    $scope.getDefaultSettings = function () {

        $scope.isBusy = true;
        $scope.alerts.length = 0;

        // Retrive database list
        Settings.query({ type: "database" }, function (response) {
            $scope.isBusy = false;
            response.tempDirectory = response.tempDirectory.replace("[DatabaseName]", response.name);
            response.imageDirectory = response.imageDirectory.replace("[DatabaseName]", response.name);
            response.transactionLogDirectory = response.transactionLogDirectory.replace("[DatabaseName]", response.name);
            response.dumpDirectory = response.dumpDirectory.replace("[DatabaseName]", response.name);

            $scope.settings = response;

            $scope.myForm.$setPristine(); // This disent work, the <select> breaks the pristine state :-(

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


        $http({ method: 'POST', url: '/adminapi/v1/databases', data: $scope.settings }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {

              $scope.isBusy = false;

              if (status == 202) {
                  // 202 Accepted
                  $scope.isBusy = true;

                  // Start polling the commandId for results
                  (function poll() {

                      $.ajax({
                          url: "/adminapi/v1/server/commands/" + response.commandId,

                          success: function (response) {

                              if (response.command == null) {
                                  $scope.showException("Invalid response, command property was null", null, ".createDatabase()");
                              }
                              else {

                                  $scope.status = response.command.message;

                                  if (response.command.isCompleted) {
                                      $scope.isBusy = false;
                                      $scope.status = "";
                                      // Check for errors
                                      if (response.command.errors.length > 0) {
                                          for (var i = 0; i < response.command.errors.length ; i++) {
                                              $scope.alerts.push({ type: 'error', msg: response.command.errors[i].message, helpLink: response.command.errors[i].helpLink });
                                          }
                                      }
                                      else {
                                          $scope.alerts.push({ type: 'success', msg: "Database " + $scope.settings.name + " was successfully created." });
                                          $scope.settings = null;
                                          // Refresh the databases list
                                          $rootScope.getDatabases();
                                          // $location.path("/databases");
                                      }
                                  }
                                  else {
                                      setTimeout(function () { poll(); }, pollFrequency); // wait 3 seconds than call ajax request again
                                  }
                              }
                          },

                          error: function (xhr, textStatus, thrownError) {

                              $scope.isBusy = false;

                              var response = JSON.parse(xhr.responseText);

                              if (xhr.status == 404) {
                                  // 404	Not Found
                                  $scope.alerts.push({ type: 'error', msg: response.message, helpLink: response.helpLink });
                              }
                              else if (xhr.status == 500) {
                                  // 500 Internal Server Error
                                  $scope.showException(response.message, response.helpLink, response.stackTrace);
                              }
                              else {
                                  $scope.showException("Unhandled http error " + textStatus, null, ".pollCommand()");
                              }

                          },

                          complete: function () {
                              //console.log("complete");
                              $scope.$apply();

                          },
                          dataType: "json",
                          timeout: pollTimeout
                      });
                  })();


              }
              else {
                  $scope.showException("Unhandled http statuscode " + status, null, ".createDatabase()");
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

    $scope.btnClick_createDatabase = function () {
        $scope.createDatabase();
    }

    $scope.btnClick_reset = function () {

        $scope.getDefaultSettings();
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