Handsontable.hooks.add('afterRenderer', function (TD, row, col, prop, value, cellProperties) {
  if (value === null) {
    TD.style.fontStyle = 'italic';
    TD.appendChild(document.createTextNode('NULL'));
  }
});
// Fix for autoscroll on cell click
(function() {
  var keyDown = false;

  function init() {
    Handsontable.Dom.addEvent(document.body, 'keydown', function(event) {
      keyDown = true;
    });
    Handsontable.Dom.addEvent(document.body, 'keyup', function(event) {
      keyDown = false;
    });

    Handsontable.hooks.add('beforeSetRangeEnd', function () {
      if (!keyDown) {
        this.view.activeWt = null;
      }
    });
  }
  $(init);
}());

// Monkey patch - fix for quotas from copied cell
Handsontable.DataMap.prototype.getCopyableText = function(start, end) {
  return this.getRange(start, end, this.DESTINATION_CLIPBOARD_GENERATOR);
};