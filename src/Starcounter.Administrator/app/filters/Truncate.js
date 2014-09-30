/**
 * Truncate Filter
 *
 * Source: Daniel Gomes's blog http://danielcsgomes.com/tutorials/how-to-create-a-custom-filter-with-angularjs-v1'
 *
 * @Param text
 * @Param length, default is 10
 * @Param end, default is "..."
 * @return string
 */
adminModule.filter('truncate', function () {
    return function (text, length, end) {
        if (isNaN(length))
            length = 10;

        if (end === undefined)
            end = "...";

        if (text.length <= length || text.length - end.length <= length) {
            return text;
        }
        else {
            return String(text).substring(0, length - end.length) + end;
        }

    };
});

/**
 * Usage
 *
 * var myText = "This is an example.";
 *
 * {{myText|Truncate}}
 * {{myText|Truncate:5}}
 * {{myText|Truncate:25:" ->"}}
 * Output
 * "This is..."
 * "Th..."
 * "This is an e ->"
 *
 */


adminModule.filter('truncateversion', function () {
    return function (text, parts) {

        if (text == null) return text;

        if (isNaN(parts))
            parts = 3;

        var truncated = "";
        var partlist = text.split(".");

        for ( i = 0; i < parts && i < partlist.length; i++) {
            if (i > 0) {
                truncated += ".";
            }
            truncated += partlist[i];
        }

        return truncated;
    };
});
