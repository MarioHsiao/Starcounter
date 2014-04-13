/**
 * ----------------------------------------------------------------------------
 * Applications page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('ApplicationsCtrl', ['$scope', '$log', 'NoticeFactory', 'HostModelService', 'ApplicationService', 'UserMessageFactory', function ($scope, $log, NoticeFactory, HostModelService, ApplicationService, UserMessageFactory) {

    // List of applications
    $scope.applications = HostModelService.applications;

    /**
     * Filter Applications
     * @param {object} application Application
     */
    $scope.filterApplications = function (application) {
        if ($scope.filterview == 'running') {
            return application.running;
        }
        return true;
    }

    /**
     * Get Console output
     * @param {object} application Application
     */
    $scope.btnGetConsoleOutput = function (application) {

        ApplicationService.refreshConsoleOuput(application, function () {

            // Success
        }, function (messageObject) {
            // Error
            UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);

        });
    }

    /**
     * Start Application
     * @param {object} application Application
     */
    $scope.btnStart = function (application) {

        ApplicationService.startApplication(application, function () {

            // Success

        }, function (messageObject) {

            // Error

            if (messageObject.isError) {
                UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
            }
            else {
                NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
            }

        });

    }


    /**
     * Stop Application
     * @param {object} application Application
     */
    $scope.btnStop = function (application) {

        var title = "Stop application";
        var message = "Do you want to stop the application " + application.Name;
        var buttons = [{ result: 0, label: 'Stop', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];

        UserMessageFactory.showMessageBox(title, message, buttons, function (result) {

            if (result == 0) {

                ApplicationService.stopApplication(application, function () { },
                    function (messageObject) {
                        // Error

                        if (messageObject.isError) {
                            UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                        }
                        else {
                            NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
                        }

                    });
            }

        });
    }


    /**
     * Restart Application
     * @param {object} application Application
     */
    $scope.btnRestart = function (application) {

        var title = "Restart application";
        var message = "Do you want to restart the application " + application.Name;
        var buttons = [{ result: 0, label: 'Restart', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];

        UserMessageFactory.showMessageBox(title, message, buttons, function (result) {

            if (result == 0) {

                ApplicationService.restartApplication(application, function () {
                    // Success

                }, function (messageObject) {
                    // Error

                    if (messageObject.isError) {
                        UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                    }
                    else {
                        NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
                    }


                });
            }

        });
    }


    /**
     * Remove Application from Cache/History
     * @param {object} application Application
     */
    $scope.btnRemove = function (application) {

        // Remove application from cache/history
        ApplicationService.removeFromHistory(application);
/*
        var title = "Remove application";
        var message = "Do you want to remove the application " + application.Name + " from the history";
        var buttons = [{ result: 0, label: 'Yes', cssClass: 'btn-danger' }, { result: 1, label: 'No', cssClass: 'btn' }];

        UserMessageFactory.showMessageBox(title, message, buttons, function (result) {

            if (result == 0) {
                ApplicationService.removeFromHistory(application);
            }

        });
*/
    }

    // Init
    // Refresh host model
    HostModelService.refreshHostModel(function () {
    }, function (messageObject) {
        // Error
        UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
    });


    // Get user state 'view'  (Icon/List)
    if (localStorage.getItem('applicationsViewMode') != null) {
        $scope.view = localStorage.getItem('applicationsViewMode');
    }
    else {
        $scope.view = "icon";
    }

    $scope.$watch('view', function (newValue, oldValue) {
        // Save user state
        localStorage.setItem('applicationsViewMode', newValue);
    });


    // Get user state 'filterview' (Running/History)
    if (localStorage.getItem('applicationsFilterView') != null) {
        $scope.filterview = localStorage.getItem('applicationsFilterView');
    }
    else {
        $scope.filterview = "running";
    }
    $scope.$watch('filterview', function (newValue, oldValue) {
        // Save user state
        localStorage.setItem('applicationsFilterView', newValue);
    });

    

}]);