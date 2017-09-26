"use strict";
//As an example, in normal JavaScript, mistyping a variable name creates a new global variable. In strict mode, this will throw an error, making it impossible to accidentally create a global variable.

function onHeadProcessing(commo) {
    console.log('onHeadProcessing()');
    
    var xmlHttp = new XMLHttpRequest();
    xmlHttp.onreadystatechange = function () {
        if (xmlHttp.readyState == 4 && xmlHttp.status == 200)
            processData(xmlHttp.responseText);
    };
    xmlHttp.open("GET", "/ContangoVisualizerData?commo="+commo, true); // true for asynchronous 
    xmlHttp.send(null);
}

function processData(dataStr)
{
   
    //Splitting data got from C#.
    var data = JSON.parse(dataStr);

    //Creating first row (dates) of webpage.
    var divTitleCont = document.getElementById("idTitleCont");
    var divTimeNow = document.getElementById("idTimeNow");
    var divLiveDataDate = document.getElementById("idLiveDataDate");
    var divLiveDataTime = document.getElementById("idLiveDataTime");
    var divFirstDataDate = document.getElementById("idFirstDataDate");
    var divLastDataDate = document.getElementById("idLastDataDate");


    divTitleCont.innerText = data.titleCont;
    divTimeNow.innerText = "Current time: " + data.timeNow;
    divLiveDataDate.innerText = "Last data time: " + data.liveDataDate;
    divLiveDataTime.innerText = data.liveDataTime;
    $('#my-link').html('<a href="' + data.dataSource + '" target="_blank">Data Source</a>');   

    creatingTables(data);

    //Setting charts visible after getting data.
    document.getElementById("inviCharts").style.visibility = "visible";
}
function creatingTables(data) {

    var monthList = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
    
    //Creating JavaScript data arrays by splitting.
    var currDataArray = data.currDataVec.split(",");
    var currDataDaysArray = data.currDataDaysVec.split(",");
    var prevDataArray = data.prevDataVec.split(",");
    var currDataDiffArray = data.currDataDiffVec.split(",");
    var currDataPercChArray = data.currDataPercChVec.split(",");
    var spotVixArray = data.spotVixVec.split(",");

    
    //Creating the HTML code of current table.
    var currTableMtx = "<table class=\"currData\"><tr align=\"center\"><td>Future Prices</td><td>F1</td><td>F2</td><td>F3</td><td>F4</td><td>F5</td><td>F6</td><td>F7</td><td>F8</td></tr><tr align=\"center\"><td align=\"left\">Current</td>";
    for (var i = 0; i < 8; i++) {
        if (currDataArray[i] == 0) {
            currTableMtx += "<td>" + "---" + "</td>";
        }else{
            currTableMtx += "<td>" + currDataArray[i] + "</td>";
        }
    }
       
    currTableMtx += "</tr><tr align=\"center\"><td align=\"left\">Previous Close</td>";
    for (var i = 0; i < 8; i++) {
        if (currDataArray[i]==0) {
            currTableMtx += "<td>" + "---"+ "</td>";
        }else{
            currTableMtx += "<td>" + prevDataArray[i] + "</td>";
        }
    }
    currTableMtx += "</tr><tr align=\"center\"><td align=\"left\">Daily Abs. Change</td>";
    for (var i = 0; i < 8; i++) {
        if (currDataArray[i] == 0) {
            currTableMtx += "<td>" + "---" + "</td>";
        }else {
            currTableMtx += "<td>" + currDataDiffArray[i] + "</td>";
        }
    }
    currTableMtx += "</tr><tr align=\"center\"><td align=\"left\">Daily % Change</td>";
    for (var i = 0; i < 8; i++) {
        if (currDataArray[i] == 0) {
            currTableMtx += "<td>" + "---" + "</td>";
        } else {
            currTableMtx += "<td>" + (currDataPercChArray[i] * 100).toFixed(2) + "%</td>";
        }
    }
    currTableMtx += "</tr><tr align=\"center\"><td align=\"left\">Cal. Days to Expiration</td>";
    for (var i = 0; i < 8; i++) {
        if (currDataArray[i] == 0) {
            currTableMtx += "<td>" + "---" + "</td>";
        }
        else {
            currTableMtx += "<td>" + currDataDaysArray[i] + "</td>";
        }
    }
    currTableMtx += "</tr></table><br><br>";
   
    currTableMtx += "<table class=\"currData\"><tr align=\"center\"><td>Contango</td><td>F2-F1</td><td>F3-F2</td><td>F4-F3</td><td>F5-F4</td><td>F6-F5</td><td>F7-F6</td><td>F8-F7</td><td>F7-F4</td><td>(F7-F4)/3</td></tr><tr align=\"center\"><td align=\"left\">Contango %</td><td><b>" + (currDataArray[i] * 100).toFixed(2) + "%</b></td>";
    for (var i = 20; i < 27; i++) {
        if (currDataArray[i] == 0) {
            currTableMtx += "<td>" + "---" + "</td>";
        } else {
            currTableMtx += "<td>" + (currDataArray[i] * 100).toFixed(2) + "%</td>";
        }
    }
    currTableMtx += "<td><b>" + (currDataArray[27] * 100).toFixed(2) + "%</b></td>";
    currTableMtx += "</tr><tr align=\"center\"><td align=\"left\">Difference</td>";
    for (var i = 10; i < 19; i++) {
        if (currDataArray[i] == 0) {
            currTableMtx += "<td>" + "---" + "</td>";
        }
        else {
            currTableMtx += "<td>" + (currDataArray[i] * 100 / 100).toFixed(2) + "</td>";
        }
    }
    currTableMtx += "</tr></table>";

    //"Sending" data to HTML file.
    var currTableMtx2 = document.getElementById("idCurrTableMtx");
    currTableMtx2.innerHTML = currTableMtx;
    
      
    var nCurrData = 7;
    var currDataPrices = new Array(nCurrData);
    for (var i = 0; i < nCurrData; i++) {
        var currDataPricesRows = new Array(2);
        currDataPricesRows[0] = currDataDaysArray[i];
        currDataPricesRows[1] = currDataArray[i];
        currDataPrices[i] = currDataPricesRows;
    }

    var prevDataPrices = new Array(nCurrData);
    for (var i = 0; i < nCurrData; i++) {
        var prevDataPricesRows = new Array(2);
        prevDataPricesRows[0] = currDataDaysArray[i];
        prevDataPricesRows[1] = prevDataArray[i];
        prevDataPrices[i] = prevDataPricesRows;
    }

    var spotVixValues = new Array(nCurrData);
    for (var i = 0; i < nCurrData; i++) {
        var spotVixValuesRows = new Array(2);
        spotVixValuesRows[0] = currDataDaysArray[i];
        spotVixValuesRows[1] = spotVixArray[i];
        spotVixValues[i] = spotVixValuesRows;
    }

    
    //Declaring data sets to charts.
    if (spotVixValues[0][1] > 0)
    {
        var datasets1 = {
            "current": {
                label: "Current",
                data: currDataPrices,
                points: { show: true },
                lines: { show: true }
            },
            "previous": {
                label: "Last close",
                data: prevDataPrices,
                points: { show: true },
                lines: { show: true }
            },
            "spot": {
                label: "Spot VIX",
                data: spotVixValues,
                points: { show: true },
                lines: { show: true }
            }
        };
    } else {
        var datasets1 = {
            "current": {
                label: "Current",
                data: currDataPrices,
                points: { show: true },
                lines: { show: true }
            },
            "previous": {
                label: "Last close",
                data: prevDataPrices,
                points: { show: true },
                lines: { show: true }
            }
        };
    }


    flotPlotMyData1(datasets1);
    

    
}
// Creating first chart: prices by days to expiration.
function flotPlotMyData1(datasets1) {
    //-- hard-code color indices to prevent them from shifting as
    //-- countries are turned on/off

    var i = 0;
    $.each(datasets1, function (key, val) {
        val.color = i;
        ++i;
    });
    
    // insert checkboxes
    $("#choices1").html("");
    var choiceContainer = $("#choices1");
    $.each(datasets1, function (key, val) {
        if (val.color < 5) {
            choiceContainer.append("<br/><input type='checkbox' name='" + key +
            "' checked='checked' id='id" + key + "'></input>" +
            "<label for='id" + key + "'>"
            + val.label + "</label>");
        }
        else {
            choiceContainer.append("<br/><input type='checkbox' name='" + key +
            "' id='id" + key + "'></input>" +
            "<label for='id" + key + "'>"
            + val.label + "</label>");
        }
    });

    choiceContainer.find("input").click(plotAccordingToChoices);

    function plotAccordingToChoices() {

        var data = [];

        choiceContainer.find("input:checked").each(function () {
            var key = $(this).attr("name");
            if (key && datasets1[key]) {
                data.push(datasets1[key]);
            }
        });


        if (data.length > 0) {
            $.plot("#placeholder1", data, {
                yaxis: {

                },
                xaxis: {
                    tickDecimals: 0,
                    min: 0,
                    max: 225
                },
                legend: {
                    position: "nw"
                }
            });
        }
    }

    plotAccordingToChoices();

}