/**
 * ----------------------------------------------------------------------------
 * Executables page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('ExecutablesCtrl', ['$scope', '$log', 'NoticeFactory', 'HostModelService', 'ExecutableService', 'UserMessageFactory', function ($scope, $log, NoticeFactory, HostModelService, ExecutableService, UserMessageFactory) {

    // List of Executables
    $scope.executables = HostModelService.executables;


    /**
     * Get Console output
     * @param {object} executable Executable
     */
    $scope.btnGetConsoleOutput = function (executable) {

        ExecutableService.refreshConsoleOuput(executable, function () {

            // TODO
            // $("#console").scrollTop($("#console")[0].scrollHeight);


            // Success
        }, function (messageObject) {
            // Error
            UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);

        });

    }


    /**
     * Stop Executable
     * @param {object} executable Executable
     */
    $scope.btnStopExecutable = function (executable) {

        var title = "Stop executable";
        var message = "Do you want to stop the executable " + executable.Name;
        var buttons = [{ result: 0, label: 'Stop', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];

        UserMessageFactory.showMessageBox(title, message, buttons, function (result) {

            if (result == 0) {

                ExecutableService.stopExecutable(executable, function () { },
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


    /**
     * Restart Executable
     * @param {object} executable Executable
     */
    $scope.btnRestartExecutable = function (executable) {

        var title = "Restart executable";
        var message = "Do you want to restart the executable " + executable.Name;
        var buttons = [{ result: 0, label: 'Restart', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];

        UserMessageFactory.showMessageBox(title, message, buttons, function (result) {

            if (result == 0) {

                ExecutableService.restartExecutable(executable, function () {
                    // Success
                    //            NoticeFactory.ShowNotice({ type: "info", msg: "Executable " + executable.path + " successfully started" });

                }, function (messageObject) {
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
    if (localStorage.getItem('executablesViewMode') != null) {
        $scope.view = localStorage.getItem('executablesViewMode');
    }
    else {
        $scope.view = "icon";
    }

    $scope.$watch('view', function (newValue, oldValue) {
        // Save user state
        localStorage.setItem('executablesViewMode', newValue);
    });

}]);