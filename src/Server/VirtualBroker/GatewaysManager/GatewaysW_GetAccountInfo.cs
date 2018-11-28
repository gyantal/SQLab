using DbCommon;
using IBApi;
using SqCommon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils = SqCommon.Utils;

namespace VirtualBroker
{
    public class AccSum
    {
        public string Tag { get; set; }
        public string Value { get; set; }
        public string Currency { get; set; }
    }

    public class AccPos
    {
        public Contract Contract { get; set; }
        public double Position { get; set; }    // in theory, position is Int (whole number) for all the examples I seen. However, IB gives back as double, just in case of a complex contract. Be prepared.
        public double AvgCost { get; set; }
        public double LastPrice { get; set; }   // MktValue can be calculated
        public double LastUnderlyingPrice { get; set; }   // In case of options DeliveryValue can be calculated
    }

    public class AccInfo
    {
        public string BrAccStr { get; set; } = String.Empty;
        public Gateway Gateway { get; set; }
        public List<AccSum> AccSums = new List<AccSum>();   // AccSummary
        public List<AccPos> AccPoss = new List<AccPos>();   // Positions
    }

    public partial class GatewaysWatcher
    {
        
        public string GetAccountsInfo(string p_input)     // p_input = @"?bAcc=Gyantal,Charmat,DeBlanzac&type=AccSum,Pos,MktVal";
        {
            Utils.Logger.Info($"GetAccountsInfo() START with parameter '{p_input}'");
            if (!m_isReady || m_mainGateway == null)
            {
                Utils.Logger.Error($"GetAccountsInfo() error. GatewaysWatcher is not ready.");
                return null;
            }

            // Problem is: GetPosition only gives back the Position: 218, Avg cost: $51.16, but that is not enough, because we would like to see the MktValue, DelivValue. 
            // So we need the LastPrice too. Even for options. And it is better to get it in here, than having a separate function call later.
            // 1. Let's collect all the AccountSum for all the ibGateways in a separate threads. This takes about 280msec
            // 2. Let's collect all the AccountPos positions for all the ibGateways in a separate threads. This takes about 28msec to 60msec. Much faster than AccountSum.
            // AccountPos (50msec) should NOT wait for finishing the AccountSum (300msec), but continue quickly processing and getting realtime prices if needed.
            // 3. If client wants RT MktValue too, collect needed RT prices (stocks, options, underlying of options, futures). Use only the mainGateway to ask a realtime quote estimate. So, one stock is not queried an all gateways. Even for options
            // 4. Fill LastPrice, LastUnderlyingPrice in all AccPos for all ibGateways. (Alternatively Calculate the MktValue, DelivValue, but better to just pass the raw data to client)

            string input = Uri.UnescapeDataString(p_input.Substring(1));    // change %20 to ' ', and %5E to '^', skip the first '?' in p_input
            string[] inputParams = input.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            if (!inputParams[0].StartsWith("bAcc=", StringComparison.CurrentCultureIgnoreCase))
            {
                Utils.Logger.Error($"GetAccountsInfo() error. No bAcc parameter.");
                return null;
            }
            if (!inputParams[1].StartsWith("type=", StringComparison.CurrentCultureIgnoreCase))
            {
                Utils.Logger.Error($"GetAccountsInfo() error. No type parameter.");
                return null;
            }
            string[] typeArr = inputParams[1].Substring("type=".Length).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            bool isNeedAccSum = typeArr.Contains("AccSum");
            bool isNeedPos = typeArr.Contains("Pos");
            bool isNeedMktVal = typeArr.Contains("MktVal");

            var allAccInfos = new List<AccInfo>();
            string[] bAccArr = inputParams[0].Substring("bAcc=".Length).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var bAcc in bAccArr)
            {
                AccInfo accSumPos = new AccInfo { BrAccStr = bAcc };
                switch (bAcc.ToUpper())
                {
                    case "GYANTAL":
                        FindGatewayAndAdd(new GatewayUser[] { GatewayUser.GyantalMain, GatewayUser.GyantalSecondary }, accSumPos);
                        break;
                    case "CHARMAT":
                        FindGatewayAndAdd(new GatewayUser[] { GatewayUser.CharmatSecondary }, accSumPos);
                        break;
                    case "DEBLANZAC":
                        FindGatewayAndAdd(new GatewayUser[] { GatewayUser.DeBlanzacSecondary }, accSumPos);
                        break;
                    default:
                        Utils.Logger.Error($"GetAccountsInfo() error. Unrecognized brokeraccount '{bAcc.ToUpper()}'");
                        continue;
                }

                allAccInfos.Add(accSumPos);
            }

