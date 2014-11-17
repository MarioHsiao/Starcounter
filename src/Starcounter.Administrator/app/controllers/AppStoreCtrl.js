/**
 * ----------------------------------------------------------------------------
 * AppStore page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('AppStoreCtrl', ['$scope', '$log', 'NoticeFactory', 'HostModelService', 'AppStoreService', 'InstalledApplicationService', 'UserMessageFactory', function ($scope, $log, NoticeFactory, HostModelService, AppStoreService, InstalledApplicationService, UserMessageFactory) {

    // List of applications
    $scope.appStoreApplications = AppStoreService.stores;
    $scope.appStoreServiceStatus = AppStoreService.status;
    $scope.appStoreMessage = null;

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

                AppStoreService.install(appStoreApplication, function () {

                    InstalledApplicationService.refreshApplications(function () {

                    }, function (messageObject) {
                        // Error
                    });

                },

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
     * Delete (Uninstall) Application
     * @param {object} installedApplication Installed Application
     */
    $scope.btnUninstall = function (installedApplication) {

        var title = "Uninstall application";
        var message = "Do you want to uninstall the application " + installedApplication.DisplayName;
        var buttons = [{ result: 0, label: 'Uninstall', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];

        UserMessageFactory.showMessageBox(title, message, buttons, function (result) {

            if (result == 0) {

                AppStoreService.uninstall(installedApplication, function () {

                    InstalledApplicationService.refreshApplications(function () {

                    }, function (messageObject) {
                        // Error
                    });
                },
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
     * Upgrade (Install) AppStore Application
     * @param {object} appStoreApplication AppStore Application
     */
    $scope.btnUpgrade = function (appStoreApplication) {

        var title = "Upgrade application";
        var message = "Do you want to upgrade the application " + appStoreApplication.DisplayName;
        var buttons = [{ result: 0, label: 'Upgrade', cssClass: 'btn-success' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];

        UserMessageFactory.showMessageBox(title, message, buttons, function (result) {

            if (result == 0) {

                AppStoreService.upgrade(appStoreApplication, function () {

                    InstalledApplicationService.refreshApplications(function () {

                    }, function (messageObject) {
                        // Error
                    });

                },

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

    // Init
    // Refresh host model
    HostModelService.refreshHostModel(function () {

    }, function (messageObject) {
        // Error
        UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
    });

    $scope.isBusy = true;
    AppStoreService.refreshApplications(function () {

        $scope.isBusy = false;
        $scope.appStoreMessage = null;

    }, function (messageObject) {
        // Error (Service not available)
        $scope.isBusy = false;
        $scope.appStoreMessage = messageObject;
    });

}]);