var globalAllPlots = new Array();
var globalTests = new Tests();
var showPersonal = false;
var showBuildNames = true;
var maxNumTests = 50;

function RemoveAllPlots() {
    for (var i = 0; i < globalAllPlots.length; i++) {
        globalAllPlots[i].remove();
    }
    globalAllPlots = new Array();
}

function DrawPlot(plotName, plotWidth, plotHeight, testInfo) {

	var r = Raphael(plotName, plotWidth, plotHeight), txtattr = { font: "medium Arial Black" };
    
    var testName = r.text(600, 10, testInfo.name).attr(txtattr);

    var hover = function () {
		this.tags = r.set();

		for (var i = 0, n = this.y.length; i < n; i++) {
			this.tags.push(
                r.tag(this.x, this.y[i],
                    "Value: " + this.values[i] +
                    "\n \nBuild number: " + testInfo.values[this.axis].buildNumber +
                    "\n \nTest date: " + testInfo.values[this.axis].testDate + 
                    "\n \nBuild agent: " + testInfo.values[this.axis].buildServerName +
                    "\n \nPersonal build: " + testInfo.values[this.axis].personalBuild, 0, 0).attr([{ fill: "#000000" }, { fill: "#ffffff" }])).insertBefore(this);
		}
	};
	
	var unhover = function () {
		this.tags && this.tags.remove();
	};
    
    var plotXValues = new Array();
    var plotYValues = new Array();
   
    for (var i = 0; i < testInfo.values.length; i++) {
        plotXValues[i] = i;
        plotYValues[i] = testInfo.values[i].testValue;
    }
        
	var lines = r.linechart(
        100,
        100,
        plotWidth - 400,
        plotHeight - 200,
        [plotXValues],
        [plotYValues],
        { nostroke: false, axis: "0 0 0 1", symbol: "circle", smooth: true, width: 3, colors: ["#008888"], dash: ["-"] }
        ).hoverColumn(hover, unhover);

	lines.symbols.attr({ r: 10 });
	lines.symbols[0].attr({ stroke: "#fff" });

    // Changing color of personal build knobs.
	for (var i = 0; i < testInfo.values.length; i++) {
	    if (testInfo.values[i].personalBuild)
	        lines.symbols[0][i].attr("fill", "#888888");
            
        if (showBuildNames)
            r.tag(lines.symbols[0][i].attrs.cx, lines.symbols[0][i].attrs.cy, "#" + testInfo.values[i].buildNumber, 60, 0).attr([{ fill: "#ffffff", stroke: "none" }, { fill: "#000000" }]).insertBefore(testName);
	}

    // Adding to global plots.
    globalAllPlots.push(r);
}

function RenderAllPlots(tests) {
    // Removing all old plot entries.
    RemoveAllPlots();

    // Drawing new plot entries.
    for (var i = 0; i < globalTests.allTests.length; i++)
        DrawPlot("canvas" + i, 1200, 600, tests.allTests[i]);
}

window.onload = function () {
    globalTests.ParseTests(false, 20);
    
    // Creating list of all tests.
    var toc = "";
    for (var i = 0; i < globalTests.allTests.length; i++) {
        toc += "<br>" + i + ": " + "<a href=\"#canvas" + i + "\">" + globalTests.allTests[i].name + "</a>";
    }
    
    document.getElementById("ListOfAllTests").innerHTML += toc;
    
    RenderAllPlots(globalTests);
};

function HandlePersonalBuildsBoolean(cb) {
    showPersonal = cb.checked;
    globalTests.ParseTests(showPersonal, maxNumTests);
    RenderAllPlots(globalTests);
}

function HandleBuildsNamesBoolean(cb) {
    showBuildNames = cb.checked;
    RenderAllPlots(globalTests);
}

function HandleMaxNumTestsRange(cb) {
    maxNumTests = cb.value;

    globalTests.ParseTests(showPersonal, maxNumTests);
    RenderAllPlots(globalTests);
}

function HandleMaxNumTestsText(cb) {
    maxNumTests = cb.value;
    document.getElementById("MaxNumTestsText").innerHTML = maxNumTests;
}