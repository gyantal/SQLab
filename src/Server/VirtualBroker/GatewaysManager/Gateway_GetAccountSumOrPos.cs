using DbCommon;
using SqCommon;
using IBApi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utils = SqCommon.Utils;
using System.Text;

namespace VirtualBroker
{
    public partial class Gateway
    {
        // this service should be implemented using the low-level BrokerWrappers (so if should work, no matter it is IB or YF or GF BrokerWrapper)
        // this will be called multiple times in parallel. So, be careful when to use Shared resources, we need locking
        public string GetAccountSumOrPos(string p_input)
        {
            string resultPrefix = "", resultPostfix = "";

            int reqId = -1;
            try
            {
                // caret(^) is not a valid URL character, so it is encoded to %5E; skip the first '?'  , convert everything to Uppercase, because '%5e', and '%5E' is the same for us
                string input = Uri.UnescapeDataString(p_input.Substring(1));    // change %20 to ' ', and %5E to '^'  , "^VIX" is encoded in the URI as "^%5EVIX"
                //reqId = BrokerWrapper.ReqAccountSummary();

                BrokerWrapper.ReqPositions();

                //string[] inputParams = input.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                //if (BrokerWrapper.GetMktDataSnapshot(contract, ref rtPrices))
                //{
                //    tickerList.Add(new Tuple<string, Dictionary<int, PriceAndTime>, int>(sqTicker, rtPrices, -1));  // -1 means: isTemporaryTicker = false, it was permanently subscribed
                //}



                // 2. Assuming BrokerWrapper.GetMktDataSnapshot() now has all the data
                StringBuilder jsonResultBuilder = new StringBuilder(resultPrefix + "[");
                //bool isFirstTickerWrittenToOutput = false;

                jsonResultBuilder.Append(@"]" + resultPostfix);
                Utils.Logger.Info("GetRealtimePriceService() ended properly: " + jsonResultBuilder.ToString());
                return jsonResultBuilder.ToString();

            }
            catch (Exception e)
            {
                Utils.Logger.Error("GetAccountSumOrPos ended with exception: " + e.Message);
                return resultPrefix + @"{ ""Message"":  ""Exception in VBroker app GetAccountSumOrPos(). Exception: " + e.Message + @""" }" + resultPostfix;
            }
            finally
            {
                if (reqId != -1)
                    BrokerWrapper.CancelAccountSummary(reqId);
            }
        }
    }

    }
