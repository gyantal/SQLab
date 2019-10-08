"use strict";
//As an example, in normal JavaScript, mistyping a variable name creates a new global variable. In strict mode, this will throw an error, making it impossible to accidentally create a global variable.
function getQueryVariable(variable) {
    var query = window.location.search.substring(1);
    var vars = query.split("&");
    for (var i = 0; i < vars.length; i++) {
        var pair = vars[i].split("=");
        if (pair[0] == variable) { return pair[1]; }
    }
    return ("JUVE");
}

function onHeadProcessing() {
    console.log('onHeadProcessing()');

    var xmlHttp = new XMLHttpRequest();
    xmlHttp.onreadystatechange = function () {
        if (xmlHttp.readyState == 4 && xmlHttp.status == 200)
            processData(xmlHttp.responseText);
    };
    var commo = getQueryVariable("user");
    xmlHttp.open("GET", "/SINAddictionData?commo="+commo, true); // true for asynchronous 
    xmlHttp.send(null);
}

function perc2color(perc, min, max) {
    var base = (max - min);

    if (base == 0 || perc > max) { perc = 100; }
    else if (perc < min) { perc = 0; }
    else {
        perc = (perc - min) / base * 100;
         }
    var r, g, b = 0;
        if (perc < 50) {
            r = 255;
            b = 110;
            g = Math.round(127+2.55 * perc);
        }
        else {
            g = 255;
            r = Math.round(382 - 2.55 * perc);
            b = 110;
        }
    var h = r * 0x10000 + g * 0x100 + b * 0x1;
    return '#' + ('000000' + h.toString(16)).slice(-6);
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
    var divTimeNow = document.getElementById("idTimeNow");
    var divLiveDataTime = document.getElementById("idLiveDataTime");
    var divCurrentPV = document.getElementById("idCurrentPV");
    var divDailyProfit = document.getElementById("idDailyProfit");
    var divMonthlyProfit = document.getElementById("idMonthlyProfit");
    var divYearlyProfit = document.getElementById("idYearlyProfit");
    var divBondPerc = document.getElementById("idBondPerc");
   

    divTitleCont.innerHTML = data.titleCont + ' <sup><small><a href="' + data.gDocRef + '" target="_blank">(Study)</a></small></sup>';
    divTimeNow.innerHTML = data.requestTime;
    divLiveDataTime.innerHTML = data.lastDataTime;
    divCurrentPV.innerHTML = "Current PV: <span class=\"pv\">$" + data.currentPV + "</span> (based on <a href=" + data.gSheetRef + '" target="_blank" >these current positions</a> updated for ' + data.currentPVDate + ")";
    if (data.dailyProfSig !== "N/A") { divDailyProfit.innerHTML = "<b>Daily Profit/Loss: <span class=\"" + data.dailyProfString + "\">" + data.dailyProfSig + data.dailyProfAbs + " ("+data.dailyProfPerc+"%)</span></b>"; }
    if (data.monthlyProfSig !== "N/A") { divMonthlyProfit.innerHTML = "<b>MTD Profit/Loss: <span class=\"" + data.monthlyProfString + "\">" + data.monthlyProfSig + data.monthlyProfAbs + " (" + data.monthlyProfPerc +"%)</span></b>"; }
    if (data.yearlyProfSig !== "N/A") { divYearlyProfit.innerHTML = "<b>YTD Profit/Loss: <span class=\"" + data.yearlyProfString + "\">" + data.yearlyProfSig + data.yearlyProfAbs + " (" + data.yearlyProfPerc + "%)</span></b>"; }
    divBondPerc.innerHTML = "<span class=\"notDaily\">Current / Required Bond Percentage: " + data.currBondPerc + " / " + data.nextBondPerc + ".&emsp;&emsp; Used Leverage: " + data.leverage +".&emsp;Used Maximum Bond Percentage: "+data.maxBondPerc+".</span>";
    
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

    var assChartMtxTemp = data.assetChangesToChartMtx.split("ß ");
    var assChartMtx = new Array();
    for (var i = 0; i < assChartMtxTemp.length; i++) {
        assChartMtx[i] = assChartMtxTemp[i].split(",");
    }

    var assScoresMtxTemp = data.assetScoresMtx.split("ß ");
    var assScoresMtx = new Array();
    for (var i = 0; i < assScoresMtxTemp.length; i++) {
        assScoresMtx[i] = assScoresMtxTemp[i].split(",");
    }

    //Creating the HTML code of tables.
    var currTableMtx = "<table class=\"currData\"><tr align=\"center\"><td bgcolor=\"#66CCFF\"></td>";
    for (var i = 0; i < assetNames2Array.length - 1; i++) {
        currTableMtx += "<td bgcolor=\"#66CCFF\"><a href=https://finance.yahoo.com/quote/" + assetNames2Array[i] + ' target="_blank">' + assetNames2Array[i] + "</a></td>";
    }
    currTableMtx += "<td bgcolor=\"#66CCFF\">" + assetNames2Array[assetNames2Array.length - 1] + "</td>";

    currTableMtx += "</tr > <tr align=\"center\"><td align=\"center\" rowspan=\"2\" bgcolor=\"#FFABAB\">" + data.nextTradingDay + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx += "<td bgcolor=\"#FFFFD1\">" + nextPosValArray[i] + "</td>";
    }

    currTableMtx += "</tr > <tr>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx += "<td bgcolor=\"#FFFFD1\">" + nextPosNumArray[i] + "</td>";
    }

    currTableMtx += "</tr > <tr align=\"center\"><td align=\"center\" rowspan=\"2\" bgcolor=\"#FFABAB\">" + data.currPosDate + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx += "<td bgcolor=\"#E7FFAC\">" + currPosValArray[i] + "</td>";
    }

    currTableMtx += "</tr > <tr>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx += "<td bgcolor=\"#E7FFAC\">" + currPosNumArray[i] + "</td>";
    }

    currTableMtx += "</tr > <tr align=\"center\"><td align=\"center\" rowspan=\"2\" bgcolor=\"#FFABAB\">Change in Positions</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx += "<td bgcolor=\"#C4FAF8\">" + diffPosValArray[i] + "</td>";
    }

    currTableMtx += "</tr > <tr>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx += "<td bgcolor=\"#C4FAF8\">" + diffPosNumArray[i] + "</td>";
    }

    currTableMtx += "</tr></table>";

    var currTableMtx3 = "<table class=\"currData\"><tr align=\"center\"  bgcolor=\"#1E90FF\"><td rowspan=\"2\"></td><td rowspan=\"2\">TAA Percentile Channel Score</td><td rowspan=\"2\">Required Asset Weight</td><td colspan=\"7\">Percentage Change of Stock Price</td></tr><tr align=\"center\" bgcolor=\"#6EB5FF\"><td>1-Day</td><td>1-Week</td><td>2-Weeks</td><td>1-Month</td><td>3-Months</td><td>6-Months</td><td>1-Year</td></tr>";
    for (var i = 0; i < assetNames2Array.length-2; i++) {
        currTableMtx3 += "<tr align=\"center\">";
        currTableMtx3 += "<td bgcolor=\"#66CCFF\"><a href=https://finance.yahoo.com/quote/" + assetNames2Array[i] + ' target="_blank">' + assetNames2Array[i] + "</a></td>";
        currTableMtx3 += "<td bgcolor=\"" + perc2color(parseFloat(assScoresMtx[i][0]),-100,100) + "\">" + assScoresMtx[i][0] + "</td>";
        currTableMtx3 += "<td bgcolor=\"" + perc2color(parseFloat(assScoresMtx[i][1]), -20, 20) + "\">" + assScoresMtx[i][1] + "</td>";
        for (var j = 0; j < 7; j++) {
            currTableMtx3 += "<td bgcolor=\"" + perc2color(parseFloat(assChartMtx[i][j]), -40, 40) + "\">" + assChartMtx[i][j] + "</td>";
        }
        currTableMtx3 += "</tr>";
    }
    currTableMtx3 += "<tr align=\"center\">";
    currTableMtx3 += "<td bgcolor=\"#66CCFF\"><a href=https://finance.yahoo.com/quote/" + assetNames2Array[assetNames2Array.length - 2] + ' target="_blank">' + assetNames2Array[assetNames2Array.length - 2] + "</a></td>";
    currTableMtx3 += "<td bgcolor=\"#FFF5BA\">" + assScoresMtx[assetNames2Array.length - 2][0] + "</td>";
    currTableMtx3 += "<td bgcolor=\"" + perc2color(parseFloat(assScoresMtx[assetNames2Array.length - 2][1]), -20, 20) + "\">" + assScoresMtx[assetNames2Array.length - 2][1] + "</td>";
    for (var j = 0; j < 7; j++) {
        currTableMtx3 += "<td bgcolor=\"" + perc2color(parseFloat(assChartMtx[assetNames2Array.length - 2][j]), -40, 40) + "\">" + assChartMtx[assetNames2Array.length - 2][j] + "</td>";
    }
    currTableMtx3 += "</tr>";
    currTableMtx3 += "</table>";

    

    //"Sending" data to HTML file.
    var currTableMtx2 = document.getElementById("idCurrTableMtx");
    currTableMtx2.innerHTML = currTableMtx;
    var currTableMtx4 = document.getElementById("idCurrTableMtx3");
    currTableMtx4.innerHTML = currTableMtx3;
   

    
    //Declaring data sets to charts.
    var retHistLBPeriods = data.pastPerfDaysName.split(", ");
    var retHistLBPeriodsNoS = data.pastPerfDaysNum.split(", ");
    var retHistLBPeriodsNo = [];
    for (var i = 0; i < retHistLBPeriodsNoS.length; i++) { retHistLBPeriodsNo[i] = parseInt(retHistLBPeriodsNoS[i]); }
    

    var lengthOfChart = 21;
    var indOfLength = retHistLBPeriodsNo.indexOf(lengthOfChart);
    var divChartLength = document.getElementById("idChartLength");
    divChartLength.innerHTML = "<div class=\"DDM\"><strong>in the Last &emsp;<select class=\"DDM\" id=\"limit2\"><option value=\"1\">1 Day</option><option value=\"5\">1 Week</option><option value=\"10\">2 Weeks</option><option value=\"21\" selected>1 Month</option><option value=\"63\">3 Months</option><option value=\"126\">6 Months</option><option value=\"252\">1 Year</option>" + retHistLBPeriods[indOfLength] + "</select></strong ></div>";
    creatingChartData1();

    $('.DDM').hover(function () {
        var count = $(this).children().length;
        $(this).attr('size', count);
        
    },
        function () {
            $(this).removeAttr('size');
        });


    $('#limit2').on('mouseenter','option', function () {
        lengthOfChart = parseInt(this.value);
        indOfLength = retHistLBPeriodsNo.indexOf(lengthOfChart);
        //remove selected one
        $('option:selected', this).removeAttr('selected');
        //Using the value
        this.selected = true;
        creatingChartData1();
    });

    //$('#limit2').bind('change', function () {
    //    lengthOfChart = parseInt(this.value);
    //    indOfLength = retHistLBPeriodsNo.indexOf(lengthOfChart);
    //    creatingChartData1();
    //});

    function creatingChartData1() {

        var nCurrData = 1;
        var noAssets = assetNames2Array.length - 1;

        var yTicksH = new Array(noAssets);
        for (var i = 0; i < noAssets; i++) {
            var yTicksHRows = new Array(2);
            yTicksHRows[0] = i;
            yTicksHRows[1] = assetNames2Array[i];
            yTicksH[i] = yTicksHRows;
        }


        var listH = [];
        for (var j = 0; j < noAssets; j++) {
            var assChartPerc1 = new Array(nCurrData);
            for (var i = 0; i < nCurrData; i++) {
                var assChartPerc1Rows = new Array(2);
                assChartPerc1Rows[1] = j;
                assChartPerc1Rows[0] = parseFloat(assChartMtx[j][indOfLength]);
                assChartPerc1[i] = assChartPerc1Rows;
            }
            listH.push({ label: assetNames2Array[j], data: assChartPerc1, bars: { show: true } });
        }


        var datasets1 = listH;





        flotPlotMyData1(datasets1, noAssets, yTicksH, nCurrData, retHistLBPeriods[indOfLength]);

    }
        

    }
    // Creating charts.
