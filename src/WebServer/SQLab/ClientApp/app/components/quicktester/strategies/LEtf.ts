import { QuickTesterComponent } from '../quicktester.component';
import {Http} from '@angular/http';
import {Observable} from 'rxjs/Rx';
import 'rxjs/add/operator/map';
import { Strategy } from './Strategy';

export class LEtf extends Strategy {
    public assets: string = "TVIX,TMV";
    public assetsConstantWeightPct: string = "-35,-65";             // "-35,-65";
    public rebalancingFrequency: string = "Daily,1d";   // "Daily,2d"(trading days),"Weekly,Fridays", "Monthly,T-1"/"Monthly,T+0" (last/first trading day of the month)

    constructor(p_app: QuickTesterComponent) {
        super("LEtf", p_app);
        this.SetParams("idParamSetHL_- 35TVIX_- 65TMV");
    }

    IsMenuItemIdHandled(p_subStrategyId: string): boolean {
        return (p_subStrategyId == "idMenuItemLETFDiscrRebToNeutral") || (p_subStrategyId == "idMenuItemLETFDiscrAddToWinner") || (p_subStrategyId == "idMenuItemLETFHarryLong");
    }

    OnStrategySelected(p_subStrategyId: string): boolean {
        if ((p_subStrategyId == "idMenuItemLETFDiscrRebToNeutral") || (p_subStrategyId == "idMenuItemLETFDiscrAddToWinner"))
            this.SetParams("idParamSetHL_-50URE_-50SRS");
        else if (p_subStrategyId == "idMenuItemLETFHarryLong")
            this.SetParams("idParamSetHL_-50Comb.SQ_-120H_coctailAgy6");
        return true;
    }

    GetHtmlUiName(p_subStrategyId: string): string {     // go to HTML UI
        switch (p_subStrategyId) {
            case "idMenuItemLETFDiscrRebToNeutral":
                return "L-ETF Discr.ToNeutral";
            case "idMenuItemLETFDiscrAddToWinner":
                return "L-ETF Discr.AddWinner";
            case "idMenuItemLETFHarryLong":
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
            case "idMenuItemLETFDiscrRebToNeutral":
                return "LETFDiscrRebToNeutral";
            case "idMenuItemLETFDiscrAddToWinner":
                return "LETFDiscrAddToWinner";
            case "idMenuItemLETFHarryLong":
                return "LETFHarryLong";
            default:
                return "Wrong subStrategyId!";
        }
    }

