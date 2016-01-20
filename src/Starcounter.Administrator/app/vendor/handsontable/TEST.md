Updating Handsontable? Make sure the following checklist passes in Chrome, Firefox and Edge:

## Handsontable build

1. Handsontable used in this project is customized. Check if your build has included this modules: 
 - AutoColumnSize
 - CopyPaste
 - ManualColumnResize

## Executing SQL query

1. Clicking CTRL+ENTER in SQL textarea refreshes Handsontable (also after a cell was selected).

## Scrolling

1. Displaying the result of SELECT * FROM MaterializedTable scrolls correctly.
2. Displaying the result of SELECT * FROM BigTable () scrolls correctly vertically and horizontally:
 - with mouse wheel
 - with keyboard arrows
 - with window scrollbar "^", "v", "<", ">" icons
 - with window scrollbar dragging
 - with clicking on window scrollbar blank space

## Pressing CTRL+A

1. Should select all cells in HOT only if a cell (or cell fragment) is selected.

## Single cell selection

1. By default, no cell in HOT should be selected.
2. Clicking on a cell in HOT should select it.
3. Clicking outside of HOT should deselect the cell.

## Fragment selection vs multiple cell selection

1. Should be possible to select a text fragment in a single cell. Pressing CTRL+C should copy only that fragment.
2. Should behave like regular multiple cell selection, when pressing LMB on a cell and moving cursor over another cell. Pressing CTRL+C should copy selected cells.
3. Should behave like regular multiple cell selection, when clicking LMB on a cell and clicking on another cell with Shift. Pressing CTRL+C should copy selected cells.
