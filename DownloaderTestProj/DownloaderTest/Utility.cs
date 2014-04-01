using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace lib
{
    public class Utility
    {
        public static string GetClassDesc(string name, string[] fields, string[] vals)
        {
            StringBuilder builder = new StringBuilder(name);
            builder.Append("{");

            for(int i = 0; i < vals.Length; i++)
            {
                if(i > 0)
                    builder.Append(",");
                builder.AppendFormat("{0}:{1}", fields[i],vals[i]);
            }

            builder.Append("}");
            return builder.ToString();
        }

        public static string ByteToStr(byte[] buf)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < buf.Length; i++)
            {
                builder.AppendFormat("{0:X2}", buf[i]);
            }
            return builder.ToString();
        }

        public static string ArrayToStr(object[] arr)
        {
            StringBuilder builder = new StringBuilder("{");
            for (int i = 0; i < arr.Length; i++)
            {
                builder.Append(arr[i].ToString());
            }
            builder.Append("}");
            return builder.ToString();
        }
    }
}