    GetHelpUri(p_subStrategyId: string): string {     // go to HTML UI as gDoc URL
        switch (p_subStrategyId) {
            case "idMenuItemLETFDiscrRebToNeutral":
            case "idMenuItemLETFDiscrAddToWinner":
                return "https://docs.google.com/document/d/1IpqNT6THDP5B1C-Vugt1fA96Lf1Ms9Tb-pq0LzT3GnY";
            case "idMenuItemLETFHarryLong":
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

    public MenuItemParamSetsClicked(event: any) {
        console.log("MenuItemParamSetsClicked()");
        var target = event.target || event.srcElement || event.currentTarget;
        var idAttr = target.attributes.id;
        var idValue = idAttr.nodeValue;
        this.SetParams(idValue);
    }

    public SetParams(idValue: any) {
        switch (idValue) {
            case "idParamSetHL_-25TVIX_-75TMV":     // original Harry Long
                this.assets = "TVIX,TMV";
                this.assetsConstantWeightPct = "-25,-75";   // %, negative is Short
                break;
            case "idParamSetHL_50VXX_50XIV":
                this.assets = "VXXB,XIV";
                this.assetsConstantWeightPct = "50,50";   // %, negative is Short
                break;
            case "idParamSetHL_-50VXX.SQ_225TLT":
                this.assets = "VXXB.SQ,TLT";
                this.assetsConstantWeightPct = "-50,225";   // %, negative is Short
                break;
            case "idParamSetHL_-50VXX_225TLT":
                this.assets = "VXXB,TLT";
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
                this.assets = "VXXB,XIV";
                this.assetsConstantWeightPct = "-50,-50";   // %, negative is Short
                break;
            case "idParamSetHL_-50VXZ_-50ZIV":
                this.assets = "VXZB,ZIV";
                this.assetsConstantWeightPct = "-50,-50";   // %, negative is Short
                break;
            case "idParamSetHL_-35TVIX_-65TMV":
                this.assets = "TVIX,TMV";
                this.assetsConstantWeightPct = "-35,-65";   // %, negative is Short
                break;
            case "idParamSetHL_-35TVIX_-25TMV_-28UNG_-8USO_-4JJC":
                this.assets = "TVIX,TMV,UNG,USO,JJCTF";
                this.assetsConstantWeightPct = "-35,-25,-28,-8,-4";   // %, negative is Short
                break;
            case "idParamSetHL_-70VXX.SQ_-75TLT_coctailDC":
                this.assets = "VXXB.SQ,TLT,USO,UNG,JJCTF,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "-70,75,-8,-13,-4,10,5,0";   // %, negative is Short
                break;
            case "idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy":
                this.assets = "VXXB.SQ,TLT,USO,UNG,JJCTF,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "-70,75,-8,-28,-4,0,0,0";   // %, negative is Short
                break;
            case "idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy2":
                this.assets = "VXXB.SQ,TLT,USO,UNG,JJCTF,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "-70,105,-10,-30,-5,0,0,0";   // %, negative is Short
                break;
            case "idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy3":   // Markowitz MPT optimal weight using 100% allocation
                this.assets = "VXXB.SQ,TLT,USO,UNG,JJCTF,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "-70,111,-11,-34,0,0,0,0";   // %, negative is Short
                break;
            case "idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy4":   // Markowitz MPT optimal weight using 135% allocation
                this.assets = "VXXB.SQ,TLT,USO,UNG,JJCTF,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "-70,171,-17,-52,0,0,0,0";   // %, negative is Short
                break;
            case "idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy5":   // Markowitz MPT optimal weight using 135% allocation
                this.assets = "VXXB.SQ,TLT,USO,UNG,CPER,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "-70,213,-21,-66,0,0,0,0";   // %, negative is Short
                break;
            case "idParamSetHL_-50Comb.SQ_-80H_coctailAgy6":   // shortVol is 50%, Hedge: 80%
                this.assets = "SVXY.SQ,VXXB.SQ,ZIV.SQ,TQQQ.SQ,TLT,USO,UNG,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "15,-5, 10, 20, 171,-15,-54,0,0,0";   // %, negative is Short
                break;
            case "idParamSetHL_-50Comb.SQ_-100H_coctailAgy6":   // shortVol is 50%, Hedge: 100%
                this.assets = "SVXY.SQ,VXXB.SQ,ZIV.SQ,TQQQ.SQ,TLT,USO,UNG,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "15,-5, 10, 20, 213,-21,-66,0,0,0";   // %, negative is Short
                break;
            case "idParamSetHL_-50Comb.SQ_-120H_coctailAgy6":   // shortVol is 55%, Hedge: 120%, introducing SVXY!Light0.5x.SQ
                // 2018-04: JJC went to JJCTF and went to OTC. Because it is OTC, IB doesn't give realtime price, so chart will go until yesterday only. CPER has realtime price, but its history from 2011, instead of 2007. As JJCTF weight is 0, delete it from here. Backtest will be faster anyway.
            default:
                this.assets = "SVXY!Light0.5x.SQ,VXXB.SQ,ZIV.SQ,TQQQ.SQ,TLT,USO,UNG,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "15,-5, 10, 25, 255,-27,-78,0,0,0";   // %, negative is Short
                break;
        }
    }

}

export function AngularInit_LEtf(app: QuickTesterComponent) {
    console.log("AngularInit_LEtf() START, AppComponent.version: " + app.m_versionShortInfo);

    //app.etfPairs = ["URE-SRS", "DRN-DRV", "FAS-FAZ", "XIV-VXX", "ZIV-VXZB",];
    //app.selectedEtfPairs = "URE-SRS";
    ////app.selectedEtfPairsIdx = 1;   // zero based, so it is December

    //app.rebalancingFrequency = "5d";
}