/**
 * ----------------------------------------------------------------------------
 * Applications page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('ApplicationsCtrl', ['$scope', '$log', 'NoticeFactory', 'HostModelService', 'ApplicationService', 'InstalledApplicationService', 'AppStoreService', 'UserMessageFactory', function ($scope, $log, NoticeFactory, HostModelService, ApplicationService, InstalledApplicationService, AppStoreService, UserMessageFactory) {

    // List of applications
    $scope.applications = HostModelService.applications;
    $scope.installedApplications = HostModelService.installedApplications;
    $scope.databases = HostModelService.databases;

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

    }

    /**
     * Start Installed Application
     * @param {object} application Application
     */
    $scope.btnStartInstalled = function (installedapplication) {

        InstalledApplicationService.startApplication(installedapplication, installedapplication._databaseName, function () {
            // Success

            // Init
            // Refresh host model
            HostModelService.refreshHostModel(function () {

            }, function (messageObject) {
                // Error
                UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
            });

        }, function (messageObject) {
            // Error
            installedapplication.task = null;
            if (messageObject.isError) {
                UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
            }
            else {
                NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
            }
        });
        //ApplicationService.startInstalledApplication(installedapplication, installedapplication._databaseName, function () {
        //    // Success
        //}, function (messageObject) {
        //    // Error
        //    installedapplication.task = null;
        //    if (messageObject.isError) {
        //        UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
        //    }
        //    else {
        //        NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
        //    }
        //});
    }

    /**
     * Uninstall Application
     * @param {object} installedApplication Installed Application
     */
    $scope.btnUninstall = function (installedApplication) {

        var title = "Uninstall application";
        var message = "Do you want to uninstall the application " + installedApplication.DisplayName;
        var buttons = [{ result: 0, label: 'Uninstall', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];

        UserMessageFactory.showMessageBox(title, message, buttons, function (result) {

            if (result == 0) {

                AppStoreService.uninstall(installedApplication, function () {

                    InstalledApplicationService.refreshApplications();
                },
                    function (messageObject) {
                        // Error
                        InstalledApplicationService.refreshApplications(); // TODO: Handle callbacks
                        
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