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

function choseall(nameS) {
    var x = document.getElementsByName(nameS);
    var y = 0;
    for (var i = 0; i < x.length; i++) {
        if (document.getElementsByName(nameS)[i].checked) {
            y += 1;
        }
    }
    for (var i = 0; i < x.length; i++) {

        if (y == x.length) {
            document.getElementsByName(nameS)[i].checked = true;
            document.getElementsByName(nameS)[i].click();
        } else {
            if (document.getElementsByName(nameS)[i].checked != true) {
                document.getElementsByName(nameS)[i].checked = false;
                document.getElementsByName(nameS)[i].click();
            }
        }
    }
}

function checkAll(ele) {
    var x = document.getElementsByClassName('szpari');
    var y = 0;
    for (var i = 0; i < x.length; i++) {
        if (x[i].checked) {
            y += 1;
        }
    }
    for (var i = 0; i < x.length; i++) {

        if (y == x.length) {
            x[i].checked = true;
            document.getElementsByClassName('szpari')[i].click();              
        } else {
            if (x[i].checked != true){
                x[i].checked = false;
                document.getElementsByClassName('szpari')[i].click();
            }
        }
    }
}

function onHeadProcessing() {
    console.log('onHeadProcessing()');

    var xmlHttp = new XMLHttpRequest();
    xmlHttp.onreadystatechange = function () {
        if (xmlHttp.readyState == 4 && xmlHttp.status == 200)
            processData(xmlHttp.responseText);
    }
    var commo = getQueryVariable("lbp");
    xmlHttp.open("GET", "/VolDragData?commo=" + commo, true); // true for asynchronous 
    xmlHttp.send(null);
}

