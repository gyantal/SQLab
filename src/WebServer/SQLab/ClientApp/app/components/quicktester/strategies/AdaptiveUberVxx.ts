import { QuickTesterComponent } from '../quicktester.component';
import { Http } from '@angular/http';
import { Observable } from 'rxjs/Rx';
import 'rxjs/add/operator/map';
import { Strategy } from './Strategy';

export class SubStrategy {
    public Name = "Unknown";
    public Priority = "1";	// 0 (don't play), other: a number between 1-10.	// if Component clash at the same priority, those will be Convolved by default: added together
    public Combination = "Avg";	 //CombinationWithSamePriorities: Averaging, Adding=SimpleConvolution, InterpolationOfConvolutionAndProperIntersectionPdf
    public StartDate = "";
    public EndDate = "";
    public TradingStartAt = ""; //or "20%" or "2012-01-02" (a proper date) or "100td"(trading days) "730cd" (calendar days) 2*365=730 days means 2 years, or "2y"
    public Param = "";  // params shouldn't use &, because that is used for URI.Query parameters. Better to avoid it. Use CSV, come separation instead. Or better: ";"

    constructor(p_name: string, p_priority: string, p_combination: string, p_tradingStartAt: string, p_param: string) {
        this.Name = p_name;
        this.Priority = p_priority;
        this.Combination = p_combination;
        this.TradingStartAt = p_tradingStartAt;
        this.Param = p_param;
    }

    public toUrlQueryString(): string {
        return "Name=" + this.Name +
            "&Priority=" + this.Priority +
            "&Combination=" + this.Combination +
            "&StartDate=" + this.StartDate +
            "&EndDate=" + this.EndDate +
            "&TradingStartAt=" + this.TradingStartAt +
            "&Param=" + this.Param;
    }
}

export class AdaptiveUberVxx extends Strategy {

    public bullishTradingInstrument = ["Long SPY", "Long ^GSPC", "Long ^IXIC", "Long ^RUT", "Long QQQ", "Long QLD", "Long TQQQ", "Long IWM", "Long IYR", "Short VXXB", "Short VXXB.SQ", "Short VXZB", "Short VXZB.SQ"];
    public selectedBullishTradingInstrument = this.bullishTradingInstrument[0];
    public param = "UseKellyLeverage=false;MaxLeverage=1.0";      // params shouldn't use &, because that is used for URI.Query parameters. Better to avoid it. Use CSV, come separation instead. Or better: ";"

    public fomc = new SubStrategy("FOMC", "3", "Avg", "2y", "");
    public holiday = new SubStrategy("Holiday", "", "", "", "");
    public totm = new SubStrategy("TotM", "2", "Avg", "2y", "TrainingTicker=SPY");
    public connor = new SubStrategy("Connor", "1", "Avg", "100td", "LookbackWindowDays=100;ProbDailyFTThreshold=47"); // LookbackDays is not all the previous history, only little window

    constructor(p_app: QuickTesterComponent) {
        super("AdaptiveUberVxx", p_app);
    }

    IsMenuItemIdHandled(p_subStrategyId: string): boolean {
        return p_subStrategyId == "idMenuItemAdaptiveUberVxx";
    }

    GetHtmlUiName(p_subStrategyId: string): string {     // go to HTML UI
        return "Learning version of UberVxx";
    }

    GetTradingViewChartName(p_subStrategyId: string): string {     // go to HTML UI
        return "Adaptive UberVxx";
    }

    GetWebApiName(p_subStrategyId: string): string {
        return "AdaptiveUberVxx";
    }

    GetHelpUri(p_subStrategyId: string): string {     // go to HTML UI as gDoc URL
        return "https://docs.google.com/document/d/1SBi8XZVB_JHsI2IIbhVpx7uDEVEXv1AVBAqPw2EkLuM";
    }

    GetStrategyParams(p_subStrategyId: string): string {
        return "&BullishTradingInstrument=" + this.selectedBullishTradingInstrument + "&param=" + this.param
            + "&" + this.fomc.toUrlQueryString()
            + "&" + this.holiday.toUrlQueryString()
            + "&" + this.totm.toUrlQueryString()
            + "&" + this.connor.toUrlQueryString();
    }




    public bullishTradingInstrumentChanged(newValue: any) {
        console.log("bullishTradingInstrumentChanged(): " + newValue);
        this.selectedBullishTradingInstrument = newValue;
        this.app.tipToUser = this.selectedBullishTradingInstrument;
    }


}

