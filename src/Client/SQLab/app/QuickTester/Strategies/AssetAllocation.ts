import { AppComponent } from '../app.component';
import { Http } from '@angular/http';
import { Observable } from 'rxjs/Rx';
import 'rxjs/add/operator/map';
import { Strategy } from './Strategy';

export class AssetAllocation extends Strategy {

    public assets: string = "MDY,ILF,FEZ,EEM,EPP,VNQ,TLT";
    public assetsConstantLeverage: string = "";             // "1,1,1,-1,1.5,2,2";
    public rebalancingFrequency: string = "Weekly,Fridays";   // "Daily,2d"(trading days),"Weekly,Fridays", "Monthly,T-1"/"Monthly,T+0" (last/first trading day of the month)
    public pctChannelLookbackDays: string = "60-120-180-252";
    public pctChannelPctLimits: string = "30-70";
    public isPctChannelActiveEveryDay: string = "Yes";      // "Yes"
    public isPctChannelConditional: string = "";            // "Yes"

    public histVolLookbackDays: string = "20";
    public isCashAllocatedForNonActives: string = "";       // "Yes"
    public cashEquivalentTicker: string = "SHY";               // "SHY"
    public dynamicLeverageClmtParams: string = "";          // "SMA(SPX,50d,200d); PR(XLU,VTI,20d)";   // SPX 50/200 crossover; PR=PriceRatio of XLU/VTI for 20 days
    public uberVxxEventsParams: string = "";                // "FOMC;Holidays"  */
    public debugDetailToHtml: string = "Date,PV,AssetFinalWeights,CashWeight,AssetData,PctChannels";                  // "Date,PV,AssetFinalWeights,AssetData,PctChannels"

    constructor(p_app: AppComponent) {
        super("AssetAllocation", p_app);
        this.SetParams("idParamSetTAA_VaradiOriginal");     // temporary for Development
    }

    IsMenuItemIdHandled(p_subStrategyId: string): boolean {
        return p_subStrategyId == "idMenuItemTAA";
    }

    GetHtmlUiName(p_subStrategyId: string): string {     // go to HTML UI
        return "Tactical Asset Allocation (TAA)";
    }

    GetTradingViewChartName(p_subStrategyId: string): string {     // go to HTML UI
        return "Asset Allocation";
    }

    GetWebApiName(p_subStrategyId: string): string {
        return "TAA";
    }

    GetHelpUri(p_subStrategyId: string): string {     // go to HTML UI as gDoc URL
        return "https://docs.google.com/document/d/1onl2TKr-8RJqlQjsdIcN6X55fSK-4LO9k5T0Ts79-WU";
    }

