import {AppComponent} from '../app.component';
import {Http} from '@angular/http';
import {Observable} from 'rxjs/Rx';
import 'rxjs/add/operator/map';
import { Strategy } from './Strategy';

export class TotM extends Strategy {

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

    constructor(p_app: AppComponent) {
        super("TotM", p_app);
    }

    IsMenuItemIdHandled(p_subStrategyId: string): boolean {
        return p_subStrategyId == "idMenuItemTotM";
    }

    GetHtmlUiName(p_subStrategyId: string): string {     // go to HTML UI
        return "Turn of the Month (mask based). Typical: Bearish:T-1, Bullish: T+1,T+2,T+3";
    }

    GetTradingViewChartName(p_subStrategyId: string): string {     // go to HTML UI
        return "Turn of the Month";
    }

    GetWebApiName(p_subStrategyId: string): string {
        return "TotM";
    }

    GetHelpUri(p_subStrategyId: string): string {     // go to HTML UI as gDoc URL
        return "https://docs.google.com/document/d/1DJtSt1FIPFbscAZsn8UAfiBBIhbeWvZcJWtQffGPTfU";
    }

    GetStrategyParams(p_subStrategyId: string): string {
        return "&BullishTradingInstrument=" + this.selectedBullishTradingInstrument
            + "&DailyMarketDirectionMaskSummerTotM=" + this.dailyMarketDirectionMaskSummerTotM + "&DailyMarketDirectionMaskSummerTotMM=" + this.dailyMarketDirectionMaskSummerTotMM
            + "&DailyMarketDirectionMaskWinterTotM=" + this.dailyMarketDirectionMaskWinterTotM + "&DailyMarketDirectionMaskWinterTotMM=" + this.dailyMarketDirectionMaskWinterTotMM;
    }






    public bullishTradingInstrumentChanged(newValue) {
        console.log("bullishTradingInstrumentChanged(): " + newValue);
        this.selectedBullishTradingInstrument = newValue;
        this.app.tipToUser = this.selectedBullishTradingInstrument;
    }

    public MenuItemPresetMasksClicked(event) {
        console.log("MenuItemPresetMasksClicked()");
        var target = event.target || event.srcElement || event.currentTarget;
        var idAttr = target.attributes.id;
        var idValue = idAttr.nodeValue;

        switch (idValue) {
            case "idMaskBuyHold":
                this.dailyMarketDirectionMaskWinterTotM = "UUUUUUUUUUUUUUUUUUUU.UUUUUUUUUUUUUUUUUUUU";    // 20 days before and 20 days after Turn of the Month is set (to be sure)
                this.dailyMarketDirectionMaskWinterTotMM = "UUUUUUUUUUUUUUUUUUUU.UUUUUUUUUUUUUUUUUUUU";
                this.dailyMarketDirectionMaskSummerTotM = "UUUUUUUUUUUUUUUUUUUU.UUUUUUUUUUUUUUUUUUUU";
                this.dailyMarketDirectionMaskSummerTotMM = "UUUUUUUUUUUUUUUUUUUU.UUUUUUUUUUUUUUUUUUUU";
                break;
            case "idMaskUberVXXOld":
                // TotM:
                //•	Long VXX on Day -1 (last trading day of the month) with 100%;
                //•	Short VXX on Day 1-3 (first three trading days of the month) with 100%.
                this.dailyMarketDirectionMaskWinterTotM = "D.UUU";
                this.dailyMarketDirectionMaskWinterTotMM = ".";
                this.dailyMarketDirectionMaskSummerTotM = "D.UUU";
                this.dailyMarketDirectionMaskSummerTotMM = ".";
                break;
            case "idMaskBuyUberVXXNew":      // Correlation and Significance Analysis of Uber VXX Strategy Parts.docx
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
    
    
  
    //public InvertVisibilityOfTableRow(event) {
    //    console.log("InvertVisibilityOfTableRow() START)");
    //    var target = event.target || event.srcElement || event.currentTarget;
    //    var idAttr = target.attributes.id;
    //    var idValue = idAttr.nodeValue;

    //    var tableRow = document.getElementById(idValue);
    //    if (tableRow.style.display == 'none')
    //        tableRow.style.display = 'table-row';
    //    else
    //        tableRow.style.display = 'none';
    //}

    // Angular2 (click) event didn't call these. We have to force somehow Angular2 to reparse the DOM for (onclicks) after we inserted the extra table. 
    // I found a workaround now in global JS, not in TS.Angular2 is still not final.Fix this after Angular2 is final.
    public InvertVisibilityOfTableRow(paramID: string) {       
        console.log("InvertVisibilityOfTableRow() START");
    
        var tableRow = document.getElementById(paramID);
        if (tableRow.style.display == 'none')
            tableRow.style.display = 'table-row';
        else
            tableRow.style.display = 'none';
    }

}

let GlobalScopeInvertVisibilityOfTableRow_SomehowItDidntWorkYet = function (paramID: any) {      // to hook to Angular2 (click) event is not good, because after initial load, Angular2 doesn't re-parse the HTML file for those (click) events. Use the General onclick event instead.
    console.log("GlobalScopeInvertVisibilityOfTableRow() START");

    var tableRow = document.getElementById(paramID);
    if (tableRow.style.display == 'none')
        tableRow.style.display = 'table-row';
    else
        tableRow.style.display = 'none';
};
