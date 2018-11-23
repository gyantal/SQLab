using DbCommon;
using IBApi;
using SqCommon;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public int Position { get; set; }
        public double LastPrice { get; set; }   // MktValue can be calculated
        public double LastUnderlyingPrice { get; set; }   // In case of options DeliveryValue can be calculated
    }

    public class AccInfo
    {
        public string BrAccStr { get; set; } = String.Empty;
        public Gateway Gateway { get; set; }
        // AccSummary
        public List<AccSum> AccSums = new List<AccSum>();

        public delegate void AccSumArrivedFunc(int p_reqId, AccInfo p_accInfo, string p_tag, string p_value, string p_currency);
        public AccSumArrivedFunc AccSumArrived;

        // Positions
        public List<AccPos> AccPoss = new List<AccPos>();
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
            // 1. Let's collect all the AccountSum + positions for all the ibGateways
            // 2. If client wants RT MktValue too, collect needed RT prices (stocks, options, underlying of options, futures). Use only the mainGateway to ask a realtime quote estimate. So, one stock is not queried an all gateways. Even for options
            // 3. Calculate the MktValue, DelivValue too for all ibGateways.

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

            Parallel.ForEach(allAccInfos, accInfo =>        // execute in parallel, so it is faster if DcMain and DeBlanzac are both queried at the same time.
            {
                //Console.WriteLine($"Acc '{accInfo.BrAccStr}', Thread Id= {Thread.CurrentThread.ManagedThreadId}");
                if (accInfo.Gateway == null)
                    return;
                accInfo.Gateway.GetAccountInfo(isNeedAccSum, isNeedPos, accInfo);
            });




            string resultPrefix = "", resultPostfix = "";
            StringBuilder jsonResultBuilder = new StringBuilder(resultPrefix + "[");
            // ...
            jsonResultBuilder.Append(@"]" + resultPostfix);
            string result = jsonResultBuilder.ToString();
            Utils.Logger.Info($"GetAccountsInfo() END with result '{result}'");
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
