import {AppComponent} from './app.component';
import {Http, HTTP_PROVIDERS} from '@angular/http';
import {Observable} from 'rxjs/Rx';
import 'rxjs/add/operator/map';

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

export class AdaptiveUberVxx {

    public bullishTradingInstrument = ["Long SPY", "Long ^GSPC", "Long ^IXIC", "Long ^RUT", "Long QQQ", "Long QLD", "Long TQQQ", "Long IWM", "Long IYR", "Short VXX", "Short VXX.SQ", "Short VXZ", "Short VXZ.SQ"];
    public selectedBullishTradingInstrument = this.bullishTradingInstrument[0];
    public param = "UseKellyLeverage=false;MaxLeverage=1.0";      // params shouldn't use &, because that is used for URI.Query parameters. Better to avoid it. Use CSV, come separation instead. Or better: ";"

    public fomc = new SubStrategy("FOMC", "3", "Avg", "2y", "");
    public holiday = new SubStrategy("Holiday", "", "", "", "");
    public totm = new SubStrategy("TotM", "2", "Avg", "2y","TrainingTicker=SPY"); 
    public connor = new SubStrategy("Connor", "1", "Avg", "100td", "LookbackWindowDays=100;ProbDailyFTThreshold=47"); // LookbackDays is not all the previous history, only little window

  
  
    app: AppComponent;
    constructor(p_app: AppComponent) {
        this.app = p_app;
        console.log("AdaptiveUberVxx ctor()");
    }

    public bullishTradingInstrumentChanged(newValue) {
        console.log("bullishTradingInstrumentChanged(): " + newValue);
        this.selectedBullishTradingInstrument = newValue;
        this.app.tipToUser = this.selectedBullishTradingInstrument;
    }
    
    public SubStrategySelected_AdaptiveUberVxx() {
        if (this.app.selectedStrategyMenuItemId == "idMenuItemAdaptiveUberVxx") {
            this.app.selectedStrategyName = "Learning version of UberVxx";
            this.app.strategyGoogleDocHelpUri = "https://docs.google.com/document/d/1SBi8XZVB_JHsI2IIbhVpx7uDEVEXv1AVBAqPw2EkLuM";
            this.app.selectedStrategyWebApiName = "AdaptiveUberVxx";
        }
    }

    public StartBacktest_AdaptiveUberVxx(http: Http) {
        console.log("StartBacktest_AdaptiveUberVxx() 1");
        if (this.app.selectedStrategyMenuItemId != "idMenuItemAdaptiveUberVxx")
            return;
        console.log("StartBacktest_AdaptiveUberVxx() 2");

        //var url = "http://localhost/qt?StartDate=&EndDate=&strategy=AdaptiveUberVxx&BullishTradingInstrument=Long%20SPY&param=UseKellyLeverage=false;MaxLeverage=1.0&Name=Fomc&Priority=3&Combination=Avg&StartDate=&EndDate=&TradingStartAt=2y&Param=&Name=Holiday&Priority=&Combination=&StartDate=&EndDate=&TradingStartAt=&Param=&Name=TotM&Priority=2&Combination=Avg&StartDate=&EndDate=&TradingStartAt=2y&Param=TrainingTicker=SPY&Name=Connor&Priority=1&Combination=Avg&StartDate=&EndDate=&TradingStartAt=100td&Param=LookbackWindowDays=100;ProbDailyFTThreshold=47
        var url = "/qt?" + this.app.generalInputParameters + "&strategy=" + this.app.selectedStrategyWebApiName +
            "&BullishTradingInstrument=" + this.selectedBullishTradingInstrument + "&param=" + this.param
            + "&" + this.fomc.toUrlQueryString()
            + "&" + this.holiday.toUrlQueryString()
            + "&" + this.totm.toUrlQueryString()
            + "&" + this.connor.toUrlQueryString();

        http.get(url)
            .map(res => res.json()) // Call map on the response observable to get the parsed people object
            .subscribe(data => { // Subscribe to the observable to get the parsed people object and attach it to the component
                console.log("StartBacktest_AdaptiveUberVxx(): data received 1: " + data);
                this.app.tradingViewChartName = "Adaptive UberVxx";
                this.app.ProcessStrategyResult(data);
            }, error => {
                console.log("ERROR. StartBacktest(): data received error: " + error);
                this.app.errorMessage = error;
            });
    }

}

