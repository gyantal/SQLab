using SqCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DbCommon
{
    public static partial class SqlTools
    {

        // 2016-03-10: "System.Data.Linq" doesn't exist in DotNetCore, so I have to do it manually by text SQL queries. Downside of DotNetCore and Linux
        // Why to use SqlParameters: http://stackoverflow.com/questions/22025928/why-use-sqlparameter-and-not-embed-parameters
        // 1. Against SQL injection: but my Input data of Parameters is not tampered by any custom user-input editbox. So, SQL injection is impossible in my Console app
        // 2. using parameters also allows SQL Server to take advantage of cached query plans. (or Store the final Procedure as a Stored Procedure at the end for even faster Compiling/Caching)
        // "Being best practice, I used SqlParameter, but I've noticed there are disadvantages to this. First, debugging is not straightforward: I can't see the actual stored proc call through VS debugging. It doesn't show up in SQL Profiler if the call breaks, so I can't get the call from there."
        // So, I don't use it for now. Later, when the query works, put it into Stored SQL procedure. (or write it in C# on the server)
#region DeveloperNotes
        /*            

        // "I am not declaring myself as an expert but no, SQL Server does not support arrays", http://stackoverflow.com/questions/8349915/set-variable-value-to-array-of-strings
        // one approach is parse into a TEMP table then join on that. like this: 
        //declare  @tab table(FirstName  varchar(100)) ;            insert into @tab values('John'), ('Sarah'), ('George')
        // but then it is a lot of CPU computation on the server, inserting to a table one by one. Which we don't want, we want to optimize CPU on SQLServer.
        // Better to just create the full SQL text query by hand in C#  "SELECT * FROM myTable WHERE myColumn in ('1','2','3');"

        **************** 1. The simple solution was this
        "
        SELECT ID, Name, UserID FROM FileSystemItem WHERE (Name='! AdaptiveConnor,VXX autocorrelation (VXX-XIV, stocks, noHedge) Agy Live' AND UserID=3) OR (Name='! AdaptiveConnor,VXX autocorrelation (VXX-XIV, stocks, noHedge) Live' AND UserID=2)
        ;
        SELECT PortfolioID, TransactionType, AssetTypeID, AssetSubTableID, Volume, Price, Date FROM PortfolioItem INNER JOIN
            (SELECT ID FROM FileSystemItem WHERE (Name='! AdaptiveConnor,VXX autocorrelation (VXX-XIV, stocks, noHedge) Agy Live' AND UserID=3) OR (Name='! AdaptiveConnor,VXX autocorrelation (VXX-XIV, stocks, noHedge) Live' AND UserID=2)) as FileSystemReduced
            ON PortfolioItem.PortfolioID = FileSystemReduced.ID
        ;
        SELECT StockSplitDividend.* FROM StockSplitDividend INNER JOIN
                (SELECT DISTINCT AssetSubTableID FROM PortfolioItem INNER JOIN
            (SELECT * FROM FileSystemItem WHERE (Name='! AdaptiveConnor,VXX autocorrelation (VXX-XIV, stocks, noHedge) Agy Live' AND UserID=3) OR (Name='! AdaptiveConnor,VXX autocorrelation (VXX-XIV, stocks, noHedge) Live' AND UserID=2)) as FileSystemReduced
            ON PortfolioItem.PortfolioID = FileSystemReduced.ID
            WHERE AssetTypeID = 2) as TempStockIDs
            ON StockSplitDividend.StockID = TempStockIDs.AssetSubTableID
            ORDER By StockID, Date
        "

        - However, the 'FileSystemItem WHERE (Name='	was calculated 3 times for each resultset.
        That is a small table in memory, but it requres processing. Better to calculate only once. 

        - Table alias is not available accross multiple SQL result sets, like this
        "SELECT *...) as FileSystemReduced"

        - This CTE is not available accross multiple SQL result sets
        >George: OK. So, CTE (common table expression) is not a good solution. It cannot return multiple result sets.
        Creating a Table is not a big deal for me, as my resulting table is small.
        WITH cte
        AS
        (select orderid ord from orders 

        - So, only solution left is CREATE a #temp table.

        -I create the #temp table for the FileSystemItem, because that is small,
        but I don't create the temp table for the PortfolioItem, because that can be a huge table. 
        It can be 1000 lines per Portfolio. 2000 lines for 2 portfolios, that is 2000 INSERTS.
        Instead of 2000 INSERTS It is better to go through the whole PortfolioItem table twice in Step2 and in Step 3 too.

        - "GO" is not accepted, maybe because it has to have a \newline too, and those string operations eliminated the newline. So use ';' instead

        - SplitDividend ordering: SQL query asked it to be ordered by the table-native <StockID, Date>.
        <Date, StockID> would be more useful, but we don't want sorting computation in the SQL server, do it locally)

        ************************* 2. The second solution

        IF OBJECT_ID('tempdb..#TempFileSystemReduced') IS NOT NULL DROP TABLE #TempFileSystemReduced
        ;
        SELECT * INTO #TempFileSystemReduced FROM (
           SELECT ID, Name, UserID FROM FileSystemItem WHERE (Name='! AdaptiveConnor,VXX autocorrelation (VXX-XIV, stocks, noHedge) Agy Live' AND UserID=3) OR (Name='! AdaptiveConnor,VXX autocorrelation (VXX-XIV, stocks, noHedge) Live' AND UserID=2)
        ) as x
        ;
        SELECT * FROM #TempFileSystemReduced
        ;
        SELECT PortfolioID, TransactionType, AssetTypeID, AssetSubTableID, Volume, Price, Date FROM PortfolioItem INNER JOIN
# TempFileSystemReduced
	        ON PortfolioItem.PortfolioID = #TempFileSystemReduced.ID
	        ORDER By PortfolioID, Date
        ;
        SELECT StockSplitDividend.* FROM StockSplitDividend INNER JOIN
	        (SELECT DISTINCT AssetSubTableID FROM PortfolioItem INNER JOIN #TempFileSystemReduced
		        ON PortfolioItem.PortfolioID = #TempFileSystemReduced.ID WHERE AssetTypeID = 2) as TempStockIDs
	        ON StockSplitDividend.StockID = TempStockIDs.AssetSubTableID
	        ORDER By StockID, Date


        ************************* 3. This is the final solution added Ticker/FullTicker/Currency info.  It only handles stocks, not Futures, and Options.
IF OBJECT_ID('tempdb..#TempFileSystemReduced') IS NOT NULL DROP TABLE #TempFileSystemReduced
;
SELECT * INTO #TempFileSystemReduced FROM (SELECT ID, Name, UserID FROM FileSystemItem WHERE (Name='! AdaptiveConnor,VXX autocorrelation (VXX-XIV, stocks, noHedge) Agy Live' AND UserID=3) OR (Name='! AdaptiveConnor,VXX autocorrelation (VXX-XIV, stocks, noHedge) Live' AND UserID=2)) as x
;
SELECT * FROM #TempFileSystemReduced
;
SELECT PortfolioID, TransactionType, AssetTypeID, AssetSubTableID, Volume, Price, Date FROM PortfolioItem INNER JOIN #TempFileSystemReduced
	ON PortfolioItem.PortfolioID = #TempFileSystemReduced.ID
	ORDER By PortfolioID, Date
;
SELECT StockSplitDividend.* FROM StockSplitDividend INNER JOIN
    (SELECT DISTINCT AssetSubTableID FROM PortfolioItem INNER JOIN #TempFileSystemReduced
		ON PortfolioItem.PortfolioID = #TempFileSystemReduced.ID WHERE AssetTypeID = 2) as TempStockIDs
	ON StockSplitDividend.StockID = TempStockIDs.AssetSubTableID
    ORDER By StockID, Date
;
SELECT 2 AS AssetTypeID, ReducedStocks.ID AS AssetSubTableID,
  ReducedStocks.Ticker COLLATE SQL_Latin1_General_CP1_CI_AS AS Ticker,  
 (StockExchange.Name + ':' + ReducedStocks.Ticker COLLATE SQL_Latin1_General_CP1_CI_AS) AS FullTicker,
 ReducedStocks.StockExchangeID,
 CurrencyID
FROM (SELECT * FROM Stock INNER JOIN
    (SELECT DISTINCT AssetSubTableID FROM PortfolioItem INNER JOIN #TempFileSystemReduced ON PortfolioItem.PortfolioID = #TempFileSystemReduced.ID WHERE AssetTypeID = 2) as TempStockIDs 
		ON Stock.ID = TempStockIDs.AssetSubTableID) as ReducedStocks
 LEFT JOIN dbo.StockExchange ON (StockExchange.ID = ReducedStocks.StockExchangeID)

        */
#endregion
        public static async Task<bool> LoadHistoricalPipsFromDbAndCalculateTodayPips(List<DbPortfolio> p_portfolios)
        {
            // 1. Get All Historical Pips + All split Info
            SqCommon.Utils.Logger.Debug($"LoadHistoricalPipsFromDbAndCalculateTodayPips() start.");

            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.Append("SELECT * INTO #TempFileSystemReduced FROM (SELECT ID, Name, UserID FROM FileSystemItem WHERE ");
            for (int i = 0; i < p_portfolios.Count; i++)
            {
                var portf = p_portfolios[i];
                if (i > 0)
                    sqlBuilder.Append($" OR ");
                sqlBuilder.Append($"(Name='{portf.Name}' AND UserID={(int)portf.HQUserID})");
            }
            sqlBuilder.Append(@") as x;
SELECT * FROM #TempFileSystemReduced;
SELECT PortfolioID, TransactionType, AssetTypeID, AssetSubTableID, Volume, Price, Date FROM PortfolioItem INNER JOIN
#TempFileSystemReduced
	ON PortfolioItem.PortfolioID = #TempFileSystemReduced.ID
	ORDER By PortfolioID, Date;
SELECT StockSplitDividend.* FROM StockSplitDividend INNER JOIN
    (SELECT DISTINCT AssetSubTableID FROM PortfolioItem INNER JOIN #TempFileSystemReduced
		ON PortfolioItem.PortfolioID = #TempFileSystemReduced.ID WHERE AssetTypeID = 2) as TempStockIDs
	ON StockSplitDividend.StockID = TempStockIDs.AssetSubTableID
    ORDER By StockID, Date;
SELECT 2 AS AssetTypeID, ReducedStocks.ID AS AssetSubTableID,
  ReducedStocks.Ticker COLLATE SQL_Latin1_General_CP1_CI_AS AS Ticker,  
 (StockExchange.Name + ':' + ReducedStocks.Ticker COLLATE SQL_Latin1_General_CP1_CI_AS) AS FullTicker,
 ReducedStocks.StockExchangeID,
 CurrencyID
FROM (SELECT * FROM Stock INNER JOIN
    (SELECT DISTINCT AssetSubTableID FROM PortfolioItem INNER JOIN #TempFileSystemReduced ON PortfolioItem.PortfolioID = #TempFileSystemReduced.ID WHERE AssetTypeID = 2) as TempStockIDs 
		ON Stock.ID = TempStockIDs.AssetSubTableID) as ReducedStocks
 LEFT JOIN dbo.StockExchange ON (StockExchange.ID = ReducedStocks.StockExchangeID)");
            var sqlResult = await SqlTools.ExecuteSqlQueryAsync(sqlBuilder.ToString(), null, null);
            
            // 2. Do the GetPortfolioAtTime() TransactionAccumulator locally
            TransactionAccumulator(p_portfolios, sqlResult, DateTime.UtcNow);

            SqCommon.Utils.Logger.Debug($"LoadHistoricalPipsFromDbAndCalculateTodayPips() end.");
            return true;
        }

//   INSERT INTO PortfolioItem([PortfolioID] , [TransactionType] , [AssetTypeID] , [AssetSubTableID] , [Volume] , [Price] , [Date] , [Note]) VALUES
//                      (27,2,2,3745,84,19.2, '2007-07-20 16:48:00', NULL),
//                      (27,2,2,3745,84,19.2, '2007-07-20 16:48:00', NULL)
        public static async Task<bool> InsertTransactionsToDB(List<DbTransaction> p_transactions)
        {
            if (p_transactions.Count <= 0)
                return true;
            StringBuilder sqlBuilder = new StringBuilder();
            sqlBuilder.Append("INSERT INTO PortfolioItem([PortfolioID] , [TransactionType] , [AssetTypeID] , [AssetSubTableID] , [Volume] , [Price] , [Date] , [Note]) VALUES ");
            for (int i = 0; i < p_transactions.Count; i++)
            {
                if (i > 0)
                    sqlBuilder.Append($" , ");

                DbTransaction transaction = p_transactions[i];
                string note = String.IsNullOrEmpty(transaction.Note)? "NULL" : $"'{transaction.Note}'";
                sqlBuilder.Append($"({transaction.PortfolioID},{(int)transaction.TransactionType},{(int)transaction.AssetTypeID},{transaction.SubTableID},{(int)(transaction.Volume +0.0001)},{transaction.Price}, '{transaction.DateTime.ToString("yyyy-MM-dd HH:mm:ss")}',{note})");
            }
            var sqlResult = await SqlTools.ExecuteSqlQueryAsync(sqlBuilder.ToString(), null, null);
            return true;
        }

        private static void TransactionAccumulator(List<DbPortfolio> p_portfolios, IList<List<object[]>> sqlResult, DateTime p_accumulationEndDate)
        {
            var fileSystemTbl = sqlResult[0];

            List<object[]> transactionsTbl = null;  // some portfolios don't have transactions, and SQL DB returns nothing, and sqlResult[1] indexing would crash
            if (sqlResult.Count >= 2)
                transactionsTbl = sqlResult[1]; // SQL query asked it to be ordered by PortfolioID, Date
            else
                transactionsTbl = new List<object[]>();

            List<object[]> splitDividendTbl = null; // some portfolios don't have splits or dividends, and SQL DB returns nothing, and sqlResult[2] indexing would crash
            if (sqlResult.Count >= 3)
                splitDividendTbl = sqlResult[2]; // SQL query asked it to be ordered by StockID, Date (Date, StockID would be more useful, but we don't want sorting computation in the SQL server, do it locally)
            else
                splitDividendTbl = new List<object[]>();

            List<object[]> stockAssetTbl = null;
            if (sqlResult.Count >= 4)
                stockAssetTbl = sqlResult[3];   // sqlResult[3] indexing would crash if it is not there
            else
                stockAssetTbl = new List<object[]>();

            // 1. Prepare data of assets, splits, portfolios
            p_portfolios.ForEach(p =>
            {
                p.TodayPositions = new List<PortfolioPosition>();
                p.PortfolioID = (int)fileSystemTbl.Find(row => (string)row[1] == p.Name && (HQUserID)row[2] == p.HQUserID)[0];
            });

            Dictionary<IAssetID, AssetDesc> assetDescs = new Dictionary<IAssetID, AssetDesc>();
            foreach (var stockAsset in stockAssetTbl)
            {
                int stockID = (int)stockAsset[1];
                IAssetID assetID = DbUtils.MakeAssetID(AssetType.Stock, stockID);
                assetDescs.Add(assetID, new AssetDesc() { AssetID = assetID, Ticker = (string)stockAsset[2], FullTicker = (string)stockAsset[3], StockExchangeID = (StockExchangeID)(byte)stockAsset[4], CurrencyID = (CurrencyID)(short)stockAsset[5] });
            }

            var splitDivInfosByDate = splitDividendTbl.Select(r =>
            {
                int stockID = (int)r[0];
                IAssetID assetID = DbUtils.MakeAssetID(AssetType.Stock, stockID);
                DateTime dateLocal = (DateTime)r[1];
                StockExchangeID stockExchangeId = assetDescs[assetID].StockExchangeID;
                DateTime timeUtc = TimeZoneInfo.ConvertTime(dateLocal, DbUtils.StockExchangeToTimeZoneData[(int)stockExchangeId].TimeZoneInfo, TimeZoneInfo.Utc);
                return new SplitAndDividendInfo() { StockID = stockID, TimeUtc = timeUtc, IsSplit = (bool)r[2], DividendOrPrevClosePrice = (double)(float)r[3], OldVolume = (int)r[4], NewVolume = (int)r[5] };
            }).OrderBy(r => r.TimeUtc).ToList();



            // 2. Process transactions one by one
            int trInd = 0;
            while (trInd < transactionsTbl.Count)   // ordered by PortfolioID, Date
            {
                var transaction = transactionsTbl[trInd];
                int portfInd = (int)transaction[0];
                DbPortfolio portf = p_portfolios.Where(r => r.PortfolioID == portfInd).FirstOrDefault();
                
                int splitIndInclusive = 0;
                while (trInd < transactionsTbl.Count)   // essentially while(true), // consume the whole portfolio
                {
                    transaction = transactionsTbl[trInd];
                    TransactionType transactionType = (TransactionType)transaction[1];
                    AssetType assetType = (AssetType)transaction[2];
                    int assetSubTableID = (int)transaction[3];
                    IAssetID assetID = DbUtils.MakeAssetID(assetType, assetSubTableID);
                    double volume = (double)(int)transaction[4];
                    double price = (double)(float)transaction[5];
                    DateTime trTime = (DateTime)transaction[6];

                    // 2.1. Process splits until the point of TransactionTime
                    splitIndInclusive = ConsumeSplitsUntilTime(assetDescs, splitDivInfosByDate, portf, splitIndInclusive, trTime);

                    // 2.2. Process transaction
                    var pipT = portf.TodayPositions.FirstOrDefault(r => r.AssetID == assetID);
                    switch (transactionType)
                    {
                        case TransactionType.Deposit:
                            if (pipT == null)
                            {
                                pipT = new PortfolioPosition() { AssetID = assetID };
                                portf.TodayPositions.Add(pipT);
                            }
                            pipT.Volume += (assetType == AssetType.HardCash) ? volume * price : volume;    // for Cash, Volume = 1, Price = $5000. The reason was, because in DB, the Volume was Int32, so the Price as Double was used to store non-Integer values
                            pipT.LastTransactionTimeUtc = trTime;
                            pipT.LastSplitAdjustedTransactionPrice = price;
                            break;
                        case TransactionType.BuyAsset:
                        case TransactionType.SellAsset:     // Shorting is equivalent to Selling. There is no difference in IB. Let's try to use that freedom.
                        case TransactionType.ShortAsset:    // decided that we allow Selling even if we have no position. It is the freedom how IB works. So, in the future we allow it in transactions
                        case TransactionType.CoverAsset:    // in real life at IB, imagine that we have 5 stocks, and we sell 8. The result is that we will have -3 shorts. It is allowed there. Let's allow here too.
                            double assetVolumeMultiplier = (transactionType == TransactionType.BuyAsset || transactionType == TransactionType.CoverAsset) ? 1.0 : -1.0;
                            // I. find currencyID and the corresponding cash and book the cash
                            CurrencyID currencyID = assetDescs[assetID].CurrencyID;
                            var pipCashInCurrency = portf.TodayPositions.FirstOrDefault(r => r.AssetTypeID == AssetType.HardCash && r.SubTableID == (int)currencyID);
                            if (pipCashInCurrency == null)
                            {
                                pipCashInCurrency = new PortfolioPosition()
                                {
                                    AssetID = DbUtils.MakeAssetID(AssetType.HardCash, (int)currencyID),
                                    Volume = 0.0,
                                    LastSplitAdjustedTransactionPrice = 0,
                                    LastTransactionTimeUtc = trTime
                                };
                                portf.TodayPositions.Add(pipCashInCurrency);
                            }
                            pipCashInCurrency.Volume += -1 * assetVolumeMultiplier * volume * price;
                            // II. add the Asset
                            if (pipT == null)
                            {
                                pipT = new PortfolioPosition() { AssetID = assetID, Volume = 0.0 };
                                portf.TodayPositions.Add(pipT);
                            }
                            //Console.WriteLine($"Transaction ind: {trInd}, OldVolume: {pipT.Volume}, NewVolume: {pipT.Volume + assetVolumeMultiplier * volume}  ");
                            pipT.Volume += assetVolumeMultiplier * volume;
                            pipT.LastTransactionTimeUtc = trTime;
                            pipT.LastSplitAdjustedTransactionPrice = price;
                            if (SqCommon.Utils.IsNearZero(pipT.Volume))
                                portf.TodayPositions.Remove(pipT);
                            break;
                        default:
                            throw new Exception("not expected transactionType");
                    }

                    trInd++;
                    bool isLastTransactionOfPortfolio = false;
                    if (trInd >= transactionsTbl.Count)
                        isLastTransactionOfPortfolio = true;
                    else
                        isLastTransactionOfPortfolio = ((int)(transactionsTbl[trInd][0]) != portfInd);  // if the next transaction is a different portfolio // remember: transactions are grouped by PortfolioID

                    if (isLastTransactionOfPortfolio)  // exit inner loop
                    {
                        // 2.3. consume Splits from last transaction until today/p_accumulationEndDate
                        splitIndInclusive = ConsumeSplitsUntilTime(assetDescs, splitDivInfosByDate, portf, splitIndInclusive, DateTime.UtcNow);
                        break;
                    }
                }   // inner while for one portfolio

            } // outer while for all portfolios
            
        }   // method ends

        private static int ConsumeSplitsUntilTime(Dictionary<IAssetID, AssetDesc> assetDescs, List<SplitAndDividendInfo> splitDivInfosByDate, DbPortfolio portf, int splitIndInclusive, DateTime trTime)
        {
            for (int i = splitIndInclusive; i < splitDivInfosByDate.Count; i++)
            {
                if (splitDivInfosByDate[i].TimeUtc > trTime)
                    break;
                SplitAndDividendInfo splitInfo = splitDivInfosByDate[i];
                var pipS = portf.TodayPositions.FirstOrDefault(r => r.SubTableID == splitInfo.StockID && r.AssetTypeID == AssetType.Stock);
                if (pipS != null)
                {
                    if (splitInfo.IsSplit)
                    {
                        // VXX: OldVolume: 4, NewVolume: 1 means that for every 4 old stocks, there is 1 new stock. The Fractional stocks can be calculated using PrevClosePrice as cash
                        double oldVolume = pipS.Volume;
                        int nOldGroups = (int)(oldVolume / (double)splitInfo.OldVolume + 0.0001);
                        double fractionalOldShares = oldVolume - nOldGroups * splitInfo.OldVolume;
                        int nNewShares = nOldGroups * splitInfo.NewVolume; // rounding to the nearest Integer, but consider these are doubles
                        double cashSaleInCurrency = fractionalOldShares * splitInfo.DividendOrPrevClosePrice;
                        SqCommon.Utils.Logger.Debug($"TransactionAccumulator splits. Date:{splitInfo.TimeUtc.ToString("yyyy-MM-dd")}, oldVolume:{oldVolume:F2}, fractionalOldShares:{fractionalOldShares}, nNewShares:{nNewShares}, cashSaleInCurrency:{cashSaleInCurrency:F2}");
                        pipS.Volume = nNewShares;
                        pipS.LastSplitAdjustedTransactionPrice *= (double)splitInfo.OldVolume / (double)splitInfo.NewVolume;

                        // cashSaleInCurrency should be added to Cash, find currencyID and the corresponding cash and book the cash
                        if (Math.Abs(cashSaleInCurrency) > 0.002)
                        {
                            CurrencyID currencyID = assetDescs[pipS.AssetID].CurrencyID;
                            var pipCashInCurrency = portf.TodayPositions.FirstOrDefault(r => r.AssetTypeID == AssetType.HardCash && r.SubTableID == (int)currencyID);
                            if (pipCashInCurrency == null)
                            {
                                pipCashInCurrency = new PortfolioPosition()
                                {
                                    AssetID = DbUtils.MakeAssetID(AssetType.HardCash, (int)currencyID),
                                    Volume = 0.0,
                                    LastSplitAdjustedTransactionPrice = 0,
                                    LastTransactionTimeUtc = trTime
                                };
                                portf.TodayPositions.Add(pipCashInCurrency);
                            }

                            pipCashInCurrency.Volume += cashSaleInCurrency;

                        }
                    }
                    else
                        throw new Exception("Dividend is not implemented yet.");
                }
                splitIndInclusive++;
            }

            return splitIndInclusive;
        }
    }
}