            Task task1 = Task.Run(() =>
            {
                try
                {
                    Stopwatch sw1 = Stopwatch.StartNew();
                    Parallel.ForEach(allAccInfos, accInfo =>        // execute in parallel, so it is faster if DcMain and DeBlanzac are both queried at the same time.
                    {
                        if (accInfo.Gateway == null)
                            return;
                        accInfo.Gateway.GetAccountSums(accInfo.AccSums);        // takes 300msec each
                    });
                    sw1.Stop();
                    Console.WriteLine($"GetAccountsInfo()-AccSum ends in {sw1.ElapsedMilliseconds}ms, Thread Id= {Thread.CurrentThread.ManagedThreadId}");
                }
                catch (Exception e)
                {
                    Utils.Logger.Error("GetAccountsInfo()-AccSum ended with exception: " + e.Message);
                }
            });

            Task task2 = Task.Run(() =>
            {
                try
                {
                    Stopwatch sw2 = Stopwatch.StartNew();
                    Parallel.ForEach(allAccInfos, accInfo =>        // execute in parallel, so it is faster if DcMain and DeBlanzac are both queried at the same time.
                    {
                        if (accInfo.Gateway == null)
                            return;
                        accInfo.Gateway.GetAccountPoss(accInfo.AccPoss);    // takes 50msec each
                    });
                    //If client wants RT MktValue too, collect needed RT prices (stocks, options, underlying of options, futures). Use only the mainGateway to ask a realtime quote estimate. So, one stock is not queried an all gateways. Even for options
                    if (isNeedMktVal)
                    {
                        
                    }

                    sw2.Stop();
                    Console.WriteLine($"GetAccountsInfo()-AccPos ends in {sw2.ElapsedMilliseconds}ms, Thread Id= {Thread.CurrentThread.ManagedThreadId}");
                }
                catch (Exception e)
                {
                    Utils.Logger.Error("GetAccountsInfo()-AccPos ended with exception: " + e.Message);
                }
            });


            Stopwatch sw = Stopwatch.StartNew();
            Task.WaitAll(task1, task2);     // AccountSummary() task takes 280msec in local development. (ReqAccountSummary(): 280msec, ReqPositions(): 50msec)
            sw.Stop();
            Console.WriteLine($"GetAccountsInfo() ends in {sw.ElapsedMilliseconds}ms, Thread Id= {Thread.CurrentThread.ManagedThreadId}");




          



            string resultPrefix = "", resultPostfix = "";
            StringBuilder jsonResultBuilder = new StringBuilder(resultPrefix + "[");
            for (int i = 0; i < allAccInfos.Count; i++)
            {
                AccInfo accInfo = allAccInfos[i];
                if (i != 0)
                    jsonResultBuilder.AppendFormat(",");
                jsonResultBuilder.Append($"{{\"BrAcc\":\"{accInfo.BrAccStr}\"");
                jsonResultBuilder.Append($",\"AccSums\":[");
                for (int j = 0; j < accInfo.AccSums.Count; j++)
                {
                    AccSum accSum = accInfo.AccSums[j];
                    if (j != 0)
                        jsonResultBuilder.AppendFormat(",");
                    jsonResultBuilder.Append($"{{\"Tag\":\"{accSum.Tag}\",\"Value\":\"{accSum.Value}\",\"Currency\":\"{accSum.Currency}\"}}");
                }
                jsonResultBuilder.Append($"]");
                jsonResultBuilder.Append($",\"AccPoss\":[");
                for (int j = 0; j < accInfo.AccPoss.Count; j++)
                {
                    AccPos accPos = accInfo.AccPoss[j];
                    if (j != 0)
                        jsonResultBuilder.AppendFormat(",");
                    jsonResultBuilder.Append($"{{\"Symbol\":\"{accPos.Contract.Symbol}\",\"SecType\":\"{accPos.Contract.SecType}\",\"Currency\":\"{accPos.Contract.Currency}\",\"Pos\":\"{accPos.Position}\",\"AvgCost\":\"{accPos.AvgCost:0.00}\"");
                    if (accPos.Contract.SecType == "OPT")
                        jsonResultBuilder.Append($",\"LastTradeDate\":\"{accPos.Contract.LastTradeDateOrContractMonth}\",\"Right\":\"{accPos.Contract.Right}\",\"Strike\":\"{accPos.Contract.Strike}\",\"Multiplier\":\"{accPos.Contract.Multiplier}\",\"LocalSymbol\":\"{accPos.Contract.LocalSymbol}\"");
                    jsonResultBuilder.Append($"}}");
                }
                jsonResultBuilder.Append($"]");

                jsonResultBuilder.Append($"}}");
            }

            jsonResultBuilder.Append(@"]" + resultPostfix);
            string result = jsonResultBuilder.ToString();
            Utils.Logger.Info($"GetAccountsInfo() END with result '{result}'");
            //Console.WriteLine($"GetAccountsInfo() END with result '{result}'");
            return result;
        }

        private void FindGatewayAndAdd(GatewayUser[] p_possibleGwUsers, AccInfo p_accSumPos)
        {
            foreach (var gwUser in p_possibleGwUsers)
            {
                Gateway gw = m_gateways.Find(r => r.GatewayUser == gwUser);
                if (gw != null)
                {
                    p_accSumPos.Gateway = gw;
                    return;
                }
            }
        }
    }
}
