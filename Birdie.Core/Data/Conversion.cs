using Birdie.Watcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Birdie.Data
{
    /// <summary>
    /// Describes the base types supported by the watcher.
    /// </summary>
  
    public enum BaseTypes
    {
        Bool,
        Int8,
        Int16,
        Int32,
        Int64, 
        UInt8,
        UInt16,
        UInt32,
        UInt64,
        Float32,
        Float64,
        ANSIString,
        UTF8String,
        HEXPattern
    }

    /// <summary>
    /// Class used to convert memory information to a format fitting the type.
    /// </summary>
    public static class DataConverter
    {
        #region Delegates        
        public delegate string ConversionFunction(WatchMemoryObject watchOject);
        #endregion

        #region Methods   
        static DataConverter()
        {
            BaseConversionFunctions.RegisterConversionFunctions();
        }

        /// <summary>
        /// Static constructor which adds all the conversion methods for base types.
        /// </summary>
        public static void AddConversionFunction(string type, ConversionFunction function)
        {
            if (!conversionFunctions.ContainsKey(type))
                conversionFunctions.Add(type, function);
        }

        public static string Convert(WatchMemoryObject watchMemoryObject)
        {
            if (!conversionFunctions.ContainsKey(watchMemoryObject.Type))
                return null;

            return conversionFunctions[watchMemoryObject.Type](watchMemoryObject);
        }
        #endregion

        #region Fields
        private static Dictionary<string, ConversionFunction> conversionFunctions = new Dictionary<string,ConversionFunction>();
        #endregion
    }

    internal static class BaseConversionFunctions
    {
        #region Methods
        public static void RegisterConversionFunctions()
        {
            DataConverter.AddConversionFunction(BaseTypes.Bool.ToString(), BoolToString);
            DataConverter.AddConversionFunction(BaseTypes.Int8.ToString(), Int8ToString);
            DataConverter.AddConversionFunction(BaseTypes.Int16.ToString(), Int16ToString);
            DataConverter.AddConversionFunction(BaseTypes.Int32.ToString(), Int32ToString);
            DataConverter.AddConversionFunction(BaseTypes.Int64.ToString(), Int64ToString);

            DataConverter.AddConversionFunction(BaseTypes.UInt8.ToString(), UInt8ToString);
            DataConverter.AddConversionFunction(BaseTypes.UInt16.ToString(), UInt16ToString);
            DataConverter.AddConversionFunction(BaseTypes.UInt32.ToString(), UInt32ToString);
            DataConverter.AddConversionFunction(BaseTypes.UInt64.ToString(), UInt64ToString);

            DataConverter.AddConversionFunction(BaseTypes.Float32.ToString(), Float32ToString);
            DataConverter.AddConversionFunction(BaseTypes.Float64.ToString(), Float64ToString);

            DataConverter.AddConversionFunction(BaseTypes.ANSIString.ToString(), ANSIStringToString);
            DataConverter.AddConversionFunction(BaseTypes.UTF8String.ToString(), UTF8StringToString);
            DataConverter.AddConversionFunction(BaseTypes.HEXPattern.ToString(), HEXPatternToString);
        }

        public static string BoolToString(WatchMemoryObject watchMemoryObject)
        {
            return watchMemoryObject.Data[0] > 0 ? "true" : "false";
        }

        public static string Int8ToString(WatchMemoryObject watchMemoryObject)
        {
            return ((SByte)watchMemoryObject.Data[0]).ToString();
        }

        public static string Int16ToString(WatchMemoryObject watchMemoryObject)
        {
            return BitConverter.ToInt16(watchMemoryObject.Data, 0).ToString();
        }

        public static string Int32ToString(WatchMemoryObject watchMemoryObject)
        {
            return BitConverter.ToInt32(watchMemoryObject.Data, 0).ToString();
        }

        public static string Int64ToString(WatchMemoryObject watchMemoryObject)
        {
            return BitConverter.ToInt64(watchMemoryObject.Data, 0).ToString();
        }

        public static string UInt8ToString(WatchMemoryObject watchMemoryObject)
        {
            return watchMemoryObject.Data[0].ToString();
        }

        public static string UInt16ToString(WatchMemoryObject watchMemoryObject)
        {
            return BitConverter.ToUInt16(watchMemoryObject.Data, 0).ToString();
        }

        public static string UInt32ToString(WatchMemoryObject watchMemoryObject)
        {
            return BitConverter.ToUInt32(watchMemoryObject.Data, 0).ToString();
        }

        public static string UInt64ToString(WatchMemoryObject watchMemoryObject)
        {
            return BitConverter.ToUInt64(watchMemoryObject.Data, 0).ToString();
        }

        public static string Float32ToString(WatchMemoryObject watchMemoryObject)
        {
            return BitConverter.ToSingle(watchMemoryObject.Data, 0).ToString();
        }

        public static string Float64ToString(WatchMemoryObject watchMemoryObject)
        {
            return BitConverter.ToDouble(watchMemoryObject.Data, 0).ToString();
        }

        public static string ANSIStringToString(WatchMemoryObject watchMemoryObject)
        {
            return Encoding.Default.GetString(watchMemoryObject.Data, 0, (int)watchMemoryObject.MaxSize);
        }

        public static string UTF8StringToString(WatchMemoryObject watchMemoryObject)
        {
            return Encoding.UTF8.GetString(watchMemoryObject.Data, 0, (int)watchMemoryObject.MaxSize);
        }

        public static string HEXPatternToString(WatchMemoryObject watchMemoryObject)
        {
            string hex = "";

            foreach (byte b in watchMemoryObject.Data)
                hex += string.Format("{0:x2} ", b);

            return hex;
        }
        #endregion
    }
}
