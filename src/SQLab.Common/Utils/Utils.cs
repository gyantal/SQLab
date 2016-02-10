using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SQCommon
{
    public enum Platform
    {
        Windows,
        Linux,
        Mac
    }


    public static partial class Utils
    {
        public static Platform RunningPlatform()
        {
            if (Environment.NewLine == "\n")
                return Platform.Linux;
            else
                return Platform.Windows;   // "\r\n" for non-Unix platforms

            //switch (Environment.OSVersion.Platform)     // Environment.OSVersion doesn't exist in DotNetCore
            //{
            //    case PlatformID.Unix:
            //        // Well, there are chances MacOSX is reported as Unix instead of MacOSX.
            //        // Instead of platform check, we'll do a feature checks (Mac specific root folders)
            //        if (Directory.Exists("/Applications")
            //            & Directory.Exists("/System")
            //            & Directory.Exists("/Users")
            //            & Directory.Exists("/Volumes"))
            //            return Platform.Mac;
            //        else
            //            return Platform.Linux;

            //    case PlatformID.MacOSX:
            //        return Platform.Mac;

            //    default:
            //        return Platform.Windows;
            //}
        }

        public static string FromCodedString(string p_str)
        {
            return "";
            //return Encoding.UTF8.GetString(Convert.FromBase64String(p_str));
        }
       
    }
}
