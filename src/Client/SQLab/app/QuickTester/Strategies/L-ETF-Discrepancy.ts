import {AppComponent} from '../app.component';
import {Http} from '@angular/http';
import {Observable} from 'rxjs/Rx';
import 'rxjs/add/operator/map';
import { Strategy } from './Strategy';

export class LEtfDistcrepancy extends Strategy {
    public assets: string = "TVIX,TMV";
    public assetsConstantWeightPct: string = "-35,-65";             // "-35,-65";
    public rebalancingFrequency: string = "Daily,1d";   // "Daily,2d"(trading days),"Weekly,Fridays", "Monthly,T-1"/"Monthly,T+0" (last/first trading day of the month)


    //public etfPairs = ["URE-SRS", "DRN-DRV", "FAS-FAZ", "XIV-VXX", "ZIV-VXZ",];
    //public selectedEtfPairs: string = "URE-SRS";
    ////app.selectedEtfPairsIdx = 1;   // zero based, so it is December
    //public rebalancingFrequency: string = "1d";  // "5d"
    //// Harry Long params
    //public etf1: string = "TVIX";
    //public weight1: string = "-35";      // 25% short
    //public etf2: string = "TMV";
    //public weight2: string = "-65";

    constructor(p_app: AppComponent) {
        super("LEtfDistcrepancy", p_app);
        this.SetParams("idParamSetHL_- 35TVIX_- 65TMV");
    }

    IsMenuItemIdHandled(p_subStrategyId: string): boolean {
        return (p_subStrategyId == "idMenuItemLETFDiscrepancy1") || (p_subStrategyId == "idMenuItemLETFDiscrepancy2") || (p_subStrategyId == "idMenuItemLETFDiscrepancy3") || (p_subStrategyId == "idMenuItemLETFDiscrepancy4");
    }

    OnStrategySelected(p_subStrategyId: string): boolean {
        if ((p_subStrategyId == "idMenuItemLETFDiscrepancy1") || (p_subStrategyId == "idMenuItemLETFDiscrepancy2") || (p_subStrategyId == "idMenuItemLETFDiscrepancy3"))
            this.SetParams("idParamSetHL_-50URE_-50SRS");
        else if (p_subStrategyId == "idMenuItemLETFDiscrepancy4")
            this.SetParams("idParamSetHL_-35TVIX_-65TMV");
        return true;
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
        return "L-ETF Discrepancy";
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
        return "&Assets=" + this.assets + "&AssetsConstantWeightPct=" + this.assetsConstantWeightPct
            + "&RebalancingFrequency=" + this.rebalancingFrequency;
    }








    //public etfPairsChanged(newValue) {
    //    console.log("etfPairsChanged(): " + newValue);
    //    this.selectedEtfPairs = newValue;
    //    this.app.tipToUser = this.selectedEtfPairs + "+" + this.selectedEtfPairs;
    //}

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
                this.assets = "TVIX,TMV";
                this.assetsConstantWeightPct = "-25,-75";   // %, negative is Short
                break;
            case "idParamSetHL_50VXX_50XIV":
                this.assets = "VXX,XIV";
                this.assetsConstantWeightPct = "50,50";   // %, negative is Short
                break;
            case "idParamSetHL_-50VXX.SQ_225TLT":
                this.assets = "VXX.SQ,TLT";
                this.assetsConstantWeightPct = "-50,225";   // %, negative is Short
                break;
            case "idParamSetHL_-50VXX_225TLT":
                this.assets = "VXX,TLT";
                this.assetsConstantWeightPct = "-50,225";   // %, negative is Short
                break;
            case "idParamSetHL_50XIV_225TLT":
                this.assets = "XIV,TLT";
                this.assetsConstantWeightPct = "50,225";   // %, negative is Short
                break;
            case "idParamSetHL_25XIV_112TLT":   // long only strategy (good) play with half leverage. would we do it? No forced Buy-in of shorts.
                this.assets = "XIV,TLT";
                this.assetsConstantWeightPct = "25,112";   // %, negative is Short
                break;
            case "idParamSetHL_-25TVIX_75CASH":
                this.assets = "TVIX,Cash";
                this.assetsConstantWeightPct = "-25,75";   // %, negative is Short
                break;
            case "idParamSetHL_25CASH_-75TMV":
                this.assets = "Cash,TMV";
                this.assetsConstantWeightPct = "25,-70";   // %, negative is Short
                break;
            // *** 50%-50% shorts ***
            case "idParamSetHL_-50URE_-50SRS":
                this.assets = "URE,SRS";
                this.assetsConstantWeightPct = "-50,-50";   // %, negative is Short
                break;
            case "idParamSetHL_-50DRN_-50DRV":
                this.assets = "DRN,DRV";
                this.assetsConstantWeightPct = "-50,-50";   // %, negative is Short
                break;
            case "idParamSetHL_-50FAS_-50FAZ":
                this.assets = "FAS,FAZ";
                this.assetsConstantWeightPct = "-50,-50";   // %, negative is Short
                break;
            case "idParamSetHL_-50VXX_-50XIV":
                this.assets = "VXX,XIV";
                this.assetsConstantWeightPct = "-50,-50";   // %, negative is Short
                break;
            case "idParamSetHL_-50VXZ_-50ZIV":
                this.assets = "VXZ,ZIV";
                this.assetsConstantWeightPct = "-50,-50";   // %, negative is Short
                break;
            case "idParamSetHL_-35TVIX_-65TMV":
            default:
                this.assets = "TVIX,TMV";
                this.assetsConstantWeightPct = "-35,-65";   // %, negative is Short
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