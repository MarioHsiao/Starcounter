/**
 * ----------------------------------------------------------------------------
 * Application page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('ApplicationCtrl', ['$scope', '$log', '$sce', '$routeParams', 'UserMessageFactory', 'NoticeFactory', 'HostModelService', 'ApplicationService', function ($scope, $log, $sce, $routeParams, UserMessageFactory, NoticeFactory, HostModelService, ApplicationService) {

    var self = this;

    this.application = null;

    /**
     * Get Console output
     * @param {object} application aplication
     */
    this.btnGetConsoleOutput = function (application) {

        ApplicationService.refreshConsoleOuput(application, function () {

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



    // Init
    // Refresh host model
    HostModelService.refreshHostModel(function () {

        // Success
        self.application = HostModelService.getApplication($routeParams.dbName, $routeParams.name);

    }, function (messageObject) {

        // Error
        UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
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

    $scope.calcHeight = function () {
        var border = 12;
        var ht = $("#console");
        var offset = ht.offset();
        if (!offset) {
            return;
        }
        var topOffset = offset.top;

        var height = $scope.winHeight - topOffset - 2 * border;
        if (height < 150) {
            return 150;
        }
        return height;
    };

    $scope.sizeStyle = function () {
        return { "height": $scope.calcHeight() + "px", "background-color": "#ff0000" };
    }

}]);