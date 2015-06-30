
(function() {
  function logMessageHostFilter(logMessageHostPrefix) {
    return function(text) {
      var rest = text.replace(logMessageHostPrefix, '');
      var result = '';

      if (rest === '') {
        result = '';
      } else if (rest === '/service') {
        result = 'Service';
      } else if (rest === '/networkgateway') {
        result = 'Network Gateway';
      } else {
        result = '#' + rest;
      }

      return result;
    };
  }
  logMessageHostFilter.$inject = ['logMessageHostPrefix'];

  adminModule.filter('logMessageHost', logMessageHostFilter);
}());