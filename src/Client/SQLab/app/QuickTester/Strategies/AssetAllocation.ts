import {AppComponent} from '../app.component';
import {Http} from '@angular/http';
import {Observable} from 'rxjs/Rx';
import 'rxjs/add/operator/map';
import { Strategy } from './Strategy';

export class AssetAllocation extends Strategy {

    constructor(p_app: AppComponent) {
        super("AssetAllocation", p_app);
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
        return "";
    }


}

