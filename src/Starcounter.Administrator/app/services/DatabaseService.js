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
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
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
     * @param {string} databaseName Database name
     * @return {object} Database or null
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

        }, function (response) {

            // Error
            if (typeof (errorCallback) == "function") {
                errorCallback(response);
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
     * @param {object} database Database
     */
    this._onNewDatabase = function (database) {
        $log.debug("_onNewDatabase:" + database.name);

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
        $log.debug("_onRemovedDatabase:" + database.name);
        if (database.running) {
            ConsoleService.unregisterEventListener(database.consoleListener);
        }
    }


    /**
     * On database started event
     * @param {object} database Database
     */
    this._onDatabaseStarted = function (database) {
        $log.debug("_onDatabaseStarted:" + database.name);
        ConsoleService.registerEventListener(database.consoleListener);
    }

    /**
     * On database stopped event
     * @param {object} database Database
     */
    this._onDatabaseStopped = function (database) {
        $log.debug("_onDatabaseStopped:" + database.name);
        ConsoleService.unregisterEventListener(database.consoleListener);
    }


    /**
     * Start database
     * @param {object} database Database
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
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

            // TODO: Refresh applications (HostModelService)

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
     * @param {object} database Database
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.stopDatabase = function (database, successCallback, errorCallback) {

        var errorHeader = "Failed to stop database";
        var job = { message: "Stopping database " + database.name };

        JobFactory.AddJob(job);
        var databaseUri = UtilsFactory.toRelativePath(database.engineUri);

        $http.delete(databaseUri, { Name: name }).then(function (response) {

            // Success (202, 204)
            JobFactory.RemoveJob(job);
            $log.info("Database " + database.name + " was successfully stopped");

            // TODO: Refresh applications (HostModelService)

            // Refresh databases
            self.refreshDatabases(successCallback, errorCallback);

        }, function (response) {

            // Error
            JobFactory.RemoveJob(job);
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
     * Delete database
     * @param {object} database Database
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.deleteDatabase = function (database, successCallback, errorCallback) {

        var errorHeader = "Failed to delete database";
        var job = { message: "Deleting database " + database.name };


        JobFactory.AddJob(job);
        var databaseUri = UtilsFactory.toRelativePath(database.uri);

        $http.delete(databaseUri).then(function (response) {

            // Success
            JobFactory.RemoveJob(job);
            $log.info("Database " + database.name + " was successfully deleted");

            // TODO: Refresh applications (HostModelService)

            // Refresh databases
            self.refreshDatabases(successCallback, errorCallback);

        }, function (response) {

            // Error
            JobFactory.RemoveJob(job);
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
                    if (response.data.hasOwnProperty("Text") == true) {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data.Text, response.data.Helplink, null);
                    } else {
                        messageObject = UtilsFactory.createErrorMessage(errorHeader, response.data, null, null);
                    }
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
     * Get Database settings
     * @param {object} database Database
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
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
     * @param {object} database Database
     * @param {object} settings Settings
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
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
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
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
     * @param {object} settings Settings
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
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
     * @param {object} settings Settings
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
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
