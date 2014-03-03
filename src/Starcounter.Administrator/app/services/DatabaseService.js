/**
 * ----------------------------------------------------------------------------
 * Databases Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('DatabaseService', ['$http', '$log', '$sce', 'UtilsFactory', 'JobFactory', 'ConsoleService', function ($http, $log, $sce, UtilsFactory, JobFactory, ConsoleService) {

    var self = this;

    // List of databases
    // {
    //     "Name":"tracker",
    //     "Uri":"http://machine:1234/api/databases/mydatabase",
    //     "HostUri":"http://machine:1234/api/engines/mydatabase/db",
    //     "Running":true,
    //
    //     "console":"",
    //     "consoleManualMode": false
    // }
    this.databases = [];

    // Console buffer size
    this.bufferSize = 10000;


    /**
     * Get all databases
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.getDatabases = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrive a list of databases";
        var uri = "/api/admin/databases";

        $http.get(uri).then(function (response) {
            // Success

            $log.info("Databases (" + response.data.Databases.length + ") successfully retrived");
            if (typeof (successCallback) == "function") {
                successCallback(response.data.Databases);
            }


        }, function (response) {
            // Error
            var messageObject;

            if (response instanceof SyntaxError) {
                messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
            }
            else if (response.status == 500) {
                // 500 Server Error
                errorHeader = "Internal Server Error";
                messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.message, response.data.helplink, response.data.stackTrace);
            }
            else {
                // Unhandle Error
                if (response.data.hasOwnProperty("Text") == true) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                } else {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                }
            }

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {
                errorCallback(messageObject);
            }


        });


    }


    /**
     * Get database
     * @param {databaseName} databaseName Database name
     * @return {database} Database or null
     */
    this.getDatabase = function (databaseName) {

        for (var i = 0 ; i < self.databases.length ; i++) {
            if (self.databases[i].name == databaseName) {
                return self.databases[i];
            }
        }
    }


    /**
     * Refresh databases
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.refreshDatabases = function (successCallback, errorCallback) {

        this.getDatabases(function (databases) {
            // Success
            self._updateDatabaseList(databases);

            if (typeof (successCallback) == "function") {
                successCallback();
            }

        }, function (response) {
            // Error

            if (typeof (errorCallback) == "function") {
                errorCallback(response);
            }

        });
    }


    /**
     * Refresh executable console output
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.refreshConsoleOuput = function (database, successCallback, errorCallback) {

        var filter = null;

        ConsoleService.getConsoleOuput(database.name, filter, function (text) {
            // Success

            self._onConsoleOutputEvent(database, text, false);

            if (typeof (successCallback) == "function") {
                successCallback();
            }

        }, errorCallback);
    }


    /**
     * Callback when there is an incoming console message
     * @param {database} database
     * @param {text} text
     * @param {bAppend} bAppend
     */
    this._onConsoleOutputEvent = function (database, text, bAppend) {

        var htmlText = text.replace(/\r\n/g, "<br>");

        if (bAppend) {
            database.console = $sce.trustAsHtml(database.console + htmlText);
        }
        else {
            database.console = $sce.trustAsHtml(htmlText);
        }

        // Limit the buffer
        if (database.console.length > self.bufferSize) {
            database.console = $sce.trustAsHtml(database.console.substr(database.console.length - self.bufferSize));
        }

    }


    /**
     * Update current database list with new list
     * @param {newDatabasess} New database list
     */
    this._updateDatabaseList = function (newDatabases) {

        var newList = [];
        var removeList = [];

        // Check for new databases and update current databases
        for (var i = 0; i < newDatabases.length; i++) {
            var newDatabase = newDatabases[i];
            var database = this.getDatabase(newDatabase.name);
            if (database == null) {
                newList.push(newDatabase);
            } else {
                UtilsFactory.updateObject(newDatabase, database, function (arg) {

                    $log.debug("Propertychanged", arg);

                    if (arg.propertyName == "running") {

                        if (arg.newValue) {
                            ConsoleService.registerEventListener(arg.source.consoleListener);
                        }
                        else {
                            ConsoleService.unregisterEventListener(arg.source.consoleListener);
                        }
                    }


                });
            }
        }

        // Remove removed databases from database list
        for (var i = 0; i < self.databases.length; i++) {

            var database = self.databases[i];
            var bExists = false;
            // Check if it exist in newList
            for (var i = 0; i < newDatabases.length; i++) {
                var newDatabase = newDatabases[i];

                if (database.name == newDatabase.name) {
                    bExists = true;
                    break;
                }
            }

            if (bExists == false) {
                removeList.push(database);
            }
        }


        // Remove database from database list
        for (var i = 0; i < removeList.length; i++) {
            var index = self.databases.indexOf(removeList[i]);
            if (index > -1) {
                self.databases.splice(index, 1);
            }
            this._onRemovedDatabase(database);
        }

        // Add new databases
        for (var i = 0; i < newList.length; i++) {
            self.databases.push(newList[i]);
            this._onNewDatabase(newList[i]);
        }

    }


    /**
     * On New database Event
     * @param {database} database
     */
    this._onNewDatabase = function (database) {

        // Add additional properties
        database.console = "";
        database.consoleManualMode = false;

        // Socket event listener
        database.consoleListener = {
            databaseName: database.name,
            onEvent: function (text) {

                self._onConsoleOutputEvent(database, text, true);

            },
            onError: function (messageObject) {

                // Sliently fallback to manual mode
                database.consoleManualMode = true;

            },
            filter: null
        }

        if (database.running) {
            ConsoleService.registerEventListener(database.consoleListener);
        }

    }


    /**
     * On Executable Removed event
     * @param {executable} Executable
     */
    this._onRemovedDatabase = function (database) {
        ConsoleService.unregisterEventListener(database.consoleListener);
    }


    /**
     * Start database
     * @param {Database} database
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.startDatabase = function (database, successCallback, errorCallback) {

        var job = { message: "Starting database " + database.name };

        JobFactory.AddJob(job);

        $http.post('/api/engines', { Name: database.name }).then(function (response) {
            // Success
            // 200
            // 201
            JobFactory.RemoveJob(job);

            $log.info("Database " + database.name + " was successfully started");

            // Refresh databases
            self.refreshDatabases(successCallback, errorCallback);

        }, function (response) {
            JobFactory.RemoveJob(job);
            // Error
            var errorHeader = "Failed to start database";
            var messageObject;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 404) {
                    // 404 A database with the specified name was not found.
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                }
                else if (response.status == 422) {
                    // 422 Database engine name not specified
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                }
                else if (response.status == 500) {
                    // 500 Server Error
                    errorHeader = "Internal Server Error";
                    if (response.data.hasOwnProperty("Text") == true) {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                    } else {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                    }
                }
                else {
                    // Unhandle Error
                    if (response.data.hasOwnProperty("Text") == true) {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                    } else {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                    }
                }
                errorCallback(messageObject);
            }

        });


    }


    /**
     * Stop database
     * @param {Database} database
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.stopDatabase = function (database, successCallback, errorCallback) {

        var errorHeader = "Failed to stop database";
        var job = { message: "Stopping database " + database.name };

        JobFactory.AddJob(job);
        var databaseUri = UtilsFactory.toRelativePath(database.engineUri);

        $http.delete(databaseUri, { Name: name }).then(function (response) {
            // Success
            // 202
            // 204
            JobFactory.RemoveJob(job);

            $log.info("Database " + database.name + " was successfully stopped");

            // Refresh databases
            self.refreshDatabases(successCallback, errorCallback);

        }, function (response) {
            JobFactory.RemoveJob(job);
            // Error
            var errorHeader = "Failed to stop database";
            var messageObject;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 404) {
                    // 404 A database with the specified name was not found.
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                }
                else if (response.status == 409) {
                    // 409 The executable is already running or the Engine is not started.
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                }
                else if (response.status == 500) {
                    // 500 Server Error
                    errorHeader = "Internal Server Error";
                    if (response.data.hasOwnProperty("Text") == true) {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                    } else {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                    }
                }
                else {
                    // Unhandle Error
                    if (response.data.hasOwnProperty("Text") == true) {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                    } else {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                    }
                }
                errorCallback(messageObject);
            }

        });

    }


    /**
     * Get Database settings
     * @param {Database} database
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.getDatabaseSettings = function (database, successCallback, errorCallback) {

        var errorHeader = "Failed to retrive database settings";

        var job = { message: "Retriving database settings" };
        JobFactory.AddJob(job);
        var uri = "/api/admin/databases/" + database.name + "/settings";

        $http.get(uri).then(function (response) {
            // success handler
            JobFactory.RemoveJob(job);

            if (response.hasOwnProperty("data") == true && response.data.hasOwnProperty("settings") == true) {
                $log.info("Databases settings successfully retrived");
                if (typeof (successCallback) == "function") {
                    successCallback(response.data.settings);
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

            JobFactory.RemoveJob(job);
            // Error
            var messageObject;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 500) {
                    // 500 Server Error
                    errorHeader = "Internal Server Error";
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.message, response.data.helplink, response.data.stackTrace);
                }
                else {
                    // Unhandle Error
                    if (response.data.hasOwnProperty("Text") == true) {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                    } else {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                    }
                }


                errorCallback(messageObject);
            }


        });
    }


    /**
     * Save database settings
     * @param {database} Database
     * @param {settings} settings
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.saveDatabaseSettings = function (database, settings, successCallback, errorCallback) {

        var errorHeader = "Failed to save database settings";

        var job = { message: "Saving database settings" };
        JobFactory.AddJob(job);
        var uri = "/api/admin/databases/" + database.name + "/settings";

        $http.put(uri, settings).then(function (response) {
            // success handler
            JobFactory.RemoveJob(job);

            if (response.data.hasOwnProperty("errors") == true) {
                var messageObjectList = [];

                for (var i = 0; i < response.data.errors.length; i++) {
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.errors[i].message, response.data.errors[i].helplink);
                    messageObjectList.push(messageObject);
                }

                if (errorCallback != null) {
                    errorCallback(messageObjectList);
                }

            }
            else {

                var messageObject = null;
                if (response.data.hasOwnProperty("message") == true) {
                    messageObject = UtilsFactory.createMessage("success", response.data.message, null);
                }

                if (successCallback != null) {
                    // TODO: Return the new settings
                    successCallback(messageObject);
                }

            }

        }, function (response) {

            // Error
            JobFactory.RemoveJob(job);
            var messageObject;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 403) {
                    // 403 forbidden
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                }
                else if (response.status == 500) {
                    // 500 Server Error
                    errorHeader = "Internal Server Error";
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.message, response.data.helplink, response.data.stackTrace);
                }
                else {
                    // Unhandle Error
                    if (response.data.hasOwnProperty("Text") == true) {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                    } else {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                    }
                }


                errorCallback(messageObject);
            }

        });

    }


    /**
     * Get Database default settings
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.getDatabaseDefaultSettings = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrive database default settings";

        var job = { message: "Retriving database defult settings" };
        JobFactory.AddJob(job);

        $http.get('/api/admin/settings/database').then(function (response) {
            // success handler
            JobFactory.RemoveJob(job);

            if (response.hasOwnProperty("data") == true && response.data.hasOwnProperty("settings") == true) {
                $log.info("Default Databases settings successfully retrived");
                if (typeof (successCallback) == "function") {
                    successCallback(response.data.settings);
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

            JobFactory.RemoveJob(job);
            // Error
            var messageObject;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 500) {
                    // 500 Server Error
                    errorHeader = "Internal Server Error";
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.message, response.data.helplink, response.data.stackTrace);
                }
                else {
                    // Unhandle Error
                    if (response.data.hasOwnProperty("Text") == true) {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                    } else {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                    }
                }

                errorCallback(messageObject);
            }


        });
    }


    /**
     * Verify database settings
     * @param {settings} Settings
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.verifyDatabaseSettings = function (settings, successCallback, errorCallback) {

        var errorHeader = "Failed to verify database settings";
        var job = { message: "Verifying database settings" };
        JobFactory.AddJob(job);

        $http.post('/api/admin/verify/databaseproperties', settings).then(function (response) {
            // success handler
            JobFactory.RemoveJob(job);

            if (response.hasOwnProperty("data") == true && response.data.hasOwnProperty("validationErrors") == true) {

                $log.info("Default Databases settings successfully verified");
                if (typeof (successCallback) == "function") {
                    successCallback(response.data.validationErrors);
                }

            }
            else {

                if (typeof (errorCallback) == "function") {
                    var messageObject = UtilsFactory.createErrorMessage(errorHeader, "Invalid response content", null, null);
                    errorCallback(messageObject);
                }

            }

        }, function (response) {
            // Error
            JobFactory.RemoveJob(job);
            var messageObject;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 403) {
                    // 403 forbidden
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                }
                else if (response.status == 500) {
                    // 500 Server Error
                    errorHeader = "Internal Server Error";
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.message, response.data.helplink, response.data.stackTrace);
                }
                else {
                    // Unhandle Error
                    if (response.data.hasOwnProperty("Text") == true) {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                    } else {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                    }
                }

                errorCallback(messageObject);
            }


        });

    }


    /**
     * Create database
     * @param {settings} settings
     * @param {successCallback} successCallback function
     * @param {errorCallback} errorCallback function
     */
    this.createDatabase = function (settings, successCallback, errorCallback) {

        var errorHeader = "Failed to create database";

        var job = { message: "Creating database " + settings.name };
        JobFactory.AddJob(job);

        $http.post('/api/admin/databases/' + settings.name + '/createdatabase', settings).then(function (response) {
            // success handler
            JobFactory.RemoveJob(job);

            if (response.data.hasOwnProperty("errors") == true) {
                var messageObjectList = [];

                for (var i = 0; i < response.data.errors.length; i++) {
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.errors[i].message, response.data.errors[i].helplink);
                    messageObjectList.push(messageObject);
                }

                if (errorCallback != null) {
                    errorCallback(messageObjectList);
                }

            }
            else {

                if (successCallback != null) {
                    // TODO: Return the newly create database
                    successCallback();
                }

            }

        }, function (response) {

            // Error
            JobFactory.RemoveJob(job);
            var messageObject;

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 403) {
                    // 403 forbidden
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                }
                    //else if (response.status == 422) {
                    //    // 422 Unprocessable Entity (WebDAV; RFC 4918)
                    //    // The request was well-formed but was unable to be followed due to semantic errors
                    //    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                    //}
                else if (response.status == 500) {
                    // 500 Server Error
                    errorHeader = "Internal Server Error";
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.message, response.data.helplink, response.data.stackTrace);
                }
                else {
                    // Unhandle Error
                    if (response.data.hasOwnProperty("Text") == true) {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                    } else {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                    }
                }

                errorCallback(messageObject);
            }

        });

    }

}]);
