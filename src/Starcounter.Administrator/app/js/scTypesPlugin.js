/**
 * predefined cell types in Handsontable are:
 * text, numeric, checkbox, autocomplete, handsontable
 *
 * numeric should be used with numbers in range [-4294967295, 4294967295] inclusive
 * http://stackoverflow.com/questions/307179/what-is-javascripts-max-int-whats-the-highest-integer-value-a-number-can-go-t
 */

Handsontable.cellTypes.Boolean = Handsontable.cellTypes.checkbox;

Handsontable.cellTypes.Byte = Handsontable.cellTypes.text; //or numeric? you decide

Handsontable.cellTypes.DateTime = Handsontable.cellTypes.text; //or date? you decide, I don't know about the format used, so it may or may not work

Handsontable.cellTypes.Decimal = Handsontable.cellTypes.text; //it can be numeric if it's in range [-4294967295, 4294967295]
Handsontable.cellTypes.Single = Handsontable.cellTypes.text; //it can be numeric if it's in range [-4294967295, 4294967295]
Handsontable.cellTypes.Double = Handsontable.cellTypes.text; //it can be numeric if it's in range [-4294967295, 4294967295]
Handsontable.cellTypes.Int64 = Handsontable.cellTypes.text; //it can be numeric if it's in range [-4294967295, 4294967295]
Handsontable.cellTypes.Int32 = Handsontable.cellTypes.numeric;
Handsontable.cellTypes.Int16 = Handsontable.cellTypes.numeric;

Handsontable.cellTypes.Object = Handsontable.cellTypes.text;

Handsontable.cellTypes.SByte = Handsontable.cellTypes.text; //or numeric? you decide
Handsontable.cellTypes.String = Handsontable.cellTypes.text;

Handsontable.cellTypes.UInt64 = Handsontable.cellTypes.text; //it can be numeric if it's in range [-4294967295, 4294967295]
Handsontable.cellTypes.UInt32 = Handsontable.cellTypes.text; //it can be numeric if it's in range [-4294967295, 4294967295]
Handsontable.cellTypes.UInt16 = Handsontable.cellTypes.text; //it can be numeric if it's in range [-4294967295, 4294967295]

Handsontable.cellTypes.Binary = Handsontable.cellTypes.text; //Handsontable now does not have a default type, so we must map all other types as text
Handsontable.cellTypes.LargeBinary = Handsontable.cellTypes.text;
Handsontable.cellTypes.Key = Handsontable.cellTypes.text;

(function() {
  // maxWidth renderer
  Handsontable.cellTypes.autoDetect = {
    editor: Handsontable.cellTypes.text.editor,
    renderer: function(instance, TD, row, col, prop, value, cellProperties) {
      var maxWidth = cellProperties.maxWidth;
      var columnWidth = instance.getPlugin('autoColumnSize').getColumnWidth(col);
      var rendererName;

      if (maxWidth && columnWidth >= maxWidth) {
        Handsontable.cellTypes.logMessage.renderer.apply(this, arguments);
        instance.getPlugin('autoColumnSize').widths[col] = maxWidth;
      } else {
        rendererName = cellProperties.type.charAt(0).toUpperCase() + cellProperties.type.substr(1, cellProperties.type.length - 1);

        if (Handsontable.renderers[rendererName + 'Renderer']) {
          Handsontable.renderers[rendererName + 'Renderer'].apply(this, arguments);
        } else {
          Handsontable.renderers.TextRenderer.apply(this, arguments);
        }
      }
    }
  };

  // logMessage renderer
  Handsontable.cellTypes.logMessage = {
    renderer: function(instance, TD, row, col, prop, value, cellProperties) {
      var
        escaped = Handsontable.helper.stringify(value),
        className = 'wrapper',
        logElement;

      Handsontable.Dom.addClass(TD, 'log-message');
      Handsontable.renderers.BaseRenderer.apply(this, arguments);
      logElement = getLogTemplate(escaped);

      // Detect width for multiline logs or json data
      if (/\n/.test(escaped) || /^\{"(.*)\}$/.test(escaped)) {
        logElement.firstChild.style.width = detectLogWidth(escaped) + 'px';
      }
      Handsontable.Dom.empty(TD);
      Handsontable.Dom.addClass(logElement, className);
      TD.appendChild(logElement);
    }
  };

  // logHost renderer
  Handsontable.cellTypes.logHost = {
    renderer: function(instance, TD, row, col, prop, value, cellProperties) {
      var escaped = Handsontable.helper.stringify(value),
        logElement;

      Handsontable.renderers.BaseRenderer.apply(this, arguments);
      logElement = getLogHostTemplate(escaped);

      Handsontable.Dom.empty(TD);
      TD.appendChild(logElement);
    }
  };

  function getLogTemplate(logMessage) {
    var tpl = document.createElement('div'),
      code = document.createElement('code'),
      pre = document.createElement('pre');

    pre.appendChild(code);
    tpl.appendChild(pre);
    code.textContent = logMessage;

    return tpl;
  }

  function getLogHostTemplate(logMessage) {
    var tpl, url, dbPath;

    if (logMessage[0] === '#') {
      dbPath = logMessage.split('/');
      url = '/#/databases/' + dbPath[3] + '/' + dbPath[4];
      tpl = document.createElement('a');
      tpl.href = url;
      tpl.textContent = dbPath[4];
    } else {
      tpl = document.createTextNode(logMessage);
    }

    return tpl;
  }

  function detectLogWidth(logMessage) {
    var el = document.querySelector('.log-message-detector'),
      width;

    if (el) {
      el.firstChild.firstChild.textContent = logMessage;
    } else {
      el = getLogTemplate(logMessage);
      el.style.position = 'absolute';
      el.style.top = '-10000px';
      el.style.left = '-10000px';
      el.style.visibility = 'hidden';
      el.className = 'log-message-detector';
      document.body.appendChild(el);
    }
    width = el.clientWidth;

    return width;
  }
}());