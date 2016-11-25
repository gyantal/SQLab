using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualBroker
{
    public class TAAConfig
    {
        public string[] Tickers = new string[]          { "MDY","ILF","FEZ","EEM","EPP","VNQ","IBB" };   // 7 items
        public string[] Traded2xTickers = new string[]  { "MVV", "ILF", "FEZ", "EEM", "EPP", "URE", "BIB" };   // 7 items, https://www.stocktrader.com/long-etfs-bullish-etf-funds-2x-3x-leverage/
        public string[] Traded3xTickers = new string[]  { "MVV", "ILF", "FEZ", "EEM", "EPP", "DRN", "LABU" };   // check the viability of the triple ETFs later.
        public double[] TradedLeverages = new double[] { 1, 2, 2, 2, 2, 1, 1 };     // ILF has no 2x, only "LBJ: Daily Latin America Bull 3x – S&P Latin America 40 Index"

        public DateTime CommonAssetStartDate = new DateTime(2004, 9, 29);       // VNQ startdate: 2004-09-29, others started earlier. (TLT: 2002-07-30). Helpful so we don't download unnecessary data from SQLdb
        public double[] AssetsConstantLeverages = new double[] { 2, 2, 2, 2, 2, 2, 2 };
        public RebalancingPeriodicity RebalancingPeriodicity = RebalancingPeriodicity.Daily;
        public int DailyRebalancingDays = 1;
        public DayOfWeek WeeklyRebalancingWeekDay = DayOfWeek.Friday;
        public int MonthlyRebalancingOffset = -1;       // +1 means T+1, -1 means T-1
        public int[] PctChannelLookbackDays = new int[] { 50, 80, 120, 150 };
        public double PctChannelPctLimitLower = 20 / 100.0;
        public double PctChannelPctLimitUpper = 75 / 100.0;
        public bool IsPctChannelActiveEveryDay = true;
        public int HistVolLookbackDays = 20;
        public bool IsCashAllocatedForNonActives = true;
        public string CashEquivalentTicker = "TLT";
    }
}