    GetStrategyParams(p_subStrategyId: string): string {
        return "&Assets=" + this.assets + "&AssetsConstantLeverage=" + this.assetsConstantLeverage
            + "&RebalancingFrequency=" + this.rebalancingFrequency + "&PctChannelLookbackDays=" + this.pctChannelLookbackDays
            + "&PctChannelPctLimits=" + this.pctChannelPctLimits + "&IsPctChannelActiveEveryDay=" + this.isPctChannelActiveEveryDay + "&IsPctChannelConditional=" + this.isPctChannelConditional
            + "&HistVolLookbackDays=" + this.histVolLookbackDays + "&IsCashAllocatedForNonActives=" + this.isCashAllocatedForNonActives + "&CashEquivalentTicker=" + this.cashEquivalentTicker
            + "&DynamicLeverageClmtParams=" + this.dynamicLeverageClmtParams + "&UberVxxEventsParams=" + this.uberVxxEventsParams + "&DebugDetailToHtml=" + this.debugDetailToHtml;
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
            case "idParamSetTAA_GlobVnqIbb_WF":
                this.assets = "MDY,ILF,FEZ,EEM,EPP,VNQ,IBB"
                this.assetsConstantLeverage = "";
                this.rebalancingFrequency = "Daily,1d"    // Balazs: "•	Rebalance period: weekly rebalance is recommended (instead of monthly);"
                this.pctChannelLookbackDays = "60-120-180-252";  // Balazs: "•	Modified Percentile Channel Look-back Periods: 30-, 60-, 120- and 252-day is recommended (instead of 60-, 120-, 180- and 252-day);", but later decided we need Longer Term signals, so we don't switch off/on from stock too often', Balazs is using the original Varadi in all his Matlab code.
                this.pctChannelPctLimits = "30-70";             // Balazs: "•	Percentile Channel Threshold: original 25% is recommended;", but George overrides this
                this.isPctChannelActiveEveryDay = "Yes";
                this.isPctChannelConditional = "";
                this.histVolLookbackDays = "20";
                this.isCashAllocatedForNonActives = "Yes";
                this.cashEquivalentTicker = "TLT";
                this.dynamicLeverageClmtParams = "";
                this.uberVxxEventsParams = "";
                this.debugDetailToHtml = "Date,PV,AssetFinalWeights,CashWeight,AssetData,PctChannels";
                break;
            case "idParamSetTAA_Glob_Live":
                this.assets = "MDY,ILF,FEZ,EEM,EPP,VNQ"
                this.assetsConstantLeverage = "";
                this.rebalancingFrequency = "Monthly,T-1"
                this.pctChannelLookbackDays = "60-120-180-252";
                this.pctChannelPctLimits = "25-75";
                this.isPctChannelActiveEveryDay = "Yes";
                this.isPctChannelConditional = "";
                this.histVolLookbackDays = "20";
                this.isCashAllocatedForNonActives = "Yes";
                this.cashEquivalentTicker = "TLT";
                this.dynamicLeverageClmtParams = "";
                this.uberVxxEventsParams = "";
                this.debugDetailToHtml = "Date,PV,AssetFinalWeights,CashWeight,AssetData,PctChannels";
                break;
            case "idParamSetTAA_GC_Live":
                this.assets = "AAPL,AMZN,BABA,BIDU,FB,GOOGL,GWPH,NFLX,NVDA,PCLN,TSLA"
                this.assetsConstantLeverage = "";
                this.rebalancingFrequency = "Daily,1d"
                this.pctChannelLookbackDays = "60-120-180-252";
                this.pctChannelPctLimits = "25-75";
                this.isPctChannelActiveEveryDay = "Yes";
                this.isPctChannelConditional = "";
                this.histVolLookbackDays = "20";
                this.isCashAllocatedForNonActives = "Yes";
                this.cashEquivalentTicker = "";
                this.dynamicLeverageClmtParams = "";
                this.uberVxxEventsParams = "";
                this.debugDetailToHtml = "Date,PV,AssetFinalWeights,CashWeight,AssetData,PctChannels";
                break;
            case "idParamSetTAA_VaradiOriginal":
                this.assets = "VTI,ICF,LQD,DBC"     // Varadi uses SHY (1-3 Year Treasury Bond) for Cash, but Cash is fine. SHY = 0.47% CAGR only, not even half percent
                this.assetsConstantLeverage = "";
                this.rebalancingFrequency = "Monthly,T-1"
                this.pctChannelLookbackDays = "60-120-180-252";
                this.pctChannelPctLimits = "25-75";
                this.isPctChannelActiveEveryDay = "Yes";
                this.isPctChannelConditional = "";
                this.histVolLookbackDays = "20";
                this.isCashAllocatedForNonActives = "Yes";
                this.cashEquivalentTicker = "SHY";
                this.dynamicLeverageClmtParams = "";
                this.uberVxxEventsParams = "";
                this.debugDetailToHtml = "Date,PV,AssetFinalWeights,CashWeight,AssetData,PctChannels";
                break;
            default:
                //this.etf1 = "TVIX";
                //this.weight1 = "-35";      // %, negative is Short
                //this.etf2 = "TMV";
                //this.weight2 = "-65";      // %, negative is Short
                break;
        }
    }   // SetParams()

}

