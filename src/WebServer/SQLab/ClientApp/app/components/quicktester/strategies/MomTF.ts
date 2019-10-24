import { QuickTesterComponent } from '../quicktester.component';
import { Http } from '@angular/http';
import { Observable } from 'rxjs/Rx';
import 'rxjs/add/operator/map';
import { Strategy } from './Strategy';

export class MomTF extends Strategy {

    public assets: string = "SPY";
    public assetsConstantLeverage: string = "1";                // "1,-1,1.5,2";
    public rebalancingFrequency: string = "Weekly,Fridays";     // "Daily,2d"(trading days),"Weekly,Fridays", "Monthly,T-1"/"Monthly,T+0" (last/first trading day of the month)
    public momOrTF: string = "Mom";
    public lookbackDays: string = "240";
    public excludeLastDays: string = "20";
    public isCashAllocatedForNonActives: string = "";           // "Yes"
    public cashEquivalentTicker: string = "";                   // "" (default, so to measure the effect of TF, not the effect of TLT at naive running),"SHY", "TLT"  // both SHY and TLT starts on 2002-07-26. SHY is more realistic cache. However, TLT is more meaningful substitute to use in real life.
    public isShortInsteadOfCash: string = "";                   // empty string means "No", only "Yes" means "Yes"
    public isVolScaledPos: string = "";                         // empty string means "No", only "Yes" means "Yes"
    public histVolLookbackDays: string = "";                    // only if isVolScaledPos=Yes

    public debugDetailToHtml: string = "Date,PV,AssetFinalWeights,CashWeight,AssetData";                  // "Date,PV,AssetFinalWeights,AssetData"

    constructor(p_app: QuickTesterComponent) {
        super("MomTF", p_app);
        this.SetParams("idParamSetMomTF_SPY12mTFTLT");     // temporary for Development
    }

    IsMenuItemIdHandled(p_subStrategyId: string): boolean {
        return p_subStrategyId == "idMenuItemMomTF";
    }

    GetHtmlUiName(p_subStrategyId: string): string {     // go to HTML UI
        return "Momentum / TF: TrendFollowing";
    }

    GetTradingViewChartName(p_subStrategyId: string): string {     // go to HTML UI
        return "Momentum or TF";
    }

    GetWebApiName(p_subStrategyId: string): string {
        return "MomTF";
    }

    GetHelpUri(p_subStrategyId: string): string {     // go to HTML UI as gDoc URL
        return "https://docs.google.com/document/d/1uLm3raWgkiW6ccdC1YFpC41iFeys6fr-FLFucMz5kug";
    }

    GetStrategyParams(p_subStrategyId: string): string {
        return "&Assets=" + this.assets + "&AssetsConstantLeverage=" + this.assetsConstantLeverage
            + "&RebalancingFrequency=" + this.rebalancingFrequency + "&MomOrTF=" + this.momOrTF
            + "&LookbackDays=" + this.lookbackDays + "&ExcludeLastDays=" + this.excludeLastDays + "&IsCashAllocatedForNonActives=" + this.isCashAllocatedForNonActives + "&CashEquivalentTicker=" + this.cashEquivalentTicker +
            "&IsShortInsteadOfCash=" + this.isShortInsteadOfCash + "&IsVolScaledPos=" + this.isVolScaledPos + "&HistVolLookbackDays=" + this.histVolLookbackDays + "&DebugDetailToHtml=" + this.debugDetailToHtml;
    }

    public MenuItemParamSetsClicked(event: any) {
        console.log("MenuItemParamSetsClicked()");
        var target = event.target || event.srcElement || event.currentTarget;
        var idAttr = target.attributes.id;
        var idValue = idAttr.nodeValue;
        this.SetParams(idValue);
    }

    public SetDefaultParams() {
        this.assets = "";
        this.assetsConstantLeverage = "1";
        this.rebalancingFrequency = "Daily,1d";
        this.momOrTF = "Mom";
        this.lookbackDays = "240";
        this.excludeLastDays = "20";
        this.isCashAllocatedForNonActives = "Yes";
        this.cashEquivalentTicker = "";
        this.isShortInsteadOfCash = "";
        this.isVolScaledPos = "";
        this.histVolLookbackDays = "";
        this.debugDetailToHtml = "Date,PV,AssetFinalWeights,CashWeight,AssetData";
    }

    public SetParams(idValue: any) {
        this.SetDefaultParams();
        switch (idValue) {
            case "idParamSetMomTF_SPY12mMom":
                this.assets = "SPY";
                this.momOrTF = "Mom";
                break;
            case "idParamSetMomTF_SPY12mTF":
                this.assets = "SPY";
                this.momOrTF = "TF";
                break;
            case "idParamSetMomTF_SPY12mMomTLT":
                this.assets = "SPY";
                this.momOrTF = "Mom";
                this.cashEquivalentTicker = "TLT";
                break;
            case "idParamSetMomTF_SPY12mTFTLT":
                this.assets = "SPY";
                this.momOrTF = "TF";
                this.cashEquivalentTicker = "TLT";
                break;
            case "idParamSetMomTF_SPY12mMom0_BuyHold": // for comparing Momentum (or TF) vs. Buy&Hold
                this.assets = "SPY";
                this.momOrTF = "Mom";
                this.lookbackDays = "0";            // lookback days 0 means we compare current day price >= 0 days before. It is always true, so Momentum will be bullish on every day.
                this.excludeLastDays = "0";
                break;        
            case "idParamSetMomTF_TAA12mMoTLT":     // for comparing Momentum (or TF) vs. TAA Varadi's percentile channels
                this.assets = "MDY,ILF,FEZ,EEM,EPP,VNQ,IBB";
                this.assetsConstantLeverage = "2,2,2,2,2,2,2";
                this.momOrTF = "TF";
                this.lookbackDays = "150";
                this.excludeLastDays = "20";
                this.cashEquivalentTicker = "TLT";
                this.isVolScaledPos = "Yes";
                this.histVolLookbackDays = "20";
                break;
            default:
                break;
        }
    }   // SetParams()

}

