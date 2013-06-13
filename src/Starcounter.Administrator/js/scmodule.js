/**
 * ----------------------------------------------------------------------------
 * sc.engine
 * ----------------------------------------------------------------------------
 */
var engineModule = angular.module('sc.engine', ['sc.engine.service'], function ($routeProvider) { });

engineModule.config(function ($routeProvider) {

    // List of all Engines
    $routeProvider.when('/engines', {
        templateUrl: '/partials/engines.html',
        controller: 'EnginesCtrl'
    });

    // One Engine
    $routeProvider.when('/engines/:name', {
        templateUrl: '/partials/engine.html',
        controller: 'EngineCtrl'
    });

});


/**
 * Engines Controller
 */
engineModule.controller('EnginesCtrl', ['$scope', '$dialog', 'Engine', function ($scope, $dialog, Engine) {

    $scope.alerts.length = 0;

    $scope.stopEngine = function (engine) {

        var job = { message: "Stopping engine " + engine.name };
        $scope.jobs.push(job);

        Engine.stop({ name: engine.name }, function (response) {
            // Success
            //
            // Example response: 
            // {
            //     "Code":10024,
            //     "Message":"The database engine is not running."
            // }

            // Remove job
            var index = $scope.jobs.indexOf(job);
            if (index != -1) {
                $scope.jobs.splice(index, 1);
            }

            $scope.refreshEngineAndExecutableList();

            // Remove engine or refresh engines
            //var index = $scope.engines.indexOf(engine);
            //if (index == -1) {
            //    // Recover by getting the compleat list
            //    $scope.refreshEnginesList();
            //}
            //else {
            //    $scope.engines.splice(index, 1);
            //}


        }, function (response) {
            // Error

            // Remove job
            var index = $scope.jobs.indexOf(job);
            if (index != -1) {
                $scope.jobs.splice(index, 1);
            }

            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
            }
            else {
                $scope.showException(response.data, null, null);
            }
        });
    }

    $scope.btnStopEngine = function (engine) {

        $scope.alerts.length = 0;

        var title = 'Stop engine';
        var msg = 'Do you want to stop the engine ' + engine.name;
        var btns = [{ result: 1, label: 'Cancel', cssClass: 'btn' }, { result: 0, label: 'Stop', cssClass: 'btn-danger' }];

        $dialog.messageBox(title, msg, btns)
          .open()
          .then(function (result) {
              if (result == 0) {
                  $scope.stopEngine(engine);
              }
          });


    }

    // Init
    $scope.refreshEnginesList();

}]);


/**
 * Engine Controller (TODO)
 */
engineModule.controller('EngineCtrl', ['$scope', '$routeParams', 'Engine', 'HostProcess', 'Database', function ($scope, $routeParams, Engine, HostProcess, Database) {

    $scope.alerts.length = 0;

    $scope.refreshEnginesList(function () {
        // Success
        for (var i = 0; i < $scope.engines.length ; i++) {
            if ($scope.engines[i].name == $routeParams.name) {
                $scope.engine = $scope.engines[i];
                break;
            }
        }

    }, function () {
        // Error
    });

}]);


/**
 * sc.engine.service
 */
angular.module('sc.engine.service', ['ngResource'], function ($provide) {

    // Engine
    $provide.factory('Engine', function ($resource) {
        return $resource('/api/engines/:name', { name: '@name' }, {
            query: { method: 'GET', isArray: false },
            get: { method: 'GET' },
            start: { method: 'POST' },
            stop: { method: 'DELETE' },
            startExecutable: { method: 'POST', url: '/api/engines/:name/executables' }
        });
    });



});

/*
 * ----------------------------------------------------------------------------
 * sc.database
 * ----------------------------------------------------------------------------
 */
var databaseModule = angular.module('sc.database', ['sc.database.service'], function ($routeProvider) { });

databaseModule.config(function ($routeProvider) {

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


});


/**
 * Databases Controller
 */
