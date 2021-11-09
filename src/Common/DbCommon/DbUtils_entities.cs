using SqCommon;
using System;
using System.Collections.Generic;
//using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace DbCommon
{
    public enum CountryID : byte    // there are 192 countries in the world. warning: 2009-06: the Company.BaseCountryID is in reality CountryCode
    {
        UnitedStates = 1,
        UnitedKingdom = 2,
        China = 3,
        Japan = 4,
        Germany = 5,
        France = 6,
        Canada = 7,
        Russia = 8,
        Brazil = 9,
        India = 10,
        Hungary = 11,

        // DBUtils.g_defaultMarketHolidays exploits that 0 < CountryID.USA,UK,Germany < 20

        Unknown = 255
    }

    public enum CurrencyID : byte   // there are 192 countries in the world, and less than 192 currencies
    {                               // PortfolioEvaluator.BulkPreparer.Plan() exploits that all values are in 1..62
        USD = 1,
        EUR = 2,
        GBX = 3,
        JPY = 4,
        HUF = 5,
        CNY = 6,

        //  Unknown = -1
        Unknown = 255       // AGY
    }

    public enum StockIndexID : short    // According to dbo.StockIndex
    {
        SP500 = 1,
        VIX,
        Nasdaq,
        DowJones,
        Russell2000,
        Russell1000,
        PHLX_Semiconductor,
        VXN,
        Unknown = -1
    }

    public enum StockExchangeID : sbyte // differs from dbo.StockExchange, which is 'int'
    {
        NASDAQ = 1,
        NYSE = 2,
        //[Description("NYSE MKT LLC")]
        AMEX = 3,
        //[Description("Pink OTC Markets")]
        PINK = 4,
        CDNX = 5,       // Canadian Venture Exchange, postfix: ".V"
        LSE = 6,        // London Stock Exchange, postfix: ".L"
        //[Description("XTRA")]
        XETRA = 7,      // Exchange Electronic Trading (Germany)
        CBOE = 8,
        //[Description("NYSE ARCA")]
        ARCA = 9,
        BATS = 10,
        //[Description("OTC Bulletin Boards")]
        OTCBB = 11,

        Unknown = -1    // BooleanFilterWith1CacheEntryPerAssetID.CacheRec.StockExchangeID exploits that values fit in an sbyte
                        // TickerProvider.OldStockTickers exploits that values fit in a byte
    }

    public enum DbTimeZoneID : byte   // dbo.StockExchange.TimeZone, there is another TimeZone id in Utils, not in DbUtils, so better to have a different name
    {
        [SystemTimeZoneId("Eastern Standard Time")]     // string ID for System.TimeZoneInfo.FindSystemTimeZoneById()
        EST = 1,
        [SystemTimeZoneId("GMT Standard Time")]
        GMT = 2,
        [SystemTimeZoneId("Central Europe Standard Time")]
        CEST = 3,

        Unknown = 255
    }
    /// <summary> Specifies a string ID for System.TimeZoneInfo.FindSystemTimeZoneById() </summary>
    internal sealed class SystemTimeZoneIdAttribute : Attribute
    {
        public string Id { get; private set; }
        public SystemTimeZoneIdAttribute(string p_name) { Id = p_name; }
    }

    public enum HQUserID            // According to dbo.HQUser
    {
        AllUser = 1,
        drcharmat = 2,
        gyantal = 3,
        zrabai = 4,
        robin = 5,
        test = 6,
        lnemeth = 7,
        sa = 8,
        SQExperiment = 9,
        SQArchive = 10,
        blukucz = 11,
        Unknown = -1
    }

    public enum HQUserGroupID       // According to dbo.HQUserGroup
    {
        Users = 1,
        Administrators = 2,
        Guests = 3,
        Researchers = 4,
        /// <summary> Bestows read+write permission over certain users' portfolios, contents-only.
        /// "Certain users" := 'Users' group. To narrow this set of users, you should introduce a new GroupID for every particular set. </summary>
        CanModifyUsersPortfolioContents = 5,
        /// <summary> Bestows Move + Delete + Create permission over all FileSystemItems that this user can see </summary>
        CanMoveFoldersBetweenUsers = 6,
        // IMPORTANT: keep values within [0..63] (except Unknown) -- exploited in DAC_DefaultAppCollection.Controllers.HQUserPermCache
        Unknown = -1
    }

    // Gain = currentValue - InsertedToPortfolio + WithdrawFromPortfolio
    public enum TransactionType : byte
    {
        Unknown = 0,
        Deposit = 1,                // to the portfolio from Outside (e.g. initial deposit), invested Assets
        WithdrawFromPortfolio = 2,  // to Outside
                                    /// <summary> Volume=1, Price=the cost, AssetTypeID=HardCash, SubTableID=CurrencyID </summary>
        TransactionCost = 3,
        //[Description("Buy")]
        BuyAsset = 4,
        //[Description("Sell")]
        SellAsset = 5,
        ShortAsset = 6,         // the begin of shorting, when we sell the stock
        CoverAsset = 7,         // the end of shorting, when we buy back the stock
        WriteOption = 8,            //
        BuybackWrittenOption = 9,   // the same 'logical' check as the (ShortAsset, CoverAsset) equivalent to (SellAsset, BuyAsset), but we would like a mental check
                                    /// <summary> Price=0, Volume>=0 </summary>
        ExerciseOption = 10,
        //SplitOccurred = 101,      // optional, maybe not here, it can be determined automatically
        //OptionExpired = 102,      // optional, maybe not here, it can be determined automatically
    }

    // dbo.DateProperties.Flags complements dbo.DateProperties.Comment: comments that have fixed string value
    // (because occur regularly) should be specified in Flags, to avoid accidental differences in the string values.
    // Whenever the Flags are changed, the dbo.DateProperties.StrProperties computed column should be updated
    // to keep the table human-readable. For an example on how to do this, see GDriveHedgeQuant\robin\SQLtest\DateProperties.160504.sql
    // If (row.date < 1998)
    // MarketOpenDayHolidays = ColombusDay OR SuperBowl
    // else
    // MarketOpenDayHolidays = ColombusDay OR SuperBowl OR VeteranDay
    // Balazs's HolidayResearch Excel table should contain old dates for SuperBowl and other holidays, Future dates should be collected by a Crawler, or by a Warning email to Supervisors.
    [Flags]
    public enum DatePropertiesFlags : short // dbo.DateProperties.Flags
    {
        //None = 0,

        // Flags specific to the USA:
        NewYear = 1,
        MLutherKing = 2,
        SuperBowl = 3,          // as of 2016, this is not a stock market holiday, only civil holiday, 2017-02-03: it is not in our database, but I am happy to skip this, it shouldn't be significant
        Presidents = 4,
        GoodFriday = 5,
        Memorial = 6,
        Independence = 7,
        Labor = 8,
        Columbus = 9,           // as of 2016, this is not a stock market holiday, only civil holiday
        Veterans = 10,          // as of 2016, this is not a stock market holiday, only civil holiday
        Thanksgiving = 11,
        Xmas = 12,
        Juneteenth = 13,    // Emancipation (slave freedom) day. The day was recognized as a federal holiday on June 17, 2021 by Joe Biden 
        // The above names are not in any particular order. New ones should be added - not inserted (avoid changing existing values).
        _KindOfUsaHoliday = 15,
        // New flags should be added BELOW the existing ones until the ↑two↓ abuts each other
        EcbMeeting = 2048,
        FomcMinutesRelease = 4096,
        FomcMeetingLastDay = 8192,

        _KindOfUsaHolidayAndAllRegularEvents = 15 + 2048 + 4096 + 8192,     // "911Attacks", "HurricaneSandy", "FuneralReagen" are StockMarketClosed days, but one time events, not regular

        // Flags specific to the UK:
        // ...

        // Global flags (for all countries):
        StockMarketClosed = 16384
    }

    public class DateProperty
    {
        public DateTime DateLoc { get; set; }       // Date. usually with time component of 0:0. Meaning it is in its local country/event time zone
        public CountryID CountryID { get; set; }
        public DatePropertiesFlags Flags { get; set; }
        public string Comment { get; set; }
    }

    public class StockExchangeTimeZoneData
    {
        public StockExchangeID StockExchangeID { get; set; }
        public CountryID CountryID { get; set; }
        public TimeZoneId TimeZoneId { get; set; }
        public TimeZoneInfo TimeZoneInfo { get; set; }
    }

    public class AssetDesc
    {
        public IAssetID AssetID { get; set; }
        public string Ticker { get; set; }
        public string FullTicker { get; set; }
        public StockExchangeID StockExchangeID { get; set; }
        public CurrencyID CurrencyID { get; set; }

    }

    public class SplitAndDividendInfo
    {
        public int StockID { get; set; }
        public DateTime TimeUtc { get; set; }   // database contains Local, but we convert it to Utc after loading

        /// <summary> Local 00:00 (usally) </summary>
        //public DateTime TimeLoc { get; set; }
        /// <summary> TimeLoc converted to UTC </summary>
        //public DateTime TimeUtc { get { return TimeLoc.ToUtc(StockExchangeID); } }
        //public DateTime TimeUtc { get { return TimeZoneInfo.ConvertTime(TimeLoc, DbUtils.g_timeZoneInfo[(int)StockExchangeID].TimeZoneInfo, TimeZoneInfo.Utc); } }
        //public StockExchangeID StockExchangeID { get; set; }
        public int NewVolume { get; set; }
        public int OldVolume { get; set; }
        public double DividendOrPrevClosePrice { get; set; }  // this is price if it is split; or dividend otherwise
        public bool IsSplit { get; set; }

        //public override string ToString()   // used for error log, e.g. in StockPriceAdjustmentFactor.ObtainData()
        //{
        //    return Utils.FormatInvCult(IsSplit ? "{0} Split {1}(new):{2}(old), ClosePrice={3:g6}"
        //        : "{0} Dividend {3:g6}",    // note: currency sign is appended to this string in TransactionsAccumulator.Event.ToString()
        //        DBUtils.IsTimeZoneInitialized ? Utils.UtcDateTime2Str(TimeUtc) : Utils.DateTime2Str(TimeLoc),
        //        NewVolume, OldVolume, DividendOrPrevClosePrice);
        //}
    }
}

