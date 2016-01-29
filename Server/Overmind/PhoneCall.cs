using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;


namespace Overmind
{
    public enum Caller { Gyantal, Robin, RobinLL }
    public static class CallerExtensions
    {
        public static string AsString(this Caller p_id)
        {
            switch (p_id)
            {
                case Caller.Gyantal: return Controller.g_controller.g_configuration.GetSection("PhoneNumberGyantal").Value;
                case Caller.Robin: return Controller.g_controller.g_configuration.GetSection("PhoneNumberRobin").Value;
                case Caller.RobinLL: return Controller.g_controller.g_configuration.GetSection("PhoneNumberRobinLL").Value;
                default: return null;
            }
        }
    }



    // Twilio docs/examples in Azure documentation:
    // http://www.windowsazure.com/en-us/documentation/articles/twilio-dotnet-how-to-use-for-voice-sms/
    // Official API docs:
    // https://www.twilio.com/docs/api/rest

    // works under Mono, because it is a simple REST API call
    public class PhoneCall
    {
        public string ToNumber;
        public string Message = "Default message";
        public Caller FromNumber = Caller.Gyantal;
        public string ResultJSON, Error;
        public int NRepeatAll = 1; // with this setting = 1, the phone call say the Message only once. However it is common that we want that the message is repeated, so we use = 2.

        /// <summary> Returns true when Twilio's server accepted our request, BEFORE the phone begins to ring!
        /// Returns false if the server rejected our request with error message, or .NET exception occurred </summary>
        public bool MakeTheCall()
        {
            return MakeTheCallAsync().Result;
        }

        async System.Threading.Tasks.Task<bool> MakeTheCallAsync()
        {
            string caller = CallerExtensions.AsString(FromNumber);
            if (caller == null)
                throw new ArgumentException(FromNumber.ToString(), "FromNumber");
            if (String.IsNullOrEmpty(ToNumber))
                throw new ArgumentException(ToNumber ?? "null", "ToNumber");
            string xml = null;
            if (1 < NRepeatAll)
            {

                var say = new System.Xml.XmlDocument().CreateElement("Say");
                say.InnerText = Message;
                xml = "<Response>" + String.Join("<Pause length=\"2\"/>", Enumerable.Repeat(say.OuterXml, NRepeatAll)) + "</Response>";
            }

            var client = new System.Net.Http.HttpClient();  // System.Net.Http.dll
            var sid = Controller.g_controller.g_configuration.GetSection("TwilioSid").Value;
            var token = Controller.g_controller.g_configuration.GetSection("TwilioToken").Value;
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(sid + ":" + token)));
            try
            {
                System.Net.Http.HttpResponseMessage response = await client.PostAsync(
                    "https://api.twilio.com/2010-04-01/Accounts/" + sid + "/Calls.json",  // could be .csv as well, see https://www.twilio.com/docs/api/rest/tips
                    new System.Net.Http.FormUrlEncodedContent(new Dictionary<string, string>() {
                        { "From", caller },
                        { "To",   ToNumber },
                        { "Method", "GET" },
                        { "Url",  xml != null ? "http://twimlets.com/echo?Twiml=" + Uri.EscapeDataString(xml)
                                              : "http://twimlets.com/message?Message%5B0%5D=" + Uri.EscapeDataString(Message) }   // O.K.
                        //{ "Url",  "http://twimlets.com/message?" + Uri.EscapeDataString("Message[0]=" + p_message) } // <Response/>  -- successful but empty call
                        //{ "Url",  "http://twimlets.com/message?Message%5B0%5D=Hello+this+is+a+test+call+from+Twilio." }  // O.K.
                        //{ "Url",  "http://twimlets.com/message?Message[0]=Hello%2C+this+is+a+test+call+from+Twilio." }  // Error: 11100 Invalid URL format 
                        //{ "Url",  "http://twimlets.com/message?Message[0]=Hello,+this+is+a+test+call+from+Twilio." }  // Error: 11100 Invalid URL format 
                        //{ "Url",  "http://twimlets.com/message?Message[0]=" + Uri.EscapeDataString(p_message) } // Error: 11100 Invalid URL format 
                        //{ "Url",  "http://www.snifferquant.com/robin/twimlet.xml" }  // O.K.
                    }));
                string resp = await response.Content.ReadAsStringAsync();
                if (resp.StartsWith("{\"sid\":"))
                    ResultJSON = resp;
                else
                    Error = resp;
            }
            catch (Exception e)
            {
                Error = ToStringWithoutStackTrace(e);
                Program.gLogger.Info("Error: " + Error);
                Console.WriteLine("Error: " + Error);
            }
            return Error == null;
        }

        static string ToStringWithoutStackTrace(Exception e)
        {
            string s = (e == null ? null : e.ToString()) ?? String.Empty;
            return s.Substring(0, Math.Min(s.Length, s.IndexOf("\n   at ") & int.MaxValue));
        }
    }
}
