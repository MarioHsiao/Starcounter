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

Handsontable.hooks.add('beforeKeyDown', function(event) {
  var activeElement = document.activeElement;

  if (getSelectionText() ||
      (activeElement && activeElement.nodeName === 'TEXTAREA' && activeElement.className !== 'copyPaste')) {
    Handsontable.dom.stopImmediatePropagation(event);
  }
})

function getSelectionText() {
  var text = '';

  if (window.getSelection) {
    text = window.getSelection().toString();
  } else if (document.selection && document.selection.type !== 'Control') {
    text = document.selection.createRange().text;
  }

  return text;
}