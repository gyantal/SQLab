"use strict";
//As an example, in normal JavaScript, mistyping a variable name creates a new global variable. In strict mode, this will throw an error, making it impossible to accidentally create a global variable.

function onHeadProcessing(commo) {
    console.log('onHeadProcessing()');

    var xmlHttp = new XMLHttpRequest();
    xmlHttp.onreadystatechange = function () {
        if (xmlHttp.readyState == 4 && xmlHttp.status == 200)
            processData(xmlHttp.responseText);
    };
    xmlHttp.open("GET", "/UberTAAGChData?commo=" + commo, true); // true for asynchronous 
    xmlHttp.send(null);
}

function processData(dataStr) {
    //Printing error message from C#.  

    if (dataStr == "Error")
    {
        var divErrorCont = document.getElementById("idErrorCont");
        divErrorCont.innerHTML = "Error during downloading data. Please, try again later!"
        document.getElementById("errorMessage").style.visibility="visible";
        document.getElementById("inviCharts").style.visibility = "hidden";
        
        return;
    }

    document.getElementById("errorMessage").style.display = "none";

    //Splitting data got from C#.
    var data = JSON.parse(dataStr);

    //Creating first rows of webpage.
    var divTitleCont = document.getElementById("idTitleCont");
    var divWarningCont = document.getElementById("idWarningCont");
    var divTimeNow = document.getElementById("idTimeNow");
    var divLiveDataTime = document.getElementById("idLiveDataTime");
    var divCurrentPV = document.getElementById("idCurrentPV");
    var divCLMTString = document.getElementById("idCLMTString");
    var divPosLastString = document.getElementById("idPosLast");
    var divPosFutString = document.getElementById("idPosFut");
    
    
    divTitleCont.innerHTML = data.titleCont + ' <sup><small><a href="' + data.gDocRef + '" target="_blank">(Study)</a></small></sup>';
    divWarningCont.innerHTML = data.warningCont;
    divTimeNow.innerHTML = data.requestTime;
    divLiveDataTime.innerHTML = data.lastDataTime;
    divCurrentPV.innerHTML = "Current PV: <span class=\"pv\">$ " + data.currentPV + "</span> (based on <a href=" + data.gSheetRef + '" target="_blank">these current positions</a> updated for ' + data.currentPVDate + ")";
    divCLMTString.innerHTML = "Current Combined Leverage Market Timer signal is <span class=\"clmt\">" + data.clmtSign + "</span> (SPX 50/200-day MA: " + data.spxMASign + ", XLU/VTI: " + data.xluVtiSign + ").";
    divPosLastString.innerHTML = "Position weights in the last 20 days:";
    divPosFutString.innerHTML = "Future events:";
    //$('#my-link1').html('<a href="' + data.gDocRef + '" target="_blank">Study</a>'); - currently not used
    //$('#my-link2').html('<a href="' + data.gSheetRef + '" target="_blank">Live spreadsheet</a>'); - currently not used
    var warnLength = data.warningCont.length;
    if (warnLength>0){
        divWarningCont.innerHTML = data.warningCont + '<br> <a href="https://docs.google.com/spreadsheets/d/1fmvGBi2Q6MxnB_8AjUedy1QVTOlWE7Ck1rICjYSSxyY" target="_blank">Google sheet with current positions</a> and <a href="https://docs.google.com/document/d/1_m3MMGag7uBZSdvc4IgXKMvj3d4kzLxwvnW14RkCyco" target="_blank">the latest study in connection with the strategy</a>';
    }
    
    creatingTables(data);

    //Setting charts visible after getting data.
    document.getElementById("inviCharts").style.visibility = "visible";
}
function creatingTables(data) {

    //Creating JavaScript data arrays by splitting.
    var assetNames2Array = data.assetNames2.split(", ");
    var currPosNumArray = data.currPosNum.split(", ");
    var currPosValArray = data.currPosVal.split(", ");
    var nextPosNumArray = data.nextPosNum.split(", ");
    var nextPosValArray = data.nextPosVal.split(", ");
    var diffPosNumArray = data.posNumDiff.split(", ");
    var diffPosValArray = data.posValDiff.split(", ");

    var prevPosMtxTemp = data.prevPositionsMtx.split("ß ");
    var prevPosMtx = new Array();
    for (var i = 0; i < prevPosMtxTemp.length; i++) {
        prevPosMtx[i] = prevPosMtxTemp[i].split(",");
    }

    var prevAssetEventMtxTemp = data.prevAssEventMtx.split("ß ");
    var prevAssetEventMtx = new Array();
    for (var i = 0; i < prevAssetEventMtxTemp.length; i++) {
        prevAssetEventMtx[i] = prevAssetEventMtxTemp[i].split(",");
    }

    var futPosMtxTemp = data.futPositionsMtx.split("ß ");
    var futPosMtx = new Array();
    for (var i = 0; i < futPosMtxTemp.length; i++) {
        futPosMtx[i] = futPosMtxTemp[i].split(",");
    }

    var futAssetEventMtxTemp = data.futAssEventMtx.split("ß ");
    var futAssetEventMtx = new Array();
    for (var i = 0; i < futAssetEventMtxTemp.length; i++) {
        futAssetEventMtx[i] = futAssetEventMtxTemp[i].split(",");
    }

    var assChartMtxTemp = data.assetChangesToChartMtx.split("ß ");
    var assChartMtx = new Array();
    for (var i = 0; i < assChartMtxTemp.length; i++) {
        assChartMtx[i] = assChartMtxTemp[i].split(",");
    }

    var rsiChartMtxTemp = data.xluVtiPercToChartMtx.split("ß ");
    var rsiChartMtx = new Array();
    for (var i = 0; i < rsiChartMtxTemp.length; i++) {
        rsiChartMtx[i] = rsiChartMtxTemp[i].split(",");
    }

    var spxChartMtxTemp = data.spxMAToChartMtx.split("ß ");
    var spxChartMtx = new Array();
    for (var i = 0; i < spxChartMtxTemp.length; i++) {
        spxChartMtx[i] = spxChartMtxTemp[i].split(",");
    }


    //Creating the HTML code of tables.
    var currTableMtx = "<table class=\"currData\"><tr align=\"center\"><td bgcolor=\"#66CCFF\"></td>";
    for (var i = 0; i < assetNames2Array.length - 1; i++) {
        currTableMtx += "<td bgcolor=\"#66CCFF\"><a href=https://finance.yahoo.com/quote/" + assetNames2Array[i] + ' target="_blank">' + assetNames2Array[i] + "</a></td>";
    }
    currTableMtx += "<td bgcolor=\"#66CCFF\">" + assetNames2Array[assetNames2Array.length - 1] + "</td>";

    currTableMtx += "</tr > <tr align=\"center\"><td align=\"center\" rowspan=\"2\" bgcolor=\"#FF6633\">" + data.nextTradingDay + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx += "<td bgcolor=\"#" + prevAssetEventMtx[1][i + 1] + "\">" + nextPosValArray[i] + "</td>";
    }

    currTableMtx += "</tr > <tr>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx += "<td bgcolor=\"#" + prevAssetEventMtx[1][i + 1] + "\">" + nextPosNumArray[i] + "</td>";
    }

    currTableMtx += "</tr > <tr align=\"center\"><td align=\"center\" rowspan=\"2\" bgcolor=\"#FF6633\">" + data.currPosDate + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx += "<td bgcolor=\"#" + prevAssetEventMtx[2][i + 1] + "\">" + currPosValArray[i] + "</td>";
    }

    currTableMtx += "</tr > <tr>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx += "<td bgcolor=\"#" + prevAssetEventMtx[2][i + 1] + "\">" + currPosNumArray[i] + "</td>";
    }

    currTableMtx += "</tr > <tr align=\"center\"><td align=\"center\" rowspan=\"2\" bgcolor=\"#FF6633\">Change in Positions</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx += "<td bgcolor=\"#FFFF00\">" + diffPosValArray[i] + "</td>";
    }

    currTableMtx += "</tr > <tr>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx += "<td bgcolor=\"#FFFF00\">" + diffPosNumArray[i] + "</td>";
    }

    currTableMtx += "</tr></table>";

    var currTableMtx3 = "<table class=\"currData\"><tr align=\"center\">";
    for (var i = 0; i < prevPosMtxTemp.length; i++) {
        for (var j = 0; j < assetNames2Array.length + 2; j++) {
            currTableMtx3 += "<td bgcolor=\"#" + prevAssetEventMtx[i][j] + "\">" + prevPosMtx[i][j] + "</td>";
        }
        currTableMtx3 += "</tr>";
    }
    currTableMtx3 += "</table>";

    var currTableMtx5 = "<table class=\"currData\"><tr align=\"center\">";
    for (var i = 0; i < futPosMtxTemp.length; i++) {
        for (var j = 0; j < assetNames2Array.length; j++) {
            currTableMtx5 += "<td bgcolor=\"#" + futAssetEventMtx[i][j] + "\">" + futPosMtx[i][j] + "</td>";
        }
        currTableMtx5 += "</tr>";
    }
    currTableMtx5 += "</table>";

    var currTableMtx7 = "<table class=\"currData2\"><tr><td bgcolor=\"#1E90FF\">Earnings Day</td><td bgcolor=\"#228B22\">FOMC Bullish Day</td><td bgcolor=\"#FF0000\">FOMC Bearish Day</td></tr><tr><td bgcolor=\"#7B68EE\">Pre-Earnings Day</td><td bgcolor=\"#7CFC00\">Holiday Bullish Day</td><td bgcolor=\"#DC143C\">Holiday Bearish Day</td></tr><tr><td bgcolor=\"#00FFFF\">CLMT Bullish Day</td><td bgcolor=\"#A9A9A9\">CLMT Neutral Day</td><td bgcolor=\"#FF8C00\">CLMT Bearish Day</td></tr></table > ";

    //"Sending" data to HTML file.
    var currTableMtx2 = document.getElementById("idCurrTableMtx");
    currTableMtx2.innerHTML = currTableMtx;
    var currTableMtx4 = document.getElementById("idCurrTableMtx3");
    currTableMtx4.innerHTML = currTableMtx3;
    var currTableMtx6 = document.getElementById("idCurrTableMtx5");
    currTableMtx6.innerHTML = currTableMtx5;
    var currTableMtx8 = document.getElementById("idCurrTableMtx7");
    currTableMtx8.innerHTML = currTableMtx7;

    
    //Declaring data sets to charts.

    var nCurrData = parseInt(data.chartLength) + 1;
    
    var xTicksH = new Array(nCurrData);
    for (var i = 0; i < nCurrData; i++) {
        var xTicksHRows = new Array(2);
        xTicksHRows[0] = i;
        xTicksHRows[1] = assChartMtx[i][0];
        xTicksH[i] = xTicksHRows;
    }

    var noAssets = assetNames2Array.length - 2;
    var listH = [];
    for (var j = 0; j < noAssets; j++) {
        var assChartPerc1 = new Array(nCurrData);
        for (var i = 0; i < nCurrData; i++) {
            var assChartPerc1Rows = new Array(2);
            assChartPerc1Rows[0] = i;
            assChartPerc1Rows[1] = parseFloat(assChartMtx[i][j+1]);
            assChartPerc1[i] = assChartPerc1Rows;
        }
        listH.push({ label: assetNames2Array[j], data: assChartPerc1, points: { show: true }, lines: { show: true } });
    }
        
    
    var rsiXlu = new Array(nCurrData);
    var rsiVti = new Array(nCurrData);
    for (var i = 0; i < nCurrData; i++) {
        var rsiXluRows = new Array(2);
        var rsiVtiRows = new Array(2);
        rsiXluRows[0] = i;
        rsiXluRows[1] = parseFloat(rsiChartMtx[i][1]);
        rsiXlu[i] = rsiXluRows;
        rsiVtiRows[0] = i;
        rsiVtiRows[1] = parseFloat(rsiChartMtx[i][2]);
        rsiVti[i] = rsiVtiRows;

    }

    var spxSpot = new Array(nCurrData);
    var spx50MA = new Array(nCurrData);
    var spx200MA = new Array(nCurrData);
    for (var i = 0; i < nCurrData; i++) {
        var spxSpotRows = new Array(2);
        var spx50MARows = new Array(2);
        var spx200MARows = new Array(2);
        spxSpotRows[0] = i;
        spxSpotRows[1] = parseFloat(spxChartMtx[i][1]);
        spxSpot[i] = spxSpotRows;
        spx50MARows[0] = i;
        spx50MARows[1] = parseFloat(spxChartMtx[i][2]);
        spx50MA[i] = spx50MARows;
        spx200MARows[0] = i;
        spx200MARows[1] = parseFloat(spxChartMtx[i][3]);
        spx200MA[i] = spx200MARows;
    }


    var datasets1 = listH;

    var datasets2 = {
        "spotSPX": {
            label: "SPX Spot",
            data: spxSpot,
            points: { show: true },
            lines: { show: true }
        },
        "ma50SPX": {
            label: "SPX 50-Day MA",
            data: spx50MA,
            points: { show: true },
            lines: { show: true }
        },
        "ma200SPX": {
            label: "SPX 200-Day MA",
            data: spx200MA,
            points: { show: true },
            lines: { show: true }
        }
    };

    var datasets3 = {
        "XLUdata": {
            label: "XLU",
            data: rsiXlu,
            points: { show: true },
            lines: { show: true }
        },
        "VTIdata": {
            label: "VTI",
            data: rsiVti,
            points: { show: true },
            lines: { show: true }
        }
        };



        flotPlotMyData1(datasets1, nCurrData, xTicksH, noAssets);
        flotPlotMyData2(datasets2, nCurrData, xTicksH);
        flotPlotMyData3(datasets3, nCurrData, xTicksH);

        

    }
    // Creating charts.
    function flotPlotMyData1(datasets1, nCurrData, xTicksH, noAssets) {
        
        var dataB = [];
        $.each(datasets1, function (key) {
            dataB.push(datasets1[key]);
        });

        $.plot("#placeholder1", dataB,
            {
                yaxis: {
                    axisLabel: "Percentage Change",
                    tickFormatter: function (v, axis) {
                        return v.toFixed(0)+"%";
                    }

                },
                xaxis: {
                    //tickDecimals: 0,
                    min: 0,
                    //max: nCurrData-1,
                    ticks: xTicksH,
                    axisLabel: "Day"
                },
                legend: {
                    position: "nw",
                    // noColumns: noAssets,
                    noColumns: 11,
                    backgroundColor: "#F4F6F6"
                },
                grid: {
                    backgroundColor: "#F4F6F6",
                    hoverable: true
                },
                tooltip: {
                    show: true,
                    content: function (label, x, y) {
                        var xVals = [];
                        for (var i = 0; i < nCurrData; i++) {
                            xVals[i] = dataB[0].data[i][0];
                        };
                        var indi = xVals.indexOf(x);
                       
                        var text = "<b>" + label + "<br/></b><i>" + indi + "-Day Percentage Changes on " + xTicksH[indi][1] + "<br/></i>";
                        dataB.forEach(function (series) {
                            text += series.label + ' : ' + series.data[indi][1] + "%<br/>";
                        });

                        return text;
                    }
                }

            });

    }
    
    function flotPlotMyData2(datasets2, nCurrData, xTicksH) {

        var dataB = [];
        $.each(datasets2, function (key) {

            dataB.push(datasets2[key]);

        });
        $.plot("#placeholder2", dataB,
            {
                yaxis: {
                    axisLabel: "Index Value",
                    tickFormatter: function (v, axis) {
                        return v.toFixed(0);
                    }

                },
                xaxis: {
                    //tickDecimals: 0,
                    min: 0,
                    //max: nCurrData-1,
                    ticks: xTicksH,
                    axisLabel: "Day"
                },
                legend: {
                    position: "nw",
                    noColumns: 3,
                    backgroundColor: "#F4F6F6"
                },
                colors: ["#0022FF", "#148F77", "#CB4335"],
                grid: {
                    backgroundColor: "#F4F6F6",
                    hoverable: true
                },
                tooltip: {
                    show: true,
                    content: function (label, x, y) {
                        var xVals = [];
                        for (var i = 0; i < nCurrData; i++) {
                            xVals[i] = dataB[0].data[i][0];
                        };
                        var indi = xVals.indexOf(x);
                        var text = "<b>" + label + "<br/></b><i>Index Values on " + xTicksH[indi][1] + "<br/></i>";
                        dataB.forEach(function (series) {
                            text += series.label + ' : ' + series.data[indi][1] + "<br/>";
                        });

                        return text;
                    }
                }

            });

    }
  
    function flotPlotMyData3(datasets3, nCurrData, xTicksH) {

        var dataB = [];
        $.each(datasets3,function (key) {
            
                dataB.push(datasets3[key]);
            
        });
        $.plot("#placeholder3", dataB,
            {
                yaxis: {
                    axisLabel: "RSI",
                    tickFormatter: function (v, axis) {
                        return v.toFixed(0);
                    }

                },
                xaxis: {
                    //tickDecimals: 0,
                    min: 0,
                    //max: nCurrData-1,
                    ticks: xTicksH,
                    axisLabel: "Day"
                },
                legend: {
                    position: "nw",
                    noColumns: 2,
                    backgroundColor: "#F4F6F6"
                },
                colors: ["#0022FF", "#148F77", "#CB4335"],
                grid: {
                    backgroundColor: "#F4F6F6",
                    hoverable: true
                },
                tooltip: {
                    show: true,
                    content: function (label, x, y) {
                        var xVals = [];
                        for (var i = 0; i < nCurrData; i++) {
                            xVals[i] = dataB[0].data[i][0];
                        };
                        var indi = xVals.indexOf(x);
                        var text = "<b>" + label + "<br/></b><i>Relative Strength Indexes on " + xTicksH[indi][1] + "<br/></i>";
                        dataB.forEach(function (series) {
                        text += series.label + ' : ' + series.data[indi][1] + "<br/>";
                        });

                        return text;
                    }
                }

            });
   
    }