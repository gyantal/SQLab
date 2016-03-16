using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using SqCommon;

namespace HealthMonitor
{
    public partial class HealthMonitor
    {
        DateTime m_lastVbGatewaysManagerErrorPhoneCallTime = DateTime.MinValue;    // don't call if a call was made in the last 30 minutes
        List<Tuple<DateTime, bool, HealthMonitorMessage>> m_VbGatewaysManagerReportWasOk = new List<Tuple<DateTime, bool, HealthMonitorMessage>>(); // List<> is not thread safe


        void ProcessMessage(TcpClient p_tcpClient, HealthMonitorMessage p_message)
        {
            switch (p_message.ID)
            {
                case HealthMonitorMessageID.TestHardCash:
                    throw new Exception("Testing Hard Crash by Throwing this Exception");
                case HealthMonitorMessageID.ReportErrorFromVirtualBroker:
                    ErrorFromVirtualBroker(p_tcpClient, p_message);
                    break;
                case HealthMonitorMessageID.ReportOkFromVirtualBroker:
                    OkFromVirtualBroker(p_tcpClient, p_message);
                    break;

            }

        }



        // this is called every time the VbGatewaysManager send OK or Error emails to the user
        private void OkFromVirtualBroker(TcpClient p_tcpClient, HealthMonitorMessage p_message)
        {
            if (!m_persistedState.IsProcessingVbGatewaysManagerMessagesEnabled)
                return;

            // sometime the message seems OK, but if the messageParam contains the word "Error" treat it as error. For example, if this email was sent to the user, with the Message that everything is OK, treat it as error
            // "***Trade: ERROR"
            // "*** StrongAssert failed (severity==Exception): BrokerAPI.GetStockMidPoint(VXX,...) failed"
            // "ibNet_ErrorMsg(). TickerID: 742, ErrorCode: 404, ErrorMessage: 'Order held while securities are located.'
            // "Error. A transaction was not executed. p_brokerAPI.GetExecutionData = null for Sell VXX Volume: 266. Check that it was not executed and if not, perform it manually then enter into the DB.
            bool isError = (p_message.ParamStr.IndexOf("Error", StringComparison.CurrentCultureIgnoreCase) != -1);  // in DotNetCore, there is no StringComparison.InvariantCultureIgnoreCase
            if (isError)
                ErrorFromVirtualBroker(p_tcpClient, p_message);   // this will add it to m_VbGatewaysManagerReportWasError list
            else {
                lock (m_VbGatewaysManagerReportWasOk)
                    m_VbGatewaysManagerReportWasOk.Add(new Tuple<DateTime, bool, HealthMonitorMessage>(DateTime.UtcNow, true, p_message));

                Utils.TcpClientDispose(p_tcpClient);
            }
        }

        private void ErrorFromVirtualBroker(TcpClient p_tcpClient, HealthMonitorMessage p_message)
        {
            if (!m_persistedState.IsProcessingVbGatewaysManagerMessagesEnabled)
                return;

            lock (m_VbGatewaysManagerReportWasOk)
                    m_VbGatewaysManagerReportWasOk.Add(new Tuple<DateTime, bool, HealthMonitorMessage>(DateTime.UtcNow, false, p_message));

            if (p_message.ResponseFormat == HealthMonitorMessageResponseFormat.String)
            {
                BinaryWriter bw = new BinaryWriter(p_tcpClient.GetStream());
                bw.Write("FromServer: Message received, saved and starting processing: " + p_message.ParamStr);
            }
            Utils.TcpClientDispose(p_tcpClient);

            Utils.Logger.Info("ErrorFromVbGatewaysManager().");
            //if (!m_isCheckAmazonAwsInstancesEmailWasSent)
            //{
                Utils.Logger.Info("ErrorFromVbGatewaysManager(). Sending Warning email.");
                new Email
                {
                    ToAddresses = Utils.Configuration["EmailGyantal"],
                    Subject = "SQ HealthMonitor: ERROR from VbGatewaysManager.",
                    Body = $"SQ HealthMonitor: ERROR from VbGatewaysManager. MessageParamStr: { p_message.ParamStr}",
                    IsBodyHtml = false
                }.Send();
                m_isCheckAmazonAwsInstancesEmailWasSent = true;
            //}

#if RELEASE
            Utils.Logger.Info("ErrorFromVbGatewaysManager(). Making Phonecall.");

            TimeSpan timeFromLastCall = DateTime.UtcNow - m_lastVbGatewaysManagerErrorPhoneCallTime;
            if (timeFromLastCall > TimeSpan.FromMinutes(30))
            {
                var call = new PhoneCall
                {
                    FromNumber = Caller.Gyantal,
                    ToNumber = PhoneCall.PhoneNumbers[Caller.Gyantal],
                    Message = "There is an Error in Virtual Brokers Gateways Manager. ... I repeat: Error in Virtual Brokers Gateways Manager.",
                    NRepeatAll = 2
                };
                // skipped temporarily
                bool didTwilioAcceptedTheCommand = call.MakeTheCall();
                if (didTwilioAcceptedTheCommand)
                {
                    Utils.Logger.Debug("PhoneCall instruction was sent to Twilio.");
                    m_lastVbGatewaysManagerErrorPhoneCallTime = DateTime.UtcNow;
                }
                else
                    Utils.Logger.Error("PhoneCall instruction was NOT accepted by Twilio.");
            }
#endif


        }


    }
}
