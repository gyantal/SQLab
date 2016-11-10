import {AppComponent} from './app.component';
import {Http} from '@angular/http';
import {Observable} from 'rxjs/Rx';
import 'rxjs/add/operator/map';

export class VXX_SPY_Controversial {
    public vxxLongOrShortTrade = ["Long", "Short", "Cash"];
    public selectedVXXLongOrShortTrade: string = "Cash";
    public spyMinPctMove = "0.01";
    public vxxMinPctMove = "0.01";   // corresponding to 0.25% of VIX move, with VXX Beta = 2 approximately
    // Balazs's parameter was 0.1% and 0.125%, but that decreased the profit
    // with spyMinPctMove == 0.01, vxxMinPctMove = 0.01, go To Cash: I got better CAGR than the Going Short (Going Long is bad, because of volatility drag)
    // increasing vxxMinPctMove is not good, because when vxxPctMove is very, very high, next day can be strong MR, so VXX can go down a lot. We don't want to miss those profits, so we don't increase the vxxMinPctMove too much
    //this.app.selectedEtfPairsIdx = 1;   // zero based, so it is December
    
    app: AppComponent;

    constructor(p_app: AppComponent) {
        this.app = p_app;
        console.log("VXX_SPY_Controversial ctor()");
    }

    public vxxLongOrShortTradeChanged(newValue) {
        console.log("vxxLongOrShortTradeChanged(): " + newValue);
        this.selectedVXXLongOrShortTrade = newValue;
        this.app.tipToUser = this.selectedVXXLongOrShortTrade + "+" + this.selectedVXXLongOrShortTrade;
    }

    public SubStrategySelected() {
        if (this.app.selectedStrategyMenuItemId == "idMenuItemVXX_SPY_Controversial") {
            this.app.selectedStrategyName = "Buy&Hold XIV with VXX-SPY ControversialDay: Cash if VXX & SPY move in the same direction";
            this.app.strategyGoogleDocHelpUri = "https://docs.google.com/document/d/1G1gjvt9GdqB4yrAvLV4ELnVDYNd587tovcWrVzTwqak";
            this.app.selectedStrategyWebApiName = "VXX_SPY_Controversial";
        }
    }

    public StartBacktest(http: Http) {
        console.log("StartBacktest_VXX()");
        if (this.app.selectedStrategyMenuItemId != "idMenuItemVXX_SPY_Controversial")
            return;

        //var url = "http://localhost:52174/qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=SRS-URE&rebalanceFrequency=5d";
        //var url = "http://localhost:52174/qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=" + this.app.selectedEtfPairs + "&rebalancingFrequency=" + this.app.rebalancingFrequency;
        //var url = "///qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=" + this.app.selectedEtfPairs + "&rebalancingFrequency=" + this.app.rebalancingFrequency;
        //var url = "/qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=" + this.app.selectedEtfPairs + "&rebalancingFrequency=" + this.app`.rebalancingFrequency;
        var url = "/qt?" + this.app.generalInputParameters + "&strategy=" + this.app.selectedStrategyWebApiName + "&SpyMinPctMove=" + this.spyMinPctMove + "&VxxMinPctMove=" + this.vxxMinPctMove + "&LongOrShortTrade=" + this.selectedVXXLongOrShortTrade;

        http.get(url)
            .map(res => res.json()) // Call map on the response observable to get the parsed people object
            .subscribe(data => { // Subscribe to the observable to get the parsed people object and attach it to the component
                this.app.tradingViewChartName = "VXX-SPY ControversialDay";
                this.app.ProcessStrategyResult(data);
            }, error => {
                console.log("ERROR. StartBacktest(): data received error: " + error);
                this.app.errorMessage = error;
            });
    }


}