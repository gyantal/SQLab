import {AppComponent} from './app.component';
import {Http} from '@angular/http';
import {Observable} from 'rxjs/Rx';
import 'rxjs/add/operator/map';

export class LEtfDistcrepancy {
    public etfPairs = ["URE-SRS", "DRN-DRV", "FAS-FAZ", "XIV-VXX", "ZIV-VXZ",];
    public selectedEtfPairs: string = "URE-SRS";
    //app.selectedEtfPairsIdx = 1;   // zero based, so it is December
    public rebalancingFrequency: string = "1d";  // "5d"
    // Harry Long params
    public etf1: string = "TVIX";
    public weight1: string = "-35";      // 25% short
    public etf2: string = "TMV";
    public weight2: string = "-65";

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
            this.app.selectedStrategyName = "L-ETF Discr.Test";
            this.app.strategyGoogleDocHelpUri = "https://docs.google.com/document/d/1IpqNT6THDP5B1C-Vugt1fA96Lf1Ms9Tb-pq0LzT3GnY";
            this.app.selectedStrategyWebApiName = "LETFDiscrepancy1";
        }
        if (this.app.selectedStrategyMenuItemId == "idMenuItemLETFDiscrepancy2") {
            this.app.selectedStrategyName = "L-ETF Discr.ToNeutral";
            this.app.strategyGoogleDocHelpUri = "https://docs.google.com/document/d/1IpqNT6THDP5B1C-Vugt1fA96Lf1Ms9Tb-pq0LzT3GnY";
            this.app.selectedStrategyWebApiName = "LETFDiscrepancy2";
        }
        if (this.app.selectedStrategyMenuItemId == "idMenuItemLETFDiscrepancy3") {
            this.app.selectedStrategyName = "L-ETF Discr.AddWinner";
            this.app.strategyGoogleDocHelpUri = "https://docs.google.com/document/d/1IpqNT6THDP5B1C-Vugt1fA96Lf1Ms9Tb-pq0LzT3GnY";
            this.app.selectedStrategyWebApiName = "LETFDiscrepancy3";
        }
        if (this.app.selectedStrategyMenuItemId == "idMenuItemLETFDiscrepancy4") {
            this.app.selectedStrategyName = "Harry Long";
            this.app.strategyGoogleDocHelpUri = "https://docs.google.com/document/d/1nrWOxJNFYnLQvIxuF83ZypD_YUObiAv5nXa7Cq1x41s";
            this.app.selectedStrategyWebApiName = "LETFDiscrepancy4";
        }
    }

    public MenuItemParamSetsClicked(event) {
        console.log("MenuItemParamSetsClicked()");
        var target = event.target || event.srcElement || event.currentTarget;
        var idAttr = target.attributes.id;
        var idValue = idAttr.nodeValue;

        switch (idValue) {
            case "idParamSetHL_-25TVIX_-75TMV":     // original Harry Long
                this.etf1 = "TVIX";
                this.weight1 = "-25";      // %, negative is Short
                this.etf2 = "TMV";
                this.weight2 = "-75";      // %, negative is Short
                break;
            case "idParamSetHL_-50VXX_-50XIV":
                this.etf1 = "VXX";
                this.weight1 = "-50";      // %, negative is Short
                this.etf2 = "XIV";
                this.weight2 = "-50";      // %, negative is Short
                break;
            case "idParamSetHL_50VXX_50XIV":
                this.etf1 = "VXX";
                this.weight1 = "50";      // %, negative is Short
                this.etf2 = "XIV";
                this.weight2 = "50";      // %, negative is Short
                break;
            case "idParamSetHL_-50URE_-50SRS":
                this.etf1 = "URE";
                this.weight1 = "-50";      // %, negative is Short
                this.etf2 = "SRS";
                this.weight2 = "-50";      // %, negative is Short
                break;
            case "idParamSetHL_-50VXX.SQ_225TLT":
                this.etf1 = "VXX.SQ";
                this.weight1 = "-50";      // %, negative is Short
                this.etf2 = "TLT";
                this.weight2 = "225";      // %, negative is Short
                break;
            case "idParamSetHL_-50VXX_225TLT":
                this.etf1 = "VXX";
                this.weight1 = "-50";      // %, negative is Short
                this.etf2 = "TLT";
                this.weight2 = "225";      // %, negative is Short
                break;
            case "idParamSetHL_50XIV_225TLT":
                this.etf1 = "XIV";
                this.weight1 = "50";      // %, negative is Short
                this.etf2 = "TLT";
                this.weight2 = "225";      // %, negative is Short
                break;
            case "idParamSetHL_25XIV_112TLT":   // long only strategy (good) play with half leverage. would we do it? No forced Buy-in of shorts.
                this.etf1 = "XIV";
                this.weight1 = "25";      // %, negative is Short
                this.etf2 = "TLT";
                this.weight2 = "112";      // %, negative is Short
                break;
            case "idParamSetHL_-25TVIX_75CASH":
                this.etf1 = "TVIX";
                this.weight1 = "-25";      // %, negative is Short
                this.etf2 = "Cash";
                this.weight2 = "75";      // %, negative is Short
                break;
            case "idParamSetHL_25CASH_-75TMV":
                this.etf1 = "Cash";
                this.weight1 = "25";      // %, negative is Short
                this.etf2 = "TMV";
                this.weight2 = "-75";      // %, negative is Short
                break;
            case "idParamSetHL_-35TVIX_-65TMV":
            default:
                this.etf1 = "TVIX";
                this.weight1 = "-35";      // %, negative is Short
                this.etf2 = "TMV";
                this.weight2 = "-65";      // %, negative is Short
                break;
        }

    }

    public StartBacktest_LEtfDistcrepancy(http: Http) {
        console.log("StartBacktest_LEtfDistcrepancy()");
        if (this.app.selectedStrategyMenuItemId != "idMenuItemLETFDiscrepancy1" && this.app.selectedStrategyMenuItemId != "idMenuItemLETFDiscrepancy2" && this.app.selectedStrategyMenuItemId != "idMenuItemLETFDiscrepancy3" && this.app.selectedStrategyMenuItemId != "idMenuItemLETFDiscrepancy4")
            return;

        //var url = "http://localhost:52174/qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=SRS-URE&rebalanceFrequency=5d";
        //var url = "http://localhost:52174/qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=" + this.app.selectedEtfPairs + "&rebalancingFrequency=" + this.app.rebalancingFrequency;
        //var url = "///qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=" + this.app.selectedEtfPairs + "&rebalancingFrequency=" + this.app.rebalancingFrequency;
        //var url = "/qt?jsonp=JSON_CALLBACK&strategy=LETFDiscrepancy1&ETFPairs=" + this.app.selectedEtfPairs + "&rebalancingFrequency=" + this.app.rebalancingFrequency;
        //var url = "/qt?jsonp=JSON_CALLBACK" + this.app.generalInputParameters + "&strategy=" + this.app.selectedStrategyWebApiName + "&ETFPairs=" + this.selectedEtfPairs + "&rebalancingFrequency=" + this.rebalancingFrequency;
        var url = "/qt?" + this.app.generalInputParameters + "&strategy=" + this.app.selectedStrategyWebApiName
            + "&ETFPairs=" + this.selectedEtfPairs + "&RebalancingFrequency=" + this.rebalancingFrequency
            + "&ETF1=" + this.etf1 + "&Weight1=" + this.weight1 + "&ETF2=" + this.etf2 + "&Weight2=" + this.weight2;

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