import { AppComponent } from '../app.component';
import { Http } from '@angular/http';

export class Strategy {
    name: string;
    app: AppComponent;

    constructor(p_name: string, p_app: AppComponent) {
        this.name = p_name;
        this.app = p_app;
        console.log("Strategy ctor()");
    }

    // override these in derived classes BEGIN
    IsMenuItemIdHandled(p_subStrategyId: string): boolean {
        console.log(`Strategy.IsMenuItemId(): ${this.name}`);
        return false;
    }

    GetHtmlUiName(p_subStrategyId: string): string {     // go to HTML UI
        console.log(`Strategy.GetUiName(): ${this.name}`);
        return "Unknown strategy";
    }

    GetTradingViewChartName(p_subStrategyId: string): string {     // go to HTML UI
        console.log(`Strategy.GetUiName(): ${this.name}`);
        return "Unknown strategy";
    }

    GetWebApiName(p_subStrategyId: string): string {
        console.log(`Strategy.GetWebApiName(): ${this.name}`);
        return "https://www.google.com/";
    }

    GetHelpUri(p_subStrategyId: string): string {     // go to HTML UI as gDoc URL
        console.log(`Strategy.GetHelpUri(): ${this.name}`);
        return "https://www.google.com/";
    }

    GetStrategyParams(p_subStrategyId: string): string {
        console.log(`Strategy.GetStrategyParams(): ${this.name}`);
        return '';
    }
    // override these in derived classes END


    public StartBacktest(p_http: Http, p_generalInputParameters: string, p_subStrategyId: string) {
        console.log("StartBacktest() 1");
        if (!this.IsMenuItemIdHandled(p_subStrategyId))
            return;
        console.log("StartBacktest() 2");

        //var url = "http://localhost/qt?StartDate=&EndDate=&strategy=AdaptiveUberVxx&BullishTradingInstrument=Long%20SPY&param=UseKellyLeverage=false;MaxLeverage=1.0&Name=Fomc&Priority=3&Combination=Avg&StartDate=&EndDate=&TradingStartAt=2y&Param=&Name=Holiday&Priority=&Combination=&StartDate=&EndDate=&TradingStartAt=&Param=&Name=TotM&Priority=2&Combination=Avg&StartDate=&EndDate=&TradingStartAt=2y&Param=TrainingTicker=SPY&Name=Connor&Priority=1&Combination=Avg&StartDate=&EndDate=&TradingStartAt=100td&Param=LookbackWindowDays=100;ProbDailyFTThreshold=47
        var url = "/qt?" + p_generalInputParameters + "&strategy=" + this.GetWebApiName(p_subStrategyId) +
            this.GetStrategyParams(p_subStrategyId);

        p_http.get(url)
            .map(res => res.json()) // Call map on the response observable to get the parsed people object
            .subscribe(data => { // Subscribe to the observable to get the parsed people object and attach it to the component
                console.log("StartBacktest(): data received 1: " + data);
                this.app.tradingViewChartName = this.GetTradingViewChartName(p_subStrategyId);
                this.app.ProcessStrategyResult(data);
            }, error => {
                console.log("ERROR. StartBacktest(): data received error: " + error);
                this.app.errorMessage = error;
            });
    }

}