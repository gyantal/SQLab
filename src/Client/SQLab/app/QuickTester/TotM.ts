import {AppComponent} from './app.component';
import {Http, HTTP_PROVIDERS} from '@angular/http';
import {Observable} from 'rxjs/Rx';
import 'rxjs/add/operator/map';

export class TotM {

    public bullishTradingInstrument = ["Long SPY", "Long ^GSPC", "Long ^IXIC", "Long ^RUT", "Long QQQ", "Long QLD", "Long TQQQ", "Long IWM", "Long IYR", "Short VXX", "Short VXX.SQ", "Short VXZ", "Short VXZ.SQ"];
    public selectedBullishTradingInstrument = this.bullishTradingInstrument[0];

    //public totMStock = ["SPY", "QQQ", "VXX"];
    //public selectedTotMStock = "SPY";

    //public totMLongOrShortWhenBullish = ["Long", "Short"];
    //public selectedTotMLongOrShortWhenBullish = "Long";

    //public dailyMarketDirectionMaskSummerTotM = "D.UUU00DD";  //Mask: D0.UU, Up: Market Up, D: Down, 0:Cash (B is not good because Bullish, Bearish): other option Comma separation, but not necessary here
    //public dailyMarketDirectionMaskSummerTotMM = "DDUU.UU";
    //public dailyMarketDirectionMaskWinterTotM = "D.UUU00DD";
    //public dailyMarketDirectionMaskWinterTotMM = "DDUU.UU";

    // before significance test: SPY: CAGR:  25.30%  Annualized StDev:  16.50%  Sharpe:  1.53; (15+19)/2=17 days per month
    //public dailyMarketDirectionMaskWinterTotM = "UUUD.UUU";//Mask: D0.UU, Up: Market Up, D: Down, 0:Cash (B is not good because Bullish, Bearish): other option Comma separation, but not necessary here
    //public dailyMarketDirectionMaskWinterTotMM = "DDUU.UU00UU";
    //public dailyMarketDirectionMaskSummerTotM = "DDDDUUD.UDD";
    //public dailyMarketDirectionMaskSummerTotMM = "DDUU.UU00DDD";

    // after significance test: SPY: CAGR:  23.27%  Annualized StDev:  14.23%  Sharpe:  1.64; (15+8)/2=11.5 days per month //sharpe increased! more reliable 
    public dailyMarketDirectionMaskWinterTotM = "UUUD.UUU";//Mask: D0.UU, Up: Market Up, D: Down, 0:Cash (B is not good because Bullish, Bearish): other option Comma separation, but not necessary here
    public dailyMarketDirectionMaskWinterTotMM = "DDUU.UU00UU"; // winter didn't change after Significance test.
    public dailyMarketDirectionMaskSummerTotM = "DD00U00.U";
    public dailyMarketDirectionMaskSummerTotMM = "D0UU.0U";

    app: AppComponent;
    constructor(p_app: AppComponent) {
        this.app = p_app;
        console.log("TotM ctor()");
    }

    public bullishTradingInstrumentChanged(newValue) {
        console.log("bullishTradingInstrumentChanged(): " + newValue);
        this.selectedBullishTradingInstrument = newValue;
        this.app.tipToUser = this.selectedBullishTradingInstrument;
    }

