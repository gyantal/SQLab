"use strict";
var __extends = (this && this.__extends) || function (d, b) {
    for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p];
    function __() { this.constructor = d; }
    d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
};
require("rxjs/add/operator/map");
var Strategy_1 = require("./Strategy");
var LEtf = (function (_super) {
    __extends(LEtf, _super);
    function LEtf(p_app) {
        var _this = _super.call(this, "LEtf", p_app) || this;
        _this.assets = "TVIX,TMV";
        _this.assetsConstantWeightPct = "-35,-65"; // "-35,-65";
        _this.rebalancingFrequency = "Daily,1d"; // "Daily,2d"(trading days),"Weekly,Fridays", "Monthly,T-1"/"Monthly,T+0" (last/first trading day of the month)
        _this.SetParams("idParamSetHL_- 35TVIX_- 65TMV");
        return _this;
    }
    LEtf.prototype.IsMenuItemIdHandled = function (p_subStrategyId) {
        return (p_subStrategyId == "idMenuItemLETFDiscrRebToNeutral") || (p_subStrategyId == "idMenuItemLETFDiscrAddToWinner") || (p_subStrategyId == "idMenuItemLETFHarryLong");
    };
    LEtf.prototype.OnStrategySelected = function (p_subStrategyId) {
        if ((p_subStrategyId == "idMenuItemLETFDiscrRebToNeutral") || (p_subStrategyId == "idMenuItemLETFDiscrAddToWinner"))
            this.SetParams("idParamSetHL_-50URE_-50SRS");
        else if (p_subStrategyId == "idMenuItemLETFHarryLong")
            this.SetParams("idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy");
        return true;
    };
    LEtf.prototype.GetHtmlUiName = function (p_subStrategyId) {
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
    };
    LEtf.prototype.GetTradingViewChartName = function (p_subStrategyId) {
        return "L-ETF Discrepancy";
    };
    LEtf.prototype.GetWebApiName = function (p_subStrategyId) {
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
    };
    LEtf.prototype.GetHelpUri = function (p_subStrategyId) {
        switch (p_subStrategyId) {
            case "idMenuItemLETFDiscrRebToNeutral":
            case "idMenuItemLETFDiscrAddToWinner":
                return "https://docs.google.com/document/d/1IpqNT6THDP5B1C-Vugt1fA96Lf1Ms9Tb-pq0LzT3GnY";
            case "idMenuItemLETFHarryLong":
                return "https://docs.google.com/document/d/1nrWOxJNFYnLQvIxuF83ZypD_YUObiAv5nXa7Cq1x41s";
            default:
                return "Wrong subStrategyId!";
        }
    };
    LEtf.prototype.GetStrategyParams = function (p_subStrategyId) {
        return "&Assets=" + this.assets + "&AssetsConstantWeightPct=" + this.assetsConstantWeightPct
            + "&RebalancingFrequency=" + this.rebalancingFrequency;
    };
    //public etfPairsChanged(newValue) {
    //    console.log("etfPairsChanged(): " + newValue);
    //    this.selectedEtfPairs = newValue;
    //    this.app.tipToUser = this.selectedEtfPairs + "+" + this.selectedEtfPairs;
    //}
    LEtf.prototype.MenuItemParamSetsClicked = function (event) {
        console.log("MenuItemParamSetsClicked()");
        var target = event.target || event.srcElement || event.currentTarget;
        var idAttr = target.attributes.id;
        var idValue = idAttr.nodeValue;
        this.SetParams(idValue);
    };
    LEtf.prototype.SetParams = function (idValue) {
        switch (idValue) {
            case "idParamSetHL_-25TVIX_-75TMV":
                this.assets = "TVIX,TMV";
                this.assetsConstantWeightPct = "-25,-75"; // %, negative is Short
                break;
            case "idParamSetHL_50VXX_50XIV":
                this.assets = "VXX,XIV";
                this.assetsConstantWeightPct = "50,50"; // %, negative is Short
                break;
            case "idParamSetHL_-50VXX.SQ_225TLT":
                this.assets = "VXX.SQ,TLT";
                this.assetsConstantWeightPct = "-50,225"; // %, negative is Short
                break;
            case "idParamSetHL_-50VXX_225TLT":
                this.assets = "VXX,TLT";
                this.assetsConstantWeightPct = "-50,225"; // %, negative is Short
                break;
            case "idParamSetHL_50XIV_225TLT":
                this.assets = "XIV,TLT";
                this.assetsConstantWeightPct = "50,225"; // %, negative is Short
                break;
            case "idParamSetHL_25XIV_112TLT":
                this.assets = "XIV,TLT";
                this.assetsConstantWeightPct = "25,112"; // %, negative is Short
                break;
            case "idParamSetHL_-25TVIX_75CASH":
                this.assets = "TVIX,Cash";
                this.assetsConstantWeightPct = "-25,75"; // %, negative is Short
                break;
            case "idParamSetHL_25CASH_-75TMV":
                this.assets = "Cash,TMV";
                this.assetsConstantWeightPct = "25,-70"; // %, negative is Short
                break;
            // *** 50%-50% shorts ***
            case "idParamSetHL_-50URE_-50SRS":
                this.assets = "URE,SRS";
                this.assetsConstantWeightPct = "-50,-50"; // %, negative is Short
                break;
            case "idParamSetHL_-50DRN_-50DRV":
                this.assets = "DRN,DRV";
                this.assetsConstantWeightPct = "-50,-50"; // %, negative is Short
                break;
            case "idParamSetHL_-50FAS_-50FAZ":
                this.assets = "FAS,FAZ";
                this.assetsConstantWeightPct = "-50,-50"; // %, negative is Short
                break;
            case "idParamSetHL_-50VXX_-50XIV":
                this.assets = "VXX,XIV";
                this.assetsConstantWeightPct = "-50,-50"; // %, negative is Short
                break;
            case "idParamSetHL_-50VXZ_-50ZIV":
                this.assets = "VXZ,ZIV";
                this.assetsConstantWeightPct = "-50,-50"; // %, negative is Short
                break;
            case "idParamSetHL_-35TVIX_-65TMV":
                this.assets = "TVIX,TMV";
                this.assetsConstantWeightPct = "-35,-65"; // %, negative is Short
                break;
            case "idParamSetHL_-35TVIX_-25TMV_-28UNG_-8USO_-4JJC":
                this.assets = "TVIX,TMV,UNG,USO,JJC";
                this.assetsConstantWeightPct = "-35,-25,-28,-8,-4"; // %, negative is Short
                break;
            case "idParamSetHL_-70VXX.SQ_-75TLT_coctailDC":
                this.assets = "VXX.SQ,TLT,USO,UNG,JJC,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "-70,75,-8,-13,-4,10,5,0"; // %, negative is Short
                break;
            case "idParamSetHL_-70VXX.SQ_-75TLT_coctailAgy":
            default:
                this.assets = "VXX.SQ,TLT,USO,UNG,JJC,GLD,UUP,EEM";
                this.assetsConstantWeightPct = "-70,75,-8,-28,-4,0,0,0"; // %, negative is Short
                break;
        }
    };
    return LEtf;
}(Strategy_1.Strategy));
exports.LEtf = LEtf;
function AngularInit_LEtf(app) {
    console.log("AngularInit_LEtf() START, AppComponent.version: " + app.m_versionShortInfo);
    //app.etfPairs = ["URE-SRS", "DRN-DRV", "FAS-FAZ", "XIV-VXX", "ZIV-VXZ",];
    //app.selectedEtfPairs = "URE-SRS";
    ////app.selectedEtfPairsIdx = 1;   // zero based, so it is December
    //app.rebalancingFrequency = "5d";
}
exports.AngularInit_LEtf = AngularInit_LEtf;
//# sourceMappingURL=LEtf.js.map