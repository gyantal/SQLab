import {AppComponent} from './app.component';
import {Http, HTTP_PROVIDERS} from '@angular/http';
import {Observable} from 'rxjs/Rx';
import 'rxjs/add/operator/map';

export class LEtfDistcrepancy {
    public etfPairs = ["URE-SRS", "DRN-DRV", "FAS-FAZ", "XIV-VXX", "ZIV-VXZ",];
    public selectedEtfPairs: string = "URE-SRS";
    //app.selectedEtfPairsIdx = 1;   // zero based, so it is December
    public rebalancingFrequency: string = "5d";

    app: AppComponent;

    constructor(p_app: AppComponent) {
        this.app = p_app;
        console.log("LEtfDistcrepancy ctor()");
    }

    public etfPairsChanged(newValue) {
        console.log("etfPairsChanged(): " + newValue);
        this.selectedEtfPairs = newValue;
        this.app.tipToUser = this.selectedEtfPairs + "+" + this.selectedEtfPairs;
    }

    public SubStrategySelected_LEtfDistcrepancy() {
        if (this.app.selectedStrategyMenuItemId == "idMenuItemLETFDiscrepancy1") {
            this.app.selectedStrategyName = "L-ETF Discrepancy 1";
            this.app.strategyGoogleDocHelpUri = "https://docs.google.com/document/d/1IpqNT6THDP5B1C-Vugt1fA96Lf1Ms9Tb-pq0LzT3GnY";
            this.app.selectedStrategyWebApiName = "LETFDiscrepancy1";
        }
        if (this.app.selectedStrategyMenuItemId == "idMenuItemLETFDiscrepancy2") {
            this.app.selectedStrategyName = "L-ETF Discrepancy 2";
            this.app.strategyGoogleDocHelpUri = "https://docs.google.com/document/d/1JI7sttAtI2Yzix1WbVUCTNP8SujiVInvNyaQyrC30Us";
            this.app.selectedStrategyWebApiName = "LETFDiscrepancy2";
        }
        if (this.app.selectedStrategyMenuItemId == "idMenuItemLETFDiscrepancy3") {
            this.app.selectedStrategyName = "L-ETF Discrepancy 3";
            this.app.strategyGoogleDocHelpUri = "https://docs.google.com/document/d/1Ey9Su6JcGGt2XtcCV0PgUTZ6F5waJ6tm5_c_phYmQZU";
            this.app.selectedStrategyWebApiName = "LETFDiscrepancy3";
        }
    }

    public StartBacktest_LEtfDistcrepancy(http: Http) {
        console.log("StartBacktest_LEtfDistcrepancy()");
        if (this.app.selectedStrategyMenuItemId != "idMenuItemLETFDiscrepancy1" && this.app.selectedStrategyMenuItemId != "idMenuItemLETFDiscrepancy2" && this.app.selectedStrategyMenuItemId != "idMenuItemLETFDiscrepancy3")
            return;

        //var url = "http://localhost:52174/qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=SRS-URE&rebalanceFrequency=5d";
        //var url = "http://localhost:52174/qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=" + this.app.selectedEtfPairs + "&rebalancingFrequency=" + this.app.rebalancingFrequency;
        //var url = "///qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=" + this.app.selectedEtfPairs + "&rebalancingFrequency=" + this.app.rebalancingFrequency;
        //var url = "/qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=" + this.app.selectedEtfPairs + "&rebalancingFrequency=" + this.app.rebalancingFrequency;
        //var url = "/qt?jsonp=JSON_CALLBACK" + this.app.generalInputParameters + "&strategy=" + this.app.selectedStrategyWebApiName + "&ETFPairs=" + this.selectedEtfPairs + "&rebalancingFrequency=" + this.rebalancingFrequency;
        var url = "/qt?" + this.app.generalInputParameters + "&strategy=" + this.app.selectedStrategyWebApiName + "&ETFPairs=" + this.selectedEtfPairs + "&rebalancingFrequency=" + this.rebalancingFrequency;

        http.get(url)
            .map(res => res.json()) // Call map on the response observable to get the parsed people object
            .subscribe(data => { // Subscribe to the observable to get the parsed people object and attach it to the component
                this.app.tradingViewChartName = "L-ETF Discrepancy 1";
                this.app.ProcessStrategyResult(data);
            }, error => {
                console.log("ERROR. StartBacktest(): data received error: " + error);
                this.app.errorMessage = error;
            });
    }
}

export function AngularInit_LEtfDistcrepancy(app: AppComponent) {
    console.log("AngularInit_LEtfDistcrepancy() START, AppComponent.version: " + app.m_versionShortInfo);

    //app.etfPairs = ["URE-SRS", "DRN-DRV", "FAS-FAZ", "XIV-VXX", "ZIV-VXZ",];
    //app.selectedEtfPairs = "URE-SRS";
    ////app.selectedEtfPairsIdx = 1;   // zero based, so it is December

    //app.rebalancingFrequency = "5d";
}