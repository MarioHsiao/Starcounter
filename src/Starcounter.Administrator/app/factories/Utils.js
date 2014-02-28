
adminModule.factory('UtilsFactory', ['$log', function ($log) {
    var factory = {};

    /**
     * Retrives the relative path of an url
     * Example:
     * Input: http://localhost:8080/foo/bar?123
     * Output: /foo/bar
     */
    factory.toRelativePath = function (url) {
        var a = document.createElement('a');
        a.href = url;
        return a.pathname;
    };

    /**
     * Create a Message object
     */
    factory.createMessage = function (header, message, helpLink) {
        return { isError: false, header: header, message: message, helpLink: (helpLink) ? helpLink : null, stackTrace: null };
    }

    /**
     * Create a Error Message object
     */
    factory.createErrorMessage = function (header, message, helpLink, stackTrace) {
        return { isError: true, header: header, message: message, helpLink: (helpLink) ? helpLink : null, stackTrace: (stackTrace) ? stackTrace : null };
    }


    /**
     * Update Object
     * @param {source} New object source
     * @param {destination} object to update
     */
    factory.updateObject = function (source, destination, properyChangedCallback) {

        for (var propertyName in source) {

            if (Object.prototype.toString.call(source[propertyName]) === '[object Array]') {
                $log.warn("TODO: Handle Array (" + propertyName + ")");
                // TODO: Compare Arrays
                continue;
            }
            else if (typeof source[propertyName] == "object") {
                this.updateObject(source[propertyName], destination[propertyName]);
            }
            else if (destination[propertyName] != source[propertyName]) {

                var oldValue = destination[propertyName];

                destination[propertyName] = source[propertyName];

                if (typeof (properyChangedCallback) == "function") {
                    properyChangedCallback({ propertyName: propertyName, source:destination, newValue: destination[propertyName], oldValue: oldValue });

                }

            }

        }

    }


    return factory;

}]);
