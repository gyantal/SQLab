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
using System.Diagnostics;

namespace VirtualBroker
{
  

    public partial class Gateway
    {
        AccInfo m_accInfo;
        private readonly object m_getAccountSummaryLock = new object();

        public void AccSumArrived(int p_reqId, string p_tag, string p_value, string p_currency)
        {
            m_accInfo.AccSums.Add(new AccSum() { Tag = p_tag, Value = p_value, Currency = p_currency });
        }

        ManualResetEventSlim m_getAccountSummaryMres;
        public void AccSumEnd(int p_reqId)
        {
            if (m_getAccountSummaryMres != null)
                m_getAccountSummaryMres.Set();  // Sets the state of the event to signaled, which allows one or more threads waiting on the event to proceed.

            // if you don't cancel it, all the data update come every 1 minute, which might be good, because we can give it to user instantenously....
            // However, it would be an unnecessary traffic all the time... So, better to Cancel the data streaming.
            BrokerWrapper.CancelAccountSummary(p_reqId);
        }


        public bool GetAccountInfo(bool p_isNeedAccSum, bool p_isNeedPos, AccInfo p_accInfo)
        {
            Console.WriteLine($"GetAccountInfo(), GW user: '{this.GatewayUser}', Thread Id= {Thread.CurrentThread.ManagedThreadId}");
            m_accInfo = p_accInfo;
            int accReqId = -1;
            try
            {
                Task task1 = Task.Run(() =>
                {
                    try
                    {
                        Stopwatch sw1 = Stopwatch.StartNew();
                        Console.WriteLine($"GetAccountSummary()-1, GW user: '{this.GatewayUser}', Thread Id= {Thread.CurrentThread.ManagedThreadId}");
                        lock (m_getAccountSummaryLock)          // IB only allows one query at a time, so next client has to wait
                        {
                            if (m_getAccountSummaryMres == null)
                                m_getAccountSummaryMres = new ManualResetEventSlim(false);  // initialize as unsignaled
                            else
                                m_getAccountSummaryMres.Reset();        // set to unsignaled, which makes thread to block

                            accReqId = BrokerWrapper.ReqAccountSummary();

                            bool wasLightSet = m_getAccountSummaryMres.Wait(5000);     // timeout at 5sec
                            if (!wasLightSet)
                                Utils.Logger.Error("ReqAccountSummary() ended with timeout error.");
                            //m_getAccountSummaryMres.Dispose();    // not necessary. We keep it for the next sessions for faster execution.
                        }
                        sw1.Stop();
                        Console.WriteLine($"GetAccountSummary()-2 ends in {sw1.ElapsedMilliseconds}ms GW user: '{this.GatewayUser}', Thread Id= {Thread.CurrentThread.ManagedThreadId}");
                    }
                    catch (Exception e)
                    {
                        Utils.Logger.Error("GetAccountSummary() ended with exception: " + e.Message);
                    }
                });

                Task task2 = Task.Run(() =>
                {
                    try
                    {
                        Console.WriteLine($"ReqPositions(), GW user: '{this.GatewayUser}', Thread Id= {Thread.CurrentThread.ManagedThreadId}");
                        //Thread.Sleep(2000);
                        // BrokerWrapper.ReqPositions();
                    }
                    catch { }
                });

                Stopwatch sw = Stopwatch.StartNew();
                Task.WaitAll(task1, task2);     // AccountSummary() task takes 308msec in local development.
                sw.Stop();
                Console.WriteLine($"GetAccountInfo() ends in {sw.ElapsedMilliseconds}ms, GW user: '{this.GatewayUser}', Thread Id= {Thread.CurrentThread.ManagedThreadId}");

                return true;
            }
            catch (Exception e)
            {
                Utils.Logger.Error("GetAccountInfo() ended with exception: " + e.Message);
                return false;
            }
            finally
            {
                if (accReqId != -1)
                    BrokerWrapper.CancelAccountSummary(accReqId);
            }
        }
    }

    }
