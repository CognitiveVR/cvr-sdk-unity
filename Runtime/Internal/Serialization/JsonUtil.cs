using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Cognitive3D.Serialization
{

    internal static class JsonUtil
    {
        /// <returns>"name":["obj","obj","obj"]</returns>
        public static StringBuilder SetListString(string name, List<string> list, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":[");
            for (int i = 0; i < list.Count; i++)
            {
                builder.Append("\"");
                builder.Append(list[i]);
                builder.Append("\",");
            }
            builder.Remove(builder.Length - 1, 1);
            builder.Append("]");
            return builder;
        }

        /// <returns>"name":[obj,obj,obj]</returns>
        public static StringBuilder SetListObject<T>(string name, List<T> list, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":{");
            for (int i = 0; i < list.Count; i++)
            {
                builder.Append(list[i].ToString());
                builder.Append(",");
            }
            builder.Remove(builder.Length - 1, 1);
            builder.Append("}");
            return builder;
        }

        /// <returns>"name":"stringval"</returns>
        public static StringBuilder SetString(string name, string stringValue, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":\"");

            builder.Append(stringValue);
            builder.Append("\"");

            return builder;
        }

        /// <returns>"name":"intValue"</returns>
        public static StringBuilder SetInt(string name, int intValue, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":");
            builder.Concat(intValue);
            return builder;
        }

        /// <returns>"name":"floatValue"</returns>
        public static StringBuilder SetFloat(string name, float floatValue, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":");
            builder.Concat(floatValue);
            return builder;
        }

        /// <returns>"name":"doubleValue"</returns>
        public static StringBuilder SetDouble(string name, double doubleValue, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":");
            builder.ConcatDouble(doubleValue);
            return builder;
        }

        /// <returns>"name":"doubleValue"</returns>
        public static StringBuilder SetLong(string name, long longValue, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":");
            builder.Concat(longValue);
            return builder;
        }

        /// <returns>"name":"null value"</returns>
        public static StringBuilder SetNull(string name, StringBuilder builder)
        {
            builder.Append("null");
            return builder;
        }

        /// <returns>"name":objectValue.ToString()</returns>
        public static StringBuilder SetObject(string name, object objectValue, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":");

            if (objectValue.GetType() == typeof(bool))
                builder.Append(objectValue.ToString().ToLower());
            else
                builder.Append(objectValue.ToString());

            return builder;
        }

        /// <returns>"name":[0.1,0.2,0.3]</returns>
        public static StringBuilder SetVector(string name, float[] pos, StringBuilder builder, bool centimeterLimit = false)
        {
            if (pos.Length < 3) { pos = new float[3] { 0, 0, 0 }; }

            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":[");

            if (centimeterLimit)
            {
                builder.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.00}", pos[0]));

                builder.Append(",");
                builder.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.00}", pos[1]));

                builder.Append(",");
                builder.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.00}", pos[2]));

            }
            else
            {
                builder.Concat(pos[0]);
                builder.Append(",");
                builder.Concat(pos[1]);
                builder.Append(",");
                builder.Concat(pos[2]);
            }

            builder.Append("]");
            return builder;
        }

        /// <returns>"name":[0.1,0.2,0.3]</returns>
        public static StringBuilder SetVectorRaw(string name, float posx, float posy, float posz, StringBuilder builder, bool centimeterLimit = false)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":[");

            builder.Concat(posx);
            builder.Append(',');
            builder.Concat(posy);
            builder.Append(',');
            builder.Concat(posz);

            builder.Append(']');
            return builder;
        }

        /// <returns>"name":[0.1,0.2</returns>
        public static StringBuilder SetVector2(string name, float[] pos, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":[");

            builder.Concat(pos[0]);
            builder.Append(",");
            builder.Concat(pos[1]);

            builder.Append("]");
            return builder;
        }

        /// <returns>"name":[0.1,0.2,0.3,0.4]</returns>
        public static StringBuilder SetQuat(string name, float[] quat, StringBuilder builder)
        {
            if (quat.Length < 4) { quat = new float[4] { 0, 0, 0, 0 }; }

            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":[");

            builder.Concat(quat[0]);
            builder.Append(",");
            builder.Concat(quat[1]);
            builder.Append(",");
            builder.Concat(quat[2]);
            builder.Append(",");
            builder.Concat(quat[3]);

            builder.Append("]");
            return builder;
        }

        /// <returns>"name":[0.1,0.2,0.3,0.4]</returns>
        public static StringBuilder SetQuatRaw(string name, float quatx, float quaty, float quatz, float quatw, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":[");

            builder.Concat(quatx);
            builder.Append(',');
            builder.Concat(quaty);
            builder.Append(',');
            builder.Concat(quatz);
            builder.Append(',');
            builder.Concat(quatw);

            builder.Append(']');
            return builder;
        }
    }
}