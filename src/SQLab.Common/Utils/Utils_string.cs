using System;
using System.Collections.Generic;
using System.Text;

namespace SqCommon
{
    public static partial class Utils
    {
        // wrote this, but later I didn't use it in YahooFinanceAPI.Token, however it is a good idea to have a file like this here.
        //// DecodeFromUtf8("d\u00C3\u00A9j\u00C3\u00A0"); // it will give back as déjà
        //// https://stackoverflow.com/questions/11293994/how-to-convert-a-utf-8-string-into-unicode
        //public static string DecodeFromUtf8(this string utf8String)
        //{
        //    // copy the string as UTF-8 bytes.
        //    byte[] utf8Bytes = new byte[utf8String.Length];
        //    for (int i = 0; i < utf8String.Length; ++i)
        //    {
        //        //Debug.Assert( 0 <= utf8String[i] && utf8String[i] <= 255, "the char must be in byte's range");
        //        utf8Bytes[i] = (byte)utf8String[i];
        //    }

        //    return Encoding.UTF8.GetString(utf8Bytes, 0, utf8Bytes.Length);
        //}


    }
}
