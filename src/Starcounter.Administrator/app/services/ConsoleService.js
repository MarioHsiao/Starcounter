/**
 * ----------------------------------------------------------------------------
 * Executable Console output Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('ConsoleService', ['$http', '$log', '$sce', '$rootScope', 'UserMessageFactory', 'UtilsFactory', 'JobFactory', function ($http, $log, $sce, $rootScope, UserMessageFactory, UtilsFactory, JobFactory) {

    var self = this;
    this.isWebsocketSupported = ("WebSocket" in window);

    // Console buffer size
    this.bufferSize = 10000;

    // Listeners
    // { databaseName: databaseName:"default", onEvent: function() {}, onError : function() {} }
    this.listeners = [];

    // List of socket connections, one connection per database (host/engine).
    // { socket:socket, databaseName:"default", listener:{} }
    this.connections = [];


    /**
     * Get Console ouput
     * @param {databaseName} databaseName Databasename
     * @param {successCallback} successCallback Success callback function
     * @param {errorCallback} errorCallback Error callback function
     */
    this.getConsoleOuput = function (databaseName, filter, successCallback, errorCallback) {

        $log.info("Retriving console output for database " + databaseName);

        $http.get("/__" + databaseName + "/console").then(function (response) {
            // Success
            $log.info("Successfully retriving console output from database " + databaseName);

            if (typeof (successCallback) == "function") {

                var text = "";
                for (var i = 0 ; i < response.data.Items.length ; i++) {
                    var consoleEvent = response.data.Items[i];

                    if (filter) {
                        if (filter == consoleEvent.applicationName) {
                            text = text + consoleEvent.text;
                        }

                    } else {
                        text = text + consoleEvent.text;
                    }
                }

                successCallback(text);

            }

        }, function (response) {
            // Error
            var messageObject;
            var errorHeader = "Failed to retrive the console output from database " + databaseName;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }

                else if (response.status == 404) {
                    // 404 Not found
                    var message = "Caused by a not started database or there is no Executable running in the database";
                    messageObject = UtilsFactory.createMessage(errorHeader, message, response.data.Helplink);
                }
                else if (response.status == 500) {
                    // 500 Server Error
                    errorHeader = "Internal Server Error";
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.message, response.data.helplink, response.data.stackTrace);
                }
                else {
                    // Unhandle Error
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                }

                errorCallback(messageObject);
            }

        });


    }


    /**
     * Register Console event listener
     * @param { databaseName:"default", onEvent: function () { },  onError: function (messageObject) {}  } listener Listener
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
        } else {
            // TODO: Get buffer 
        }

    }


    /**
     * Unregister log listener
     * @param { databaseName:"default", onEvent: function () { },  onError: function (messageObject) {}  } listener Listener
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
     * @param {databaseName} databaseName Databasename
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
     * @param {databaseName} databaseName Databasename
     */
    this.openConnection = function (databaseName) {

        // TODO: Handle this..
        if (this.isWebsocketSupported == false) return;

        try {

            var connection = { socket: null, databaseName: databaseName };

            var errorHeader = "Websocket error";

            connection.socket = new WebSocket("ws://" + location.host + "/__" + databaseName + "/console");

            connection.socket.onopen = function (evt) {
                $log.info("Successfully connected to database " + databaseName);
            };

            connection.socket.onclose = function (evt) {

                $log.info("Diconnected from database " + databaseName );

                var index = self.connections.indexOf(connection);
                if (index > -1) {
                    self.connections.splice(index, 1);
                }
            };

            connection.socket.onmessage = function (evt) {

                $rootScope.$apply(function () {
                    var result = JSON.parse(evt.data);

                    $log.warn("TODO: Consolidate message to each listener");

                    for (var i = 0 ; i < result.Items.length ; i++) {
                        var consoleEvent = result.Items[i];

                        for (var x = 0; x < self.listeners.length ; x++) {

                            if (self.listeners[x].databaseName == databaseName) {
                                if (self.listeners[x].filter) {
                                    if (self.listeners[x].filter == consoleEvent.applicationName) {
                                        self.listeners[x].onEvent(consoleEvent.text);
                                    }
                                }
                                else {
                                    self.listeners[x].onEvent(consoleEvent.text);
                                }
                            }

                        }

                    }
                });

            }

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
     * @param {connection} connection Connection
     */
    this.closeConnection = function (connection) {

        // readyState: (0) CONNECTING // (2) CLOSING, (3) CLOSED
        if (connection.socket.readyState == 0 || connection.socket.readyState == 2 || connection.socket.readyState == 3) {
            return;
        }
        connection.socket.close();
    }

}]);