databaseModule.controller('DatabasesCtrl', ['$scope', '$dialog', '$http', 'Database', function ($scope, $dialog, $http, Database) {

    $scope.alerts.length = 0;

    $scope.stopDatabase = function (database) {


        $scope.refreshEnginesList(function () {
            // Success

            var engine = $scope.getEngine(database.name);
            if (engine == null) {
                // TODO: Refresh list and try again
                $scope.alerts.push({ type: 'error', msg: "Can not find database engine " + database.name });
            }
            else {
            }

            $http({ method: 'DELETE', url: engine.databaseUri }).

              // A response status code between 200 and 299 is considered a success status
              success(function (response, status, headers, config) {

                  // Refresh engine and executable list
                  $scope.refreshEngineAndExecutableList();

                  // Refresh database list
                  $scope.refreshDatabaseProcessStatus();

              }).
              error(function (response, status, headers, config) {

                  if (status == 405) {
                      // 405 MethodNotAllowed
                      $scope.alerts.push({ type: 'info', msg: "Database " + database.name + " can not be stopped (405 MethodNotAllowed)", helpLink: null });
                  }
                  else if (status == 500) {
                      // 500 Internal Server Error
                      $scope.showException(response.message, response.helpLink, response.stackTrace);
                  }
                  else {
                      $scope.showException("Unhandled http statuscode " + status, null, ".stopDatabase()");
                  }

              });


        }, function () {
            // Error
        })

    }

    $scope.deleteDatabase = function (database) {

        Database.delete({ name: database.name }, function () {
            // Success

            // Refresh engine and executable list
            $scope.refreshEngineAndExecutableList();

            // Refresh database list
            $scope.refreshDatabaseProcessStatus();

        }, function (response) {
            // Error

            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
            }
            else {
                if (response.status == 405) {
                    // 405 MethodNotAllowed
                    $scope.alerts.push({ type: 'info', msg: "Database " + database.name + " can not be deleted (405 MethodNotAllowed)", helpLink: null });
                }
                else {
                    $scope.showException(response.data, null, null);
                }
            }
        });

    }

    $scope.btn_createDatabase = function () {
        $scope.alerts.length = 0;
        $scope.alerts.push({ type: 'info', msg: "Not implemented" });
    }

    $scope.btnDeleteDatabase = function (database) {

        $scope.alerts.length = 0;

        var title = 'Stop engine';
        var msg = 'Do you want to delete the database ' + database.name;
        var btns = [{ result: 1, label: 'Cancel', cssClass: 'btn' }, { result: 0, label: 'Delete', cssClass: 'btn-danger' }];


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

        var title = 'Stop engine';
        var msg = 'Do you want to stop the database ' + database.name;
        var btns = [{ result: 1, label: 'Cancel', cssClass: 'btn' }, { result: 0, label: 'Stop', cssClass: 'btn-danger' }];

        $dialog.messageBox(title, msg, btns)
          .open()
          .then(function (result) {
              if (result == 0) {
                  $scope.stopDatabase(database);
              }
          });


    }

    // Init
    $scope.refreshDatabaseList();

}]);


/**
 * Database Controller
 */
