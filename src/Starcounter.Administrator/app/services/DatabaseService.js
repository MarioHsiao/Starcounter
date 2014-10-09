/**
 * ----------------------------------------------------------------------------
 * Databases Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('DatabaseService', ['$http', '$log', 'UtilsFactory', 'JobFactory', 'ConsoleService', function ($http, $log, UtilsFactory, JobFactory, ConsoleService) {

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
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.getDatabases = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve a list of databases";
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
                messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
            }
            else {
                // Unhandle Error
                messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
            }

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {
                errorCallback(messageObject);
            }

        });


    }

    /**
     * Get database
     * @param {string} databaseName Database name
     * @return {object} Database or null
     */
    this.getDatabase = function (databaseName) {

        for (var i = 0 ; i < self.databases.length ; i++) {
            if (self.databases[i].name == databaseName) {
                return self.databases[i];
            }
        }
        return null;
    }

    /**
     * Refresh databases
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.refreshDatabases = function (successCallback, errorCallback) {

        this.getDatabases(function (databases) {

            // Success
            self._updateDatabaseList(databases);

            if (typeof (successCallback) == "function") {
                successCallback();
            }

        }, function (messageObject) {

            // Error
            if (typeof (errorCallback) == "function") {
                errorCallback(messageObject);
            }

        });
    }

    /**
     * Refresh database console output
     * @param {object} database Database
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.refreshConsoleOuput = function (database, successCallback, errorCallback) {


        ConsoleService.getConsoleOuput(database.name, { databaseName: database.name }, function (consoleEvents) {

            // Success

            var consoleText = "";
            for (var i = 0; i < consoleEvents.length; i++) {
                consoleText = consoleText + consoleEvents[i].text;
            }
            self._onConsoleOutputEvent(database, consoleText, false);

            if (typeof (successCallback) == "function") {
                successCallback();
            }

        }, errorCallback);
    }

    /**
     * Callback when there is an incoming console message
     * @param {string} database Database
     * @param {text} text Text to console
     * @param {boolean} bAppend True is Text will be appended to current console text
     */
    this._onConsoleOutputEvent = function (database, text, bAppend) {

        var result;

        if (bAppend) {
            result = database.console + text;
        }
        else {
            result = text;
        }

        // Limit the buffer
        if (result.length > self.bufferSize) {
            result = result.substr(result.length - self.bufferSize);
        }
        
        database.console = result;
    }

    /**
     * Update current database list with new list
     * @param {array} newDatabases New database list
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

                    if (arg.propertyName == "running") {

                        if (arg.newValue) {
                            self._onDatabaseStarted(arg.source);
                        }
                        else {
                            self._onDatabaseStopped(arg.source);
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
            for (var n = 0; n < newDatabases.length; n++) {
                var newDatabase = newDatabases[n];

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
            this._onRemovedDatabase(removeList[i]);
        }

        // Add new databases
        for (var i = 0; i < newList.length; i++) {
            self.databases.push(newList[i]);
            this._onNewDatabase(newList[i]);
        }
    }

    /**
     * On New database Event
     * @param {object} database Database
     */
    this._onNewDatabase = function (database) {

        // Add additional properties
        database.console = "";
        database.consoleManualMode = false;

        // Socket event listener
        database.consoleListener = {
            databaseName: database.name,
            onEvent: function (consoleEvents) {

                var consoleText = "";
                for (var i = 0; i < consoleEvents.length; i++) {
                    consoleText = consoleText + consoleEvents[i].text;
                }
                self._onConsoleOutputEvent(database, consoleText, true);

            },
            onError: function (messageObject) {

                // Sliently fallback to manual mode
                database.consoleManualMode = true;

            },
            filter: { databaseName: database.name }
        }

        if (database.running) {
            ConsoleService.registerEventListener(database.consoleListener);
        }
    }

    /**
     * On database Removed event
     * @param {object} database Database
     */
    this._onRemovedDatabase = function (database) {

        if (database.running) {
            ConsoleService.unregisterEventListener(database.consoleListener);
        }
    }

    /**
     * On database started event
     * @param {object} database Database
     */
    this._onDatabaseStarted = function (database) {

        database.console = "";
        ConsoleService.registerEventListener(database.consoleListener);
    }

    /**
     * On database stopped event
     * @param {object} database Database
     */
    this._onDatabaseStopped = function (database) {

        database.console = "";
        ConsoleService.unregisterEventListener(database.consoleListener);
    }

    /**
     * Start database
     * @param {object} database Database
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.startDatabase = function (database, successCallback, errorCallback) {

        database.task = { "Text": "Starting" };

        $http.post('/api/engines', { Name: database.name }).then(function (response) {
            // Success

            database.task = null;
            $log.info("Database " + database.name + " was successfully started");

            // Refresh databases
            self.refreshDatabases(successCallback, errorCallback);

        }, function (response) {
            // Error

            database.task = null;
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
     * Stop database
     * @param {object} database Database
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.stopDatabase = function (database, successCallback, errorCallback) {

        database.task = { "Text": "Stopping" };
        var databaseUri = UtilsFactory.toRelativePath(database.engineUri);

        $http.delete(databaseUri, { Name: name }).then(function (response) {
            // Success (202, 204)

            database.task = null;
            $log.info("Database " + database.name + " was successfully stopped");

            // Refresh databases
            self.refreshDatabases(successCallback, errorCallback);

        }, function (response) {
            // Error

            database.task = null;

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
                    // 409 The database is already running or the Engine is not started.
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
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
     * Create database
     * @param {object} settings Settings
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.createDatabase = function (settings, successCallback, errorCallback) {

        var job = { message: "Creating database " + settings.Name };
        JobFactory.AddJob(job);

        $http.post('/api/admin/databases', settings).then(function (response) {

            // success handler
            JobFactory.RemoveJob(job);

            // Refresh databases
            self.refreshDatabases(function () {

                // Success
                var database = self.getDatabase(response.data.name);

                if (successCallback != null) {
                    // TODO: Return the newly create database
                    successCallback(database);
                }

            }, function (messageObject) {

                if (errorCallback != null) {
                    // TODO: Return the newly create database
                    errorCallback(messageObject);
                }
            });


        }, function (response) {

            // Error
            JobFactory.RemoveJob(job);
            var messageObject;

            var errorHeader = "Failed to create database";

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
                }
                else if (response.status == 403) {
                    // 403 forbidden (Validation errors)
                    if (response.data.hasOwnProperty("Items") == true) {
                        // Validation errors
                        errorCallback(null, response.data.Items);
                        return;
                    }
                    // TODO:
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data, response.data.Helplink);

                }
                else if (response.status == 404) {
                    // 404 Not found
                    // The database was not created
                    messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                }
                else if (response.status == 422) {
                    // 422 Unprocessable Entity (WebDAV; RFC 4918)
                    // The request was well-formed but was unable to be followed due to semantic errors
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
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
     * Delete database
     * @param {object} database Database
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.deleteDatabase = function (database, successCallback, errorCallback) {

        database.task = { "Text": "Deleting" };

        var databaseUri = UtilsFactory.toRelativePath(database.uri);

        $http.delete(databaseUri).then(function (response) {
            // Success

            database.task = null;
            $log.info("Database " + database.name + " was successfully deleted");

            // Refresh databases
            self.refreshDatabases(successCallback, errorCallback);

        }, function (response) {
            // Error

            database.task = null;

            var errorHeader = "Failed to delete database";
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
                    // 409 The database is already running or the Engine is not started.
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
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
     * Get Database settings
     * @param {object} database Database
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.getSettings = function (database, successCallback, errorCallback) {


//        var job = { message: "Retriving database settings" };
//        JobFactory.AddJob(job);
        var uri = "/api/admin/databases/" + database.name + "/settings";

        $http.get(uri).then(function (response) {
            // success handler
            //JobFactory.RemoveJob(job);

            $log.info("Databases settings successfully retrived");
            if (typeof (successCallback) == "function") {
                successCallback(response.data);
            }


        }, function (response) {

//            JobFactory.RemoveJob(job);
            // Error
            var messageObject;

            var errorHeader = "Failed to retrieve the database settings";

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {

                if (response instanceof SyntaxError) {
                    messageObject = UtilsFactory.createErrorMessage(errorHeader, response.message, null, response.stack);
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
     * Get Database default settings
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.getDatabaseDefaultSettings = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve the database default settings";

        //        var job = { message: "Retriving database defult settings" };
        //        JobFactory.AddJob(job);

        $http.get('/api/admin/settings/database').then(function (response) {
            // success handler
            //            JobFactory.RemoveJob(job);

            $log.info("Default Databases settings successfully retrived");
            if (typeof (successCallback) == "function") {
                successCallback(response.data);
            }

        }, function (response) {

            //            JobFactory.RemoveJob(job);
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
     * Save database settings
     * @param {object} database Database
     * @param {object} settings Settings
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.saveSettings = function (database, settings, successCallback, errorCallback) {

        var errorHeader = "Failed to save database settings";

        var job = { message: "Saving database settings" };
        JobFactory.AddJob(job);
        var uri = "/api/admin/databases/" + database.name + "/settings";

        $http.put(uri, settings).then(function (response) {

            // success
            JobFactory.RemoveJob(job);

            if (successCallback != null) {
                successCallback(response.data);
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
                    // 403 forbidden (Validation errors)

                    // Validation errors
                    if (response.data.hasOwnProperty("Items") == true) {
                        errorCallback(null, response.data.Items);
                        return;
                    }

                    // TODO:
                    messageObject = UtilsFactory.createMessage(errorHeader, response.data, response.data.Helplink);


                }
                else if (response.status == 404) {
                    // 404 Not found
                    // The database was not created
                    messageObject = UtilsFactory.createServerErrorMessage(errorHeader, response.data);
                }
                    //else if (response.status == 422) {
                    //    // 422 Unprocessable Entity (WebDAV; RFC 4918)
                    //    // The request was well-formed but was unable to be followed due to semantic errors
                    //    messageObject = UtilsFactory.createMessage(errorHeader, response.data.Text, response.data.Helplink);
                    //}
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