function processData(dataStr) {
    //Printing error message from C#.  

    if (dataStr == "Error") {
        document.getElementById("waitingMessage").style.display = "none";
        var divErrorCont = document.getElementById("idErrorCont");
        divErrorCont.innerHTML = "Error during downloading data. Please, try again later!"
        document.getElementById("errorMessage").style.visibility = "visible";
        document.getElementById("inviCharts").style.visibility = "hidden";

        return;
    }

    document.getElementById("errorMessage").style.display = "none";



    //Splitting data got from C#.
    var data = JSON.parse(dataStr);

    //Creating first rows of webpage.
    //var divTitleCont = document.getElementById("idTitleCont");
    var divTimeNow = document.getElementById("idTimeNow");
    var divLiveDataTime = document.getElementById("idLiveDataTime");

    divTimeNow.innerHTML = data.requestTime;
    divLiveDataTime.innerHTML = data.lastDataTime;

    var volAssetNamesArray = data.volAssetNames.split(", ");
    var etpAssetNamesArray = data.etpAssetNames.split(", ");
    var gchAssetNamesArray = data.gchAssetNames.split(", ");
    var gmAssetNamesArray = data.gmAssetNames.split(", ");
    var defCheckedListArray = data.defCheckedList.split(", ");



    var chBxs = "<p class=\"left\"><button class=\"button2\" style=\"background: url(/images/vix.jpg); background-size: cover;\" title=\"Volatility ETPs\" onclick=\"choseall('volA')\"/></button>&emsp;&emsp;";
    for (var iAssets = 0; iAssets < volAssetNamesArray.length; iAssets++) {
        chBxs += "<input class= \"szpari\" type=\"checkbox\" name=\"volA\" id=\"" + volAssetNamesArray[iAssets] + "\"/><a target=\"_blank\" href=\"https://finance.yahoo.com/quote/" + volAssetNamesArray[iAssets].split("_")[0] + "\">" + volAssetNamesArray[iAssets] + "</a> &emsp;";
    }
    chBxs += "<br><button class=\"button2\" style=\"background: url(/images/ImportantEtps.png); background-size: cover;\" title=\"Important ETPs\" onclick=\"choseall('etpA')\"></button>&emsp;&emsp;";
    for (var iAssets = 0; iAssets < etpAssetNamesArray.length; iAssets++) {
        chBxs += "<input class= \"szpari\" type=\"checkbox\" name=\"etpA\" id=\"" + etpAssetNamesArray[iAssets] + "\"/><a target=\"_blank\" href=\"https://finance.yahoo.com/quote/" + etpAssetNamesArray[iAssets].split("_")[0] + "\">" + etpAssetNamesArray[iAssets] + "</a> &emsp;";
    }
    chBxs += "<br><button class=\"button2\" style=\"background: url(/images/GameChangers.png); background-size: cover;\" title=\"GameChanger Stocks\" onclick=\"choseall('gchA')\"></button>&emsp;&emsp;";
    for (var iAssets = 0; iAssets < gchAssetNamesArray.length; iAssets++) {
        chBxs += "<input class= \"szpari\" type=\"checkbox\" name=\"gchA\" id=\"" + gchAssetNamesArray[iAssets] + "\"/><a target=\"_blank\" href=\"https://finance.yahoo.com/quote/" + gchAssetNamesArray[iAssets].split("_")[0] + "\">" + gchAssetNamesArray[iAssets] + "</a> &emsp;";
    }
    chBxs += "<br><button class=\"button2\" style=\"background: url(/images/GlobalAssets.png); background-size: cover;\" title=\"Global Assets\" onclick=\"choseall('gmA')\"></button>&emsp;&emsp;";
    for (var iAssets = 0; iAssets < gmAssetNamesArray.length; iAssets++) {
        chBxs += "<input class= \"szpari\" type=\"checkbox\" name=\"gmA\" id=\"" + gmAssetNamesArray[iAssets] + "\" /><a target=\"_blank\" href=\"https://finance.yahoo.com/quote/" + gmAssetNamesArray[iAssets].split("_")[0] + "\">" + gmAssetNamesArray[iAssets] + "</a> &emsp;";
    }
    chBxs += "</p ><p class=\"center\"><button class=\"button3\" style=\"background: url(/images/selectall.png); background-size: cover;\" title=\"Select/Deselect All\" onclick=\"checkAll(this)\"/></button>&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;<button class=\"button3\" style=\"background: url(/images/updateall.png); background-size: cover;\" title=\"Update Charts and Tables\" id='update_all'></button></p> ";
    


    var checkBoxes = document.getElementById("idChBxs");
    checkBoxes.innerHTML = chBxs;

    $("input:checkbox").each(function () {
        if (jQuery.inArray($(this).attr("id"), defCheckedListArray) !== -1) {
            $(this).prop('checked', true);
        }
        $(this).checked;
    });

    creatingTables(data);

   

    $("input:checkbox:not(:checked)").each(function () {
        var column = "table ." + $(this).attr("id");
        $(column).hide();
    });

    $("#update_all").click(function () {
        $("input:checkbox:not(:checked)").each(function () {
            var column = "table ." + $(this).attr("id");
            $(column).hide();
        });
        $("input:checkbox:checked").each(function () {
            var column = "table ." + $(this).attr("id");
            $(column).show();

        });
    });

    //$('.header').click(function () {
    //    $(this).find('span').text(function (_, value) { return value == '-' ? '+' : '-' });
    //    $(this).nextUntil('tr.header').slideToggle(100); // or just use "toggle()"
    //}).click();


    $(document).ready(function () {
        $('table.currData').each(function () {
            var $table = $(this);
            $table.find('.parent').click(function () {
                $(this).nextUntil('.parent').toggle();
            });

            var $childRows = $table.find('tbody tr').not('.parent').hide();
            $table.find('span.hide').click(function () {
                $childRows.hide();
            });
            $table.find('span.show').click(function () {
                $childRows.filter('.child').show();
            });
        });
    });


    function show(min, max) {
        var $table = $('#mytable'), $rows = $table.find('tbody tr');
        min = min ? min - 1 : 0;
        max = max ? max : $rows.length;
        $rows.hide().slice(min, max).show();
        return false;
    }
    show(0, 21);

    $('#limit').bind('change', function () {
        show(0, this.value);
    });

    


    //Setting charts visible after getting data.
    document.getElementById("waitingMessage").style.display = "none";
    document.getElementById("inviCharts").style.visibility = "visible";
}
function creatingTables(data) {

    
    //Creating JavaScript data arrays by splitting.
    var assetNamesArray = data.assetNames.split(", ");
    var dailyDatesArray = data.quotesDateVector.split(", ");

    var volLBPeriod = data.volLBPeri;

    var dailyVolDragsTemp = data.dailyVolDrags.split("ß ");
    var dailyVolDragsMtx = new Array();
    for (var i = 0; i < dailyVolDragsTemp.length; i++) {
        dailyVolDragsMtx[i] = dailyVolDragsTemp[i].split(",");
    }

    var dailyVIXMasArray = data.dailyVIXMas.split(", ");
    var yearListArray = data.yearList.split(", ");
    var yearMonthListArray = data.yearMonthList.split(", ");

    var yearlyAvgsTemp = data.yearlyAvgs.split("ß ");
    var yearlyAvgsMtx = new Array();
    for (var i = 0; i < yearlyAvgsTemp.length; i++) {
        yearlyAvgsMtx[i] = yearlyAvgsTemp[i].split(",");
    }

    for (var i = 0; i < yearlyAvgsTemp.length; i++) {
        for (var j = 0; j < yearlyAvgsMtx[0].length; j++) {
            if (yearlyAvgsMtx[i][j] == " 0%") {
                yearlyAvgsMtx[i][j] = "---";
            }
        }
    }

    var monthlyAvgsTemp = data.monthlyAvgs.split("ß ");
    var monthlyAvgsMtx = new Array();
    for (var i = 0; i < monthlyAvgsTemp.length; i++) {
        monthlyAvgsMtx[i] = monthlyAvgsTemp[i].split(",");
    }

    for (var i = 0; i < monthlyAvgsTemp.length; i++) {
        for (var j = 0; j < monthlyAvgsMtx[0].length; j++) {
            if (monthlyAvgsMtx[i][j] == " 0%") {
                monthlyAvgsMtx[i][j] = "---";
            }
        }
    }

    var yearlyVIXAvgsArray = data.yearlyVIXAvgs.split(", ");
    var monthlyVIXAvgsArray = data.monthlyVIXAvgs.split(", ");
    var yearlyCountsArray = data.yearlyCounts.split(", ");
    var monthlyCountsArray = data.monthlyCounts.split(", ");
    var totDays = data.noTotalDays;
    var vixAvgTot = data.vixAvgTotal;
    var volDragsAvgsTotalArray = data.volDragsAvgsTotalVec.split(", ");
    var noColumns = assetNamesArray.length + 3;

    var noInnerYears = yearListArray.length - 2;
    var noLastYearMonths = yearMonthListArray.length - 10 - noInnerYears * 12;

    var retHistLBPeriods = data.retLBPeris.split(", ");
    var retHistLBPeriodsNoS = data.retLBPerisNo.split(", ")
    var retHistLBPeriodsNo = [];
    for (var i = 0; i < retHistLBPeriodsNoS.length; i++) { retHistLBPeriodsNo[i] = parseInt(retHistLBPeriodsNoS[i]); }
    var retHistChartLength = data.retHistLBPeri;


    var histRetsTemp = data.histRetMtx.split("ß ");
    var histRetsMtx = new Array();
    for (var i = 0; i < histRetsTemp.length; i++) {
        histRetsMtx[i] = histRetsTemp[i].split(",");
    }

    var histRets2ChartsTemp = data.histRet2Chart.split("ß ");
    var histRets2ChartsMtx = new Array();
    for (var i = 0; i < histRets2ChartsTemp.length; i++) {
        histRets2ChartsMtx[i] = histRets2ChartsTemp[i].split(",");
    }

    //Creating the HTML code of tables.

    var currTableMtx = "<table class=\"currDataB\"><tr align=\"center\"><td colspan=\"" + (noColumns - 1) + "\" bgcolor=\"#66CCFF\"><b>Current Monthly Volatility Drag</b></td></tr><tr align=\"center\"><td bgcolor=\"#66CCFF\">Date</td><td class=\"first_name\" bgcolor=\"#66CCFF\">VIX MA(" + volLBPeriod + ")</td>";
    for (var i = 0; i < assetNamesArray.length - 1; i++) {
        currTableMtx += "<td class=\"" + assetNamesArray[i] + "\" bgcolor=\"#66CCFF\">" + assetNamesArray[i] + "</td>";
    }
    currTableMtx += "<td class=\"" + assetNamesArray[assetNamesArray.length - 1] + "\" bgcolor=\"#66CCFF\">" + assetNamesArray[assetNamesArray.length - 1] + "</td></tr>";
    currTableMtx += "<tr align=\"center\"><td>" + dailyDatesArray[dailyDatesArray.length - 1] + "</td>";
    currTableMtx += "<td class=\"first_name\">" + dailyVIXMasArray[dailyVIXMasArray.length - 1] + "</td>";
    for (var i = 0; i < assetNamesArray.length; i++) {
        currTableMtx += "<td class=\"" + assetNamesArray[i] + "\">" + dailyVolDragsMtx[dailyVolDragsMtx.length - 1][i] + "</td>";
    }
    currTableMtx += "</tr></table>";


    var currTableMtx3 = "<table class=\"currData\"><thead><tr align=\"center\" ><td colspan=\"" + noColumns + "\" bgcolor=\"#66CCFF\"><b>Monthly Volatility Drag by Years and Months</b></td></tr><tr align=\"center\"><td bgcolor=\"#66CCFF\"><span class=\"years\">Only Years</span> / <span class=\"years\">Years+Months</span></td><td bgcolor=\"#66CCFF\">No. Days</td><td bgcolor=\"#66CCFF\">VIX MA(" + volLBPeriod + ")</td>";
    for (var i = 0; i < assetNamesArray.length - 1; i++) {
        currTableMtx3 += "<td class=\"" + assetNamesArray[i] + "\" bgcolor=\"#66CCFF\">" + assetNamesArray[i] + "</td>";
    }
    currTableMtx3 += "<td class=\"" + assetNamesArray[assetNamesArray.length - 1] + "\" bgcolor=\"#66CCFF\">" + assetNamesArray[assetNamesArray.length - 1] + "</td></tr></thead>";
    currTableMtx3 += "<tbody><tr class=\"parent\"><td><span class=\"years\">" + yearListArray[0] + "</span></td><td>" + yearlyCountsArray[0] + "</td><td>" + yearlyVIXAvgsArray[0] + "</td>";
    for (var i = 0; i < assetNamesArray.length; i++) {
        currTableMtx3 += "<td class=\"" + assetNamesArray[i] + "\">" + yearlyAvgsMtx[0][i] + "</td>";
    }
    for (var i = 1; i < 10; i++) {
        currTableMtx3 += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[i] + "&emsp;</i></td><td><i>" + monthlyCountsArray[i] + "</i></td><td><i>" + monthlyVIXAvgsArray[i] + "</i></td>"
        for (var j = 0; j < assetNamesArray.length; j++) {
            currTableMtx3 += "<td class=\"" + assetNamesArray[j] + "\"><i>" + monthlyAvgsMtx[i][j] + "</i></td>";
        }
        currTableMtx3 += "</tr>";
    }
    for (var k = 0; k < noInnerYears; k++) {
        currTableMtx3 += "<tr class=\"parent\"><td><span class=\"years\">" + yearListArray[k + 1] + "</span></td><td>" + yearlyCountsArray[k + 1] + "</td><td>" + yearlyVIXAvgsArray[k + 1] + "</td>";
        for (var i = 0; i < assetNamesArray.length; i++) {
            currTableMtx3 += "<td class=\"" + assetNamesArray[i] + "\">" + yearlyAvgsMtx[k + 1][i] + "</td>";
        }
        for (var i = 0; i < 12; i++) {
            currTableMtx3 += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[10 + k * 12 + i] + "&emsp;</i></td><td><i>" + monthlyCountsArray[10 + k * 12 + i] + "</i></td><td><i>" + monthlyVIXAvgsArray[10 + k * 12 + i] + "</i></td>"
            for (var j = 0; j < assetNamesArray.length; j++) {
                currTableMtx3 += "<td class=\"" + assetNamesArray[j] + "\"><i>" + monthlyAvgsMtx[10 + k * 12 + i][j] + "</i></td>";
            }
            currTableMtx3 += "</tr>";
        }
    }
    currTableMtx3 += "<tr class=\"parent\"><td><span class=\"years\">" + yearListArray[yearListArray.length - 1] + "</span></td><td>" + yearlyCountsArray[yearListArray.length - 1] + "</td><td>" + yearlyVIXAvgsArray[yearListArray.length - 1] + "</td>";
    for (var i = 0; i < assetNamesArray.length; i++) {
        currTableMtx3 += "<td class=\"" + assetNamesArray[i] + "\">" + yearlyAvgsMtx[yearListArray.length - 1][i] + "</td>";
    }
    for (var i = 0; i < noLastYearMonths; i++) {
        currTableMtx3 += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[10 + noInnerYears * 12 + i] + "&emsp;</i></td><td><i>" + monthlyCountsArray[10 + noInnerYears * 12 + i] + "</i></td><td><i>" + monthlyVIXAvgsArray[10 + noInnerYears * 12 + i] + "</i></td>"
        for (var j = 0; j < assetNamesArray.length; j++) {
            currTableMtx3 += "<td class=\"" + assetNamesArray[j] + "\"><i>" + monthlyAvgsMtx[10 + noInnerYears * 12 + i][j] + "</i></td>";
        }
        currTableMtx3 += "</tr>";
    }
    currTableMtx3 += "<tr class=\"parent\" style=\"cursor: text\"><td><span class=\"total\">Total 2004-" + yearListArray[yearListArray.length - 1] + "</span></td><td>" + totDays + "</td><td>" + vixAvgTot + "</td>";
    for (var i = 0; i < assetNamesArray.length; i++) {
        currTableMtx3 += "<td class=\"" + assetNamesArray[i] + "\">" + volDragsAvgsTotalArray[i] + "</td>";
    }
    currTableMtx3 += "</tr></tbody></table>";

    var currTableMtx5 = "<table class=\"currDataB\"><tr align=\"center\"><td colspan=\"" + (noColumns - 2) + "\" bgcolor=\"#66CCFF\"><b>Recent Performance of Stocks - Percent Changes of Prices</b></td></tr><tr align=\"center\"><td bgcolor=\"#66CCFF\"></td>";
    for (var i = 0; i < assetNamesArray.length - 1; i++) {
        currTableMtx5 += "<td class=\"" + assetNamesArray[i] + "\" bgcolor=\"#66CCFF\">" + assetNamesArray[i] + "</td>";
    }
    currTableMtx5 += "<td class=\"" + assetNamesArray[assetNamesArray.length - 1] + "\" bgcolor=\"#66CCFF\">" + assetNamesArray[assetNamesArray.length - 1] + "</td></tr>";
    for (var j = 0; j < retHistLBPeriods.length; j++) {
        currTableMtx5 += "<tr align=\"center\"><td>" + retHistLBPeriods[j] + "</td>";
        for (var i = 0; i < assetNamesArray.length; i++) {
            currTableMtx5 += "<td class=\"" + assetNamesArray[i] + "\">" + histRetsMtx[j][i] + "</td>";
        }
        currTableMtx5 += "</tr>"
    }
    currTableMtx5 += "</table>";


    var currTableMtx7 = "<table id=\"mytable\" class=\"currDataB2\"><thead><tr align=\"center\"><td colspan=\"" + (noColumns - 1) + "\" bgcolor=\"#66CCFF\"><b>Monthly Volatility Drag History</b></td></tr><tr align=\"center\"><td bgcolor=\"#66CCFF\"><select id=\"limit\"><option value=\"5\">1-Week</option><option value=\"21\" selected>1-Month</option><option value=\"63\">3-Month</option><option value=\"126\">6-Month</option><option value=\"252\">1-Year</option><option value=\"" + dailyDatesArray.length + "\">All</option></select ></td><td bgcolor=\"#66CCFF\">VIX MA(" + volLBPeriod + ")</td>";
    for (var i = 0; i < assetNamesArray.length - 1; i++) {
        currTableMtx7 += "<td class=\"" + assetNamesArray[i] + "\" bgcolor=\"#66CCFF\">" + assetNamesArray[i] + "</td>";
    }
    currTableMtx7 += "<td class=\"" + assetNamesArray[assetNamesArray.length - 1] + "\" bgcolor=\"#66CCFF\">" + assetNamesArray[assetNamesArray.length - 1] + "</td></tr></thead><tbody>";
    for (var j = dailyVolDragsMtx.length - 1; j >= 0; j--) {
        currTableMtx7 += "<tr align=\"center\"><td>" + dailyDatesArray[j] + "</td>";
        currTableMtx7 += "<td>" + dailyVIXMasArray[j] + "</td>";

        for (var i = 0; i < assetNamesArray.length; i++) {
            currTableMtx7 += "<td class=\"" + assetNamesArray[i] + "\">" + dailyVolDragsMtx[j][i] + "</td>";
        }
        currTableMtx7 += "</tr>"
    }
    currTableMtx7 += "</tbody></table>";

    //"Sending" data to HTML file.
    var currTableMtx2 = document.getElementById("idCurrTableMtx");
    currTableMtx2.innerHTML = currTableMtx;
    var currTableMtx4 = document.getElementById("idCurrTableMtx3");
    currTableMtx4.innerHTML = currTableMtx3;
    var currTableMtx6 = document.getElementById("idCurrTableMtx5");
    currTableMtx6.innerHTML = currTableMtx5;
    var currTableMtx8 = document.getElementById("idCurrTableMtx7");
    currTableMtx8.innerHTML = currTableMtx7;

    var lengthOfChart = 20;
    var indOfLength = retHistLBPeriodsNo.indexOf(lengthOfChart);
    var divChartLength = document.getElementById("idChartLength");
    divChartLength.innerHTML = "<strong>Percentage Changes of Prices in the Last &emsp;<select id=\"limit2\"><option value=\"1\">1 Day</option><option value=\"3\">3 Days</option><option value=\"5\">1 Week</option><option value=\"10\">2 Weeks</option><option value=\"20\" selected>1 Month</option><option value=\"63\">3 Months</option><option value=\"126\">6 Months</option><option value=\"252\">1 Year</option>" + retHistLBPeriods[indOfLength] + "</strong >";

    creatingChartData1();
    creatingChartData2();

    $('#limit2').bind('change', function () {
        lengthOfChart = parseInt(this.value);
        indOfLength = retHistLBPeriodsNo.indexOf(lengthOfChart)
        creatingChartData1();
    });

    //Declaring data sets to charts.
    function creatingChartData1() {
        var lengthSubSums = [];
        lengthSubSums[0] = 0;
        lengthSubSums[1] = retHistLBPeriodsNo[0];
        for (var i = 2; i < retHistLBPeriodsNo.length + 1; i++) {
            lengthSubSums[i] = lengthSubSums[i - 1] + retHistLBPeriodsNo[i - 1];
        }
        
        var chartStart = lengthSubSums[indOfLength];
        var chartEnd = lengthSubSums[indOfLength + 1] - 1;
        var nCurrData = lengthOfChart + 1;

        var xTicksH = new Array(nCurrData);
        for (var i = 0; i < nCurrData; i++) {
            var xTicksHRows = new Array(2);
            xTicksHRows[0] = i;
            xTicksHRows[1] = dailyDatesArray[dailyDatesArray.length - nCurrData + i];
            xTicksH[i] = xTicksHRows;
        }

        var noAssets = assetNamesArray.length;
        var listH = [];
        for (var j = 0; j < noAssets; j++) {
            var assChartPerc1 = new Array(nCurrData);
            var assChartPerc1Rows0 = new Array(2);
            assChartPerc1Rows0[0] = 0;
            assChartPerc1Rows0[1] = 0;
            assChartPerc1[0] = assChartPerc1Rows0;
            for (var i = 1; i < nCurrData; i++) {
                var assChartPerc1Rows = new Array(2);
                assChartPerc1Rows[0] = i;
                assChartPerc1Rows[1] = parseFloat(histRets2ChartsMtx[chartStart - 1 + i][j]);
                assChartPerc1[i] = assChartPerc1Rows;
            }
            listH.push({ label: assetNamesArray[j], data: assChartPerc1, points: { show: true, radius: Math.min(40 / nCurrData,2) }, lines: { show: true } });
        }

        
        var datasets1 = listH;
        
        flotPlotMyData1(datasets1, nCurrData, xTicksH, noAssets, assetNamesArray);
        

        
    }
    function creatingChartData2() {
        
        var noAssets = assetNamesArray.length;
       
        var nCurrDataVD = dailyDatesArray.length;

        var xTicksHVD = new Array(nCurrDataVD);
        for (var i = 0; i < nCurrDataVD; i++) {
            var xTicksHVDRows = new Array(2);
            xTicksHVDRows[0] = i;
            xTicksHVDRows[1] = dailyDatesArray[i];
            xTicksHVD[i] = xTicksHVDRows;
        }

        var listHVD = [];
        for (var j = 0; j < noAssets; j++) {
            var assChartVD = new Array(nCurrDataVD);
            for (var i = 0; i < nCurrDataVD; i++) {
                var assChartVDRows = new Array(2);
                assChartVDRows[0] = i;
                assChartVDRows[1] = parseFloat(dailyVolDragsMtx[i][j]);
                assChartVD[i] = assChartVDRows;
            }
            listHVD.push({ label: assetNamesArray[j], data: assChartVD, points: { show: true, radius: 0.01 }, lines: { show: true, lineWidth: 1 } });
        }

        
        var datasets2 = listHVD;

        flotPlotMyData2(datasets2, nCurrDataVD, xTicksHVD, noAssets, assetNamesArray);
        
    }
}
    // Creating charts.
