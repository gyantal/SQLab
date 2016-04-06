using SqCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HealthMonitor
{
    public partial class HealthMonitor
    {
        Object m_lastInformSupervisorLock = new Object();   // null value cannot be locked, so we have to create an object
        DateTime m_lastVbErrorEmailTime = DateTime.MinValue;    // don't email if it was made in the last 10 minutes
        DateTime m_lastVbErrorPhoneCallTime = DateTime.MinValue;    // don't call if it was made in the last 30 minutes
        List<Tuple<DateTime, bool, HealthMonitorMessage>> m_VbReport = new List<Tuple<DateTime, bool, HealthMonitorMessage>>(); // List<> is not thread safe


        // this is called every time the VirtualBroker send OK or Error: after every simulated trading
        private void OkFromVirtualBroker(TcpClient p_tcpClient, HealthMonitorMessage p_message)
        {
            if (!m_persistedState.IsProcessingVirtualBrokerMessagesEnabled)
                return;
            // sometimes the message seems OK, but if the messageParam contains the word "Error" treat it as error. For example, if this email was sent to the user, with the Message that everything is OK, treat it as error
            // "***Trade: ERROR"   + "*** StrongAssert failed (severity==Exception): BrokerAPI.GetStockMidPoint(VXX,...) failed"
            // "ibNet_ErrorMsg(). TickerID: 742, ErrorCode: 404, ErrorMessage: 'Order held while securities are located.'
            // "Error. A transaction was not executed. p_brokerAPI.GetExecutionData = null for Sell VXX Volume: 266. Check that it was not executed and if not, perform it manually then enter into the DB.
            bool isError = (p_message.ParamStr.IndexOf("Error", StringComparison.CurrentCultureIgnoreCase) != -1);  // in DotNetCore, there is no StringComparison.InvariantCultureIgnoreCase
            if (isError)
                ErrorFromVirtualBroker(p_tcpClient, p_message);   // this will add it to m_VbReportWasError list
            else {
                lock (m_VbReport)
                    m_VbReport.Add(new Tuple<DateTime, bool, HealthMonitorMessage>(DateTime.UtcNow, true, p_message));

                if (p_message.ResponseFormat == HealthMonitorMessageResponseFormat.String)
                {
                    BinaryWriter bw = new BinaryWriter(p_tcpClient.GetStream());
                    bw.Write("FromServer: OK message received, saved.");
                }
            }
        }

        private void ErrorFromVirtualBroker(TcpClient p_tcpClient, HealthMonitorMessage p_message)
        {
            if (!m_persistedState.IsProcessingVirtualBrokerMessagesEnabled)
                return;

            lock (m_VbReport)
                    m_VbReport.Add(new Tuple<DateTime, bool, HealthMonitorMessage>(DateTime.UtcNow, false, p_message));

            if (p_message.ResponseFormat == HealthMonitorMessageResponseFormat.String)
            {
                BinaryWriter bw = new BinaryWriter(p_tcpClient.GetStream());
                bw.Write("FromServer: Message received, saved and starting processing: " + p_message.ParamStr);
            }

            Utils.Logger.Info("ErrorFromVirtualBroker().");
            InformSupervisors("SQ HealthMonitor: ERROR from VirtualBroker.", $"SQ HealthMonitor: ERROR from VirtualBroker. MessageParamStr: { p_message.ParamStr}", "There is an Error in Virtual Broker. ... I repeat: Error in Virtual Broker.");
        }

        private void InformSupervisors(string p_emailSubject, string p_emailBody, string p_phonecallText)
        {
            bool doInformSupervisors = false;
            lock (m_lastInformSupervisorLock)   // if InformSupervisors is called on two different threads at the same time, (if VBroker notified us twice very quickly), we still want to inform user only once
            {
                TimeSpan timeFromLastEmail = DateTime.UtcNow - m_lastVbErrorEmailTime;
                if (timeFromLastEmail > TimeSpan.FromMinutes(10))
                {
                    doInformSupervisors = true;
                    m_lastVbErrorEmailTime = DateTime.UtcNow;
                }
            }

            if (!doInformSupervisors)
                return;

            Utils.Logger.Info("InformSupervisors(). Sending Warning email.");
            new Email
            {
                ToAddresses = Utils.Configuration["EmailGyantal"],
                Subject = p_emailSubject,
                Body = p_emailBody,
                IsBodyHtml = false
            }.Send();

            if (Utils.RunningPlatform() == Platform.Linux)    // assuming production environment on Linux, Other ways to customize: ifdef DEBUG/RELEASE  ifdef PRODUCTION/DEVELOPMENT, etc. this Linux/Windows is fine for now
            {
                Utils.Logger.Info("InformSupervisors(). Making Phonecall.");

                TimeSpan timeFromLastCall = DateTime.UtcNow - m_lastVbErrorPhoneCallTime;
                if (timeFromLastCall > TimeSpan.FromMinutes(30))
                {
                    var call = new PhoneCall
                    {
                        FromNumber = Caller.Gyantal,
                        ToNumber = PhoneCall.PhoneNumbers[Caller.Gyantal],
                        Message = p_phonecallText,
                        NRepeatAll = 2
                    };
                    // skipped temporarily
                    bool didTwilioAcceptedTheCommand = call.MakeTheCall();
                    if (didTwilioAcceptedTheCommand)
                    {
                        Utils.Logger.Debug("PhoneCall instruction was sent to Twilio.");
                        m_lastVbErrorPhoneCallTime = DateTime.UtcNow;
                    }
                    else
                        Utils.Logger.Error("PhoneCall instruction was NOT accepted by Twilio.");
                }
            }


        }

        public void CheckOKMessageArrived(DateTime p_utcStart, string p_triggeredTaskSchemaName) // p_triggeredTaskSchemaName = "UberVXX"
        {
            Tuple<DateTime, bool, HealthMonitorMessage> expectedMessage = null;
            lock (m_VbReport)
            {
                for (int i = 0; i < m_VbReport.Count; i++)
                {
                    if (m_VbReport[i].Item1 > p_utcStart)
                    {
                        bool isOK = m_VbReport[i].Item2;
                        string strategyName = String.Empty;
                        int strategyNameInd1 = m_VbReport[i].Item3.ParamStr.IndexOf("BrokerTask ");  // "BrokerTask UberVXX was OK" or "had ERROR"
                        if (strategyNameInd1 != -1)
                        {
                            int strategyNameInd2 = strategyNameInd1 + "BrokerTask ".Length;
                            int strategyNameInd3 = m_VbReport[i].Item3.ParamStr.IndexOf(" ", strategyNameInd2);
                            if (strategyNameInd3 != -1)
                            {
                                strategyName = m_VbReport[i].Item3.ParamStr.Substring(strategyNameInd2, strategyNameInd3 - strategyNameInd2);
                                if (strategyName == p_triggeredTaskSchemaName)
                                {
                                    expectedMessage = m_VbReport[i];
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (expectedMessage == null)    // Send email, make phonecall
            {
                InformSupervisors($"SQ HealthMonitor: VirtualBroker Message from {p_triggeredTaskSchemaName} didn't arrive.", $"SQ HealthMonitor: VirtualBroker Message from {p_triggeredTaskSchemaName} did't arrive.", $"Virtual Broker message from from {p_triggeredTaskSchemaName} didn't arrive. ... I repeat: Virtual Broker message from from {p_triggeredTaskSchemaName} didn't arrive.");
            } else
            {
                // do nothing. If it was an Error message, the Phonecall was already made when the Error message arrived
            }
        }
    }
}
