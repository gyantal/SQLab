using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DbCommon
{
    public static partial class DbUtils
    {
        /// <summary> Returns DBUtils.DefaultAssetIDString() (e.g. "Stock(103)")
        /// if the ticker is unknown </summary>
        public static string ToString(this IAssetID p_assetID, TickerProvider p_tp, DateTime? p_timeUtc = null)
        {
            return (p_assetID == null) ? null : p_tp.GetTicker(p_assetID, p_timeUtc);
        }
    }

    public class TickerProvider
    {
        //DBManager m_dbManager;
        //IOptionProvider m_optionProvider;
        //IFuturesProvider m_futuresProvider;
        //OldStockTickers m_oldTickersCache;
        //ParseTickerHelper m_parseTickerHelper;

        static TickerProvider g_singleton;
        public static TickerProvider Singleton
        {
            get
            {
                if (g_singleton == null)
                {
                    g_singleton = new TickerProvider();
                    // do it offline temporarily
                }
                return g_singleton;
            }
        }

#if DEBUG
        // don't include All Tickers here, because that is 500KB in the cs file, increasing Github size and RAM requirement of the EXE too unnecessarily
        List<Tuple<byte, int, string>> m_tickerSnapshot201603 = new List<Tuple<byte, int, string>>()
        {
new Tuple<byte,int,string>(1,1,"USD"),
new Tuple<byte,int,string>(1,2,"EUR"),
new Tuple<byte,int,string>(1,3,"GBX"),
new Tuple<byte,int,string>(1,4,"JPY"),
new Tuple<byte,int,string>(1,5,"HUF"),
new Tuple<byte,int,string>(1,6,"CNY"),
new Tuple<byte,int,string>(1,255,"NaN"),
new Tuple<byte,int,string>(2,8000,"VXX"),
new Tuple<byte,int,string>(2,8836,"SVXY"),
new Tuple<byte,int,string>(8,4,"^DJI"),
new Tuple<byte,int,string>(8,1,"^GSPC"),
new Tuple<byte,int,string>(8,3,"^IXIC"),
new Tuple<byte,int,string>(8,6,"^RUI"),
new Tuple<byte,int,string>(8,5,"^RUT"),
new Tuple<byte,int,string>(8,7,"^SOX"),
new Tuple<byte,int,string>(8,2,"^VIX"),
new Tuple<byte,int,string>(8,8,"^VXN"),
new Tuple<byte,int,string>(8,9,"^VXV"),
new Tuple<byte,int,string>(8,11,"CCI-US"),
new Tuple<byte,int,string>(8,10,"WLIW")
        };
#endif


        /// <summary> Returns null if does not know the answer </summary>
        public string GetTicker(IAssetID p_assetID, DateTime? p_timeUtc = null)
        {
#if DEBUG
            foreach (var item in m_tickerSnapshot201603)
            {
                if (item.Item1 == (byte)p_assetID.AssetTypeID && item.Item2 == p_assetID.ID)
                    return item.Item3;
            }
            return p_assetID.ToString();
#else
            return p_assetID.AssetTypeID.ToString() + ":" + p_assetID.ID.ToString();
#endif

        }
    }

}