function flotPlotMyData1(datasets1, nCurrData, xTicksH, noAssets, assetNamesArray) {
        $("#update_all").click(plotAccordingToChoices);
        function plotAccordingToChoices() {
            var dataB = [];
            //$.each(datasets1, function (key) {
            //    dataB.push(datasets1[key]);
            //});

            $("input:checkbox:checked").each(function () {
                var key = $(this).attr("id");
                var aaa = assetNamesArray.indexOf(key);
                if (key && datasets1[aaa]) {
                    dataB.push(datasets1[aaa]);
                }
            });

            $.plot("#placeholder1", dataB,
                {
                    yaxis: {
                        axisLabel: "Percentage Change",
                        tickFormatter: function (v, axis) {
                            return v.toFixed(0) + "%";
                        }      
                    },
                    xaxis: {
                        //tickDecimals: 0,
                        min: 0,
                        //max: nCurrData-1,
                        ticks: xTicksH,
                        axisLabel: "Date"
                    },
                    legend: {
                        position: "nw",
                        noColumns: Math.min(noAssets, 10),
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
        plotAccordingToChoices();
        formatXTicks();
}

function flotPlotMyData2(datasets2, nCurrDataVD, xTicksHVD, noAssets, assetNamesArray) {
    $("#update_all").click(plotAccordingToChoicesVD);
    function plotAccordingToChoicesVD() {
        var dataBVD = [];
        //$.each(datasets1, function (key) {
        //    dataB.push(datasets1[key]);
        //});

        $("input:checkbox:checked").each(function () {
            var keyVD = $(this).attr("id");
            var aaaVD = assetNamesArray.indexOf(keyVD);
            if (keyVD && datasets2[aaaVD]) {
                dataBVD.push(datasets2[aaaVD]);
            }
        });

        var placeholder = $("#placeholder2");
        var data = dataBVD
        var options =
            {
                yaxis: {
                    axisLabel: "Volatility Drag",
                    tickFormatter: function (v, axis) {
                        return v.toFixed(0) + "%";
                        //},
                        //transform: function (v) {
                        //    return Math.log(v + 0.0001);
                    }
                    //},
                    //tickDecimals: 3
                    //zoomRange: [0, 10],
                    //panRange: false

                },
                xaxis: {
                    //tickDecimals: 0,
                    min: 0,
                    //max: nCurrData-1,
                    ticks: xTicksHVD,
                    axisLabel: "Date"
                    //zoomRange: [1, 10],
                    //panRange: [0, nCurrDataVD]
                },
                legend: {
                    position: "nw",
                    noColumns: Math.min(noAssets, 10),
                    backgroundColor: "#F4F6F6"
                },
                //zoom: {
                //    interactive: true
                //},
                //pan: {
                //    interactive: true,
                //    cursor: "move"
                //},
                //grid: {
                //    backgroundColor: "#F4F6F6",
                //    clickable: true,
                //    hoverable: true
                //},
                tooltip: {
                    show: true,
                    content: function (label, x, y) {
                        var xValsVD = [];
                        for (var i = 0; i < nCurrDataVD; i++) {
                            xValsVD[i] = dataBVD[0].data[i][0];
                        };
                        var indiVD = xValsVD.indexOf(x);

                        var textVD = "<b>" + label + "<br/></b><i>Volatility Drag on " + xTicksHVD[indiVD][1] + "<br/></i>";
                        dataBVD.forEach(function (series) {
                            textVD += series.label + ' : ' + series.data[indiVD][1] + "%<br/>";
                        });

                        return textVD;
                    }
                },
                selection: {
                    mode: "x"
                }

            };

        

        placeholder.bind("plotselected", function (event, ranges) {

            $("#selection").text(ranges.xaxis.from.toFixed(1) + " to " + ranges.xaxis.to.toFixed(1));
                        
                $.each(plot.getXAxes(), function (_, axis) {
                    var opts = axis.options;
                    opts.min = ranges.xaxis.from;
                    opts.max = ranges.xaxis.to;
                    var xMin = Math.round(ranges.xaxis.from);
                    var xMax = Math.round(ranges.xaxis.to);
                    var yMax = 0;
                    var noLines = data.length;
                    for (var i = xMin; i < xMax + 1; i++)
                    {
                        for (var j = 0; j < noLines; j++) {
                            let v = data[j].data[i][1];
                            yMax = (v > yMax) ? v : yMax;
                           
                        }
                    }
                    plot.getAxes().yaxis.options.max = yMax*1.2;
                });
                var sdsd = plot.getAxes().yaxis.options.max;
                
                plot.setupGrid();
                plot.draw();
                plot.clearSelection();
         });

        placeholder.bind("plotunselected", function (event) {
            $("#selection").text("");
        });

        var plot = $.plot(placeholder, data, options);

        $("#clearSelection").click(plotAccordingToChoicesVD);

        
        //$("#placeholder2").bind("plotpan", function (event, plot) {
        //    var axes = plot.getAxes();
        //    $(".message").html("Panning to x: " + axes.xaxis.min.toFixed(2)
        //        + " &ndash; " + axes.xaxis.max.toFixed(2)
        //        + " and y: " + axes.yaxis.min.toFixed(2)
        //        + " &ndash; " + axes.yaxis.max.toFixed(2));
        //});

        //$("#placeholder2").bind("plotzoom", function (event, plot) {
        //    var axes = plot.getAxes();
        //    $(".message").html("Zooming to x: " + axes.xaxis.min.toFixed(2)
        //        + " &ndash; " + axes.xaxis.max.toFixed(2)
        //        + " and y: " + axes.yaxis.min.toFixed(2)
        //        + " &ndash; " + axes.yaxis.max.toFixed(2));
        //});
        //// add zoom out button 

        //$("<div class='button' style='right:20px;top:20px'>zoom out</div>")
        //    .appendTo(placeholder2)
        //    .click(function (event) {
        //        event.preventDefault();
        //        plot.zoomOut();
        //    });

        //// and add panning buttons

        //// little helper for taking the repetitive work out of placing
        //// panning arrows

        //function addArrow(dir, right, top, offset) {
        //    $("<img class='button' src='arrow-" + dir + ".gif' style='right:" + right + "px;top:" + top + "px'>")
        //        .appendTo(placeholder2)
        //        .click(function (e) {
        //            e.preventDefault();
        //            plot.pan(offset);
        //        });
        //}

        //addArrow("left", 55, 60, { left: -100 });
        //addArrow("right", 25, 60, { left: 100 });
        //addArrow("up", 40, 45, { top: -100 });
        //addArrow("down", 40, 75, { top: 100 });

    }
    plotAccordingToChoicesVD();
    //formatXTicks();
}

function formatXTicks() {
    var xWidth = $('.flot-x-axis').width();
    var xTicks = $('.flot-x-axis .flot-tick-label').length;
    var limiter = Math.floor(xWidth / xTicks);
    var xConst = 30;
    if (limiter < xConst) {
        $('.flot-x-axis .flot-tick-label').each(function (index) {
            if (index % Math.floor(xConst / limiter) > 0) {
                $(this).css("display", "none");
            }
        })
    }
}


  