/**
 * ----------------------------------------------------------------------------
 * Notice Factory
 * ----------------------------------------------------------------------------
 */
adminModule.service('JobFactory', ['$log', function ($log) {

    var factory = {};

    // object { type: "error", msg: "message", helpLink: "helpLink" }
    factory.jobs = [];

    /**
     * Add job
     * @param {object} job Job
     */
    factory.AddJob = function (job) {
        factory.jobs.push(job);
    }

    /**
     * Remove job
     * @param {object} job Job
     */
    factory.RemoveJob = function (job) {
        var index = factory.jobs.indexOf(job);
        if (index > -1) {
            factory.jobs.splice(index, 1);
        }
    }

    return factory;

}]);
