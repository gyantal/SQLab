import {AppComponent} from '../app.component';
import {Http} from '@angular/http';
import {Observable} from 'rxjs/Rx';
import 'rxjs/add/operator/map';
import { Strategy } from './Strategy';

export class LEtfDistcrepancy extends Strategy {
    public etfPairs = ["URE-SRS", "DRN-DRV", "FAS-FAZ", "XIV-VXX", "ZIV-VXZ",];
    public selectedEtfPairs: string = "URE-SRS";
    //app.selectedEtfPairsIdx = 1;   // zero based, so it is December
    public rebalancingFrequency: string = "1d";  // "5d"
    // Harry Long params
    public etf1: string = "TVIX";
    public weight1: string = "-35";      // 25% short
    public etf2: string = "TMV";
    public weight2: string = "-65";

    constructor(p_app: AppComponent) {
        super("LEtfDistcrepancy", p_app);
        this.SetParams("idParamSetHL_- 35TVIX_- 65TMV");
    }

    IsMenuItemIdHandled(p_subStrategyId: string): boolean {
        return (p_subStrategyId == "idMenuItemLETFDiscrepancy1") || (p_subStrategyId == "idMenuItemLETFDiscrepancy2") || (p_subStrategyId == "idMenuItemLETFDiscrepancy3") || (p_subStrategyId == "idMenuItemLETFDiscrepancy4");
    }

    GetHtmlUiName(p_subStrategyId: string): string {     // go to HTML UI
        switch (p_subStrategyId) {
            case "idMenuItemLETFDiscrepancy1":
                return "L-ETF Discr.Test";
            case "idMenuItemLETFDiscrepancy2":
                return "L-ETF Discr.ToNeutral";
            case "idMenuItemLETFDiscrepancy3":
                return "L-ETF Discr.AddWinner";
            case "idMenuItemLETFDiscrepancy4":
                return "Harry Long";
            default:
                return "Wrong subStrategyId!";
        }
    }

    GetTradingViewChartName(p_subStrategyId: string): string {     // go to HTML UI
        return "L- ETF Discrepancy";
    }

    GetWebApiName(p_subStrategyId: string): string {
        switch (p_subStrategyId) {
            case "idMenuItemLETFDiscrepancy1":
                return "LETFDiscrepancy1";
            case "idMenuItemLETFDiscrepancy2":
                return "LETFDiscrepancy2";
            case "idMenuItemLETFDiscrepancy3":
                return "LETFDiscrepancy3";
            case "idMenuItemLETFDiscrepancy4":
                return "LETFDiscrepancy4";
            default:
                return "Wrong subStrategyId!";
        }
    }

    GetHelpUri(p_subStrategyId: string): string {     // go to HTML UI as gDoc URL
        switch (p_subStrategyId) {
            case "idMenuItemLETFDiscrepancy1":
            case "idMenuItemLETFDiscrepancy2":
            case "idMenuItemLETFDiscrepancy3":
                return "https://docs.google.com/document/d/1IpqNT6THDP5B1C-Vugt1fA96Lf1Ms9Tb-pq0LzT3GnY";
            case "idMenuItemLETFDiscrepancy4":
                return "https://docs.google.com/document/d/1nrWOxJNFYnLQvIxuF83ZypD_YUObiAv5nXa7Cq1x41s";
            default:
                return "Wrong subStrategyId!";
        }
    }

    GetStrategyParams(p_subStrategyId: string): string {
        return "&ETFPairs=" + this.selectedEtfPairs + "&RebalancingFrequency=" + this.rebalancingFrequency
            + "&ETF1=" + this.etf1 + "&Weight1=" + this.weight1 + "&ETF2=" + this.etf2 + "&Weight2=" + this.weight2;
    }








    public etfPairsChanged(newValue) {
        console.log("etfPairsChanged(): " + newValue);
        this.selectedEtfPairs = newValue;
        this.app.tipToUser = this.selectedEtfPairs + "+" + this.selectedEtfPairs;
    }

    public MenuItemParamSetsClicked(event) {
        console.log("MenuItemParamSetsClicked()");
        var target = event.target || event.srcElement || event.currentTarget;
        var idAttr = target.attributes.id;
        var idValue = idAttr.nodeValue;
        this.SetParams(idValue);
    }

    public SetParams(idValue) {
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

}

export function AngularInit_LEtfDistcrepancy(app: AppComponent) {
    console.log("AngularInit_LEtfDistcrepancy() START, AppComponent.version: " + app.m_versionShortInfo);

    //app.etfPairs = ["URE-SRS", "DRN-DRV", "FAS-FAZ", "XIV-VXX", "ZIV-VXZ",];
    //app.selectedEtfPairs = "URE-SRS";
    ////app.selectedEtfPairsIdx = 1;   // zero based, so it is December

    //app.rebalancingFrequency = "5d";
}