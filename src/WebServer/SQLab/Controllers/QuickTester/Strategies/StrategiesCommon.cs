using DbCommon;
using Newtonsoft.Json;
using SqCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SQLab.Controllers.QuickTester.Strategies
{
    public class GeneralStrategyParameters
    {
        public DateTime startDateUtc;
        public DateTime endDateUtc;
    }

    // ideas what to include come from Portfolio123: http://imarketsignals.com/2015/best8-sp500-min-volatility-large-cap-portfolio-management-system/
    public class StrategyResult
    {
        // General Info:
        public string startDateStr;
        public string rebalanceFrequencyStr;
        public string benchmarkStr;

        // statistics as of Date
        public string endDateStr;
        public double pvStartValue;
        public double pvEndValue;
        public double totalGainPct;
        public double cagr;
        public double annualizedStDev;
        public double sharpeRatio;
        public double sortinoRatio;
        public double maxDD;
        public double ulcerInd;    // Ulcer = qMean DD
        public int maxTradingDaysInDD;
        public string winnersStr;    //write: (405/621) 65.22%) probably better if it is a whole String, 51%  (if we are in cash, that day is not a profit day.)
        public string losersStr;

        public double benchmarkCagr;
        public double benchmarkMaxDD;
        public double benchmarkCorrelation;

        // optional: it is not necessarily give by a strategy
        public double ratioOfDaysInCash = -1.0;    // 20%
        public int nTradesForNewPosition = -1;    // 

        // holdings as of Date
        public double pvCash;
        public int nPositions;
        public string holdingsListStr;  // probably comma separated

        public string htmlNoteFromStrategy;
        public string errorMessage;
        public string debugMessage;

        public List<string> chartData;
    }

    public class DailyData
    {
        public DateTime Date { get; set; }
        public double AdjClosePrice { get; set; }
    }

    public class StrategiesCommon
    {

        // You can't have async methods with ref or out parameters. So Task should give back the whole Data
        public static async Task<Tuple<IList<double?>, TimeSpan>> GetRealtimesQuotesAsync(List<string> p_tickers, CancellationToken p_canc = default(CancellationToken))
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            var rtPrices = new List<double?>(p_tickers.Count());
            for (int i = 0; i < p_tickers.Count(); i++)
            {
                rtPrices.Add(null);
            }

            //string tickerWithoutDotSQ = "";
            //if (ticker.EndsWith(".SQ"))
            //    tickerWithoutDotSQ = ticker.Substring(0, ticker.Length - ".SQ".Length);

            //string realtimeQuoteUri = "http://hqacompute.cloudapp.net/q/rtp?s=VXX,^VIX,^GSPC,SVXY,^^^VIX201410,GOOG&f=l&jsonp=myCallbackFunction";    // even if IB doesn't support ticker ^GSPC, we implemented it in the RealTime App
            //string realtimeQuoteUri = "http://hqacompute.cloudapp.net/q/rtp?s=" + String.Join(",", p_tickers.Select(r => r.EndsWith(".SQ") ? r.Substring(0, r.Length - ".SQ".Length) : r) + "&f=l&jsonp=myCallbackFunction";
            //string realtimeQuoteUri = "http://hqacompute.cloudapp.net/q/rtp?s=" + String.Join(",", p_tickers.Select(r =>r)) + "&f=l&jsonp=myCallbackFunction";
            //string realtimeQuoteUri = "http://hqacompute.cloudapp.net/q/rtp?s=" + String.Join(",", p_tickers.Select(r => r.EndsWith(".SQ") ? r.Substring(0, r.Length - ".SQ".Length) : r)) + "&f=l&jsonp=myCallbackFunction";
            string realtimeQuoteUriQuery = "?s=" + String.Join(",", p_tickers.Select(r => SqlTools.GetBaseTicker(r,r))) + "&f=l&jsonp=myCallbackFunction";

            try
            {
                var realtimeAnswerJSON = await RealtimePrice.GenerateRtpResponse(realtimeQuoteUriQuery);
                //using (WebClient webClient = new WebClient())
                //{
                //    webClient.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
                //    var realtimeAnswerJSON = await webClient.DownloadStringTaskAsync(new Uri(realtimeQuoteUri));
                    // "myCallbackFunction([{"Symbol":"URE"},{"Symbol":"SRS"}])"
                    // or
                    //myCallbackFunction([{"Symbol":"VXX","TimeUtc":"19:28:12","Last": 42.96},
                    //{"Symbol":"^VIX","TimeUtc":"19:27:41","Last": 14.22},
                    //{"Symbol":"^VXV","TimeUtc":"19:24:41","Last": 15.59},
                    //{"Symbol":"^GSPC","TimeUtc":"19:27:30","Last": 1846.24},

                    int startBracketInd = realtimeAnswerJSON.IndexOf('[');
                    int endBracketInd = realtimeAnswerJSON.LastIndexOf(']');
                    if (startBracketInd != -1 && endBracketInd != -1)
                    {
                        string realtimeAnswerWithBracketsJSON = realtimeAnswerJSON.Substring(startBracketInd, endBracketInd - startBracketInd + 1);

                        var realtimeAnswerObj = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(realtimeAnswerWithBracketsJSON);
                        realtimeAnswerObj.ForEach(dict => {
                            string symbol;
                            if (!dict.TryGetValue("Symbol", out symbol))
                                return;

                            string lastPriceStr;
                            if (!dict.TryGetValue("Last", out lastPriceStr))
                                return;
                            double lastPrice = 0.0;
                            if (Double.TryParse(lastPriceStr, out lastPrice))
                            {
                                for (int i = 0; i < p_tickers.Count; i++)       // "SVXY!Light0.5x.SQ", "SVXY.SQ", "SVXY" all require the same realtime price coming from "SVXY"
                                {
                                    if (SqlTools.GetBaseTicker(p_tickers[i], p_tickers[i]) == symbol)
                                    {
                                        rtPrices[i] = lastPrice;
                                    }
                                }
                            }
                        }); //realtimeAnswerObj.ForEach


                        //return rtPrices;
                        //string[] rows = rowsInOneLine.Split(new char[] { ',', ',' });
                        //Array.ForEach(rows, r =>
                        //{

                        //});

                    }
                //}
            }
            catch (Exception)
            {
            }

            stopWatch.Stop();
            TimeSpan realtimeQueryTimeSpan = stopWatch.Elapsed;

            return new Tuple<IList<double?>, TimeSpan>(rtPrices, realtimeQueryTimeSpan);


            //int t = await Task.Run(() => 5);
            //return rtPrices;

            //string realtimeQuoteUri = "http://hqacompute.cloudapp.net/q/rtp?s=VXX,^VIX,^GSPC,SVXY,^^^VIX201410,GOOG&f=l&jsonp=myCallbackFunction";



            ////var sqlReturnTask = Tools.GetHistoricalQuotesAsync(new[] {
            ////        new QuoteRequest { Ticker = bullishTicker, nQuotes = Int32.MaxValue, NonAdjusted = false },
            ////        new QuoteRequest { Ticker = bearishTicker, nQuotes = Int32.MaxValue,}
            ////    }, HQCommon.AssetType.Stock, true);    // Ascending date order: TRUE, better to order it at the SQL server than locally. SQL has indexers

            //return null;
        }

        public static async Task<Tuple<IList<List<DailyData>>, TimeSpan, TimeSpan>> GetHistoricalAndRealtimesQuotesAsync(DateTime p_startDateUtc, DateTime p_endDateUtc, List<string> p_tickers, CancellationToken p_canc = default(CancellationToken))
        {
            //- SPY 300K CSV SQLqueryTime (local server), msecond times for  (for Azure in-house datacenter, these will be less)
            //All data: Open, High, Low, Close, Volume : 886, 706, 1237, 761, 727, Avg = 863
            //Only ClosePrice: 662, 680, 702, 820, 663, 692, Avg = 703
            // if the Website is not local, but it is in the same Azure datacenter as the SQL center
            //SQL query time (All OHLCV data): msec: 612, 614, 667, 772, 632, 613, 665, 662, Avg = 654
            //SQL query time (only Close data): msec: 623, 624, 704, 614, 615, 621, 621, 722, 636, Avg = 642
            //Conclusion:downloading only Closeprice from SQL, we can save 100msec (LocalServer website (non-datacenter website)) or 15msec (Azure server website when SQL server is very close), still worth it
            ushort sqlReturnedColumns = QuoteRequest.TDC;       // QuoteRequest.All or QuoteRequest.TDOHLCVS

            //var sqlReturnTask = GetHistQuotesAsync(p_tickers.Select(r => new QuoteRequest { Ticker = r, nQuotes = Int32.MaxValue, NonAdjusted = false, ReturnedColumns = sqlReturnedColumns }), HQCommon.AssetType.Stock, true); // Ascending date order: TRUE, better to order it at the SQL server than locally. SQL has indexers
            var sqlReturnTask = SqlTools.GetHistQuotesAsync(p_startDateUtc, p_endDateUtc, p_tickers, sqlReturnedColumns);

            Task<Tuple<IList<double?>, TimeSpan>> realtimeReturnTask = null;
            if (p_endDateUtc >= DateTime.UtcNow)
                realtimeReturnTask = GetRealtimesQuotesAsync(p_tickers);

            // Control returns here before GetHistoricalQuotesAsync() returns.  // ... Prompt the user.
            Console.WriteLine("Please wait patiently while I do SQL and realtime price queries.");

            // Wait for the tasks to complete.            // ... Display its results.
            //var combinedAsyncTasksResults = await Task.WhenAll(sqlReturnTask, realtimeReturnTask); this cannot be done now, because the return values are different
            if (realtimeReturnTask != null)
                await Task.WhenAll(sqlReturnTask, realtimeReturnTask);  // otherwise, the next await will wait the historical data

            var sqlReturnData = await sqlReturnTask;  //as they have all definitely finished, you could also use Task.Value, "However, I recommend using await because it's clearly correct, while Result can cause problems in other scenarios."
            Tuple<IList<double?>, TimeSpan> realtimeReturnData = null;
            if (realtimeReturnTask != null)
                realtimeReturnData = await realtimeReturnTask; //as they have all definitely finished, you could also use Task.Value, "However, I recommend using await because it's clearly correct, while Result can cause problems in other scenarios."

            var sqlReturn = sqlReturnData.Item1;
            List<List<DailyData>> returnQuotes = null;
            // sql query of "VXXB.SQ" gives back tickers of VXXB and also tickers of "VXXB.SQ"
            int closePriceIndex = -1;
            if (sqlReturnedColumns == QuoteRequest.TDOHLCVS)
                closePriceIndex = 5;
            else if (sqlReturnedColumns == QuoteRequest.TDC)
                closePriceIndex = 2;
            else
                throw new NotImplementedException();

            returnQuotes = p_tickers.Select(ticker =>
            {
                IEnumerable<object[]> mergedRows = SqlTools.GetTickerAndBaseTickerRows(sqlReturn, ticker);
                return mergedRows.Select(
                    row => new DailyData()
                    {
                        Date = ((DateTime)row[1]),
                        AdjClosePrice = (double)Convert.ToDecimal(row[closePriceIndex])  // row[2] is object(decimal) (from 2017-08-25, it was object(double) before) if it is a stock (because Adjustment multiplier and AS DECIMAL(19,4) in SQL); and object(float) if it is Indices. However Convert.ToDouble(row[2]) would convert 16.66 to 16.6599999
                    }).ToList();
            }).ToList();

            if (realtimeReturnData != null)
            {
                var todayDate = DateTime.UtcNow.Date;
                var realtimeReturn = realtimeReturnData.Item1;
                for (int i = 0; i < p_tickers.Count(); i++)
                {
                    if (realtimeReturn[i] != null)
                    {
                        int todayInd = returnQuotes[i].FindLastIndex(r => r.Date == todayDate);
                        if (todayInd == -1) // if it is missing
                        {
                            returnQuotes[i].Add(new DailyData() { Date = todayDate, AdjClosePrice = (double)realtimeReturn[i] });
                        }
                        else // if it is already in the array, overwrite it
                        {
                            returnQuotes[i][todayInd].AdjClosePrice = (double)realtimeReturn[i];
                        }
                    }
                }
            }

            return new Tuple<IList<List<DailyData>>, TimeSpan, TimeSpan>(returnQuotes, sqlReturnData.Item2, (realtimeReturnData != null) ? realtimeReturnData.Item2 : TimeSpan.Zero);
        }


        public static List<DailyData> DetermineBacktestPeriodCheckDataCorrectness(List<DailyData> p_quotes, ref string p_warningToUser)
        {
            List<DailyData> pv = new List<DailyData>(p_quotes.Count());    // suggest maxSize, but it still contains 0 items

            DateTime pvStartDate = p_quotes[0].Date;

            //pv.Add(new DailyData() { Date = pvStartDate.AddDays(-1), ClosePrice = 1.0 });   // put first pv item on previous day. NO. not needed. At the end of the first day, pv will be 1.0, because we trade at Market Close

            DateTime pvEndDate = pvStartDate;
            int quotesInd = 0;
            // Start to march and if there is a missing day or a bad data, stop marching further

            while (quotesInd < p_quotes.Count())
            {
                if (true)
                {
                    pv.Add(new DailyData() { Date = p_quotes[quotesInd].Date, AdjClosePrice = p_quotes[quotesInd].AdjClosePrice });
                    quotesInd++;
                }
            }

            return pv;
        }


        //check for data integrity: 
        // - SRS (bearish one) doesn't have data for 2014-12-01, but URE has. What to do on that day: stop doing anything after that day and write a message to user that bad data. EndDate was modified.
        // FAS-FAZ: "Missing Days. Days of Data don't match in the quotes. Next date would be '2009-02-06 12:00:00 AM', but next date of ticker 'FAS' is '2009-02-10 12:00:00 AM'. Backtest goes only until this day."
        public static bool DetermineBacktestPeriodCheckDataCorrectness(IList<List<DailyData>> p_quotes, string[] p_tickers, ref string p_noteToUserCheckData, out DateTime p_startDate, out DateTime p_endDate)
        {
            p_startDate = DateTime.MaxValue;
            p_endDate = DateTime.MinValue;

            DateTime pvStartDate = DateTime.MinValue;   // find the maximum of the startDates; that is a shared startDate
            foreach (var quotes in p_quotes)
            {
                if (quotes[0].Date > pvStartDate)
                {
                    pvStartDate = quotes[0].Date;
                }
            }

            DateTime pvEndDate = pvStartDate;
            // Start to march and if there is a missing day in any of the ETFs, stop marching further
            int[] quotesInd = new int[p_quotes.Count];
            for (int i = 0; i < p_quotes.Count; i++)
            {
                int startDateInd = p_quotes[i].FindIndex(r => r.Date >= pvStartDate);
                if (startDateInd == -1) // the maximum of the StartDates is not found in another QuoteHistory. So, we cannot start from StartDay.
                    return false;
                quotesInd[i] = startDateInd;
            }

            do
            {
                bool isExitLoop = false;
                for (int i = 0; i < p_quotes.Count; i++)
                {
                    if (quotesInd[i] >= p_quotes[i].Count)
                    {
                        isExitLoop = true;
                        break;
                    }
                }
                if (isExitLoop)
                    break;

                bool isAlldatesSame = true;
                DateTime firstQuoteDate = p_quotes[0][quotesInd[0]].Date;
                for (int i = 1; i < p_quotes.Count; i++)
                {
                    if (firstQuoteDate != p_quotes[i][quotesInd[i]].Date)
                    {
                        isAlldatesSame = false;
                        p_noteToUserCheckData = $"Warning! Missing Days! Days of Data don't match in the quotes. FirstQuote({p_tickers[0]}) date: '{firstQuoteDate.ToString("yyyy-MM-dd")}' doesn't match with ticker({p_tickers[i]}) date: '{p_quotes[i][quotesInd[i]].Date.ToString("yyyy-MM-dd")}'. Backtest goes only until this day.";
                        break;
                    }
                }
                if (isAlldatesSame)
                {
                    pvEndDate = p_quotes[0][quotesInd[0]].Date;
                    //pv.Add(new DailyData() { Date = p_quotes[0][quotesInd[0]].Date, ClosePrice = p_quotes[0][quotesInd[0]].ClosePrice });
                    for (int i = 0; i < p_quotes.Count; i++)
                    {
                        quotesInd[i] = quotesInd[i] + 1;
                    }
                }
                else
                    break;


            } while (true);

            p_startDate = pvStartDate;
            p_endDate = pvEndDate;
            return true;    // return OK, because even if there was a Missing day, we could generate a meaningful StartDate/EndDate pair
        }

        public static List<DailyData> DeepCopyQuoteRange(List<DailyData> p_quotes, DateTime p_startDate, DateTime p_endDate)   // Shallow Copy is not OK, as *.ClosePrices will be overwritten
        {
            int startDateInd = p_quotes.FindIndex(r => r.Date >= p_startDate);
            int endDateInd = p_quotes.FindIndex(startDateInd, r => r.Date >= p_endDate);
            List<DailyData> pv = new List<DailyData>(endDateInd - startDateInd);
            for (int i = startDateInd; i <= endDateInd; i++)
            {
                pv.Add(new DailyData() { Date = p_quotes[i].Date, AdjClosePrice = p_quotes[i].AdjClosePrice });
            }
            //return p_quotes.GetRange(startDateInd, endDateInd - startDateInd).Select(r => new DailyData() { Date = r.Date, ClosePrice = r.ClosePrice }).ToList(); // works, but probably slow
            //return p_quotes.GetRange(startDateInd, endDateInd - startDateInd);  // an efficient way of getting a subset of the List, but it is a shallow copy is not OK, as *.ClosePrice will be overwritten in PV
            return pv;
        }

        // Sortino calculation from here: http://www.redrockcapital.com/Sortino__A__Sharper__Ratio_Red_Rock_Capital.pdf
        //The main reason we wrote this article is because in both literature and trading software packages,
        //we have seen the Sortino ratio, and in particular the target downside deviation, calculated
        //incorrectly more often than not. Most often, we see the target downside deviation calculated
        //by “throwing away all the positive returns and take the standard deviation of negative returns”.
        //We hope that by reading this article, you can see how this is incorrect
        // George: a little problem to me, but left it like this: underPerfFromTarget is distance from Zero, while in StDev, it was distance from Avg.
        public static StrategyResult CreateStrategyResultFromPV(List<DailyData> p_pv, string p_htmlNoteToUser, string p_errorToUser, string p_debugMessage)
        {
            if (p_pv == null)
            {
                return new StrategyResult() { htmlNoteFromStrategy=p_htmlNoteToUser, errorMessage = p_errorToUser, debugMessage= p_debugMessage };
            }

            //IEnumerable<string> chartDataToSend = pv.Select(row => row.Date.Year + "-" + row.Date.Month + "-" + row.Date.Day + "-" + String.Format("{0:0.00}", row.ClosePrice));
            IEnumerable<string> chartDataToSend = p_pv.Select(row => row.Date.Year + "-" + row.Date.Month + "-" + row.Date.Day + "," + String.Format("{0:0.00}", row.AdjClosePrice >= 0 ? row.AdjClosePrice : 0.0));    // postprocess: TradingViewChart cannot accept negative numbers

            DateTime startDate = p_pv[0].Date;
            DateTime endDate = p_pv[p_pv.Count() - 1].Date;


            int nTradingDays = p_pv.Count();
            double nYears = nTradingDays / 252.0;   //https://www.google.co.uk/webhp?sourceid=chrome-instant&ion=1&espv=2&ie=UTF-8#q=how%20many%20trading%20days%20in%20a%20year

            double pvStart = p_pv[0].AdjClosePrice;
            double pvEnd = p_pv[p_pv.Count() - 1].AdjClosePrice;
            double totalGainPct = pvEnd / pvStart - 1.0;
            double cagr = Math.Pow(totalGainPct + 1, 1.0 / nYears) - 1.0;

            var dailyReturns = new double[p_pv.Count() - 1];
            for (int i = 0; i < p_pv.Count() - 1; i++)
            {
                dailyReturns[i] = p_pv[i + 1].AdjClosePrice / p_pv[i].AdjClosePrice - 1.0;
            }
            double avgReturn = dailyReturns.Average();
            double dailyStdDev = Math.Sqrt(dailyReturns.Sum(r => (r - avgReturn) * (r - avgReturn)) / ((double)dailyReturns.Count() - 1.0));    //http://www.styleadvisor.com/content/standard-deviation, "Morningstar uses the sample standard deviation method: divide by n-1
            double annualizedStDev = dailyStdDev * Math.Sqrt(252.0);    //http://en.wikipedia.org/wiki/Trading_day, http://www.styleadvisor.com/content/annualized-standard-deviation

            double annualizedSharpeRatio = cagr / annualizedStDev;


            double sortinoDailyTargetReturn = 0.0;       // assume investor is happy with any positive return
            double sortinoAnnualizedTargetReturn = Math.Pow(sortinoDailyTargetReturn, 252.0);       // maybe around annual 3-6% is expected by investor
            double dailyTargetDownsideDeviation = 0.0;
            for (int i = 0; i < dailyReturns.Length; i++)
            {
                double underPerfFromTarget = dailyReturns[i] - sortinoDailyTargetReturn;
                if (underPerfFromTarget < 0.0)
                    dailyTargetDownsideDeviation += underPerfFromTarget * underPerfFromTarget;
            }
            dailyTargetDownsideDeviation = Math.Sqrt(dailyTargetDownsideDeviation / (double)dailyReturns.Length);   // see Sortino PDF for explanation why we use the 0 samples too for the Average
            double annualizedDailyTargetDownsideDeviation = dailyTargetDownsideDeviation * Math.Sqrt(252.0); //http://en.wikipedia.org/wiki/Trading_day, http://www.styleadvisor.com/content/annualized-standard-deviation
            //double dailySortinoRatio = (avgReturn - sortinoDailyTargetReturn) / dailyTargetDownsideDeviation;   // daily gave too small values
            double annualizedSortinoRatio = (cagr - sortinoAnnualizedTargetReturn) / annualizedDailyTargetDownsideDeviation;   // 

            var drawdowns = new double[p_pv.Count()];
            double maxPv = Double.NegativeInfinity;
            double maxDD = Double.PositiveInfinity;
            double quadraticMeanDD = 0.0;
            for (int i = 0; i < p_pv.Count(); i++)
            {
                if (p_pv[i].AdjClosePrice > maxPv)
                    maxPv = p_pv[i].AdjClosePrice;
                double dd = p_pv[i].AdjClosePrice / maxPv - 1.0;
                drawdowns[i] = dd;
                quadraticMeanDD += dd * dd;
                if (dd < maxDD)
                    maxDD = drawdowns[i];
            }

            double ulcerInd = Math.Sqrt(quadraticMeanDD / (double)nTradingDays);

            int maxTradingDaysInDD = 0;
            int daysInDD = 0;
            for (int i = 0; i < drawdowns.Count(); i++)
            {
                if (drawdowns[i] < 0.0)
                    daysInDD++;
                else
                {
                    if (daysInDD > maxTradingDaysInDD)
                        maxTradingDaysInDD = daysInDD;
                    daysInDD = 0;
                }
            }
            if (daysInDD > maxTradingDaysInDD) // if the current DD is the longest one, then we have to check at the end
                maxTradingDaysInDD = daysInDD;

            int winnersCount = dailyReturns.Count(r => r > 0.0);
            int losersCount = dailyReturns.Count(r => r < 0.0);

            //double profitDaysPerAllDays = (double)dailyReturns.Count(r => r > 0.0) / dailyReturns.Count();
            //double losingDaysPerAllDays = (double)dailyReturns.Count(r => r < 0.0) / dailyReturns.Count();


            StrategyResult strategyResult = new StrategyResult()
            {
                startDateStr = startDate.ToString("yyyy-MM-dd"),
                rebalanceFrequencyStr = "Daily",
                benchmarkStr = "SPX",

                endDateStr = endDate.ToString("yyyy-MM-dd"),
                pvStartValue = pvStart,
                pvEndValue = pvEnd,
                totalGainPct = totalGainPct,
                cagr = cagr,
                annualizedStDev = annualizedStDev,
                sharpeRatio = annualizedSharpeRatio,
                sortinoRatio = annualizedSortinoRatio,
                maxDD = maxDD,
                ulcerInd = ulcerInd,
                maxTradingDaysInDD = maxTradingDaysInDD,
                winnersStr = String.Format("({0}/{1})  {2:0.00}%", winnersCount, dailyReturns.Count(), 100.0 * (double)winnersCount / dailyReturns.Count()),
                losersStr = String.Format("({0}/{1})  {2:0.00}%", losersCount, dailyReturns.Count(), 100.0 * (double)losersCount / dailyReturns.Count()),

                benchmarkCagr = 0,
                benchmarkMaxDD = 0,
                benchmarkCorrelation = 0,

                pvCash = 0.0,
                nPositions = 0,
                holdingsListStr = "NotApplicable",

                chartData = chartDataToSend.ToList(),

                htmlNoteFromStrategy = p_htmlNoteToUser,
                errorMessage = p_errorToUser,
                debugMessage = p_debugMessage
            };

            return strategyResult;
        }



    }
}
