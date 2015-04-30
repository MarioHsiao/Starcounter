/**
 * ----------------------------------------------------------------------------
 * Applications Service
 * ----------------------------------------------------------------------------
 */
adminModule.service('MessageService', ['$http', '$rootScope', '$log', '$sce', 'UtilsFactory', 'JobFactory', function ($http, $rootScope, $log, $sce, UtilsFactory, JobFactory) {

    var self = this;

    this.messages = [];

    /**
     * Get Messages
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this._getMessages = function (successCallback, errorCallback) {

        var errorHeader = "Failed to retrieve messages";
        var uri = "/api/admin/messages";

        $http.get(uri).then(function (response) {
            // Success
            if (typeof (successCallback) == "function") {
                successCallback(response.data.Items);
            }
        }, function (response) {
            // Error

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {
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

                errorCallback(messageObject);
            }
        });
    }

    /**
     * Get a Message
     * @param {string} id Message ID
     * @return {object} Message or null
     */
    this._getMessage = function (id) {

        for (var i = 0 ; i < self.messages.length ; i++) {
            if (self.messages[i].ID == id) {
                return self.messages[i];
            }
        }
        return null;
    }

    /**
     * Refresh Messages
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.refreshMessages = function (successCallback, errorCallback) {

        this._getMessages(function (messages) {

            // Success
            // TODO: Dynamic update
            self._updateMessageList(messages);

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
     * Update Message
     */
    this._updateMessageInstance = function (freshApplication, application) {

        UtilsFactory.updateObject(freshApplication, application, function (arg) {

        });
    }

    /**
     * Update current message list with new list
     * @param {array} freshMessages application list
     */
    this._updateMessageList = function (freshMessages) {

        var removeList = [];

        // Check for new application and update current applications
        for (var i = 0; i < freshMessages.length; i++) {
            var freshMessage = freshMessages[i];
            var message = self._getMessage(freshMessage.ID);
            if (message == null) {
                self.messages.push(freshMessage);
            } else {
                self._updateMessageInstance(freshMessage, message);
            }
        }


        // Remove removed messages from message list
        for (var i = 0; i < self.messages.length; i++) {

            var message = self.messages[i];
            var bExists = false;
            // Check if it exist in newList
            for (var x = 0; x < freshMessages.length; x++) {
                var freshMessage = freshMessages[x];
                if (message.ID == freshMessage.ID) {
                    bExists = true;
                    break;
                }
            }

            if (bExists == false) {
                removeList.push(message);
            }
        }

        // Remove message from message list
        for (var i = 0; i < removeList.length; i++) {
            var index = self.messages.indexOf(removeList[i]);
            if (index > -1) {
                self.messages.splice(index, 1);
            }
            removeList[i].running = false;
        }

    }

    /**
     * Delete a message
     * @param {object} message Message
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.deleteMessage = function (message, successCallback, errorCallback) {

        var errorHeader = "Failed to delete message";
        var uri = "/api/admin/messages/"+ message.ID;

        $http.delete(uri).then(function (response) {
            // Success

            self.refreshMessages(function () {

                if (typeof (successCallback) == "function") {
                    successCallback();
                }

            }, function () {

                if (typeof (successCallback) == "function") {
                    successCallback();
                }
            });

        }, function (response) {
            // Error

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {
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

                errorCallback(messageObject);
            }
        });

    }

    /**
     * Delete all a messages
     * @param {function} successCallback Success Callback function
     * @param {function} errorCallback Error Callback function
     */
    this.deleteAllMessages = function (successCallback, errorCallback) {

        var errorHeader = "Failed to delete all message";
        var uri = "/api/admin/messages";

        $http.delete(uri).then(function (response) {
            // Success


            self.refreshMessages(function () {

                if (typeof (successCallback) == "function") {
                    successCallback();
                }

            }, function () {

                if (typeof (successCallback) == "function") {
                    successCallback();
                }
            });

        }, function (response) {
            // Error

            $log.error(errorHeader, response);

            if (typeof (errorCallback) == "function") {
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

                errorCallback(messageObject);
            }
        });
    }

}]);