/**
 * ----------------------------------------------------------------------------
 * Application page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('ApplicationCtrl', ['$scope', '$log', '$sce', '$routeParams', '$location', 'UserMessageFactory', 'NoticeFactory', 'HostModelService', 'DatabaseService', function ($scope, $log, $sce, $routeParams, $location, UserMessageFactory, NoticeFactory, HostModelService, DatabaseService) {

    $scope.application = null;
    $scope.database = null;

    $scope.socket = null;
    $scope.consoleText = "";

    //var self = this;

    /**
     * Start Application
     * @param {object} application Application
     */
    $scope.btnStartApplication = function (application) {

        application.Start$++;
    }

    /**
     * Stop Application
     * @param {object} application Application
     */
    $scope.btnStopApplication = function (application) {

        application.Stop$++;
    }

    /**
     * Install Application
     * @param {object} application Application
     */
    $scope.btnInstallApplication = function (application) {

        application.Install$++;
    }

    /**
     * Uninstall Application
     * @param {object} application Application
     */
    $scope.btnUninstallApplication = function (application) {

        application.Uninstall$++;
    }

    /**
     * Delete Application
     * @param {object} application Application
     */
    $scope.btnDeleteApplication = function (application) {

        var title = "Delete application";
        var message = "Do you want to delete the application " + application.DisplayName;
        var buttons = [{ result: 0, label: 'Delete', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];

        UserMessageFactory.showMessageBox(title, message, buttons, function (result) {

            if (result == 0) {

                application.Delete$++;
            }
        });
    }

    /**
     * Register console output listener
     */
    $scope.registerConsoleOutPutListeners = function () {

        $scope.socket = new WebSocket("ws://" + location.host + "/__" + $scope.database.ID + "/console/" + $scope.application.AppName);

        // Socket Open
        $scope.socket.onopen = function (evt) {

            $log.info("Successfully connected to application (" + $scope.application.DisplayName + ")");
        };

        // Socket closed
        $scope.socket.onclose = function (evt) {

            $log.info("Diconnected from application (" + $scope.application.DisplayName + ")");
        };

        // Socket Message
        $scope.socket.onmessage = function (evt) {

//            $log.info("OnMessage from application (" + $scope.application.DisplayName + ")");

            $scope.$apply(function () {
                var result = JSON.parse(evt.data);

                var text = "";
                for (var i = 0; i < result.Items.length ; i++) {
                    text = text + result.Items[i].text;
                }

                $scope.consoleText = $scope.consoleText + text;
            });
        }

        // Socket Error
        $scope.socket.onerror = function (evt) {

            $log.error(evt);
        };
    }

    $scope.$on("$destroy", function handler() {

        if ($scope.socket != null) {
            $scope.socket.close();
        }
    });

    // Set Data
    $scope.database = HostModelService.getDatabase($routeParams.name);
    $scope.application = HostModelService.getApplication($scope.database, $routeParams.appid);

    if ($scope.application != null) {
        $scope.registerConsoleOutPutListeners();
    }

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