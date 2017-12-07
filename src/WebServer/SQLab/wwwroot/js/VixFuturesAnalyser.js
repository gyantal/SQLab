"use strict";
//As an example, in normal JavaScript, mistyping a variable name creates a new global variable. In strict mode, this will throw an error, making it impossible to accidentally create a global variable.

function onHeadProcessing() {
    console.log('onHeadProcessing()');
    
    var xmlHttp = new XMLHttpRequest();
    xmlHttp.onreadystatechange = function () {
        if (xmlHttp.readyState == 4 && xmlHttp.status == 200)
            processData(xmlHttp.responseText);
    }
    xmlHttp.open("GET", "/VixFuturesAnalyserData", true); // true for asynchronous 
    xmlHttp.send(null);
}

function processData(dataStr)
{
    //Splitting data got from C#.
    var data = JSON.parse(dataStr);

    //Creating first row (dates) of webpage.
    var divTimeNow = document.getElementById("idTimeNow");
    var divLiveDataDate = document.getElementById("idLiveDataDate");
    var divLiveDataTime = document.getElementById("idLiveDataTime");
    var divFirstDataDate = document.getElementById("idFirstDataDate");
    var divLastDataDate = document.getElementById("idLastDataDate");

    divTimeNow.innerText = "Current time: " + data.timeNow;
    divLiveDataDate.innerText = "Last data time: " + data.liveDataDate;
    divLiveDataTime.innerText = data.liveDataTime;
    divFirstDataDate.innerText = "The analyser is using data from " + data.firstDataDate + " to " ;
    divLastDataDate.innerText =data.lastDataDate;
   
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

    var meanOfTotalDaysTotalArray = data.meanOfTotalDaysTotalVec.split(",");
    var numberOfTotalDaysByMonthsArray = data.numberOfTotalDaysByMonthsVec.split(",");
    var meanOfTotalDaysByMonthsTemp = data.meanOfTotalDaysByMonthsMtx.split("ß ");
    var meanOfTotalDaysByMonthsTable = new Array();
    for (var i = 0; i < 12; i++) {
        meanOfTotalDaysByMonthsTable[i] = meanOfTotalDaysByMonthsTemp[i].split(",");
    }

    var medianOfTotalDaysTotalArray = data.medianOfTotalDaysTotalVec.split(",");
    var medianOfTotalDaysByMonthsTemp = data.medianOfTotalDaysByMonthsMtx.split("ß ");
    var medianOfTotalDaysByMonthsTable = new Array();
    for (var i = 0; i < 12; i++) {
        medianOfTotalDaysByMonthsTable[i] = medianOfTotalDaysByMonthsTemp[i].split(",");
    }

    var meanOfContangoDaysTotalArray = data.meanOfContangoDaysTotalVec.split(",");
    var numberOfContangoDaysByMonthsArray = data.numberOfContangoDaysByMonthsVec.split(",");
    var percOfContangoDaysByMonthsArray = data.percOfContangoDaysByMonthsVec.split(",");
    var meanOfContangoDaysByMonthsTemp = data.meanOfContangoDaysByMonthsMtx.split("ß ");
    var meanOfContangoDaysByMonthsTable = new Array();
    for (var i = 0; i < 12; i++) {
        meanOfContangoDaysByMonthsTable[i] = meanOfContangoDaysByMonthsTemp[i].split(",");
    }

    var medianOfContangoDaysTotalArray = data.medianOfContangoDaysTotalVec.split(",");
    var medianOfContangoDaysByMonthsTemp = data.medianOfContangoDaysByMonthsMtx.split("ß ");
    var medianOfContangoDaysByMonthsTable = new Array();
    for (var i = 0; i < 12; i++) {
        medianOfContangoDaysByMonthsTable[i] = medianOfContangoDaysByMonthsTemp[i].split(",");
    }

    var meanOfBackwardDaysTotalArray = data.meanOfBackwardDaysTotalVec.split(",");
    var numberOfBackwardDaysByMonthsArray = data.numberOfBackwardDaysByMonthsVec.split(",");
    var percOfBackwardDaysByMonthsArray = data.percOfBackwardDaysByMonthsVec.split(",");
    var meanOfBackwardDaysByMonthsTemp = data.meanOfBackwardDaysByMonthsMtx.split("ß ");
    var meanOfBackwardDaysByMonthsTable = new Array();
    for (var i = 0; i < 12; i++) {
        meanOfBackwardDaysByMonthsTable[i] = meanOfBackwardDaysByMonthsTemp[i].split(",");
    }

    var medianOfBackwardDaysTotalArray = data.medianOfBackwardDaysTotalVec.split(",");
    var medianOfBackwardDaysByMonthsTemp = data.medianOfBackwardDaysByMonthsMtx.split("ß ");
    var medianOfBackwardDaysByMonthsTable = new Array();
    for (var i = 0; i < 12; i++) {
        medianOfBackwardDaysByMonthsTable[i] = medianOfBackwardDaysByMonthsTemp[i].split(",");
    }


    //Creating the HTML code of 1+6 tables.
    var currTableMtx = "<table class=\"currData\"><tr align=\"center\"><td></td><td>Date</td><td>F1</td><td>F2</td><td>F3</td><td>F4</td><td>F5</td><td>F6</td><td>F7</td><td>F8</td><td>Short Term Contango</td><td>Long Term Contango</td><td>F2-F1 Spread</td><td>F3-F2 Spread</td><td>F4-F3 Spread</td><td>F5-F4 Spread</td><td>F6-F5 Spread</td><td>F7-F6 Spread</td><td>F8-F7 Spread</td></tr><tr align=\"center\"><td align=\"left\">Currently </td><td rowspan=\"2\">" + data.lastDataDate + "</td>";
    for (var i = 0; i < 17; i++) {
        if (currDataArray[i] == 0) {
            currTableMtx += "<td>" + "---" + "</td>";
        } else if ((i==8||i==9)) {
            currTableMtx += "<td>" + (currDataArray[i]*100).toFixed(2) + " %</td>";
        }else{
            currTableMtx += "<td>" + currDataArray[i] + "</td>";
        }
    }
    currTableMtx += "</tr><tr align=\"center\"><td align=\"right\">Cal. Days to Expiration</td>";
    for (var i = 0; i < 17; i++) {
        if (currDataArray[i] == 0) {
            currTableMtx += "<td>" + "---" + "</td>";
        }
        else {
            currTableMtx += "<td>" + currDataDaysArray[i] + "</td>";
        }
    }
    currTableMtx += "</tr><tr align=\"center\"><td align=\"left\">Previous Close</td><td>"+data.prevDataDate+"</td>";
    for (var i = 0; i < 17; i++) {
        if (currDataArray[i]==0) {
            currTableMtx += "<td>" + "---"+ "</td>";
        }else if ((i==8||i==9)) {
            currTableMtx += "<td>" + (prevDataArray[i]*100).toFixed(2) + " %</td>";
        }else{
            currTableMtx += "<td>" + prevDataArray[i] + "</td>";
        }
    }
    currTableMtx += "</tr><tr align=\"center\"><td align=\"left\" colspan=\"2\">Daily Change</td>";
    for (var i = 0; i < 17; i++) {
        if (currDataArray[i] == 0) {
            currTableMtx += "<td>" + "---" + "</td>";
        }else if ((i == 8 || i == 9)) {
            currTableMtx += "<td>" + (currDataDiffArray[i] * 100).toFixed(2) + " %</td>";
        } else {
            currTableMtx += "<td>" + currDataDiffArray[i] + "</td>";
        }
    }
    currTableMtx += "</tr></table>";

    var aMeanTotalTableMtx = "<table class=\"resData\"><tr align=\"center\"><td>Arithmetic Mean - Total</td><td>No. Days</td><td>F1</td><td>F2</td><td>F3</td><td>F4</td><td>F5</td><td>F6</td><td>F7</td><td>F8</td><td>Short Term Contango</td><td>Long Term Contango</td><td>F2-F1 Spread</td><td>F3-F2 Spread</td><td>F4-F3 Spread</td><td>F5-F4 Spread</td><td>F6-F5 Spread</td><td>F7-F6 Spread</td><td>F8-F7 Spread</td></tr><tr align=\"center\"><td align=\"left\">Total </td><td>" + data.numberOfTotalDays + "</td>";
    for (var i = 0; i < 17; i++) {
        if ((i == 8 || i == 9)) {
            aMeanTotalTableMtx += "<td>" + (meanOfTotalDaysTotalArray[i] * 100).toFixed(2) + "%</td>";
        } else {
            aMeanTotalTableMtx += "<td>" + (meanOfTotalDaysTotalArray[i]*1).toFixed(2) + "</td>";
        }
    }
    aMeanTotalTableMtx += "</tr>";
    for (var i = 0; i < 12; i++) {
        aMeanTotalTableMtx += "<tr align=\"center\"><td>" + monthList[i] + "</td><td>" + numberOfTotalDaysByMonthsArray[i] + "</td>";
        for (var j = 0; j < 17; j++) {
            if ((j == 8 || j == 9)) {
                aMeanTotalTableMtx += "<td>" + (meanOfTotalDaysByMonthsTable[i][j] * 100).toFixed(2) + "%</td>";
            } else {
                aMeanTotalTableMtx += "<td>" + (meanOfTotalDaysByMonthsTable[i][j] * 1).toFixed(2) + "</td>";
            }
        }
        aMeanTotalTableMtx += "</tr>";
    }
    aMeanTotalTableMtx += "</table>";

    var aMedianTotalTableMtx = "<table class=\"resData\"><tr align=\"center\"><td>Median - Total</td><td>No. Days</td><td>F1</td><td>F2</td><td>F3</td><td>F4</td><td>F5</td><td>F6</td><td>F7</td><td>F8</td><td>Short Term Contango</td><td>Long Term Contango</td><td>F2-F1 Spread</td><td>F3-F2 Spread</td><td>F4-F3 Spread</td><td>F5-F4 Spread</td><td>F6-F5 Spread</td><td>F7-F6 Spread</td><td>F8-F7 Spread</td></tr><tr align=\"center\"><td align=\"left\">Total </td><td>" + data.numberOfTotalDays + "</td>";
    for (var i = 0; i < 17; i++) {
        if ((i == 8 || i == 9)) {
            aMedianTotalTableMtx += "<td>" + (medianOfTotalDaysTotalArray[i] * 100).toFixed(2) + "%</td>";
        } else {
            aMedianTotalTableMtx += "<td>" + (medianOfTotalDaysTotalArray[i] * 1).toFixed(2) + "</td>";
        }
    }
    aMedianTotalTableMtx += "</tr>";
    for (var i = 0; i < 12; i++) {
        aMedianTotalTableMtx += "<tr align=\"center\"><td>" + monthList[i] + "</td><td>" + numberOfTotalDaysByMonthsArray[i] + "</td>";
        for (var j = 0; j < 17; j++) {
            if ((j == 8 || j == 9)) {
                aMedianTotalTableMtx += "<td>" + (medianOfTotalDaysByMonthsTable[i][j] * 100).toFixed(2) +"%</td>";
            } else {
                aMedianTotalTableMtx += "<td>" + (medianOfTotalDaysByMonthsTable[i][j] * 1).toFixed(2) + "</td>";
            }
        }
        aMedianTotalTableMtx += "</tr>";
    }
    aMedianTotalTableMtx += "</table>";

    var aMeanContangoTableMtx = "<table class=\"resData\"><tr align=\"center\"><td>Arithmetic Mean - Contango</td><td>No. Days</td><td>% of Days</td><td>F1</td><td>F2</td><td>F3</td><td>F4</td><td>F5</td><td>F6</td><td>F7</td><td>F8</td><td>Short Term Contango</td><td>Long Term Contango</td><td>F2-F1 Spread</td><td>F3-F2 Spread</td><td>F4-F3 Spread</td><td>F5-F4 Spread</td><td>F6-F5 Spread</td><td>F7-F6 Spread</td><td>F8-F7 Spread</td></tr><tr align=\"center\"><td align=\"left\">Total </td><td>" + data.numberOfContangoDays + "</td><td>" + (data.percOfContangoDays*100).toFixed(2) + "%</td>";
    for (var i = 0; i < 17; i++) {
        if ((i == 8 || i == 9)) {
            aMeanContangoTableMtx += "<td>" + (meanOfContangoDaysTotalArray[i] * 100).toFixed(2) + "%</td>";
        } else {
            aMeanContangoTableMtx += "<td>" + (meanOfContangoDaysTotalArray[i] * 1).toFixed(2) + "</td>";
        }
    }
    aMeanContangoTableMtx += "</tr>";
    for (var i = 0; i < 12; i++) {
        aMeanContangoTableMtx += "<tr align=\"center\"><td>" + monthList[i] + "</td><td>" + numberOfContangoDaysByMonthsArray[i] + "</td>";
        aMeanContangoTableMtx += "<td>" + (percOfContangoDaysByMonthsArray[i]*100).toFixed(2) + "%</td>";
        for (var j = 0; j < 17; j++) {
            if ((j == 8 || j == 9)) {
                aMeanContangoTableMtx += "<td>" + (meanOfContangoDaysByMonthsTable[i][j] * 100).toFixed(2) + "%</td>";
            } else {
                aMeanContangoTableMtx += "<td>" + (meanOfContangoDaysByMonthsTable[i][j] * 1).toFixed(2) + "</td>";
            }
        }
        aMeanContangoTableMtx += "</tr>";
    }
    aMeanContangoTableMtx += "</table>";

    var aMedianContangoTableMtx = "<table class=\"resData\"><tr align=\"center\"><td>Median - Contango</td><td>No. Days</td><td>% of Days</td><td>F1</td><td>F2</td><td>F3</td><td>F4</td><td>F5</td><td>F6</td><td>F7</td><td>F8</td><td>Short Term Contango</td><td>Long Term Contango</td><td>F2-F1 Spread</td><td>F3-F2 Spread</td><td>F4-F3 Spread</td><td>F5-F4 Spread</td><td>F6-F5 Spread</td><td>F7-F6 Spread</td><td>F8-F7 Spread</td></tr><tr align=\"center\"><td align=\"left\">Total </td><td>" + data.numberOfContangoDays + "</td><td>" + (data.percOfContangoDays * 100).toFixed(2) + "%</td>";
    for (var i = 0; i < 17; i++) {
        if ((i == 8 || i == 9)) {
            aMedianContangoTableMtx += "<td>" + (medianOfContangoDaysTotalArray[i] * 100).toFixed(2) + "%</td>";
        } else {
            aMedianContangoTableMtx += "<td>" + (medianOfContangoDaysTotalArray[i] * 1).toFixed(2) + "</td>";
        }
    }
    aMedianContangoTableMtx += "</tr>";
    for (var i = 0; i < 12; i++) {
        aMedianContangoTableMtx += "<tr align=\"center\"><td>" + monthList[i] + "</td><td>" + numberOfContangoDaysByMonthsArray[i] + "</td>";
        aMedianContangoTableMtx += "<td>" + (percOfContangoDaysByMonthsArray[i] * 100).toFixed(2) + "%</td>";
        for (var j = 0; j < 17; j++) {
            if ((j == 8 || j == 9)) {
                aMedianContangoTableMtx += "<td>" + (medianOfContangoDaysByMonthsTable[i][j] * 100).toFixed(2) + "%</td>";
            } else {
                aMedianContangoTableMtx += "<td>" + (medianOfContangoDaysByMonthsTable[i][j] * 1).toFixed(2) + "</td>";
            }
        }
        aMedianContangoTableMtx += "</tr>";
    }
    aMedianContangoTableMtx += "</table>";


    var aMeanBackwardTableMtx = "<table class=\"resData\"><tr align=\"center\"><td>Arithmetic Mean - Backward</td><td>No. Days</td><td>% of Days</td><td>F1</td><td>F2</td><td>F3</td><td>F4</td><td>F5</td><td>F6</td><td>F7</td><td>F8</td><td>Short Term Contango</td><td>Long Term Contango</td><td>F2-F1 Spread</td><td>F3-F2 Spread</td><td>F4-F3 Spread</td><td>F5-F4 Spread</td><td>F6-F5 Spread</td><td>F7-F6 Spread</td><td>F8-F7 Spread</td></tr><tr align=\"center\"><td align=\"left\">Total </td><td>" + data.numberOfBackwardDays + "</td><td>" + (data.percOfBackwardDays * 100).toFixed(2) + "%</td>";
    for (var i = 0; i < 17; i++) {
        if (data.numberOfBackwardDays == 0) {
            aMeanBackwardTableMtx += "<td> --- </td>";
        }
        else if ((i == 8 || i == 9)) {
            aMeanBackwardTableMtx += "<td>" + (meanOfBackwardDaysTotalArray[i] * 100).toFixed(2) + "%</td>";
        } else {
            aMeanBackwardTableMtx += "<td>" + (meanOfBackwardDaysTotalArray[i] * 1).toFixed(2) + "</td>";
        }
    }
    aMeanBackwardTableMtx += "</tr>";
    for (var i = 0; i < 12; i++) {
        aMeanBackwardTableMtx += "<tr align=\"center\"><td>" + monthList[i] + "</td><td>" + numberOfBackwardDaysByMonthsArray[i] + "</td>";
        aMeanBackwardTableMtx += "<td>" + (percOfBackwardDaysByMonthsArray[i]*100).toFixed(2) + "%</td>";
        for (var j = 0; j < 17; j++) {
            if (numberOfBackwardDaysByMonthsArray[i]==0) {
                aMeanBackwardTableMtx += "<td> --- </td>";
            }
            else if ((j == 8 || j == 9)) {
                aMeanBackwardTableMtx += "<td>" + (meanOfBackwardDaysByMonthsTable[i][j] * 100).toFixed(2) + "%</td>";
            } else {
                aMeanBackwardTableMtx += "<td>" + (meanOfBackwardDaysByMonthsTable[i][j] * 1).toFixed(2) + "</td>";
            }
        }
        aMeanBackwardTableMtx += "</tr>";
    }
    aMeanBackwardTableMtx += "</table>";

    var aMedianBackwardTableMtx = "<table class=\"resData\"><tr align=\"center\"><td>Median - Backward</td><td>No. Days</td><td>% of Days</td><td>F1</td><td>F2</td><td>F3</td><td>F4</td><td>F5</td><td>F6</td><td>F7</td><td>F8</td><td>Short Term Contango</td><td>Long Term Contango</td><td>F2-F1 Spread</td><td>F3-F2 Spread</td><td>F4-F3 Spread</td><td>F5-F4 Spread</td><td>F6-F5 Spread</td><td>F7-F6 Spread</td><td>F8-F7 Spread</td></tr><tr align=\"center\"><td align=\"left\">Total </td><td>" + data.numberOfBackwardDays + "</td><td>" + (data.percOfBackwardDays * 100).toFixed(2) + "%</td>";
    for (var i = 0; i < 17; i++) {
        if (data.numberOfBackwardDays== 0) {
            aMedianBackwardTableMtx += "<td> --- </td>";
        }
        else if ((i == 8 || i == 9)) {
            aMedianBackwardTableMtx += "<td>" + (medianOfBackwardDaysTotalArray[i] * 100).toFixed(2) + "%</td>";
        } else {
            aMedianBackwardTableMtx += "<td>" + (medianOfBackwardDaysTotalArray[i] * 1).toFixed(2) + "</td>";
        }
    }
    aMedianBackwardTableMtx += "</tr>";
    for (var i = 0; i < 12; i++) {
        aMedianBackwardTableMtx += "<tr align=\"center\"><td>" + monthList[i] + "</td><td>" + numberOfBackwardDaysByMonthsArray[i] + "</td>";
        aMedianBackwardTableMtx += "<td>" + (percOfBackwardDaysByMonthsArray[i] * 100).toFixed(2) + "%</td>";
        for (var j = 0; j < 17; j++) {
            if (numberOfBackwardDaysByMonthsArray[i] == 0) {
                aMedianBackwardTableMtx += "<td> --- </td>";
            }
            else if ((j == 8 || j == 9)) {
                aMedianBackwardTableMtx += "<td>" + (medianOfBackwardDaysByMonthsTable[i][j] * 100).toFixed(2) + "%</td>";
            } else {
                aMedianBackwardTableMtx += "<td>" + (medianOfBackwardDaysByMonthsTable[i][j] * 1).toFixed(2) + "</td>";
            }
        }
        aMedianBackwardTableMtx += "</tr>";
    }
    aMedianBackwardTableMtx += "</table>";

    //"Sending" data to HTML file.
    var currTableMtx2 = document.getElementById("idCurrTableMtx");
    currTableMtx2.innerHTML = currTableMtx;
    var aMeanTotalTableMtx2 = document.getElementById("idAMeanTotalTableMtx");
    aMeanTotalTableMtx2.innerHTML = aMeanTotalTableMtx;
    var aMeanTotalTableMtx21 = document.getElementById("idMedianTotalTableMtx");
    aMeanTotalTableMtx21.innerHTML = aMedianTotalTableMtx;
    var aMeanTotalTableMtx22 = document.getElementById("idAMeanContangoTableMtx");
    aMeanTotalTableMtx22.innerHTML = aMeanContangoTableMtx;
    var aMeanTotalTableMtx23 = document.getElementById("idMedianContangoTableMtx");
    aMeanTotalTableMtx23.innerHTML = aMedianContangoTableMtx;
    var aMeanTotalTableMtx24 = document.getElementById("idAMeanBackwardTableMtx");
    aMeanTotalTableMtx24.innerHTML = aMeanBackwardTableMtx;
    var aMeanTotalTableMtx25 = document.getElementById("idMedianBackwardTableMtx");
    aMeanTotalTableMtx25.innerHTML = aMedianBackwardTableMtx;

    //Splitting data to charts. 
    var resultsToChartFutPricesTemp = data.resultsToChartFutPricesMtx.split("ß ");
    var resultsToChartFutPricesTable = new Array();
    for (var i = 0; i < resultsToChartFutPricesTemp.length; i++) {
        resultsToChartFutPricesTable[i] = resultsToChartFutPricesTemp[i].split(",");
    }

    var resultsToChartFutSpreadsTemp = data.resultsToChartFutSpreadsMtx.split("ß ");
    var resultsToChartFutSpreadsTable = new Array();
    for (var i = 0; i < resultsToChartFutSpreadsTemp.length; i++) {
        resultsToChartFutSpreadsTable[i] = resultsToChartFutSpreadsTemp[i].split(",");
    }

    var aMeanTotalPrices = new Array(resultsToChartFutPricesTable.length);
    for (var i = 0; i < resultsToChartFutPricesTable.length; i++) {
        var aMeanTotalPricesRows = new Array(2);
        aMeanTotalPricesRows[0] = resultsToChartFutPricesTable[i][0];
        aMeanTotalPricesRows[1] = resultsToChartFutPricesTable[i][1];
        aMeanTotalPrices[i] = aMeanTotalPricesRows;
    }

    var medianTotalPrices = new Array(resultsToChartFutPricesTable.length);
    for (var i = 0; i < resultsToChartFutPricesTable.length; i++) {
        var medianTotalPricesRows = new Array(2);
        medianTotalPricesRows[0] = resultsToChartFutPricesTable[i][0];
        medianTotalPricesRows[1] = resultsToChartFutPricesTable[i][2];
        medianTotalPrices[i] = medianTotalPricesRows;
    }

    var aMeanContPrices = new Array(resultsToChartFutPricesTable.length);
    for (var i = 0; i < resultsToChartFutPricesTable.length; i++) {
        var aMeanContPricesRows = new Array(2);
        aMeanContPricesRows[0] = resultsToChartFutPricesTable[i][0];
        aMeanContPricesRows[1] = resultsToChartFutPricesTable[i][4];
        aMeanContPrices[i] = aMeanContPricesRows;
    }

    var medianContPrices = new Array(resultsToChartFutPricesTable.length);
    for (var i = 0; i < resultsToChartFutPricesTable.length; i++) {
        var medianContPricesRows = new Array(2);
        medianContPricesRows[0] = resultsToChartFutPricesTable[i][0];
        medianContPricesRows[1] = resultsToChartFutPricesTable[i][5];
        medianContPrices[i] = medianContPricesRows;
    }

    var aMeanBackwardPrices = new Array(resultsToChartFutPricesTable.length);
    for (var i = 0; i < resultsToChartFutPricesTable.length; i++) {
        var aMeanBackwardPricesRows = new Array(2);
        aMeanBackwardPricesRows[0] = resultsToChartFutPricesTable[i][0];
        aMeanBackwardPricesRows[1] = resultsToChartFutPricesTable[i][7];
        aMeanBackwardPrices[i] = aMeanBackwardPricesRows;
    }

    var medianBackwardPrices = new Array(resultsToChartFutPricesTable.length);
    for (var i = 0; i < resultsToChartFutPricesTable.length; i++) {
        var medianBackwardPricesRows = new Array(2);
        medianBackwardPricesRows[0] = resultsToChartFutPricesTable[i][0];
        medianBackwardPricesRows[1] = resultsToChartFutPricesTable[i][8];
        medianBackwardPrices[i] = medianBackwardPricesRows;
    }


    var aMeanTotalSpreads = new Array(resultsToChartFutSpreadsTable.length);
    for (var i = 0; i < resultsToChartFutSpreadsTable.length; i++) {
        var aMeanTotalSpreadsRows = new Array(2);
        aMeanTotalSpreadsRows[0] = resultsToChartFutSpreadsTable[i][0];
        aMeanTotalSpreadsRows[1] = resultsToChartFutSpreadsTable[i][1];
        aMeanTotalSpreads[i] = aMeanTotalSpreadsRows;
    }

    var medianTotalSpreads = new Array(resultsToChartFutSpreadsTable.length);
    for (var i = 0; i < resultsToChartFutSpreadsTable.length; i++) {
        var medianTotalSpreadsRows = new Array(2);
        medianTotalSpreadsRows[0] = resultsToChartFutSpreadsTable[i][0];
        medianTotalSpreadsRows[1] = resultsToChartFutSpreadsTable[i][2];
        medianTotalSpreads[i] = medianTotalSpreadsRows;
    }

    var aMeanContSpreads = new Array(resultsToChartFutSpreadsTable.length);
    for (var i = 0; i < resultsToChartFutSpreadsTable.length; i++) {
        var aMeanContSpreadsRows = new Array(2);
        aMeanContSpreadsRows[0] = resultsToChartFutSpreadsTable[i][0];
        aMeanContSpreadsRows[1] = resultsToChartFutSpreadsTable[i][4];
        aMeanContSpreads[i] = aMeanContSpreadsRows;
    }

    var medianContSpreads = new Array(resultsToChartFutSpreadsTable.length);
    for (var i = 0; i < resultsToChartFutSpreadsTable.length; i++) {
        var medianContSpreadsRows = new Array(2);
        medianContSpreadsRows[0] = resultsToChartFutSpreadsTable[i][0];
        medianContSpreadsRows[1] = resultsToChartFutSpreadsTable[i][5];
        medianContSpreads[i] = medianContSpreadsRows;
    }

    var aMeanBackwardSpreads = new Array(resultsToChartFutSpreadsTable.length);
    for (var i = 0; i < resultsToChartFutSpreadsTable.length; i++) {
        var aMeanBackwardSpreadsRows = new Array(2);
        aMeanBackwardSpreadsRows[0] = resultsToChartFutSpreadsTable[i][0];
        aMeanBackwardSpreadsRows[1] = resultsToChartFutSpreadsTable[i][7];
        aMeanBackwardSpreads[i] = aMeanBackwardSpreadsRows;
    }

    var medianBackwardSpreads = new Array(resultsToChartFutSpreadsTable.length);
    for (var i = 0; i < resultsToChartFutSpreadsTable.length; i++) {
        var medianBackwardSpreadsRows = new Array(2);
        medianBackwardSpreadsRows[0] = resultsToChartFutSpreadsTable[i][0];
        medianBackwardSpreadsRows[1] = resultsToChartFutSpreadsTable[i][8];
        medianBackwardSpreads[i] = medianBackwardSpreadsRows;
    }

    //--var nCurrData = (currDataArray[7] > 0) ? 8 : 7;
    var nCurrData = 7;
    var currDataPrices = new Array(nCurrData);
    for (var i = 0; i < nCurrData; i++) {
        var currDataPricesRows = new Array(2);
        currDataPricesRows[0] = currDataDaysArray[i];
        currDataPricesRows[1] = currDataArray[i];
        currDataPrices[i] = currDataPricesRows;
    }

    var currDataSpreads = new Array(nCurrData-1);
    for (var i = 0; i < nCurrData-1; i++) {
        var currDataSpreadsRows = new Array(2);
        currDataSpreadsRows[0] = currDataDaysArray[i];
        currDataSpreadsRows[1] = currDataArray[i+10];
        currDataSpreads[i] = currDataSpreadsRows;
    }

    //Declaring data sets to charts.
    var datasets1 = {
           "current": {
            label: "Current",
            data: currDataPrices,
            points: { show: true},
            lines: { show: true }
           },
           "aMeanTotal": {
            label: "AMean Total",
            data: aMeanTotalPrices
        },
        "medianTotal": {
            label: "Median Total",
            data: medianTotalPrices
        },
        "aMeanCont": {
            label: "AMean Contango",
            data: aMeanContPrices
        },
        "medianCont": {
            label: "Median Contango",
            data: medianContPrices
        },
        "aMeanBackward": {
            label: "AMean Backwardation",
            data: aMeanBackwardPrices
        },
        "medianBackward": {
            label: "Median Backwardation",
            data: medianBackwardPrices
        }
    };

    var datasets2 = {
        "current": {
            label: "Current",
            data: currDataSpreads,
            points: { show: true },
            lines: {show: true}
        },
        "aMeanTotal": {
            label: "AMean Total",
            data: aMeanTotalSpreads
        },
        "medianTotal": {
            label: "Median Total",
            data: medianTotalSpreads
        },
        "aMeanCont": {
            label: "AMean Contango",
            data: aMeanContSpreads
        },
        "medianCont": {
            label: "Median Contango",
            data: medianContSpreads
        },
        "aMeanBackward": {
            label: "AMean Backwardation",
            data: aMeanBackwardSpreads
        },
        "medianBackward": {
            label: "Median Backwardation",
            data: medianBackwardSpreads
        }
    };

    flotPlotMyData1(datasets1);
    flotPlotMyData2(datasets2);
    

    
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
                    tickDecimals: 0
                },
                legend: {
                    position: "ne"
                }
            });
        }
    }

    plotAccordingToChoices();

}

// Creating second chart: spreads by days to expiration.
function flotPlotMyData2(datasets2) {
    // hard-code color indices to prevent them from shifting as
    // countries are turned on/off

    var i = 0;
    $.each(datasets2, function (key, val) {
        val.color = i;
        ++i;
    });

    // insert checkboxes
    var choiceContainer = $("#choices2");
    $.each(datasets2, function (key, val) {
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
            if (key && datasets2[key]) {
                data.push(datasets2[key]);
            }
        });

       
        if (data.length > 0) {
            $.plot("#placeholder2", data,{
            yaxis: {
                
            },
            xaxis: {
                    tickDecimals: 0
            },
            legend: {
                position: "ne"
            }
        });
        }
    }

    plotAccordingToChoices();

}