    public MenuItemPresetMasksClicked(predefMaskString: string) {
        switch (predefMaskString) {
            case "BuyHold":
                this.dailyMarketDirectionMaskWinterTotM = "UUUUUUUUUUUUUUUUUUUU.UUUUUUUUUUUUUUUUUUUU";    // 20 days before and 20 days after Turn of the Month is set (to be sure)
                this.dailyMarketDirectionMaskWinterTotMM = "UUUUUUUUUUUUUUUUUUUU.UUUUUUUUUUUUUUUUUUUU";
                this.dailyMarketDirectionMaskSummerTotM = "UUUUUUUUUUUUUUUUUUUU.UUUUUUUUUUUUUUUUUUUU";
                this.dailyMarketDirectionMaskSummerTotMM = "UUUUUUUUUUUUUUUUUUUU.UUUUUUUUUUUUUUUUUUUU";
                break;
            case "UberVXXOld":
                // TotM:
                //•	Long VXX on Day -1 (last trading day of the month) with 100%;
                //•	Short VXX on Day 1-3 (first three trading days of the month) with 100%.
                this.dailyMarketDirectionMaskWinterTotM = "D.UUU";
                this.dailyMarketDirectionMaskWinterTotMM = ".";
                this.dailyMarketDirectionMaskSummerTotM = "D.UUU";
                this.dailyMarketDirectionMaskSummerTotMM = ".";
                break;
            case "UberVXXNew":      // Correlation and Significance Analysis of Uber VXX Strategy Parts.docx
                // TotM:
                //•	Day -1: long VXX - both in winter and summer;
                //•	Day +1: short VXX only at turn of the quarter - both in winter and summer;
                //•	Day +2-+3: short VXX only in winter.
                // TotMM: 
                //•	Day +2: short VXX - both in winter and summer;
                //•	Day +3-+7: short VXX only in winter.
                this.dailyMarketDirectionMaskWinterTotM = "D.UUU";      // "• Day +1: short VXX only at turn of the quarter - both in winter and summer;", but I put it as Bullish anyway
                this.dailyMarketDirectionMaskWinterTotMM = ".0UUUUUU";
                this.dailyMarketDirectionMaskSummerTotM = "D.U";      // "• Day +1: short VXX only at turn of the quarter - both in winter and summer;", but I put it as Bullish anyway
                this.dailyMarketDirectionMaskSummerTotMM = ".0U";
                break;
            default:    //SPYDerived
                this.dailyMarketDirectionMaskWinterTotM = "UUUD.UUU";//Mask: D0.UU, Up: Market Up, D: Down, 0:Cash (B is not good because Bullish, Bearish): other option Comma separation, but not necessary here
                this.dailyMarketDirectionMaskWinterTotMM = "DDUU.UU00UU"; // winter didn't change after Significance test.
                this.dailyMarketDirectionMaskSummerTotM = "DD00U00.U";
                this.dailyMarketDirectionMaskSummerTotMM = "D0UU.0U";
        }
    }

    public SubStrategySelected_TotM() {
        if (this.app.selectedStrategyMenuItemId == "idMenuItemTotM") {
            this.app.selectedStrategyName = "Turn of the Month (mask based). Typical: Bearish:T-1, Bullish: T+1,T+2,T+3";
            this.app.strategyGoogleDocHelpUri = "https://docs.google.com/document/d/1DJtSt1FIPFbscAZsn8UAfiBBIhbeWvZcJWtQffGPTfU";
            this.app.selectedStrategyWebApiName = "TotM";
        }
    }

    public StartBacktest_TotM(http: Http) {
        if (this.app.selectedStrategyMenuItemId != "idMenuItemTotM")
            return;

        //var url = "http://localhost:52174/q/qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=SRS-URE&rebalanceFrequency=5d";
        //var url = "http://localhost:52174/q/qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=" + this.app.selectedEtfPairs + "&rebalancingFrequency=" + this.app.rebalancingFrequency;
        //var url = "///q/qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=" + this.app.selectedEtfPairs + "&rebalancingFrequency=" + this.app.rebalancingFrequency;
        //var url = "/q/qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=" + this.app.selectedEtfPairs + "&rebalancingFrequency=" + this.app.rebalancingFrequency;
        var url = "/q/qt?jsonp=JSON_CALLBACK" + this.app.generalInputParameters + "&strategy=" + this.app.selectedStrategyWebApiName + "&BullishTradingInstrument=" + this.selectedBullishTradingInstrument
            + "&DailyMarketDirectionMaskSummerTotM=" + this.dailyMarketDirectionMaskSummerTotM + "&DailyMarketDirectionMaskSummerTotMM=" + this.dailyMarketDirectionMaskSummerTotMM
            + "&DailyMarketDirectionMaskWinterTotM=" + this.dailyMarketDirectionMaskWinterTotM + "&DailyMarketDirectionMaskWinterTotMM=" + this.dailyMarketDirectionMaskWinterTotMM;

        http.get(url)
            .map(res => res.json()) // Call map on the response observable to get the parsed people object
            .subscribe(data => { // Subscribe to the observable to get the parsed people object and attach it to the component
                this.app.tradingViewChartName = "Turn of the Month";
                this.app.ProcessStrategyResult(data);
            }, error => { this.app.errorMessage = error; });
    }

}

export function InvertVisibilityOfTableRow(paramID: string) {
    console.log("InvertVisibilityOfTableRow() START)");
    var tableRow = document.getElementById(paramID);
    if (tableRow.style.display == 'none')
        tableRow.style.display = 'table-row';
    else
        tableRow.style.display = 'none';
}

export function MenuItemPresetMasksClicked(predefMaskString: string) {
    // Refresh Angular DOM view maybe not needed in Angular2
    //var controllerElement = document.querySelector('body');
    //var controllerScope = angular.element(controllerElement).scope();
    //controllerScope.$apply(controllerScope.MenuItemPresetMasksClicked(predefMaskString));  // use Apply from MenuClick, but you don't have to use it from an Angular function
}