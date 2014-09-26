/**
 * ----------------------------------------------------------------------------
 * Applications page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('AppStoreCtrl', ['$scope', '$log', 'NoticeFactory', 'HostModelService', 'ApplicationService', 'InstalledApplicationService', 'AppStoreService', 'UserMessageFactory', function ($scope, $log, NoticeFactory, HostModelService, ApplicationService, InstalledApplicationService, AppStoreService, UserMessageFactory) {

    // List of applications
    $scope.installedApplications = HostModelService.installedApplications;
    $scope.appStoreApplications = AppStoreService.appStoreApplications;
    $scope.appStoreService = AppStoreService.appStoreService;
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
     * Install (Install) AppStore Application
     * @param {object} appStoreApplication AppStore Application
     */
    $scope.btnInstall = function (appStoreApplication) {

        var title = "Install application";
        var message = "Do you want to install the application " + appStoreApplication.DisplayName;
        var buttons = [{ result: 0, label: 'Install', cssClass: 'btn-success' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];

        UserMessageFactory.showMessageBox(title, message, buttons, function (result) {

            if (result == 0) {

                AppStoreService.installApplication(appStoreApplication, function () { },
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
     * Start Installed Application
     * @param {object} application Application
     */
    $scope.btnStart = function (installedapplication) {

        // Create application object
        var application = {
            "Name": installedapplication.DisplayName,
            "databaseName": "default",  // TODO:
            "Path": installedapplication.Executable,
            "ApplicationFilePath": installedapplication.Executable,
            "WorkingDirectory": installedapplication.ResourceFolder,
            "StartedBy": "Starcounter Administrator"
        };

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
     * Delete (Uninstall) Application
     * @param {object} installedApplication Installed Application
     */
    $scope.btnUninstall = function (installedApplication) {

        var title = "Uninstall application";
        var message = "Do you want to uninstall the application " + installedApplication.DisplayName;
        var buttons = [{ result: 0, label: 'Uninstall', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];

        UserMessageFactory.showMessageBox(title, message, buttons, function (result) {

            if (result == 0) {

                InstalledApplicationService.deleteInstalledApplication(installedApplication, function () { },
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
 

    // Init
    // Refresh host model
    HostModelService.refreshHostModel(function () {
    }, function (messageObject) {
        // Error
        UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
    });


    AppStoreService.refreshApplications(function () {
    }, function (messageObject) {
        // Error
//        NoticeFactory.ShowNotice({ type: 'info', msg: "The Appstore is not available!" });
    });


    // Get user state 'view'  (Icon/List)
    if (localStorage.getItem('appStoreViewMode') != null) {
        $scope.view = localStorage.getItem('appStoreViewMode');
    }
    else {
        $scope.view = "icon";
    }

    $scope.$watch('view', function (newValue, oldValue) {
        // Save user state
        localStorage.setItem('appStoreViewMode', newValue);
    });
}]);