databaseModule.controller('DatabaseCtrl', ['$scope', '$routeParams', 'Database', function ($scope, $routeParams, Database) {

    $scope.alerts.length = 0;

    // Retrive the console output for a specific database
    $scope.getConsole = function (name) {

        Database.console({ name: name }, function (response) {
            // Success

            if (response.hasOwnProperty("console") == false) {
                $scope.showException("Invalid response, missing console property", null, ".getConsole()");
            }
            else {
                if (response.console != null) {
                    $scope.console = response.console.replace(/\r\n/g, "<br>");  // Replace all occurrences of \r\n with the html tag <br>
                }
                else {
                    $scope.console = "";
                }
                $("#console").scrollTop($("#console")[0].scrollHeight); // TODO: Do this in the next cycle?
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
                $scope.showException("Unhandled http statuscode " + response.status, null, ".getConsole()");
            }

        });
    }

    // User clicked the "Refresh" button
    $scope.btnClick_refreshConsole = function () {
        $scope.alerts.length = 0;
        $scope.getConsole($scope.database.name);
    }

    // Init
    $scope.getDatabaseWithConfiguration($routeParams.name, function (database) {
        // Success

        $scope.database = database;
        $scope.refreshDatabaseProcessStatus(database, function () {
            // Success
            if (database.running) {
                $scope.getConsole(database.name);
            }
        }, function () {
            // Error
        });


    }, function () {
        // Error
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
databaseModule.controller('DatabaseEditCtrl', ['$scope', '$routeParams', '$http', 'Database_Fallback', function ($scope, $routeParams, $http, Database_Fallback) {

    $scope.alerts.length = 0;
    $scope.database = null;

    // Refresh database
    $scope.refreshDatabaseFromAdminApi = function () {
        Database_Fallback.get({ name: $routeParams.name }, function (response) {
            // Success
            $scope.database = response.database;

        }, function (response) {
            // Error
            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
            }
            else {

                $scope.showException(response.data, null, null);
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

    $scope.btnSave = function () {
        $scope.saveSettings($scope.database.name);
    }

    // Init
    $scope.refreshDatabaseFromAdminApi();

}]);


/**
 * sc.database.service
 */
angular.module('sc.database.service', ['ngResource'], function ($provide) {

    // Database
    $provide.factory('Database', function ($resource) {
        return $resource('/api/databases/:name', { name: '@name' }, {
            query: { method: 'GET', isArray: false },
            get: { method: 'GET' },
            delete: { method: 'DELETE' },
            stop: { method: 'DELETE', url: '/api/engines/:name/db' },
            status: { method: 'GET', url: '/api/engines/:name/db' },
            console: { method: 'GET', url: '/adminapi/v1/databases/:name/console' }
        });
    });

    // OLD API
    $provide.factory('Database_Fallback', function ($resource) {
        return $resource('/adminapi/v1/databases/:name', { name: '@name' }, {
            get: { method: 'GET', isArray: false }
        });
    });

});


/**
 * ----------------------------------------------------------------------------
 * sc.executable
 * ----------------------------------------------------------------------------
 */
var executableModule = angular.module('sc.executable', ['sc.executable.service'], function ($routeProvider) {

    // List of all Exectuables
    $routeProvider.when('/executables', {
        templateUrl: '/partials/executables.html',
        controller: 'ExecutablesCtrl'
    });

    // List of all Exectuables
    // TODO: Use the engine name in the path
    $routeProvider.when('/executable_start', {
        templateUrl: '/partials/executable_start.html',
        controller: 'ExecutableStartCtrl'
    });
});


/**
 * Executables Controller
 */
executableModule.controller('ExecutablesCtrl', ['$scope', '$routeParams', '$dialog', 'Database', 'Engine', 'HostProcess', function ($scope, $routeParams, $dialog, Database, Engine, HostProcess) {

    $scope.alerts.length = 0;


    $scope.stopHostProcess = function (engine) {

        $scope.isBusy = true;

        var job = { message: "Stopping " + engine.name };
        $scope.jobs.push(job);

        HostProcess.stop({ name: engine.name }, function (response) {
            // Success
            //
            // Example response: 
            // {
            //    "Code":10024,
            //    "Message":"The database engine is not running."
            // }

            $scope.isBusy = false;

            // Remove job
            var index = $scope.jobs.indexOf(job);
            if (index != -1) {
                $scope.jobs.splice(index, 1);
            }



            // Remove executable
            $scope.refreshEngineAndExecutableList();

            //for (var i = 0; i < $scope.executables.length ; i++) {
            //    if ($scope.executables[i].path == executable.path && $scope.executables[i].databaseName == executable.databaseName) {
            //        $scope.executables.splice(i, 1);
            //        break;
            //    }
            //}


        }, function (response) {
            // Error

            // Remove job
            var index = $scope.jobs.indexOf(job);
            if (index != -1) {
                $scope.jobs.splice(index, 1);
            }

            $scope.isBusy = false;
            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
            }
            else {
                $scope.showException(response.data, null, null);
            }

            // recover the list
            $scope.refreshEngineAndExecutableList();
        });


    }

    $scope.btnStopAllExecutable = function (engine) {

        $scope.alerts.length = 0;

        var title = 'Stop all running executable';
        var msg = 'Do you want to stop all running executable in ' + engine.name;
        var btns = [{ result: 1, label: 'Cancel', cssClass: 'btn' }, { result: 0, label: 'Stop', cssClass: 'btn-danger' }];

        $dialog.messageBox(title, msg, btns)
          .open()
          .then(function (result) {
              if (result == 0) {
                  $scope.stopHostProcess(engine);
              }
          });
    }

    // Init
    $scope.refreshEngineAndExecutableList();

}]);


/**
 * Executable Start Controller
 */
executableModule.controller('ExecutableStartCtrl', ['$scope', '$routeParams', '$location', 'Database', 'Engine', function ($scope, $routeParams, $location, Database, Engine) {

    $scope.alerts.length = 0;
    $scope.selectedDatabaseName = null;

    $scope.file = "";

    $scope.prepareExecutable = function (job, engineName, successCallback, errorCallback) {

        $scope.assureEngine(job, engineName, function () {
            // success

            // Engine is running

            // TODO:
            var startedBy = "unknown-user@unknown-computer (via webadmin)";
            var arguments = []

            var data = { engineName: engineName, file: $scope.file, startedBy: startedBy, arguments: arguments };

            $scope.startExecutable(job, data, function (message) {
                // success

                // Refresh database process status
                $scope.refreshDatabaseList(function () {
                    // Success
                    successCallback();
                }, function () {
                    // Error

                    // Note it was only the refresh database list that failed,
                    // the executable was sucessfully started
                    successCallback();
                });


            }, function () {
                // Error
                errorCallback();
            });


        }, function () {
            // Error
            errorCallback();
        });

    }

    // Start the engine if it's not started
    $scope.assureEngine = function (job, name, successCallback, errorCallback) {

        job.message = "Retreiving engine status";

        Engine.get({ name: name }, function (response) {
            // Success
            successCallback();

        }, function (response) {

            // Error
            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
                errorCallback();
            }
            else {
                if (response.status == 404) {
                    // 404	Not Found (Not started)

                    // Start the Engine
                    //$scope.status = "Starting engine";

                    var databaseName = name;
                    var engineData = { Name: databaseName, NoDb: false, LogSteps: false };    // TODO: get NoDb and LogSteps from argumens

                    $scope.startEngine(job, engineData, function () {
                        // Success 
                        successCallback();
                    }, function () {
                        // Error 
                        errorCallback();
                    });
                }
                else {
                    $scope.showException(response.data, null, null);
                    errorCallback();
                }
            }

        });
    }

    // Start Engine
    $scope.startEngine = function (job, engineData, successCallback, errorCallback) {

        job.message = "Starting engine " + engineData.Name;

        Engine.start({}, engineData, function (response) {
            // Success
            // {"Uri":"http://headsutv19:8181/api/engines/default","NoDb":false,"LogSteps":false,"Database":{"Name":"default","Uri":"http://localhost:8181/api/databases/default"},"DatabaseProcess":{"Uri":"http://localhost:8181/api/engines/default/db","Running":true},"CodeHostProcess":{"Uri":"http://localhost:8181/api/engines/default/host","PID":7200},"Executables":{"Uri":"http://localhost/api/engines/default/executables","Executing":[]}}

            job.message = "";
            successCallback();

        }, function (response) {
            // Error

            job.message = "";

            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
            }
            else {
                // TODO: What error's can we get here?
                if (response.status == 404) {
                    // 404	Not Found
                    $scope.alerts.push({ type: 'error', msg: response.data, helpLink: null });
                }
                else {
                    $scope.showException(response.data, null, null);
                }
            }
            errorCallback();

        });

    }

    // Start Executable
    $scope.startExecutable = function (job, data, successCallback, errorCallback) {


        job.message = "Starting Executable";

        var bodyData = { Path: data.file, StartedBy: data.startedBy, Arguments: data.arguments };

        Engine.startExecutable({ name: data.engineName }, bodyData, function (response) {
            // Success
            job.message = "";

            $scope.alerts.push({ type: 'info', msg: response.Description, helpLink: null });

            successCallback();
        }, function (response) {
            // Error

            job.message = "";
            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
            }
            else {

                if (response.status == 404) {
                    // 404	Not Found
                    $scope.alerts.push({ type: 'error', msg: response.data, helpLink: null });
                }
                else if (response.status == 409) {
                    // 409	Conflict
                    $scope.alerts.push({ type: 'error', msg: response.data.Text, helpLink: response.data.Helplink });
                }
                else if (response.status == 422) {
                    // 422 Unprocessable Entity (WebDAV; RFC 4918)
                    // {"Text":"The executable file cound not be found. File: 77","ServerCode":10019,"Helplink":"http://www.starcounter.com/wiki/SCERR10019","LogEntry":""}
                    $scope.alerts.push({ type: 'error', msg: response.data.Text, helpLink: response.data.Helplink });
                }
                else if (response.status == 500) {
                    // 422 Unprocessable Entity (WebDAV; RFC 4918)
                    $scope.showException(response.data, null, null);
                }
                else {
                    $scope.showException("Error retriving the engine", null, null);
                }
                errorCallback();
            }

        });

    }

    $scope.btn_startExecutable = function () {

        // Clear any previous alerts
        $scope.alerts.length = 0;

        var job = { message: "Starting Executable" };
        $scope.jobs.push(job);

        $scope.prepareExecutable(job, $scope.selectedDatabaseName, function () {
            // success

            $scope.refreshEngineAndExecutableList();

            // remove job
            var index = $scope.jobs.indexOf(job);
            if (index != -1) {
                $scope.jobs.splice(index, 1);
            }


            //$location.path("/executables");

        }, function () {
            // Error
            var index = $scope.jobs.indexOf(job);
            if (index != -1) {
                $scope.jobs.splice(index, 1);
            }
            // $scope.isBusy = false;
        });
    }

    // Init
    $scope.refreshDatabaseList(function () {
        // success
        if ($scope.databases.length > 0) {
            $scope.selectedDatabaseName = $scope.databases[0].name;
        }

    });

}]);


/**
 * sc.executable.service
 */
angular.module('sc.executable.service', ['ngResource'], function ($provide) {

    // Host process
    $provide.factory('HostProcess', function ($resource) {
        return $resource('/api/engines/:name/host', { name: '@name' }, {
            get: { method: 'GET' },
            stop: { method: 'DELETE' }
        });
    });

});


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
            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
            }
            else {

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
            }
        });
    }


    // User clicked the "Execute" button
    $scope.btnExecute = function () {
        $scope.alerts.length = 0;
        $scope.executeQuery();
    }

    // Init
    $scope.refreshDatabaseList(function () {
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

    $provide.factory('SqlQuery', function ($resource) {
        return $resource('/adminapi/v1/sql/:name', { name: '@name' }, {
            send: { method: 'POST', isArray: false }    // We need to override this (the return type is not an array)
        });
    });


});


