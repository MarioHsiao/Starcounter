

//angular.module('scadminServices', ['ngResource']).
//    factory('Database', function ($resource) {
//        return $resource('/databases/:databaseId', { databaseId: '@databaseId' }, {
//            query: { method: 'GET', isArray: false },
//            get: { method: 'GET', headers: { 'Content-Type': 'application/json' } }
//        });
//    }).
//    factory('Log', function ($resource) {
//        return $resource('/log', {}, { query: { method: 'GET', isArray: false } });
//    }).
//    factory('Sql', function ($resource) {
//        return $resource('/sql', {}, { query: { method: 'GET', isArray: false } });
//    });

// databases
// databases/<databaseid>

angular.module('scadminServices', ['ngResource'], function ($provide) {


    $provide.service('patchService', function ($resource) {

        this.applyPatch = function (source, location, patches) {
            var patchstring = JSON.stringify(patches);

            // Send json patch(es)
            // TODO: The Port is hardcoded
            var resource = $resource('http://localhost\\:80' + location, {}, { send: { method: 'PATCH', isArray: true } });

            // Send the json-patch data
            resource.send(patchstring, function (data) {
                console.log("REQUEST (" + location + ") PATCH: " + patchstring);

                console.log("RESPONSE (" + location + ") PATCH:" + JSON.stringify(data));
                jsonpatch.apply(source, data);
            });

        }
    });


    $provide.factory('Database', function ($resource) {

        return $resource('/databases/:databaseId', { databaseId: '@databaseId' }, {
            query: { method: 'GET', isArray: false }   // We need to override this (the return type is not an array)
            //get: { method: 'GET', headers: { 'Content-Type': 'application/json' } }
        });

    });

    $provide.factory('Log', function ($resource) {

        return $resource('/log', {}, {
            query: { method: 'GET', isArray: false }    // We need to override this (the return type is not an array)
        });

    });

    $provide.factory('Sql', function ($resource) {

        return $resource('/sql', {}, {
            query: { method: 'GET', isArray: false }    // We need to override this (the return type is not an array)
        });

    });

    $provide.factory('SqlQuery', function ($resource) {


        // "/__{0}/sql", this.DatabaseName
        return $resource('/__:databaseName/sql', { databaseName: '@databaseName' }, {
            send: { method: 'POST', isArray: false }    // We need to override this (the return type is not an array)
        });

    });


    //$provide.factory('Patch', function ($resource) {

    //    return $resource('http://localhost\\:80:location', { location: '@location' }, {
    //        patch: { method: 'PATCH', isArray: true }    // We need to override this (the return type is not an array)
    //    });

    //});

});

