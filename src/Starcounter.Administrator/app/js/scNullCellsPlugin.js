Handsontable.hooks.add('afterRenderer', function (TD, row, col, prop, value, cellProperties) {
  if (value === null) {
    TD.style.fontStyle = 'italic';
    TD.appendChild(document.createTextNode('NULL'));
  }
});
// Fix for autoscroll on cell click
//Handsontable.hooks.add('beforeSetRangeEnd', function () {
//	this.view.activeWt = null;
//});

// Monkey patch - fix for quotas from copied cell
Handsontable.DataMap.prototype.getCopyableText = function(start, end) {
  return this.getRange(start, end, this.DESTINATION_CLIPBOARD_GENERATOR);
};