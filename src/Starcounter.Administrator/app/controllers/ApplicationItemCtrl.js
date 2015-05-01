/**
 * ----------------------------------------------------------------------------
 * Application page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('ApplicationItemCtrl', ['$scope', '$log', '$sce', '$routeParams', '$location', 'UserMessageFactory', 'NoticeFactory', 'HostModelService', 'DatabaseService', function ($scope, $log, $sce, $routeParams, $location, UserMessageFactory, NoticeFactory, HostModelService, DatabaseService) {

    $scope.data = HostModelService.data;
    $scope.socket = null;
    $scope.consoleText = "";
//    var self = this;

    //$scope.application = null;

    //console.log("ApplicationCtrl:" + $scope.application.DisplayName);


    $scope.gotoDatabase = function (database) {
        $location.path("/databases/" + database.ID);
    }

    $scope.$on("$destroy", function handler() {

        if ($scope.socket != null) {
            $scope.socket.close();
            $scope.socket = null;
        }

    });

    $scope.$watch("application.IsRunning", function (newValue, oldValue) {

        if ($scope.application.IsRunning) {
            $scope.connectSocketForConsoleOutput($scope.application);
        }
        else {
            if ($scope.socket != null) {
                $scope.socket.close();
                $scope.socket = null;
            }
        }

    });

    //$scope.init = function (application) {

    //  console.log("Init application:" + application.DisplayName);
    //  $scope.application = application;

    //  $scope.consoleText = application.DisplayName;

    ////    // $scope.application = application;

    ////    //This function is sort of private constructor for controller
    ////    //$scope.id = id;
    ////    //$scope.name = name;
    ////    //Based on passed argument you can make a call to resource
    ////    //and initialize more objects
    ////    //$resource.getMeBond(007)
    //};

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

    //console.log("$routeParams.name:" + $routeParams.name);

    //    HostModelService.setSelectedApplication($routeParams.name);

    /**
     * Connect to application console output socket
     * @param {object} application Application
     */
    $scope.connectSocketForConsoleOutput = function (application) {

        $scope.socket = new WebSocket("ws://" + location.host + "/__" + $scope.data.selectedDatabase.ID + "/console/" + application.AppName);

        // Socket Open
        $scope.socket.onopen = function (evt) {
            $log.info("Successfully connected to application (" + application.DisplayName + ")");
        };

        // Socket closed
        $scope.socket.onclose = function (evt) {
            $log.info("Diconnected from application (" + application.DisplayName + ")");

        };

        // Socket Message
        $scope.socket.onmessage = function (evt) {

            $log.info("OnMessage from application (" + application.DisplayName + ")");

            $scope.$apply(function () {
                var result = JSON.parse(evt.data);

                var text = "";
                for (var i = 0; i < result.Items.length ; i++) {
                    text = text + result.Items[i].text;
                }

                $scope.consoleText = $scope.consoleText + text;


                //    // Added consoleEvent to our 'buffer' consoleEvents list per database
                //    self.consoleEvents[databaseName].push.apply(self.consoleEvents[databaseName], result.Items);

                //    // Invoke consoleEvent on our listeners
                //    for (var i = 0; i < self.listeners.length ; i++) {
                //        var filteredConsoleEvents = $filter('filter')(result.Items, self.listeners[i].filter);
                //        self.listeners[i].onEvent(filteredConsoleEvents);
                //    }

            });

        }

        // Socket Error
        $scope.socket.onerror = function (evt) {

            $log.error(evt);

            //$rootScope.$apply(function () {

            //    var messageObject = UtilsFactory.createErrorMessage(errorHeader, JSON.stringify(evt), null, null);

            //    for (var i = 0; i < self.listeners.length ; i++) {
            //        if (self.listeners[i].databaseName == databaseName) {
            //            self.listeners[i].onError(messageObject);
            //        }
            //    }

            //});

            //$rootScope.$apply();
        };
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