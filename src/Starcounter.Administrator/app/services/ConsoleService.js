/**
 * ----------------------------------------------------------------------------
 * Console output Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('ConsoleService', ['$http', '$log', '$sce', '$rootScope', '$filter', 'UserMessageFactory', 'UtilsFactory', 'JobFactory', function ($http, $log, $sce, $rootScope, $filter, UserMessageFactory, UtilsFactory, JobFactory) {

    var self = this;
    this.isWebsocketSupported = ("WebSocket" in window);

    // Listeners
    // { databaseName: databaseName:"default", onEvent: function() {}, onError : function() {} }
    this.listeners = [];

    // List of socket connections, one connection per database (host/engine).
    // { socket:socket, databaseName:"default", listener:{} }
    this.connections = [];

    // Console Events per databaseName
    // (Websocket)
    // [databaseName].[{ databaseName:"default", applicationName:"myApp", text:"some text" }]
    this.consoleEvents = [];


    /**
     * Get Console ouput
     * @param {string} databaseName Databasename
     * @param {object} filter Filter object {databaseName:"default", applicationName:"myApp" }
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.getConsoleOuput = function (databaseName, filter, successCallback, errorCallback) {

        $log.info("Retriving console output for database " + databaseName);

        $http.get("/__" + databaseName + "/console").then(function (response) {
            // Success
            $log.info("Successfully retriving console output from database " + databaseName);

            if (typeof (successCallback) == "function") {
                var filteredConsoleEvents = $filter('filter')(response.data.Items, filter);
                successCallback(filteredConsoleEvents);
            }

        }, function (response) {
            // Error
            var messageObject;
            var errorHeader = "Failed to retrieve the console output from database " + databaseName;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }

                else if (response.status == 404) {
                    // 404 Not found
                    var message = "Caused by a not started database or there is no Application running in the database";
                    messageObject = UtilsFactory.createMessage(errorHeader, message, response.data.Helplink);
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


    /**
     * Register Console event listener
     * (Websocket)
     * @param {object} listener Listener object { databaseName:"default", onEvent: function (consoleEvents) { },  onError: function (messageObject) {}, filter: {databaseName:"default", applicationName:"myApp" } }
     */
    this.registerEventListener = function (listener) {

        if (this.isWebsocketSupported == false) {
            var messageObject = UtilsFactory.createErrorMessage("Websockets is not supported", "This browser dosent support websockets", null, null);
            listener.onError(messageObject);
            return;
        }

        this.listeners.push(listener);

        var connection = this.getConnection(listener.databaseName);
        if (connection == null) {
            this.openConnection(listener.databaseName);

        } else if (connection.socket.readyState == 1) {    // 0=CONNECTING, 1=OPEN, 2=CLOSING, 3=CLOSED
            // Connection already open and listened on for new console events
            // Get current buffer and invoke the listener with it.
            var filteredConsoleEvents = $filter('filter')(this.consoleEvents[listener.databaseName], listener.filter);
            listener.onEvent(filteredConsoleEvents);

        }

    }


    /**
     * Unregister log listener
     * (Websocket)
     * @param {object} listener Listener object { databaseName:"default", onEvent: function (consoleEvents) { },  onError: function (messageObject) {}, filter: {databaseName:"default", applicationName:"myApp" } }
     */
    this.unregisterEventListener = function (listener) {

        // Remove listener from list
        var index = this.listeners.indexOf(listener);
        if (index > -1) {
            this.listeners.splice(index, 1);
        }
        else {
            $log.error("Trying to remove a non existing listener");
        }

        var removeList = [];

        // Close unused connection
        for (var i = 0; i < this.connections.length ; i++) {

            var bCloseConnection = true;
            for (var x = 0; x < this.listeners.length; x++) {
                if (this.connections[i].databaseName == this.listeners[i].databaseName) {
                    // Connection is used
                    bCloseConnection = false;
                    break;
                }
            }

            if (bCloseConnection) {
                removeList.push(this.connections[i]);
            }

        }

        // Remove and close connections
        for (var i = 0; i < removeList.length ; i++) {

            var connection = removeList[i]
            this.closeConnection(connection);

            var index = self.connections.indexOf(connection);
            if (index > -1) {
                self.connections.splice(index, 1);
            }
        }

    }


    /**
     * Get connection 
     * @param {string} databaseName Databasename
     * @returns {connection} Connection
     */
    this.getConnection = function (databaseName) {

        for (var i = 0; i < this.connections.length ; i++) {
            if (this.connections[i].databaseName == databaseName) {
                return this.connections[i];
            }
        }
        return null;
    }


    /**
     * Start listening on database console outputs
     * (Websocket)
     * @param {string} databaseName Databasename
     */
    this.openConnection = function (databaseName) {

        // TODO: Handle this..
        if (this.isWebsocketSupported == false) return;

        try {

            var connection = { socket: null, databaseName: databaseName };

            // Initilize the consoleEvent list for this database
            self.consoleEvents[databaseName] = [];

            var errorHeader = "Websocket error";

            connection.socket = new WebSocket("ws://" + location.host + "/__" + databaseName + "/console");

            // Socket Open
            connection.socket.onopen = function (evt) {
                $log.info("Successfully connected to database " + databaseName);
            };

            // Socket closed
            connection.socket.onclose = function (evt) {

                $log.info("Diconnected from database " + databaseName);

                // Remove consoleEvent list for this database
                delete self.consoleEvents[databaseName];

                var index = self.connections.indexOf(connection);
                if (index > -1) {
                    self.connections.splice(index, 1);
                }
            };


            // Socket Message
            connection.socket.onmessage = function (evt) {

                $rootScope.$apply(function () {
                    var result = JSON.parse(evt.data);

                    // Added consoleEvent to our 'buffer' consoleEvents list per database
                    self.consoleEvents[databaseName].push.apply(self.consoleEvents[databaseName], result.Items);

                    // Invoke consoleEvent on our listeners
                    for (var i = 0; i < self.listeners.length ; i++) {
                        var filteredConsoleEvents = $filter('filter')(result.Items, self.listeners[i].filter);
                        self.listeners[i].onEvent(filteredConsoleEvents);
                    }

                });

            }

            // Socket Error
            connection.socket.onerror = function (evt) {

                $log.error(errorHeader, evt);

                $rootScope.$apply(function () {

                    var messageObject = UtilsFactory.createErrorMessage(errorHeader, JSON.stringify(evt), null, null);

                    for (var i = 0; i < self.listeners.length ; i++) {
                        if (self.listeners[i].databaseName == databaseName) {
                            self.listeners[i].onError(messageObject);
                        }
                    }

                });

                $rootScope.$apply();
            };

            self.connections.push(connection);

        }
        catch (exception) {

            $log.error(errorHeader, exception);

            $rootScope.$apply(function () {

                var messageObject = UtilsFactory.createErrorMessage(errorHeader, exception.message, null, exception.stack);
                for (var i = 0; i < self.listeners.length ; i++) {
                    if (self.listeners[i].databaseName == databaseName) {
                        self.listeners[i].onError(messageObject);
                    }
                }
            });

        }
    }


    /**
     * Stop listening on database console outputs
     * (Websocket)
     * @param {object} connection Connection
     */
    this.closeConnection = function (connection) {

        // readyState: (0) CONNECTING // (2) CLOSING, (3) CLOSED
        if (connection.socket.readyState == 0 || connection.socket.readyState == 2 || connection.socket.readyState == 3) {
            return;
        }
        connection.socket.close();
    }

}]);



