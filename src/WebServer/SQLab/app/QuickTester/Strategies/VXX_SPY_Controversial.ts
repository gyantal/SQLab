import {AppComponent} from '../app.component';
import {Http} from '@angular/http';
import {Observable} from 'rxjs/Rx';
import 'rxjs/add/operator/map';
import { Strategy } from './Strategy';

export class VXX_SPY_Controversial extends Strategy {
    public vxxLongOrShortTrade = ["Long", "Short", "Cash"];
    public selectedVXXLongOrShortTrade: string = "Cash";
    public spyMinPctMove = "0.01";
    public vxxMinPctMove = "0.01";   // corresponding to 0.25% of VIX move, with VXX Beta = 2 approximately
    // Balazs's parameter was 0.1% and 0.125%, but that decreased the profit
    // with spyMinPctMove == 0.01, vxxMinPctMove = 0.01, go To Cash: I got better CAGR than the Going Short (Going Long is bad, because of volatility drag)
    // increasing vxxMinPctMove is not good, because when vxxPctMove is very, very high, next day can be strong MR, so VXX can go down a lot. We don't want to miss those profits, so we don't increase the vxxMinPctMove too much
    //this.app.selectedEtfPairsIdx = 1;   // zero based, so it is December
    
    constructor(p_app: AppComponent) {
        super("VXX_SPY_Controversial", p_app);
    }

    IsMenuItemIdHandled(p_subStrategyId: string): boolean {
        return p_subStrategyId == "idMenuItemVXX_SPY_Controversial";
    }

    GetHtmlUiName(p_subStrategyId: string): string {     // go to HTML UI
        return "Buy&Hold XIV with VXX-SPY ControversialDay: Cash if VXX & SPY move in the same direction";
    }

    GetTradingViewChartName(p_subStrategyId: string): string {     // go to HTML UI
        return "VXX-SPY ControversialDay";
    }

    GetWebApiName(p_subStrategyId: string): string {
        return "VXX_SPY_Controversial";
    }

    GetHelpUri(p_subStrategyId: string): string {     // go to HTML UI as gDoc URL
        return "https://docs.google.com/document/d/1G1gjvt9GdqB4yrAvLV4ELnVDYNd587tovcWrVzTwqak";
    }

    GetStrategyParams(p_subStrategyId: string): string {
        return "&SpyMinPctMove=" + this.spyMinPctMove + "&VxxMinPctMove=" + this.vxxMinPctMove + "&LongOrShortTrade=" + this.selectedVXXLongOrShortTrade;
    }



    public vxxLongOrShortTradeChanged(newValue) {
        console.log("vxxLongOrShortTradeChanged(): " + newValue);
        this.selectedVXXLongOrShortTrade = newValue;
        this.app.tipToUser = this.selectedVXXLongOrShortTrade + "+" + this.selectedVXXLongOrShortTrade;
    }

}