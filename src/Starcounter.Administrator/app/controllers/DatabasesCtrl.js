/**
 * ----------------------------------------------------------------------------
 * Databases page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('DatabasesCtrl', ['$scope', '$log', 'NoticeFactory', 'HostModelService', 'DatabaseService', 'UserMessageFactory', function ($scope, $log, NoticeFactory, HostModelService, DatabaseService, UserMessageFactory) {

    // List of databases
    $scope.databases = HostModelService.databases;



    /**
     * Get Console output
     * @param {object} database Database
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
     * @param {object} database Database
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
     * @param {object} database Database
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


    // Get user state
    if (localStorage.getItem('databaseViewMode') != null) {
        $scope.view = localStorage.getItem('databaseViewMode');
    }
    else {
        $scope.view = "icon";
    }

    $scope.$watch('view', function (newValue, oldValue) {
        // Save user state
        localStorage.setItem('databaseViewMode', newValue);
    });

    $scope.change = function (database) {
        $log.warn("console changed");
    }


}]);