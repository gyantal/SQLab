"use strict";
//As an example, in normal JavaScript, mistyping a variable name creates a new global variable. In strict mode, this will throw an error, making it impossible to accidentally create a global variable.
function onHeadProcessing() {
    console.log('onHeadProcessing()');

    var xmlHttp = new XMLHttpRequest();
    xmlHttp.onreadystatechange = function () {
        if (xmlHttp.readyState == 4 && xmlHttp.status == 200)
            processData(xmlHttp.responseText);
    };
    xmlHttp.open("GET", "/GChBeta", true); // true for asynchronous 
    xmlHttp.send(null);
}


function processData(dataStr) {
    //Printing error message from C#.  

    if (dataStr == "Error") {
        document.getElementById("waitingMessage").style.display = "none";
        var divErrorCont = document.getElementById("idErrorCont");
        divErrorCont.innerHTML = "Error during downloading data. Please, try again later!";
        document.getElementById("errorMessage").style.visibility = "visible";
        document.getElementById("inviCharts").style.visibility = "hidden";

        return;
    }

    document.getElementById("errorMessage").style.display="none";



    //Splitting data got from C#.
    var data = JSON.parse(dataStr);

    //Creating first rows of webpage.
    var divTimeNow = document.getElementById("idTimeNow");
    var divLiveDataTime = document.getElementById("idLiveDataTime");

    divTimeNow.innerHTML = data.requestTime;
    divLiveDataTime.innerHTML = data.lastDataTime;

    //var volAssetNamesArray = data.volAssetNames.split(", ");
    //var etpAssetNamesArray = data.etpAssetNames.split(", ");
    //var gchAssetNamesArray = data.gchAssetNames.split(", ");
    //var gmAssetNamesArray = data.gmAssetNames.split(", ");
    //var defCheckedListArray = data.defCheckedList.split(", ");



    //var chBxs = "<p class=\"left\"><button class=\"button2\" style=\"background: url(/images/vix.jpg); background-size: cover;\" title=\"Volatility ETPs\" onclick=\"choseall('volA')\"/></button>&emsp;&emsp;";
    //for (var iAssets = 0; iAssets < volAssetNamesArray.length; iAssets++) {
    //    chBxs += "<input class= \"szpari\" type=\"checkbox\" name=\"volA\" id=\"" + volAssetNamesArray[iAssets] + "\"/><a target=\"_blank\" href=\"https://finance.yahoo.com/quote/" + volAssetNamesArray[iAssets].split("_")[0] + "\">" + volAssetNamesArray[iAssets] + "</a> &emsp;";
    //}
    //chBxs += "<br><button class=\"button2\" style=\"background: url(/images/ImportantEtps.png); background-size: cover;\" title=\"Important ETPs\" onclick=\"choseall('etpA')\"></button>&emsp;&emsp;";
    //for (var iAssets = 0; iAssets < etpAssetNamesArray.length; iAssets++) {
    //    chBxs += "<input class= \"szpari\" type=\"checkbox\" name=\"etpA\" id=\"" + etpAssetNamesArray[iAssets] + "\"/><a target=\"_blank\" href=\"https://finance.yahoo.com/quote/" + etpAssetNamesArray[iAssets].split("_")[0] + "\">" + etpAssetNamesArray[iAssets] + "</a> &emsp;";
    //}
    //chBxs += "<br><button class=\"button2\" style=\"background: url(/images/GameChangers.png); background-size: cover;\" title=\"GameChanger Stocks\" onclick=\"choseall('gchA')\"></button>&emsp;&emsp;";
    //for (var iAssets = 0; iAssets < gchAssetNamesArray.length; iAssets++) {
    //    chBxs += "<input class= \"szpari\" type=\"checkbox\" name=\"gchA\" id=\"" + gchAssetNamesArray[iAssets] + "\"/><a target=\"_blank\" href=\"https://finance.yahoo.com/quote/" + gchAssetNamesArray[iAssets].split("_")[0] + "\">" + gchAssetNamesArray[iAssets] + "</a> &emsp;";
    //}
    //chBxs += "<br><button class=\"button2\" style=\"background: url(/images/GlobalAssets.png); background-size: cover;\" title=\"Global Assets\" onclick=\"choseall('gmA')\"></button>&emsp;&emsp;";
    //for (var iAssets = 0; iAssets < gmAssetNamesArray.length; iAssets++) {
    //    chBxs += "<input class= \"szpari\" type=\"checkbox\" name=\"gmA\" id=\"" + gmAssetNamesArray[iAssets] + "\" /><a target=\"_blank\" href=\"https://finance.yahoo.com/quote/" + gmAssetNamesArray[iAssets].split("_")[0] + "\">" + gmAssetNamesArray[iAssets] + "</a> &emsp;";
    //}
    //chBxs += "</p ><p class=\"center\"><button class=\"button3\" style=\"background: url(/images/selectall.png); background-size: cover;\" title=\"Select/Deselect All\" onclick=\"checkAll(this)\"/></button>&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;<button class=\"button3\" style=\"background: url(/images/updateall.png); background-size: cover;\" title=\"Update Charts and Tables\" id='update_all'></button></p> ";



    //var checkBoxes = document.getElementById("idChBxs");
    //checkBoxes.innerHTML = chBxs;

    //$("input:checkbox").each(function () {
    //    if (jQuery.inArray($(this).attr("id"), defCheckedListArray) !== -1) {
    //        $(this).prop('checked', true);
    //    }
    //    $(this).checked;
    //});

    creatingTables(data);



    //$("input:checkbox:not(:checked)").each(function () {
    //    var column = "table ." + $(this).attr("id");
    //    $(column).hide();
    //});

    //$("#update_all").click(function () {
    //    $("input:checkbox:not(:checked)").each(function () {
    //        var column = "table ." + $(this).attr("id");
    //        $(column).hide();
    //    });
    //    $("input:checkbox:checked").each(function () {
    //        var column = "table ." + $(this).attr("id");
    //        $(column).show();

    //    });
    //});

    ////$('.header').click(function () {
    ////    $(this).find('span').text(function (_, value) { return value == '-' ? '+' : '-' });
    ////    $(this).nextUntil('tr.header').slideToggle(100); // or just use "toggle()"
    ////}).click();


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
            $table.find('#expandAll').click(function () {
                $childRows.filter('.child').show();
            });

            $table.find('#hideAll').click(function () {
                $childRows.hide();
            });

        });
    });
    $("#lastYearT").click();
    $("#lastYearT1").click();
    $("#lastYearT2").click();
    $("#lastYearT3").click();
    $("#lastYearT4").click();
    $("#lastYearT5").click();

    
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

    document.getElementById("showHide").onclick = function () { showHide() };
    function showHide() {
        var x = document.getElementById("suppTables");
        if (x.style.display === "none") {
            x.style.display = "block";
        } else {
            x.style.display = "none";
        }
    }


    //Setting charts visible after getting data.
    document.getElementById("waitingMessage").style.display = "none";
    document.getElementById("suppTables").style.display = "none";
    document.getElementById("inviCharts").style.visibility = "visible";


}
function creatingTables(data) {


    //Creating JavaScript data arrays by splitting.
    var assetNamesArray = data.assetNames.split(", ");
    var assetNames2Array = data.assetNames2.split(", ");
    var dailyDatesArray = data.quotesDateVector.split(", ");
    var dailyDatesMEArray = data.quotesDateMEVector.split(", ");

    //var volLBPeriod = data.volLBPeri;

    //var dailyVolDragsTemp = data.dailyVolDrags.split("ß ");
    //var dailyVolDragsMtx = new Array();
    //for (var i = 0; i < dailyVolDragsTemp.length; i++) {
    //    dailyVolDragsMtx[i] = dailyVolDragsTemp[i].split(",");
    //}

    //var dailyVIXMasArray = data.dailyVIXMas.split(", ");
    var yearListArray = data.yearList.split(", ");
    var yearMonthListArray = data.yearMonthList.split(", ");

    var betaCalcQQQTemp = data.betaCalcQQQ.split("ß ");
    var betaCalcQQQMtx = new Array();
    for (var i = 0; i < betaCalcQQQTemp.length; i++) {
        betaCalcQQQMtx[i] = betaCalcQQQTemp[i].split(",");
    }

    for (var i = 0; i < betaCalcQQQTemp.length; i++) {
        for (var j = 0; j < betaCalcQQQMtx[0].length; j++) {
            if (betaCalcQQQMtx[i][j] == " 0" || betaCalcQQQMtx[i][j] == " NaN" || betaCalcQQQMtx[i][j] == "0" || betaCalcQQQMtx[i][j] == "NaN") {
                betaCalcQQQMtx[i][j] = "---";
            }
        }
    }

    var betaCalcSPYTemp = data.betaCalcSPY.split("ß ");
    var betaCalcSPYMtx = new Array();
    for (var i = 0; i < betaCalcSPYTemp.length; i++) {
        betaCalcSPYMtx[i] = betaCalcSPYTemp[i].split(",");
    }

    for (var i = 0; i < betaCalcSPYTemp.length; i++) {
        for (var j = 0; j < betaCalcSPYMtx[0].length; j++) {
            if (betaCalcSPYMtx[i][j] == " 0" || betaCalcSPYMtx[i][j] == " NaN" || betaCalcSPYMtx[i][j] == "0" || betaCalcSPYMtx[i][j] == "NaN") {
                betaCalcSPYMtx[i][j] = "---";
            }
        }
    }

    var betaCalcQQQCurrTemp = data.betaCalcQQQCurr.split("ß ");
    var betaCalcQQQCurrMtx = new Array();
    for (var i = 0; i < betaCalcQQQCurrTemp.length; i++) {
        betaCalcQQQCurrMtx[i] = betaCalcQQQCurrTemp[i].split(",");
    }

    for (var i = 0; i < betaCalcQQQCurrTemp.length; i++) {
        for (var j = 0; j < betaCalcQQQCurrMtx[0].length; j++) {
            if (betaCalcQQQCurrMtx[i][j] == " 0" || betaCalcQQQCurrMtx[i][j] == " NaN" || betaCalcQQQCurrMtx[i][j] == "0" || betaCalcQQQCurrMtx[i][j] == "NaN") {
                betaCalcQQQCurrMtx[i][j] = "---";
            }
        }
    }

    var betaCalcSPYCurrTemp = data.betaCalcSPYCurr.split("ß ");
    var betaCalcSPYCurrMtx = new Array();
    for (var i = 0; i < betaCalcSPYCurrTemp.length; i++) {
        betaCalcSPYCurrMtx[i] = betaCalcSPYCurrTemp[i].split(",");
    }

    for (var i = 0; i < betaCalcSPYCurrTemp.length; i++) {
        for (var j = 0; j < betaCalcSPYCurrMtx[0].length; j++) {
            if (betaCalcSPYCurrMtx[i][j] == " 0" || betaCalcSPYCurrMtx[i][j] == " NaN" || betaCalcSPYCurrMtx[i][j] == "0" || betaCalcSPYCurrMtx[i][j] == "NaN") {
                betaCalcSPYCurrMtx[i][j] = "---";
            }
        }
    }

    var yearlyBetasQQQTemp = data.yearlyBetasQQQ.split("ß ");
    var yearlyBetasQQQMtx = new Array();
    for (var i = 0; i < yearlyBetasQQQTemp.length; i++) {
        yearlyBetasQQQMtx[i] = yearlyBetasQQQTemp[i].split(",");
    }

    for (var i = 0; i < yearlyBetasQQQTemp.length; i++) {
        for (var j = 0; j < yearlyBetasQQQMtx[0].length; j++) {
            if (yearlyBetasQQQMtx[i][j] == " 0" || yearlyBetasQQQMtx[i][j] == " NaN" || yearlyBetasQQQMtx[i][j] == "0" || yearlyBetasQQQMtx[i][j] == "NaN") {
                yearlyBetasQQQMtx[i][j] = "---";
            }
        }
    }

    var monthlyBetasQQQTemp = data.monthlyBetasQQQ.split("ß ");
    var monthlyBetasQQQMtx = new Array();
    for (var i = 0; i < monthlyBetasQQQTemp.length; i++) {
        monthlyBetasQQQMtx[i] = monthlyBetasQQQTemp[i].split(",");
    }

    for (var i = 0; i < monthlyBetasQQQTemp.length; i++) {
        for (var j = 0; j < monthlyBetasQQQMtx[0].length; j++) {
            if (monthlyBetasQQQMtx[i][j] == " 0" || monthlyBetasQQQMtx[i][j] == " NaN" || monthlyBetasQQQMtx[i][j] == "0" || monthlyBetasQQQMtx[i][j] == "NaN") {
                monthlyBetasQQQMtx[i][j] = "---";
            }
        }
    }

    var yearlyBetasSPYTemp = data.yearlyBetasSPY.split("ß ");
    var yearlyBetasSPYMtx = new Array();
    for (var i = 0; i < yearlyBetasSPYTemp.length; i++) {
        yearlyBetasSPYMtx[i] = yearlyBetasSPYTemp[i].split(",");
    }

    for (var i = 0; i < yearlyBetasSPYTemp.length; i++) {
        for (var j = 0; j < yearlyBetasSPYMtx[0].length; j++) {
            if (yearlyBetasSPYMtx[i][j] == " 0" || yearlyBetasSPYMtx[i][j] == " NaN" || yearlyBetasSPYMtx[i][j] == "0" || yearlyBetasSPYMtx[i][j] == "NaN") {
                yearlyBetasSPYMtx[i][j] = "---";
            }
        }
    }

    var monthlyBetasSPYTemp = data.monthlyBetasSPY.split("ß ");
    var monthlyBetasSPYMtx = new Array();
    for (var i = 0; i < monthlyBetasSPYTemp.length; i++) {
        monthlyBetasSPYMtx[i] = monthlyBetasSPYTemp[i].split(",");
    }

    for (var i = 0; i < monthlyBetasSPYTemp.length; i++) {
        for (var j = 0; j < monthlyBetasSPYMtx[0].length; j++) {
            if (monthlyBetasSPYMtx[i][j] == " 0" || monthlyBetasSPYMtx[i][j] == " NaN" || monthlyBetasSPYMtx[i][j] == "0" || monthlyBetasSPYMtx[i][j] == "NaN") {
                monthlyBetasSPYMtx[i][j] = "---";
            }
        }
    }

    var yearlyPosBetasQQQTemp = data.yearlyPosBetasQQQ.split("ß ");
    var yearlyPosBetasQQQMtx = new Array();
    for (var i = 0; i < yearlyPosBetasQQQTemp.length; i++) {
        yearlyPosBetasQQQMtx[i] = yearlyPosBetasQQQTemp[i].split(",");
    }

    for (var i = 0; i < yearlyPosBetasQQQTemp.length; i++) {
        for (var j = 0; j < yearlyPosBetasQQQMtx[0].length; j++) {
            if (yearlyPosBetasQQQMtx[i][j] == " 0" || yearlyPosBetasQQQMtx[i][j] == " NaN" || yearlyPosBetasQQQMtx[i][j] == "0" || yearlyPosBetasQQQMtx[i][j] == "NaN") {
                yearlyPosBetasQQQMtx[i][j] = "---";
            }
        }
    }

    var monthlyPosBetasQQQTemp = data.monthlyPosBetasQQQ.split("ß ");
    var monthlyPosBetasQQQMtx = new Array();
    for (var i = 0; i < monthlyPosBetasQQQTemp.length; i++) {
        monthlyPosBetasQQQMtx[i] = monthlyPosBetasQQQTemp[i].split(",");
    }

    for (var i = 0; i < monthlyPosBetasQQQTemp.length; i++) {
        for (var j = 0; j < monthlyPosBetasQQQMtx[0].length; j++) {
            if (monthlyPosBetasQQQMtx[i][j] == " 0" || monthlyPosBetasQQQMtx[i][j] == " NaN" || monthlyPosBetasQQQMtx[i][j] == "0" || monthlyPosBetasQQQMtx[i][j] == "NaN") {
                monthlyPosBetasQQQMtx[i][j] = "---";
            }
        }
    }

    var yearlyNegBetasQQQTemp = data.yearlyNegBetasQQQ.split("ß ");
    var yearlyNegBetasQQQMtx = new Array();
    for (var i = 0; i < yearlyNegBetasQQQTemp.length; i++) {
        yearlyNegBetasQQQMtx[i] = yearlyNegBetasQQQTemp[i].split(",");
    }

    for (var i = 0; i < yearlyNegBetasQQQTemp.length; i++) {
        for (var j = 0; j < yearlyNegBetasQQQMtx[0].length; j++) {
            if (yearlyNegBetasQQQMtx[i][j] == " 0" || yearlyNegBetasQQQMtx[i][j] == " NaN" || yearlyNegBetasQQQMtx[i][j] == "0" || yearlyNegBetasQQQMtx[i][j] == "NaN") {
                yearlyNegBetasQQQMtx[i][j] = "---";
            }
        }
    }

    var monthlyNegBetasQQQTemp = data.monthlyNegBetasQQQ.split("ß ");
    var monthlyNegBetasQQQMtx = new Array();
    for (var i = 0; i < monthlyNegBetasQQQTemp.length; i++) {
        monthlyNegBetasQQQMtx[i] = monthlyNegBetasQQQTemp[i].split(",");
    }

    for (var i = 0; i < monthlyNegBetasQQQTemp.length; i++) {
        for (var j = 0; j < monthlyNegBetasQQQMtx[0].length; j++) {
            if (monthlyNegBetasQQQMtx[i][j] == " 0" || monthlyNegBetasQQQMtx[i][j] == " NaN" || monthlyNegBetasQQQMtx[i][j] == "0" || monthlyNegBetasQQQMtx[i][j] == "NaN") {
                monthlyNegBetasQQQMtx[i][j] = "---";
            }
        }
    }


    var yearlyPosBetasSPYTemp = data.yearlyPosBetasSPY.split("ß ");
    var yearlyPosBetasSPYMtx = new Array();
    for (var i = 0; i < yearlyPosBetasSPYTemp.length; i++) {
        yearlyPosBetasSPYMtx[i] = yearlyPosBetasSPYTemp[i].split(",");
    }

    for (var i = 0; i < yearlyPosBetasSPYTemp.length; i++) {
        for (var j = 0; j < yearlyPosBetasSPYMtx[0].length; j++) {
            if (yearlyPosBetasSPYMtx[i][j] == " 0" || yearlyPosBetasSPYMtx[i][j] == " NaN" || yearlyPosBetasSPYMtx[i][j] == "0" || yearlyPosBetasSPYMtx[i][j] == "NaN") {
                yearlyPosBetasSPYMtx[i][j] = "---";
            }
        }
    }

    var monthlyPosBetasSPYTemp = data.monthlyPosBetasSPY.split("ß ");
    var monthlyPosBetasSPYMtx = new Array();
    for (var i = 0; i < monthlyPosBetasSPYTemp.length; i++) {
        monthlyPosBetasSPYMtx[i] = monthlyPosBetasSPYTemp[i].split(",");
    }

    for (var i = 0; i < monthlyPosBetasSPYTemp.length; i++) {
        for (var j = 0; j < monthlyPosBetasSPYMtx[0].length; j++) {
            if (monthlyPosBetasSPYMtx[i][j] == " 0" || monthlyPosBetasSPYMtx[i][j] == " NaN" || monthlyPosBetasSPYMtx[i][j] == "0" || monthlyPosBetasSPYMtx[i][j] == "NaN") {
                monthlyPosBetasSPYMtx[i][j] = "---";
            }
        }
    }

    var yearlyNegBetasSPYTemp = data.yearlyNegBetasSPY.split("ß ");
    var yearlyNegBetasSPYMtx = new Array();
    for (var i = 0; i < yearlyNegBetasSPYTemp.length; i++) {
        yearlyNegBetasSPYMtx[i] = yearlyNegBetasSPYTemp[i].split(",");
    }

    for (var i = 0; i < yearlyNegBetasSPYTemp.length; i++) {
        for (var j = 0; j < yearlyNegBetasSPYMtx[0].length; j++) {
            if (yearlyNegBetasSPYMtx[i][j] == " 0" || yearlyNegBetasSPYMtx[i][j] == " NaN" || yearlyNegBetasSPYMtx[i][j] == "0" || yearlyNegBetasSPYMtx[i][j] == "NaN") {
                yearlyNegBetasSPYMtx[i][j] = "---";
            }
        }
    }

    var monthlyNegBetasSPYTemp = data.monthlyNegBetasSPY.split("ß ");
    var monthlyNegBetasSPYMtx = new Array();
    for (var i = 0; i < monthlyNegBetasSPYTemp.length; i++) {
        monthlyNegBetasSPYMtx[i] = monthlyNegBetasSPYTemp[i].split(",");
    }

    for (var i = 0; i < monthlyNegBetasSPYTemp.length; i++) {
        for (var j = 0; j < monthlyNegBetasSPYMtx[0].length; j++) {
            if (monthlyNegBetasSPYMtx[i][j] == " 0" || monthlyNegBetasSPYMtx[i][j] == " NaN" || monthlyNegBetasSPYMtx[i][j] == "0" || monthlyNegBetasSPYMtx[i][j] == "NaN") {
                monthlyNegBetasSPYMtx[i][j] = "---";
            }
        }
    }

    //var yearlyVIXAvgsArray = data.yearlyVIXAvgs.split(", ");
    //var monthlyVIXAvgsArray = data.monthlyVIXAvgs.split(", ");
    var yearlyCountsArray = data.yearlyCounts.split(", ");
    var monthlyCountsArray = data.monthlyCounts.split(", ");
    var yearlyQQQPosCountsArray = data.yearlyQQQPosCounts.split(", ");
    var monthlyQQQPosCountsArray = data.monthlyQQQPosCounts.split(", ");
    var yearlyQQQNegCountsArray = data.yearlyQQQNegCounts.split(", ");
    var monthlyQQQNegCountsArray = data.monthlyQQQNegCounts.split(", ");
    var yearlySPYPosCountsArray = data.yearlySPYPosCounts.split(", ");
    var monthlySPYPosCountsArray = data.monthlySPYPosCounts.split(", ");
    var yearlySPYNegCountsArray = data.yearlySPYNegCounts.split(", ");
    var monthlySPYNegCountsArray = data.monthlySPYNegCounts.split(", ");
    var totDays = data.noTotalDays;
    var totQQQPosDays = data.noTotalQQQPosDays;
    var totQQQNegDays = data.noTotalQQQNegDays;
    var totSPYPosDays = data.noTotalSPYPosDays;
    var totSPYNegDays = data.noTotalSPYNegDays;
    //var vixAvgTot = data.vixAvgTotal;
    var betaQQQTotalArray = data.betaQQQTotalVec.split(", ");
    var betaSPYTotalArray = data.betaSPYTotalVec.split(", ");
    var betaQQQTotalPosArray = data.betaQQQTotalPosVec.split(", ");
    var betaQQQTotalNegArray = data.betaQQQTotalNegVec.split(", ");
    var betaSPYTotalPosArray = data.betaSPYTotalPosVec.split(", ");
    var betaSPYTotalNegArray = data.betaSPYTotalNegVec.split(", ");
    var noColumns = assetNamesArray.length + 3;

    var noInnerYears = yearListArray.length - 2;
    var noLastYearMonths = yearMonthListArray.length - 12 - noInnerYears * 12;

    var retHistLBPeriods = data.retLBPeris.split(", ");
    var retHistLBPeriodsNoS = data.retLBPerisNo.split(", ");
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

    var betaLBPeriods = data.betaLBPeris.split(", ");
    //var noLBPeriods = betaLBPeriods.length;

    var rSqArray = data.reliabRS.split(", ");

    //Creating the HTML code of tables.

    //var currTableMtx = "<table class=\"currDataB\"><tr align=\"center\"><td colspan=\"" + (noColumns - 1) + "\" bgcolor=\"#66CCFF\"><b>Current Monthly Volatility Drag</b></td></tr><tr align=\"center\"><td bgcolor=\"#66CCFF\">Date</td><td class=\"first_name\" bgcolor=\"#66CCFF\">VIX MA(" + volLBPeriod + ")</td>";
    //for (var i = 0; i < assetNamesArray.length - 1; i++) {
    //    currTableMtx += "<td class=\"" + assetNamesArray[i] + "\" bgcolor=\"#66CCFF\">" + assetNamesArray[i] + "</td>";
    //}
    //currTableMtx += "<td class=\"" + assetNamesArray[assetNamesArray.length - 1] + "\" bgcolor=\"#66CCFF\">" + assetNamesArray[assetNamesArray.length - 1] + "</td></tr>";
    //currTableMtx += "<tr align=\"center\"><td>" + dailyDatesArray[dailyDatesArray.length - 1] + "</td>";
    //currTableMtx += "<td class=\"first_name\">" + dailyVIXMasArray[dailyVIXMasArray.length - 1] + "</td>";
    //for (var i = 0; i < assetNamesArray.length; i++) {
    //    currTableMtx += "<td class=\"" + assetNamesArray[i] + "\">" + dailyVolDragsMtx[dailyVolDragsMtx.length - 1][i] + "</td>";
    //}
    //currTableMtx += "</tr></table>";

    var currTableMtxa = "<table class=\"currDataB\"><tr align=\"center\"><td colspan=\"" + noColumns + "\" bgcolor=\"#66CCFF\"><b>Current Daily Beta of GameChanger Stocks in Comparison to QQQ <BR> (<a href=\"https://www.w3schools.com\">Optimal Monthly-izer Multiplicator: 120%</a>)</b></td></tr><tr align=\"center\"><td colspan=\"2\" bgcolor=\"#66CCFF\">Lookback Period</td>";
    for (var i = 0; i < assetNames2Array.length - 1; i++) {
        currTableMtxa += "<td class=\"" + assetNames2Array[i] + "\" bgcolor=\"#66CCFF\">" + assetNames2Array[i] + "</td>";
    }
    currTableMtxa += "<td class=\"" + assetNames2Array[assetNames2Array.length - 1] + "\" bgcolor=\"#66CCFF\">" + assetNames2Array[assetNames2Array.length - 1] + "</td></tr>";
    for (var j = 0; j < 6; j++) {
        currTableMtxa += "<tr align=\"center\"><td colspan=\"2\">" + betaLBPeriods[j] + "</td>";
        for (var i = 0; i < assetNames2Array.length; i++) {
            currTableMtxa += "<td class=\"" + assetNames2Array[i] + "\">" + betaCalcQQQCurrMtx[j][i] + "</td>";
        }
        currTableMtxa += "</tr>";
    }
    currTableMtxa += "<tr align=\"center\"  style=\"font-weight: bold\"><td rowspan=\"3\">" + betaLBPeriods[6] + "</td><td>Daily Beta</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtxa += "<td class=\"" + assetNames2Array[i] + "\">" + betaCalcQQQCurrMtx[6][i] + "</td>";
    }
    currTableMtxa += "</tr><tr style=\"font-weight: bold; color: blue; font-style: italic;font-size:95%;\"><td>Reliability (R<sup>2</sup>)</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtxa += "<td class=\"" + assetNames2Array[i] + "\">" + rSqArray[i] + "</td>";
    }
    currTableMtxa += "</tr><tr style=\"font-weight: bold; color: navy; font-style: italic;font-size:95%;\"><td>Monthly-ized Beta</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtxa += "<td class=\"" + assetNames2Array[i] + "\">" + Math.round(betaCalcQQQCurrMtx[6][i] * 1.2 * 100) / 100 + "</td>";
    }
    currTableMtxa += "</tr>";
    for (var j = 7; j < betaLBPeriods.length; j++) {
        currTableMtxa += "<tr align=\"center\"><td colspan=\"2\">" + betaLBPeriods[j] + "</td>";
        for (var i = 0; i < assetNames2Array.length; i++) {
            currTableMtxa += "<td class=\"" + assetNames2Array[i] + "\">" + betaCalcQQQCurrMtx[j][i] + "</td>";
        }
        currTableMtxa += "</tr>";
    }
    currTableMtxa += "</table>";

    var currTableMtxb = "<table class=\"currDataB\"><tr align=\"center\"><td colspan=\"" + noColumns + "\" bgcolor=\"#66CCFF\"><b>Current Daily Beta of GameChanger Stocks in Comparison to SPY</b></td></tr><tr align=\"center\"><td bgcolor=\"#66CCFF\">Lookback Period</td>";
    for (var i = 0; i < assetNames2Array.length - 1; i++) {
        currTableMtxb += "<td class=\"" + assetNames2Array[i] + "\" bgcolor=\"#66CCFF\">" + assetNames2Array[i] + "</td>";
    }
    currTableMtxb += "<td class=\"" + assetNames2Array[assetNames2Array.length - 1] + "\" bgcolor=\"#66CCFF\">" + assetNames2Array[assetNames2Array.length - 1] + "</td></tr>";
    for (var j = 0; j < 6; j++) {
        currTableMtxb += "<tr align=\"center\"><td>" + betaLBPeriods[j] + "</td>";
        for (var i = 0; i < assetNames2Array.length; i++) {
            currTableMtxb += "<td class=\"" + assetNames2Array[i] + "\">" + betaCalcSPYCurrMtx[j][i] + "</td>";
        }
        currTableMtxb += "</tr>";
    }
    currTableMtxb += "<tr align=\"center\" style=\"font-weight: bold\"><td>" + betaLBPeriods[6] + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtxb += "<td class=\"" + assetNames2Array[i] + "\">" + betaCalcSPYCurrMtx[6][i] + "</td>";
    }
    currTableMtxb += "</tr>";
    for (var j = 7; j < betaLBPeriods.length; j++) {
        currTableMtxb += "<tr align=\"center\"><td>" + betaLBPeriods[j] + "</td>";
        for (var i = 0; i < assetNames2Array.length; i++) {
            currTableMtxb += "<td class=\"" + assetNames2Array[i] + "\">" + betaCalcSPYCurrMtx[j][i] + "</td>";
        }
        currTableMtxb += "</tr>";
    }
    currTableMtxb += "</table>";


    var currTableMtx3a = "<table class=\"currData\"><thead><tr align=\"center\" ><td colspan=\"" + noColumns + "\" bgcolor=\"#66CCFF\"><b>Daily Beta of GameChanger Stocks in Comparison to QQQ by Years and Months</b></td></tr><tr align=\"center\" class=\"parent2\"><td bgcolor=\"#66CCFF\"><span class=\"years\" id=\"hideAll\">Only Years</span>/ <span class=\"years\" id=\"expandAll\">Years+Months</span></td><td bgcolor=\"#66CCFF\">No. Days</td>";
    for (var i = 0; i < assetNames2Array.length - 1; i++) {
        currTableMtx3a += "<td class=\"" + assetNames2Array[i] + "\" bgcolor=\"#66CCFF\">" + assetNames2Array[i] + "</td>";
    }
    currTableMtx3a += "<td class=\"" + assetNames2Array[assetNames2Array.length - 1] + "\" bgcolor=\"#66CCFF\">" + assetNames2Array[assetNames2Array.length - 1] + "</td></tr></thead>";
    currTableMtx3a += "<tbody><tr class=\"parent\"><td><span class=\"years\">" + yearListArray[0] + "</span></td><td>" + yearlyCountsArray[0] + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3a += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyBetasQQQMtx[0][i] + "</td>";
    }
    for (var i = 1; i < 12; i++) {
        currTableMtx3a += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[i] + "&emsp;</i></td><td><i>" + monthlyCountsArray[i] + "</i></td>";
        for (var j = 0; j < assetNames2Array.length; j++) {
            currTableMtx3a += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyBetasQQQMtx[i][j] + "</i></td>";
        }
        currTableMtx3a += "</tr>";
    }
    for (var k = 0; k < noInnerYears; k++) {
        currTableMtx3a += "<tr class=\"parent\"><td><span class=\"years\">" + yearListArray[k + 1] + "</span></td><td>" + yearlyCountsArray[k + 1] + "</td>";
        for (var i = 0; i < assetNames2Array.length; i++) {
            currTableMtx3a += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyBetasQQQMtx[k + 1][i] + "</td>";
        }
        for (var i = 0; i < 12; i++) {
            currTableMtx3a += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[12 + k * 12 + i] + "&emsp;</i></td><td><i>" + monthlyCountsArray[12 + k * 12 + i] + "</i></td>";
            for (var j = 0; j < assetNames2Array.length; j++) {
                currTableMtx3a += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyBetasQQQMtx[12 + k * 12 + i][j] + "</i></td>";
            }
           currTableMtx3a += "</tr>";
        }
    }
    currTableMtx3a += "<tr class=\"parent\" id=\"lastYearT\"><td><span class=\"years\">" + yearListArray[yearListArray.length - 1] + "</span></td><td>" + yearlyCountsArray[yearListArray.length - 1] + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3a += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyBetasQQQMtx[yearListArray.length - 1][i] + "</td>";
    }
    for (var i = 0; i < noLastYearMonths; i++) {
        currTableMtx3a += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[12 + noInnerYears * 12 + i] + "&emsp;</i></td><td><i>" + monthlyCountsArray[12 + noInnerYears * 12 + i] + "</i></td>";
        for (var j = 0; j < assetNames2Array.length; j++) {
            currTableMtx3a += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyBetasQQQMtx[12 + noInnerYears * 12 + i][j] + "</i></td>";
        }
        currTableMtx3a += "</tr>";
    }
    currTableMtx3a += "<tr class=\"parent\" style=\"cursor: text\"><td><span class=\"total\">Total 2004-" + yearListArray[yearListArray.length - 1] + "</span></td><td>" + totDays + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3a += "<td class=\"" + assetNames2Array[i] + "\">" + betaQQQTotalArray[i] + "</td>";
    }
    currTableMtx3a += "</tr></tbody></table>";
    

    var currTableMtx3b = "<table class=\"currData\"><thead><tr align=\"center\" ><td colspan=\"" + noColumns + "\" bgcolor=\"#66CCFF\"><b>Daily Beta of GameChanger Stocks in Comparison to SPY by Years and Months</b></td></tr><tr align=\"center\"><td bgcolor=\"#66CCFF\"><span class=\"years\" id=\"hideAll\">Only Years</span>/ <span class=\"years\" id=\"expandAll\">Years+Months</span></td><td bgcolor=\"#66CCFF\">No. Days</td>";
    for (var i = 0; i < assetNames2Array.length - 1; i++) {
        currTableMtx3b += "<td class=\"" + assetNames2Array[i] + "\" bgcolor=\"#66CCFF\">" + assetNames2Array[i] + "</td>";
    }
    currTableMtx3b += "<td class=\"" + assetNames2Array[assetNames2Array.length - 1] + "\" bgcolor=\"#66CCFF\">" + assetNames2Array[assetNames2Array.length - 1] + "</td></tr></thead>";
    currTableMtx3b += "<tbody><tr class=\"parent\"><td><span class=\"years\">" + yearListArray[0] + "</span></td><td>" + yearlyCountsArray[0] + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3b += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyBetasSPYMtx[0][i] + "</td>";
    }
    for (var i = 1; i < 12; i++) {
        currTableMtx3b += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[i] + "&emsp;</i></td><td><i>" + monthlyCountsArray[i] + "</i></td>";
        for (var j = 0; j < assetNames2Array.length; j++) {
            currTableMtx3b += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyBetasSPYMtx[i][j] + "</i></td>";
        }
        currTableMtx3b += "</tr>";
    }
    for (var k = 0; k < noInnerYears; k++) {
        currTableMtx3b += "<tr class=\"parent\"><td><span class=\"years\">" + yearListArray[k + 1] + "</span></td><td>" + yearlyCountsArray[k + 1] + "</td>";
        for (var i = 0; i < assetNames2Array.length; i++) {
            currTableMtx3b += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyBetasSPYMtx[k + 1][i] + "</td>";
        }
        for (var i = 0; i < 12; i++) {
            currTableMtx3b += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[12 + k * 12 + i] + "&emsp;</i></td><td><i>" + monthlyCountsArray[12 + k * 12 + i] + "</i></td>";
            for (var j = 0; j < assetNames2Array.length; j++) {
                currTableMtx3b += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyBetasSPYMtx[12 + k * 12 + i][j] + "</i></td>";
            }
            currTableMtx3b += "</tr>";
        }
    }
    currTableMtx3b += "<tr class=\"parent\" id=\"lastYearT1\"><td><span class=\"years\">" + yearListArray[yearListArray.length - 1] + "</span></td><td>" + yearlyCountsArray[yearListArray.length - 1] + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3b += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyBetasSPYMtx[yearListArray.length - 1][i] + "</td>";
    }
    for (var i = 0; i < noLastYearMonths; i++) {
        currTableMtx3b += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[12 + noInnerYears * 12 + i] + "&emsp;</i></td><td><i>" + monthlyCountsArray[12 + noInnerYears * 12 + i] + "</i></td>";
        for (var j = 0; j < assetNames2Array.length; j++) {
            currTableMtx3b += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyBetasSPYMtx[12 + noInnerYears * 12 + i][j] + "</i></td>";
        }
        currTableMtx3b += "</tr>";
    }
    currTableMtx3b += "<tr class=\"parent\" style=\"cursor: text\"><td><span class=\"total\">Total 2004-" + yearListArray[yearListArray.length - 1] + "</span></td><td>" + totDays + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3b += "<td class=\"" + assetNames2Array[i] + "\">" + betaSPYTotalArray[i] + "</td>";
    }
    currTableMtx3b += "</tr></tbody></table>";

    var currTableMtx3c = "<table class=\"currData\"><thead><tr align=\"center\" ><td colspan=\"" + noColumns + "\" bgcolor=\"#66CCFF\"><b>Daily Beta of GameChanger Stocks in Comparison to QQQ by Years and Months - On Positive QQQ Days</b></td></tr><tr align=\"center\" class=\"parent2\"><td bgcolor=\"#66CCFF\"><span class=\"years\" id=\"hideAll\">Only Years</span>/ <span class=\"years\" id=\"expandAll\">Years+Months</span></td><td bgcolor=\"#66CCFF\">No. Positive Days</td>";
    for (var i = 0; i < assetNames2Array.length - 1; i++) {
        currTableMtx3c += "<td class=\"" + assetNames2Array[i] + "\" bgcolor=\"#66CCFF\">" + assetNames2Array[i] + "</td>";
    }
    currTableMtx3c += "<td class=\"" + assetNames2Array[assetNames2Array.length - 1] + "\" bgcolor=\"#66CCFF\">" + assetNames2Array[assetNames2Array.length - 1] + "</td></tr></thead>";
    currTableMtx3c += "<tbody><tr class=\"parent\"><td><span class=\"years\">" + yearListArray[0] + "</span></td><td>" + yearlyQQQPosCountsArray[0] + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3c += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyPosBetasQQQMtx[0][i] + "</td>";
    }
    for (var i = 1; i < 12; i++) {
        currTableMtx3c += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[i] + "&emsp;</i></td><td><i>" + monthlyQQQPosCountsArray[i] + "</i></td>";
        for (var j = 0; j < assetNames2Array.length; j++) {
            currTableMtx3c += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyPosBetasQQQMtx[i][j] + "</i></td>";
        }
        currTableMtx3c += "</tr>";
    }
    for (var k = 0; k < noInnerYears; k++) {
        currTableMtx3c += "<tr class=\"parent\"><td><span class=\"years\">" + yearListArray[k + 1] + "</span></td><td>" + yearlyQQQPosCountsArray[k + 1] + "</td>";
        for (var i = 0; i < assetNames2Array.length; i++) {
            currTableMtx3c += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyPosBetasQQQMtx[k + 1][i] + "</td>";
        }
        for (var i = 0; i < 12; i++) {
            currTableMtx3c += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[12 + k * 12 + i] + "&emsp;</i></td><td><i>" + monthlyQQQPosCountsArray[12 + k * 12 + i] + "</i></td>";
            for (var j = 0; j < assetNames2Array.length; j++) {
                currTableMtx3c += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyPosBetasQQQMtx[12 + k * 12 + i][j] + "</i></td>";
            }
            currTableMtx3c += "</tr>";
        }
    }
    currTableMtx3c += "<tr class=\"parent\" id=\"lastYearT2\"><td><span class=\"years\">" + yearListArray[yearListArray.length - 1] + "</span></td><td>" + yearlyQQQPosCountsArray[yearListArray.length - 1] + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3c += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyPosBetasQQQMtx[yearListArray.length - 1][i] + "</td>";
    }
    for (var i = 0; i < noLastYearMonths; i++) {
        currTableMtx3c += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[12 + noInnerYears * 12 + i] + "&emsp;</i></td><td><i>" + monthlyQQQPosCountsArray[12 + noInnerYears * 12 + i] + "</i></td>";
        for (var j = 0; j < assetNames2Array.length; j++) {
            currTableMtx3c += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyPosBetasQQQMtx[12 + noInnerYears * 12 + i][j] + "</i></td>";
        }
        currTableMtx3c += "</tr>";
    }
    currTableMtx3c += "<tr class=\"parent\" style=\"cursor: text\"><td><span class=\"total\">Total 2004-" + yearListArray[yearListArray.length - 1] + "</span></td><td>" + totQQQPosDays + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3c += "<td class=\"" + assetNames2Array[i] + "\">" + betaQQQTotalPosArray[i] + "</td>";
    }
    currTableMtx3c += "</tr></tbody></table>";
    
    var currTableMtx3d = "<table class=\"currData\"><thead><tr align=\"center\" ><td colspan=\"" + noColumns + "\" bgcolor=\"#66CCFF\"><b>Daily Beta of GameChanger Stocks in Comparison to SPY by Years and Months - On Positive SPY Days</b></td></tr><tr align=\"center\" class=\"parent2\"><td bgcolor=\"#66CCFF\"><span class=\"years\" id=\"hideAll\">Only Years</span>/ <span class=\"years\" id=\"expandAll\">Years+Months</span></td><td bgcolor=\"#66CCFF\">No. Positive Days</td>";
    for (var i = 0; i < assetNames2Array.length - 1; i++) {
        currTableMtx3d += "<td class=\"" + assetNames2Array[i] + "\" bgcolor=\"#66CCFF\">" + assetNames2Array[i] + "</td>";
    }
    currTableMtx3d += "<td class=\"" + assetNames2Array[assetNames2Array.length - 1] + "\" bgcolor=\"#66CCFF\">" + assetNames2Array[assetNames2Array.length - 1] + "</td></tr></thead>";
    currTableMtx3d += "<tbody><tr class=\"parent\"><td><span class=\"years\">" + yearListArray[0] + "</span></td><td>" + yearlySPYPosCountsArray[0] + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3d += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyPosBetasSPYMtx[0][i] + "</td>";
    }
    for (var i = 1; i < 12; i++) {
        currTableMtx3d += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[i] + "&emsp;</i></td><td><i>" + monthlySPYPosCountsArray[i] + "</i></td>";
        for (var j = 0; j < assetNames2Array.length; j++) {
            currTableMtx3d += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyPosBetasSPYMtx[i][j] + "</i></td>";
        }
        currTableMtx3d += "</tr>";
    }
    for (var k = 0; k < noInnerYears; k++) {
        currTableMtx3d += "<tr class=\"parent\"><td><span class=\"years\">" + yearListArray[k + 1] + "</span></td><td>" + yearlySPYPosCountsArray[k + 1] + "</td>";
        for (var i = 0; i < assetNames2Array.length; i++) {
            currTableMtx3d += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyPosBetasSPYMtx[k + 1][i] + "</td>";
        }
        for (var i = 0; i < 12; i++) {
            currTableMtx3d += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[12 + k * 12 + i] + "&emsp;</i></td><td><i>" + monthlySPYPosCountsArray[12 + k * 12 + i] + "</i></td>";
            for (var j = 0; j < assetNames2Array.length; j++) {
                currTableMtx3d += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyPosBetasSPYMtx[12 + k * 12 + i][j] + "</i></td>";
            }
            currTableMtx3d += "</tr>";
        }
    }
    currTableMtx3d += "<tr class=\"parent\" id=\"lastYearT3\"><td><span class=\"years\">" + yearListArray[yearListArray.length - 1] + "</span></td><td>" + yearlySPYPosCountsArray[yearListArray.length - 1] + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3d += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyPosBetasSPYMtx[yearListArray.length - 1][i] + "</td>";
    }
    for (var i = 0; i < noLastYearMonths; i++) {
        currTableMtx3d += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[12 + noInnerYears * 12 + i] + "&emsp;</i></td><td><i>" + monthlySPYPosCountsArray[12 + noInnerYears * 12 + i] + "</i></td>";
        for (var j = 0; j < assetNames2Array.length; j++) {
            currTableMtx3d += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyPosBetasSPYMtx[12 + noInnerYears * 12 + i][j] + "</i></td>";
        }
        currTableMtx3d += "</tr>";
    }
    currTableMtx3d += "<tr class=\"parent\" style=\"cursor: text\"><td><span class=\"total\">Total 2004-" + yearListArray[yearListArray.length - 1] + "</span></td><td>" + totSPYPosDays + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3d += "<td class=\"" + assetNames2Array[i] + "\">" + betaSPYTotalPosArray[i] + "</td>";
    }
    currTableMtx3d += "</tr></tbody></table>";

    var currTableMtx3e = "<table class=\"currData\"><thead><tr align=\"center\" ><td colspan=\"" + noColumns + "\" bgcolor=\"#66CCFF\"><b>Daily Beta of GameChanger Stocks in Comparison to QQQ by Years and Months - On Negative QQQ Days</b></td></tr><tr align=\"center\" class=\"parent2\"><td bgcolor=\"#66CCFF\"><span class=\"years\" id=\"hideAll\">Only Years</span>/ <span class=\"years\" id=\"expandAll\">Years+Months</span></td><td bgcolor=\"#66CCFF\">No. Negative Days</td>";
    for (var i = 0; i < assetNames2Array.length - 1; i++) {
        currTableMtx3e += "<td class=\"" + assetNames2Array[i] + "\" bgcolor=\"#66CCFF\">" + assetNames2Array[i] + "</td>";
    }
    currTableMtx3e += "<td class=\"" + assetNames2Array[assetNames2Array.length - 1] + "\" bgcolor=\"#66CCFF\">" + assetNames2Array[assetNames2Array.length - 1] + "</td></tr></thead>";
    currTableMtx3e += "<tbody><tr class=\"parent\"><td><span class=\"years\">" + yearListArray[0] + "</span></td><td>" + yearlyQQQNegCountsArray[0] + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3e += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyNegBetasQQQMtx[0][i] + "</td>";
    }
    for (var i = 1; i < 12; i++) {
        currTableMtx3e += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[i] + "&emsp;</i></td><td><i>" + monthlyQQQNegCountsArray[i] + "</i></td>";
        for (var j = 0; j < assetNames2Array.length; j++) {
            currTableMtx3e += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyNegBetasQQQMtx[i][j] + "</i></td>";
        }
        currTableMtx3e += "</tr>";
    }
    for (var k = 0; k < noInnerYears; k++) {
        currTableMtx3e += "<tr class=\"parent\"><td><span class=\"years\">" + yearListArray[k + 1] + "</span></td><td>" + yearlyQQQNegCountsArray[k + 1] + "</td>";
        for (var i = 0; i < assetNames2Array.length; i++) {
            currTableMtx3e += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyNegBetasQQQMtx[k + 1][i] + "</td>";
        }
        for (var i = 0; i < 12; i++) {
            currTableMtx3e += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[12 + k * 12 + i] + "&emsp;</i></td><td><i>" + monthlyQQQNegCountsArray[12 + k * 12 + i] + "</i></td>";
            for (var j = 0; j < assetNames2Array.length; j++) {
                currTableMtx3e += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyNegBetasQQQMtx[12 + k * 12 + i][j] + "</i></td>";
            }
            currTableMtx3e += "</tr>";
        }
    }
    currTableMtx3e += "<tr class=\"parent\" id=\"lastYearT4\"><td><span class=\"years\">" + yearListArray[yearListArray.length - 1] + "</span></td><td>" + yearlyQQQNegCountsArray[yearListArray.length - 1] + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3e += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyNegBetasQQQMtx[yearListArray.length - 1][i] + "</td>";
    }
    for (var i = 0; i < noLastYearMonths; i++) {
        currTableMtx3e += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[12 + noInnerYears * 12 + i] + "&emsp;</i></td><td><i>" + monthlyQQQNegCountsArray[12 + noInnerYears * 12 + i] + "</i></td>";
        for (var j = 0; j < assetNames2Array.length; j++) {
            currTableMtx3e += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyNegBetasQQQMtx[12 + noInnerYears * 12 + i][j] + "</i></td>";
        }
        currTableMtx3e += "</tr>";
    }
    currTableMtx3e += "<tr class=\"parent\" style=\"cursor: text\"><td><span class=\"total\">Total 2004-" + yearListArray[yearListArray.length - 1] + "</span></td><td>" + totQQQNegDays + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3e += "<td class=\"" + assetNames2Array[i] + "\">" + betaQQQTotalNegArray[i] + "</td>";
    }
    currTableMtx3e += "</tr></tbody></table>";

    var currTableMtx3f = "<table class=\"currData\"><thead><tr align=\"center\" ><td colspan=\"" + noColumns + "\" bgcolor=\"#66CCFF\"><b>Daily Beta of GameChanger Stocks in Comparison to SPY by Years and Months - On Negative SPY Days</b></td></tr><tr align=\"center\" class=\"parent2\"><td bgcolor=\"#66CCFF\"><span class=\"years\" id=\"hideAll\">Only Years</span>/ <span class=\"years\" id=\"expandAll\">Years+Months</span></td><td bgcolor=\"#66CCFF\">No. Negative Days</td>";
    for (var i = 0; i < assetNames2Array.length - 1; i++) {
        currTableMtx3f += "<td class=\"" + assetNames2Array[i] + "\" bgcolor=\"#66CCFF\">" + assetNames2Array[i] + "</td>";
    }
    currTableMtx3f += "<td class=\"" + assetNames2Array[assetNames2Array.length - 1] + "\" bgcolor=\"#66CCFF\">" + assetNames2Array[assetNames2Array.length - 1] + "</td></tr></thead>";
    currTableMtx3f += "<tbody><tr class=\"parent\"><td><span class=\"years\">" + yearListArray[0] + "</span></td><td>" + yearlySPYNegCountsArray[0] + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3f += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyNegBetasSPYMtx[0][i] + "</td>";
    }
    for (var i = 1; i < 12; i++) {
        currTableMtx3f += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[i] + "&emsp;</i></td><td><i>" + monthlySPYNegCountsArray[i] + "</i></td>";
        for (var j = 0; j < assetNames2Array.length; j++) {
            currTableMtx3f += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyNegBetasSPYMtx[i][j] + "</i></td>";
        }
        currTableMtx3f += "</tr>";
    }
    for (var k = 0; k < noInnerYears; k++) {
        currTableMtx3f += "<tr class=\"parent\"><td><span class=\"years\">" + yearListArray[k + 1] + "</span></td><td>" + yearlySPYNegCountsArray[k + 1] + "</td>";
        for (var i = 0; i < assetNames2Array.length; i++) {
            currTableMtx3f += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyNegBetasSPYMtx[k + 1][i] + "</td>";
        }
        for (var i = 0; i < 12; i++) {
            currTableMtx3f += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[12 + k * 12 + i] + "&emsp;</i></td><td><i>" + monthlySPYNegCountsArray[12 + k * 12 + i] + "</i></td>";
            for (var j = 0; j < assetNames2Array.length; j++) {
                currTableMtx3f += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyNegBetasSPYMtx[12 + k * 12 + i][j] + "</i></td>";
            }
            currTableMtx3f += "</tr>";
        }
    }
    currTableMtx3f += "<tr class=\"parent\" id=\"lastYearT5\"><td><span class=\"years\">" + yearListArray[yearListArray.length - 1] + "</span></td><td>" + yearlySPYNegCountsArray[yearListArray.length - 1] + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3f += "<td class=\"" + assetNames2Array[i] + "\">" + yearlyNegBetasSPYMtx[yearListArray.length - 1][i] + "</td>";
    }
    for (var i = 0; i < noLastYearMonths; i++) {
        currTableMtx3f += "<tr class=\"child\"><td align=\"right\"><i>" + yearMonthListArray[12 + noInnerYears * 12 + i] + "&emsp;</i></td><td><i>" + monthlySPYNegCountsArray[12 + noInnerYears * 12 + i] + "</i></td>";
        for (var j = 0; j < assetNames2Array.length; j++) {
            currTableMtx3f += "<td class=\"" + assetNames2Array[j] + "\"><i>" + monthlyNegBetasSPYMtx[12 + noInnerYears * 12 + i][j] + "</i></td>";
        }
        currTableMtx3f += "</tr>";
    }
    currTableMtx3f += "<tr class=\"parent\" style=\"cursor: text\"><td><span class=\"total\">Total 2004-" + yearListArray[yearListArray.length - 1] + "</span></td><td>" + totSPYNegDays + "</td>";
    for (var i = 0; i < assetNames2Array.length; i++) {
        currTableMtx3f += "<td class=\"" + assetNames2Array[i] + "\">" + betaSPYTotalNegArray[i] + "</td>";
    }
    currTableMtx3f += "</tr></tbody></table>";

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
        currTableMtx5 += "</tr>";
    }
    currTableMtx5 += "</table>";


    //var currTableMtx7 = "<table id=\"mytable\" class=\"currDataB2\"><thead><tr align=\"center\"><td colspan=\"" + (noColumns - 1) + "\" bgcolor=\"#66CCFF\"><b>Monthly Volatility Drag History</b></td></tr><tr align=\"center\"><td bgcolor=\"#66CCFF\"><select id=\"limit\"><option value=\"5\">1-Week</option><option value=\"21\" selected>1-Month</option><option value=\"63\">3-Month</option><option value=\"126\">6-Month</option><option value=\"252\">1-Year</option><option value=\"" + dailyDatesArray.length + "\">All</option></select ></td><td bgcolor=\"#66CCFF\">VIX MA(" + volLBPeriod + ")</td>";
    //for (var i = 0; i < assetNamesArray.length - 1; i++) {
    //    currTableMtx7 += "<td class=\"" + assetNamesArray[i] + "\" bgcolor=\"#66CCFF\">" + assetNamesArray[i] + "</td>";
    //}
    //currTableMtx7 += "<td class=\"" + assetNamesArray[assetNamesArray.length - 1] + "\" bgcolor=\"#66CCFF\">" + assetNamesArray[assetNamesArray.length - 1] + "</td></tr></thead><tbody>";
    //for (var j = dailyVolDragsMtx.length - 1; j >= 0; j--) {
    //    currTableMtx7 += "<tr align=\"center\"><td>" + dailyDatesArray[j] + "</td>";
    //    currTableMtx7 += "<td>" + dailyVIXMasArray[j] + "</td>";

    //    for (var i = 0; i < assetNamesArray.length; i++) {
    //        currTableMtx7 += "<td class=\"" + assetNamesArray[i] + "\">" + dailyVolDragsMtx[j][i] + "</td>";
    //    }
    //    currTableMtx7 += "</tr>"
    //}
    //currTableMtx7 += "</tbody></table>";

    ////"Sending" data to HTML file.
    var currTableMtx2a = document.getElementById("idCurrTableMtxa");
    currTableMtx2a.innerHTML = currTableMtxa;
    var currTableMtx2b = document.getElementById("idCurrTableMtxb");
    currTableMtx2b.innerHTML = currTableMtxb;
    var currTableMtx4a = document.getElementById("idCurrTableMtx3a");
    currTableMtx4a.innerHTML = currTableMtx3a;
    var currTableMtx4b = document.getElementById("idCurrTableMtx3b");
    currTableMtx4b.innerHTML = currTableMtx3b;
    var currTableMtx4c = document.getElementById("idCurrTableMtx3c");
    currTableMtx4c.innerHTML = currTableMtx3c;
    var currTableMtx4d = document.getElementById("idCurrTableMtx3d");
    currTableMtx4d.innerHTML = currTableMtx3d;
    var currTableMtx4e = document.getElementById("idCurrTableMtx3e");
    currTableMtx4e.innerHTML = currTableMtx3e;
    var currTableMtx4f = document.getElementById("idCurrTableMtx3f");
    currTableMtx4f.innerHTML = currTableMtx3f;
    var currTableMtx6 = document.getElementById("idCurrTableMtx5");
    currTableMtx6.innerHTML = currTableMtx5;
    //var currTableMtx8 = document.getElementById("idCurrTableMtx7");
    //currTableMtx8.innerHTML = currTableMtx7;

    var lengthOfChart = 20;
    var indOfLength = retHistLBPeriodsNo.indexOf(lengthOfChart);
    var divChartLength = document.getElementById("idChartLength");
    divChartLength.innerHTML = "<strong>Percentage Changes of Prices in the Last &emsp;<select id=\"limit2\"><option value=\"1\">1 Day</option><option value=\"3\">3 Days</option><option value=\"5\">1 Week</option><option value=\"10\">2 Weeks</option><option value=\"20\" selected>1 Month</option><option value=\"63\">3 Months</option><option value=\"126\">6 Months</option><option value=\"252\">1 Year</option>" + retHistLBPeriods[indOfLength] + "</strong >";

    var selLBPeri = 6;
    var divChartQQQLB = document.getElementById("idChartQQQLB");
    divChartQQQLB.innerHTML = "<strong>Daily Beta of GameChanger Stocks in Comparison to QQQ using &emsp;<select id=\"limit1\"><option value=\"0\">1 Week</option><option value=\"1\">2 Weeks</option><option value=\"2\">1 Month</option><option value=\"3\">2 Months</option><option value=\"4\">3 Months</option><option value=\"5\">6 Months</option><option value=\"6\" selected>1 Year</option><option value=\"7\">2 Years</option><option value=\"8\">3 Years</option><option value=\"9\">Max</option>" + betaLBPeriods[selLBPeri] + "</select> &emsp;Lookback Period</strong>";

    //var selLBPeriSPY = 6;
    //var divChartSPYLB = document.getElementById("idChartSPYLB");
    //divChartSPYLB.innerHTML = "<strong>Beta of GameChanger Stocks in Comparison to SPY using &emsp;<select id=\"limit3\"><option value=\"0\">1 Week</option><option value=\"1\">2 Weeks</option><option value=\"2\">1 Month</option><option value=\"3\">2 Months</option><option value=\"4\">3 Months</option><option value=\"5\">6 Months</option><option value=\"6\" selected>1 Year</option><option value=\"7\">2 Years</option><option value=\"8\">3 Years</option><option value=\"9\">Max</option>" + betaLBPeriods[selLBPeriSPY] + "</select> &emsp;Lookback Period</strong>";

    creatingChartData1();
    creatingChartData2();


    $('#limit2').bind('change', function () {
        lengthOfChart = parseInt(this.value);
        indOfLength = retHistLBPeriodsNo.indexOf(lengthOfChart);
        creatingChartData1();
    });

    $('#limit1').bind('change', function () {
        selLBPeri = parseInt(this.value);
        creatingChartData2();
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
            listH.push({ label: assetNamesArray[j], data: assChartPerc1, points: { show: true, radius: Math.min(40 / nCurrData, 2) }, lines: { show: true } });
        }


        var datasets1 = listH;

        flotPlotMyData1(datasets1, nCurrData, xTicksH, noAssets, assetNamesArray);



    }
    function creatingChartData2() {

        var noAssets = assetNames2Array.length;

        var nCurrDataVD = dailyDatesMEArray.length;

        var xTicksHVD = new Array(nCurrDataVD);
        for (var i = 0; i < nCurrDataVD; i++) {
            var xTicksHVDRows = new Array(2);
            xTicksHVDRows[0] = i;
            xTicksHVDRows[1] = dailyDatesMEArray[i];
            xTicksHVD[i] = xTicksHVDRows;
        }
        
        var listHVD = [];
        for (var j = 0; j < noAssets; j++) {
            var assChartVD = new Array(nCurrDataVD);
            for (var i = 0; i < nCurrDataVD; i++) {
                var assChartVDRows = new Array(2);
                assChartVDRows[0] = i;
                assChartVDRows[1] = parseFloat(betaCalcQQQMtx[i][selLBPeri * noAssets+j]);
                assChartVD[i] = assChartVDRows;
            }
            listHVD.push({ label: assetNames2Array[j], data: assChartVD, points: { show: true, radius: 0.01 }, lines: { show: true, lineWidth: 1 } });
        }


        var datasets2 = listHVD;

        flotPlotMyData3(datasets2, nCurrDataVD, xTicksHVD, noAssets, assetNames2Array, selLBPeri, betaLBPeriods);

    }
}
// Creating charts.
function flotPlotMyData1(datasets1, nCurrData, xTicksH, noAssets, assetNamesArray) {
    $("#update_all").click(plotAccordingToChoices);

    function plotAccordingToChoices() {
        var dataB = [];
        $.each(datasets1, function (key) {
            dataB.push(datasets1[key]);
        });


        var placeholder1 = $("#placeholder1");
        var data1 = dataB;
        var options1 = {
            yaxis: {
                axisLabel: "Percentage Change",
                tickFormatter: function (v, axis) {
                    return v.toFixed(2) + "%";
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

        };

        var plot1 = $.plot(placeholder1, data1, options1);

        var myDivChart1Element = placeholder1[0];

        var flotXaxisObj = myDivChart1Element.getElementsByClassName('flot-text')[0].getElementsByClassName('flot-x-axis flot-x1-axis xAxis x1Axis')[0];
        var xWidth = flotXaxisObj.clientWidth;
        var flotTickLabelObj = flotXaxisObj.childNodes;

        var xTicks = flotTickLabelObj.length;
        var limiter = Math.floor(xWidth / xTicks);
        var xConst = 30;
        if (limiter < xConst) {

            for (var i = 0; i < xTicks; i++) {
                if (i % Math.floor(xConst / limiter) > 0) {
                    flotTickLabelObj[i].style.display = "none";
                }
            }


        }
    }
    plotAccordingToChoices();


}

function flotPlotMyData3(datasets2, nCurrData2, xTicksH2, noAssets, assetNamesArray, selLBPeri, betaLBPeriods) {
    $("#update_all").click(plotAccordingToChoices2);
    function plotAccordingToChoices2() {
        var dataB2 = [];
        $.each(datasets2, function (key) {
            dataB2.push(datasets2[key]);
        });

        //$("input:checkbox:checked").each(function () {
        //    var key = $(this).attr("id");
        //    var aaa = assetNamesArray.indexOf(key);
        //    if (key && datasets2[aaa]) {
        //        dataB2.push(datasets2[aaa]);
        //    }
        //});

        var placeholder2 = $("#placeholder2");
        var data2 = dataB2;
        var options2 = {
            yaxis: {
                axisLabel: "Beta",
                tickFormatter: function (v, axis) {
                    return v.toFixed(2);
                }
            },
            xaxis: {
                //tickDecimals: 0,
                min: 0,
                //max: nCurrData-1,
                ticks: xTicksH2,
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
                    for (var i = 0; i < nCurrData2; i++) {
                        xVals[i] = dataB2[0].data[i][0];
                    };
                    var indi = xVals.indexOf(x);

                    var text = "<b>" + label + "<br/></b><i>" + betaLBPeriods[selLBPeri] + " Beta in Comparison to QQQ on " + xTicksH2[indi][1] + "<br/></i>";
                    dataB2.forEach(function (series) {
                        text += series.label + ' : ' + series.data[indi][1] + "<br/>";
                    });

                    return text;
                }
            },
            selection: {
                mode: "x"
            }

        };

        placeholder2.bind("plotselected", function (event, ranges) {

            $("#selection").text(ranges.xaxis.from.toFixed(1) + " to " + ranges.xaxis.to.toFixed(1));

            $.each(plot2.getXAxes(), function (_, axis) {
                var opts = axis.options;
                opts.min = ranges.xaxis.from;
                opts.max = ranges.xaxis.to;
                var xMin = Math.round(ranges.xaxis.from);
                var xMax = Math.round(ranges.xaxis.to);
                var yMax = 0;
                var noLines = data2.length;
                for (var i = xMin; i < xMax + 1; i++) {
                    for (var j = 0; j < noLines; j++) {
                        let v = data2[j].data[i][1];
                        yMax = (v > yMax) ? v : yMax;

                    }
                }
                plot2.getAxes().yaxis.options.max = yMax * 1.2;
            });

            var sdsd = plot2.getAxes().yaxis.options.max;
            var ssdfs = plot2.getOptions().tooltip.content;
            plot2.setupGrid();
            plot2.draw();
            plot2.clearSelection();
            xTickReDraw();

        });

        placeholder2.bind("plotunselected", function (event) {
            $("#selection").text("");
        });

        var plot2 = $.plot(placeholder2, data2, options2);

        xTickReDraw();

        $("#clearSelection").click(plotAccordingToChoices2);
    }
    plotAccordingToChoices2();


}

function xTickReDraw() {
    var myDivChart2Element = $("#placeholder2")[0];

    var flotXaxisObj2 = myDivChart2Element.getElementsByClassName('flot-text')[0].getElementsByClassName('flot-x-axis flot-x1-axis xAxis x1Axis')[0];
    var xWidth2 = flotXaxisObj2.clientWidth;
    var flotTickLabelObj2 = flotXaxisObj2.childNodes;

    var xTicks2 = flotTickLabelObj2.length;
    var limiter2 = Math.max(Math.floor(xWidth2 / xTicks2), 0.5);
    var xConst2 = 60;
    if (limiter2 < xConst2) {

        for (var i = 0; i < xTicks2; i++) {
            if (i % Math.floor(xConst2 / limiter2) > 0) {
                flotTickLabelObj2[i].style.display = "none";
            }
        }

    }
}