function flotPlotMyData1(datasets1, noAssets, yTicksH, nCurrData, retHistLBP) {
    $("#update_all").click(plotAccordingToChoices);

    function plotAccordingToChoices() {
        var dataB = [];
        $.each(datasets1, function (key) {
            dataB.push(datasets1[key]);
        });

        $.plot("#placeholder1", dataB,
            {
                bars: {
                    align: "center",
                    barWidth: 0.7,
                    horizontal: true,
                    fillColor: "#FF8000",
                    lineWidth: 1
                },
                xaxis: {
                    axisLabel: "Percentage Change",
                    tickFormatter: function (v, axis) {
                        return v.toFixed(0)+"%";
                    }
                    //color: "blue"

                },
                yaxis: {
                    //tickDecimals: 0,
                    min: -1,
                    //max: nCurrData-1,
                    ticks: yTicksH,
                    transform: function (v) { return -v; },
                    inverseTransform: function (v) { return -v; }
                    //color: "blue"
                    //axisLabel: "Stock"
                },
                legend: {
                    show: false,
                    position: "nw",
                    noColumns: noAssets,
                    backgroundColor: "#F4F6F6"
                },
                grid: {
                    borderWidth: 2,
                    backgroundColor: "#FFFFD1",
                    hoverable: true
                },
                tooltip: {
                    show: true,
                    content: function (label, x, y) {
                        var yVals = [];
                        for (var i = 0; i < noAssets; i++) {
                            yVals[i] = dataB[i].data[0][1];
                        }
                        var indi = yVals.indexOf(y);
                       
                        var text = "<b>" + label + "<br/></b><i>" + retHistLBP+" Percentage Change is " + dataB[indi].data[0][0] + "%<br/></i>";
                        
                        return text;
                    }
                }

            });
    }
    plotAccordingToChoices();  

    }