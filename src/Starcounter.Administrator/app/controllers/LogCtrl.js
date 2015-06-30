/**
 * ----------------------------------------------------------------------------
 * Log page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('LogCtrl', ['$scope', '$rootScope', '$location', '$log', '$filter', 'LogService', 'UserMessageFactory',
        function ($scope, $rootScope, $location, $log, $filter, LogService, UserMessageFactory) {

    $scope.multipleDemo = {};
    $scope.multipleDemo.list_of_string = [];

    // The model
    $scope.model = {
        "LogEntries": [],
        "filter": {
            debug: false,
            notice: false,
            warning: true,
            error: true,
            source: "",
            maxitems: 30
        },
        isWebsocketSupport: LogService.isWebsocketSupport
    };

    $scope.select2Options = {
        'multiple': true,
        'simple_tags': true,
        'tags': []
    };

    // Socket log event listener
    var socketEventListener = {
        onEvent: function () {
            $scope.getLog();
        },
        onError: function (messageObject) {

            // Workaround, the binding seems not to work.
            // Here we rebind it
            $scope.model.isWebsocketSupport = LogService.isWebsocketSupport;

            UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
        }
    }

    // Destructor
    $scope.$on('$destroy', function iVeBeenDismissed() {

        LogService.unregisterEventListener(socketEventListener);
    })

    // Register log listener
    LogService.registerEventListener(socketEventListener);

    // Set the filters from the address bar parameters to the controller
    //$scope.model.filter = $location.search();
    var searchObj = $location.search()
    if (searchObj.debug != null) {
        $scope.model.filter.debug = searchObj.debug;
    }
    if (searchObj.notice != null) {
        $scope.model.filter.notice = searchObj.notice;
    }
    if (searchObj.warning != null) {
        $scope.model.filter.warning = searchObj.warning;
    }
    if (searchObj.error != null) {
        $scope.model.filter.error = searchObj.error;
    }
    if (searchObj.source != null) {
        $scope.model.filter.source = searchObj.source;
    }
    if (searchObj.maxitems != null) {
        $scope.model.filter.maxitems = searchObj.maxitems;
    }

    // Watch for changes in the filer
    $scope.$watch('model.filter', function (newValue, oldValue) {

        if (newValue !== oldValue) {
            // do whatever you were going to do

        //console.log("model.filter changed:"+JSON.stringify(newvalue)+"=>"+JSON.stringify(oldvalue));
        // Filter changed, update the address bar
        $location.search($scope.model.filter);

        $scope.getLog();
        }

    }, true);

    // Watch for changes in the filer
    $scope.$watch('multipleDemo.list_of_string', function () {
        // Filter changed, update the address bar
        var sourceFilter = "";
        for (var i = 0 ; i < $scope.multipleDemo.list_of_string.length; i++) {
            if (sourceFilter != "") sourceFilter += ";";

            // Special handling, sometime the list_of_string can be objects and some othertimes it's string. no clue why!?!
            if ($scope.multipleDemo.list_of_string[i].hasOwnProperty('id')) {
                sourceFilter += $scope.multipleDemo.list_of_string[i].id;
            }
            else {
                sourceFilter += $scope.multipleDemo.list_of_string[i];
            }
        }

        if ($scope.model.filter.source != sourceFilter) {
            $scope.model.filter.source = sourceFilter;
        }


    }, true);

    var arr = $scope.model.filter.source.split(';');
    for (var x = 0 ; x < arr.length; x++) {
        if (arr[x] == "") continue;
        $scope.multipleDemo.list_of_string.push(arr[x]);
    }


    /**
     * Retrieve log information
     */
    $scope.getLog = function () {
        // Get log entries from the Log Service
        LogService.getLogEntries($scope.model.filter, function (response) {
            $scope.select2Options.tags.length = 0;
            $scope.select2Options.tags = [];

            $scope.model.LogEntries = response.LogEntries.map(function(log) {
              log.HostName = $filter('logMessageHost')(log.HostName);

              return log;
            });
            var filterSourceOptions = response.FilterSource.split(";");

            for (var i = 0; i < filterSourceOptions.length ; i++) {
                $scope.select2Options.tags.push(filterSourceOptions[i]);
            }
        }, function (messageObject) {
            // Error
            UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
        });
    }

    /**
     * Refresh log entries
     */
    $scope.btnRefresh = function () {
        $scope.getLog();
    }

    // Init
    $scope.getLog();
}]);