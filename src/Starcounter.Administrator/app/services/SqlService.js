/**
 * ----------------------------------------------------------------------------
 * SQL Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('SqlService', ['$http', '$log', 'UtilsFactory', 'JobFactory', function ($http, $log, UtilsFactory, JobFactory) {

    // List of databases
    // {
    //     "Name":"tracker",
    //     "Uri":"http://machine:1234/api/databases/mydatabase",
    //     "HostUri":"http://machine:1234/api/engines/mydatabase/db",
    //     "Running":true
    // }

    var self = this;


    /**
     * Execute query
     * @param {string} query Query
     * @param {string} database Database
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.executeQuery = function (query, databaseName, successCallback, errorCallback) {

        var errorHeader = "Failed to execute query";
        var uri = "/__" + databaseName + "/sql";

        //{
        // "columns":[{"title":"title","value":"Object","type":"Object"}],
        // "rows":{
        //      "rows":[{}]
        // },
        // "queryPlan":"some text",
        // "sqlException":{"message":"","helpLink":"","query":"","beginPosition":0,"endPosition":0,"scErrorCode":0,"token":"","stackTrace":""},
        // "hasSqlException":false,
        // "exception":{"message":"","helpLink":"","stackTrace":""},
        // "hasException":false
        //}
        $http.post(uri, query).then(function (response) {
            // Success

            if (response.data.hasOwnProperty("hasSqlException") && response.data.hasSqlException) {
                // Show message
                //$scope.alerts.push({ type: 'danger', msg: response.sqlException.message, helpLink: response.sqlException.helpLink });
                if (typeof (errorCallback) == "function") {
                    var messageObject = UtilsFactory.createMessage(errorHeader, response.data.sqlException.message, response.data.sqlException.helpLink);
                    errorCallback(messageObject);
                }
                return;
            }

            if (response.data.hasOwnProperty("hasException") && response.data.hasException) {
                //$scope.showServerError(response.exception.message, response.exception.helpLink, response.exception.stackTrace);
                if (typeof (errorCallback) == "function") {
                    var messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.exception.message, response.data.exception.helpLink, response.data.exception.stackTrace);
                    errorCallback(messageObject);
                }
                return;
            }

            // Validate response
            if (response.data.hasOwnProperty("rows") == true && response.data.hasOwnProperty("columns") == true) {
                $log.info("SQL query rows (" + response.data.rows.rows.length + ") successfully retrived");
                $log.info("SQL query columns (" + response.data.columns.length + ") successfully retrived");
                if (typeof (successCallback) == "function") {
                    successCallback(response.data);
                }
            }
            else {
                // Error
                $log.error(errorHeader, response);

                if (typeof (errorCallback) == "function") {
                    var messageObject = UtilsFactory.createErrorMessage(errorHeader, "Invalid response content", null, null);
                    errorCallback(messageObject);
                }
            }

        }, function (response) {
            // Error
            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                var messageObject;

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 404) {
                    // 404	Not Found
                    var message = "Failed to execute the query on " + databaseName + " database, Caused by a not started database or application.";
                    messageObject = UtilsFactory.createMessage(errorHeader, message, null);

                }
                else if (response.status == 500) {
                    // 500 Server Error
                    errorHeader = "Internal Server Error";
                    messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                }
                else {
                    // Unhandle Error
                    messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                }

                errorCallback(messageObject);
            }

        });

    }


}]);
