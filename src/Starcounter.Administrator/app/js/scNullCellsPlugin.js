Handsontable.hooks.add('afterRenderer', function (TD, row, col, prop, value, cellProperties) {
    if (value === null) {
        TD.style.fontStyle = 'italic';
        TD.appendChild(document.createTextNode('NULL'));
    }
});
// Fix for auto-scroll on cell click
Handsontable.hooks.add('beforeSetRangeEnd', function () {
    this.view.activeWt = null;
});