/**
 * sc.sqlquery.directive
 */
angular.module('sc.sqlquery.directive', ['ui.config', 'uiHandsontable']);


/**
 * ----------------------------------------------------------------------------
 * sc.log
 * ----------------------------------------------------------------------------
 */
var logModule = angular.module('sc.log', ['sc.log.service', 'sc.log.directive'], function ($routeProvider) {

    $routeProvider.when('/log', {
        templateUrl: '/partials/log.html',
        controller: 'LogCtrl'
    });

});


/**
 * Log Controller
 */
logModule.controller('LogCtrl', ['$scope', 'Log', function ($scope, Log) {

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

        Log.query($scope.filterModel, function (response) {
            // Success
            $scope.log = response;

            if (response == null) {
                $scope.showException("Invalid response, log property was null", null, ".saveSettings()");
            }

        }, function (response) {
            // Error
            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
            }
            else {

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
            }

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
 * sc.log.service
 */
angular.module('sc.log.service', ['ngResource'], function ($provide) {

    $provide.factory('Log', function ($resource) {
        return $resource('/adminapi/v1/log', {}, {
            query: { method: 'GET', isArray: false }
        });
    });

});


/**
 * sc.log.directive
 */
angular.module('sc.log.directive', ['uiHandsontable']);


/**
 * ----------------------------------------------------------------------------
 * sc.gateway
 * ----------------------------------------------------------------------------
 */
var gatewayModule = angular.module('sc.gateway', ['sc.gateway.service'], function ($routeProvider) {

    $routeProvider.when('/gateway', {
        templateUrl: '/partials/gateway.html',
        controller: 'GatewayCtrl'
    });

});


/**
 * Gateway Controller
 */
gatewayModule.controller('GatewayCtrl', ['$scope', '$http', function ($scope, $http) {

    $scope.alerts.length = 0;

    // Get Gateway information
    $scope.refreshGatewayStats = function () {

        // We need to use $http instead of $resource ($resource dosent support custom headers yet)
        $http({ method: 'GET', url: '/gwstats', headers: { 'Accept': 'text/html,text/plain,*/*' } }).
          success(function (data, status, headers, config) {
              $scope.gwStats = data;
          }).
          error(function (data, status, headers, config) {
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

    // User clicked the "Refresh" button
    $scope.btnRefresh = function () {
        $scope.alerts.length = 0;
        $scope.refreshGatewayStats();
    }

    // Init
    $scope.refreshGatewayStats();

}]);


/**
 * sc.gateway.service
 */
angular.module('sc.gateway.service', ['ngResource'], function ($provide) {

    // TODO: Headers dosent work.. (Not yet supported by angular?)
    $provide.factory('Gateway', function ($resource) {
        return $resource('/gwstats', {}, {
            send: { method: 'GET', isArray: false, headers: { 'Content-Type': 'text/html,text/plain,*/*', 'Accept': 'text/html,text/plain,*/*' } }    // We need to override this (the return type is not an array)
        });
    });

});


/**
 * ----------------------------------------------------------------------------
 * sc.server
 * ----------------------------------------------------------------------------
 */
var serverModule = angular.module('sc.server', ['sc.server.service'], function ($routeProvider) {

    $routeProvider.when('/server', {
        templateUrl: '/partials/server.html',
        controller: 'ServerCtrl'
    });

    $routeProvider.when('/server_edit', {
        templateUrl: '/partials/server_edit.html',
        controller: 'ServerEditCtrl'
    });

});


/**
 * Server Controller
 */
serverModule.controller('ServerCtrl', ['$scope', 'Server', function ($scope, Server) {

    $scope.alerts.length = 0;
    $scope.server = null;

    // Retrive server information
    $scope.refreshtServer = function () {

        Server.get(function (response) {
            $scope.server = response.server;
        }, function (response) {

            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
            }
            else {

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
            }

        });

    }

    // Init
    $scope.refreshtServer();

}]);


/**
 * Server Edit Controller
 */
serverModule.controller('ServerEditCtrl', ['$scope', '$http', 'Server', function ($scope, $http, Server) {

    $scope.alerts.length = 0;
    $scope.server = null;

    // Retrive server information
    $scope.refreshtServer = function () {

        Server.get(function (response) {
            $scope.server = response.server;
        }, function (response) {

            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
            }
            else {

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
            }

        });

    }

    // Save database information
    $scope.saveSettings = function () {

        $http({ method: 'PUT', url: '/adminapi/v1/server', data: $scope.server }).

          // A response status code between 200 and 299 is considered a success status
          success(function (response, status, headers, config) {

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

          });
    }

    $scope.btnSave = function () {
        $scope.alerts.length = 0;
        $scope.saveSettings();
    }

    $scope.refreshtServer();

}]);


/**
 * sc.server.service
 */
angular.module('sc.server.service', ['ngResource'], function ($provide) {

    $provide.factory('Server', function ($resource) {
        return $resource('/adminapi/v1/server', {}, {
            query: { method: 'GET', isArray: false }
        });
    });

});


/**
 * ----------------------------------------------------------------------------
 * scadmin
 * ----------------------------------------------------------------------------
 */
angular.module('scadmin', ['sc.engine', 'sc.database', 'sc.executable', 'sc.sqlquery', 'sc.log', 'sc.gateway', 'sc.server', 'sc.service', 'sc.directive'], function ($routeProvider, $locationProvider, $httpProvider) {


    //$routeProvider.when('/main', {
    //    templateUrl: '/partials/main.html',
    //    controller: MainCtrl
    //});

    $routeProvider.when('/database_create', {
        templateUrl: '/partials/database_create.html',
        controller: DatabaseCreateCtrl
    })

    $routeProvider.otherwise({ redirectTo: '/executables' });

    //$locationProvider.html5Mode(true);

});


/**
 * sc.service
 */
angular.module('sc.service', ['ngResource'], function ($provide) {

    // Get database or server default settings
    $provide.factory('Settings', function ($resource) {
        return $resource('/adminapi/v1/settings/default/:type', { type: '@type' }, {
            query: { method: 'GET', isArray: false }
        });
    });

    // Get command status
    $provide.factory('CommandStatus', function ($resource) {
        return $resource('/adminapi/v1/server/commands/:commandId', { commandId: '@commandId' }, {
            query: { method: 'GET', isArray: false }
        });
    });

});

/**
 * sc.directive
 */
angular.module('sc.directive', ['ui', 'ui.bootstrap']);


/**
 * App Controller
 */
function HeadCtrl($scope, $location, $http, $dialog, Engine, Database) {

    $scope.alerts = [];
    $scope.jobs = [];           // { message:"default" }
    $scope.engines = [];        // { uri:"http://localhost:8181/api/engines/default", name:"default", databaseUri:"", hostUri:"", configuration: {noDb:true, logSteps=true} }
    $scope.databases = [];      // { "uri":"http://headsutv19:8181/api/databases/default", name:"default", running:true, configuration:{} }
    $scope.executables = [];    // { path:"c:\tmp\some.exe", databaseName:"default" }

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

    // Get Remote Engine and returns a local engine
    $scope.getEngineToLocalEngine = function (name, successCallback, errorCallback) {

        Engine.get({ name: name }, function (response, headers) {
            // Success

            remoteEngine = response;

            // Make path's relative
            var relativeDatabaseUri = toRelativePath(remoteEngine.DatabaseProcess.Uri);
            var relativeHostUri = toRelativePath(remoteEngine.CodeHostProcess.Uri);


            // Create engine instance
            var engine = {
                uri: remoteEngine.Uri,
                name: name,
                databaseUri: relativeDatabaseUri,
                hostUri: relativeHostUri,
                hostPID: remoteEngine.CodeHostProcess.PID,
                databaseName: null, // TODO: is this needed?
                configuration: {
                    noDb: remoteEngine.NoDb,
                    logSteps: remoteEngine.LogSteps
                }
            };

            if (successCallback != null) {
                successCallback(engine);
            }

        }, function (response) {
            // Error

            if (errorCallback != null) {
                errorCallback();
            }
        });

    }

    $scope.getEngine = function (name) {

        for (var i = 0; i < $scope.engines.length ; i++) {
            if ($scope.engines[i].name == name) {
                return $scope.engines[i];
            }
        }

        return null;
    }

    // Refresh Engine List
    $scope.refreshEnginesList = function (successCallback, errorCallback) {
        $scope.engines.length = 0;

        Engine.query(function (response) {
            // Success
            var countDown = response.Engines.length;
            for (var i = 0; i < response.Engines.length ; i++) {

                $scope.getEngineToLocalEngine(response.Engines[i].Name, function (engine) {
                    // Success
                    countDown--;

                    $scope.engines.push(engine);

                    if (countDown == 0) {
                        if (successCallback != null) {
                            successCallback();
                        }
                    }

                }, function (response) {
                    // Error
                    countDown--;

                    if (errorCallback != null) {
                        errorCallback();
                    }
                });

            }

            if (response.Engines.length == 0) {
                if (successCallback != null) {
                    successCallback();
                }
            }

        }, function (response) {
            // Error
            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
            }
            else {

                $scope.showException(response.data, null, null);
            }

            if (errorCallback != null) {
                errorCallback();
            }

        });
    }

    // Refresh Database List and Status
    $scope.refreshDatabaseList = function (successCallback, errorCallback) {

        $scope.databases.length = 0;

        // Get database list
        Database.query(function (response) {
            // Success

            // Example response: 
            // {
            //    "Databases":[{
            //    "Name":"default",
            //    "Uri":"http://localhost:8181/api/databases/default"}
            // ]}

            // Get the running status of each database
            for (var i = 0; i < response.Databases.length ; i++) {

                var remoteDatabase = response.Databases[i];

                // Create database instance
                var database = { uri: remoteDatabase.Uri, name: remoteDatabase.Name, running: false }
                $scope.databases.push(database);

                // TODO:  Maybe move this outside the refreshDatabaseList function
                // refresh database process status
                $scope.refreshDatabaseProcessStatus(database);
            }

            if (successCallback != null) {
                successCallback();
            }

        }, function (response) {
            // Error
            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
            }
            else {

                $scope.showException(response.data, null, null);
            }

            if (errorCallback != null) {
                errorCallback();
            }
        });

    }

    // Refresh database (host) process status 
    $scope.refreshDatabaseProcessStatus = function (database, successCallback, errorCallback) {

        Database.status({ name: database.name }, function (response) {
            // Success
            //
            // Example response: 
            // {
            //     "Uri":"http://localhost:8181/api/engines/default/db",
            //     "Running":true
            // }
            database.running = response.Running;
            if (successCallback != null) {
                successCallback();
            }

        }, function (response) {
            // Error
            database.running = false;

            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
            }
            else {
                if (response.status == 404) {
                    // 404	Not Found = Not running
                }
                else {
                    $scope.showException(response.data, null, null);
                }
            }
            if (errorCallback != null) {
                errorCallback();
            }

        });

    }

    // Refresh ExecutableList
    $scope.refreshEngineAndExecutableList = function () {

        $scope.executables.length = 0;

        $scope.refreshEnginesList(function () {

            // Success
            for (var i = 0 ; i < $scope.engines.length ; i++) {

                var engine = $scope.engines[i];

                $scope.getEngineExecutableList(engine, function (executables) {
                    // Success

                    for (var n = 0 ; n < executables.length ; n++) {
                        $scope.executables.push(executables[n]);
                    }

                }, function () {
                    // Error
                });
            }
        }, function () {
            // Error
        });

    }

    $scope.getEngineExecutableList = function (engine, successCallback, errorCallback) {

        Engine.get({ name: engine.name }, function (response, headers) {
            // Success
            var executables = [];

            // Add Executables to list
            for (var n = 0; n < response.Executables.Executing.length ; n++) {
                var remoteExecutable = response.Executables.Executing[n];

                var executable = {};
                executable.path = remoteExecutable.Path;
                executable.engine = engine;

                // Get database
                executable.databaseName = response.Database.Name;

                // Generate executable name (removing the path and extention)
                // Hopefully this 'extra' property will be included in the api in the future
                //var fullpath = remoteExecutable.Path;
                //var x = fullpath.lastIndexOf("\\");
                //if (x != -1) {
                //    var filename = fullpath.slice(x + 1);
                //    x = filename.lastIndexOf(".");
                //    if (x != -1) {
                //        var filenameWithoutExtention = filename.substr(0, x);
                //        executable.name = filenameWithoutExtention;
                //    }
                //}
                executables.push(executable);
            }

            if (successCallback) {
                successCallback(executables);
            }

        }, function (response) {
            // Error
            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
            }
            else {

                if (response.status == 501) {
                    // 501 Not Implemented
                    $scope.showException("Could not retrive the engine " + engine.name + " (501 Not Implemented)", null, null);
                }
                else if (response.status == 404) {
                    // 404	Not Found
                    $scope.alerts.push({ type: 'error', msg: response.data.Text, helpLink: response.data.Helplink });
                }
                else {
                    $scope.showException(response.data, null, null);
                }
            }

            if (errorCallback) {
                errorCallback();
            }

        });

    }

    $scope.getDatabaseByName = function (name) {

        for (var i = 0 ; i < $scope.databases.length ; i++) {
            var database = $scope.databases[i];
            if (database.name == name) {
                return database;
            }
        }
        return null;
    }

    // Get database with configuration
    $scope.getDatabaseWithConfiguration = function (name, successCallback, errorCallback) {

        Database.get({ name: name }, function (response) {
            // Success  
            //
            // Example response: 
            // "Uri":"",
            // "Name":"default",
            // "Configuration":{
            //     "Uri":""
            // },
            // "Engine":{
            //     "Uri":"http://localhost:8181/api/engines/default"
            // }

            var globalDatabase = $scope.getDatabaseByName(response.Name)
            if (globalDatabase != null) {
                globalDatabase.configuration = response.Configuration; // TODO:

                if (successCallback != null) {
                    successCallback(globalDatabase);
                }

            }
            else {

                $scope.refreshDatabaseList(function () {
                    // Success
                    var globalDatabase = $scope.getDatabaseByName(response.Name)
                    if (globalDatabase != null) {
                        globalDatabase.configuration = response.Configuration; // TODO:

                        if (successCallback != null) {
                            successCallback(globalDatabase);
                        }

                    }
                    else {
                        console.log("Warning - We could not find the database " + name + " in the client global list");

                        if (errorCallback != null) {
                            errorCallback();
                        }

                    }

                });

            }


        }, function (response) {
            // Error
            if (response instanceof SyntaxError) {
                $scope.showException(response.message, null, response.stack);
            }
            else {
                if (response.status == 404) {
                    // 404	Not Found
                    $scope.alerts.push({ type: 'error', msg: "Database " + $routeParams.name + " not Found", helpLink: null });
                }
                else {
                    $scope.showException(response.data, null, null);
                }
            }

            if (errorCallback != null) {
                errorCallback();
            }


        });

    }


}


/**
 * Main Controller
 */
function MainCtrl($scope) {

}


/**
 * Database Create Controller
 */
function DatabaseCreateCtrl($scope, $http, $dialog, $rootScope, $location, Settings, Database, CommandStatus) {

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
                                          //$rootScope.getDatabases();
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


function toRelativePath(url) {
    var a = document.createElement('a');
    a.href = url;
    return a.pathname;
}
