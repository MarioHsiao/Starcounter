(function() {
  var scrollViewport = Walkontable.prototype.scrollViewport;
  var keyDown = false;

  function init() {
    Handsontable.dom.addEvent(document.body, 'keydown', function(event) {
      keyDown = true;
    });
    Handsontable.dom.addEvent(document.body, 'keyup', function(event) {
      keyDown = false;
    });

    // Fix for autoscroll on cell click
    Walkontable.prototype.scrollViewport = function(coords) {
      if (keyDown) {
        scrollViewport.call(this, coords);
      }
    };
  }

  $(init);
}());

// Monkey patch - fix for quotas from copied cell
Handsontable.DataMap.prototype.getCopyableText = function(start, end) {
  return this.getRange(start, end, this.DESTINATION_CLIPBOARD_GENERATOR);
};