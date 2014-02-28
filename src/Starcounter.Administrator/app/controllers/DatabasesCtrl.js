﻿/**
 * ----------------------------------------------------------------------------
 * Databases page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('DatabasesCtrl', ['$scope', '$log', 'NoticeFactory', 'HostModelService', 'DatabaseService', 'UserMessageFactory', function ($scope, $log, NoticeFactory, HostModelService, DatabaseService, UserMessageFactory) {

    // List of databases
    $scope.databases = HostModelService.databases;

    /**
     * Get Console output
     * @param {database} database Database name
     */
    $scope.btnGetConsoleOutput = function (database) {

        DatabaseService.refreshConsoleOuput(database, function () {

            // TODO
            // $("#console").scrollTop($("#console")[0].scrollHeight);


            // Success
        }, function (messageObject) {
            // Error
            UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);

        });

    }

    /**
     * Start database
     * @param {Database} database
     */
    $scope.btnStartDatabase = function (database) {

        DatabaseService.startDatabase(database, function () { },
            function (messageObject) {
                // Error
                if (messageObject.isError) {
                    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                }
                else {
                    NoticeFactory.ShowNotice({ type: 'error', msg: messageObject.message, helpLink: messageObject.helpLink });
                }

            });
    }


    /**
     * Stop Database
     * @param {Executable} executable
     */
    $scope.btnStopDatabase = function (database) {

        var title = "Stop database";
        var message = "Do you want to stop the database " + database.name;
        var buttons = [{ result: 0, label: 'Stop', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];

        UserMessageFactory.showMessageBox(title, message, buttons, function (result) {

            if (result == 0) {

                DatabaseService.stopDatabase(database, function () { },
                      function (messageObject) {
                          // Error
                          if (messageObject.isError) {
                              UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                          }
                          else {
                              NoticeFactory.ShowNotice({ type: 'error', msg: messageObject.message, helpLink: messageObject.helpLink });
                          }

                      });
            }

        });




    }


    // Init
    // Refresh host model
    HostModelService.refreshHostModel(function () {
    }, function (messageObject) {
        // Error
        UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
    });


    // Save user state
    if (localStorage.getItem('databaseViewMode') != null) {
        $scope.view = localStorage.getItem('databaseViewMode');
    }
    else {
        $scope.view = "list";
    }

    $scope.$watch('view', function (newValue, oldValue) {
        localStorage.setItem('databaseViewMode', newValue);
    });


}]);