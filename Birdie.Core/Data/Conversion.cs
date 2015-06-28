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
    public class DataConverter
    {
        #region Delegates        
        public delegate object ConversionFunction(WatchMemoryObject watchOject);
        #endregion

        #region Methods   
        public DataConverter()
        {
            BaseConversionFunctions.RegisterConversionFunctions(this);
        }

        /// <summary>
        /// Static constructor which adds all the conversion methods for base types.
        /// </summary>
        public void AddConversionFunction(string type, ConversionFunction function)
        {
            if (!conversionFunctions.ContainsKey(type))
                conversionFunctions.Add(type, function);
        }

        public object Convert(WatchMemoryObject watchMemoryObject)
        {
            if (!conversionFunctions.ContainsKey(watchMemoryObject.Type))
                return null;

            try
            {
                object result = conversionFunctions[watchMemoryObject.Type](watchMemoryObject);
                return result;
            }
            catch (SystemException excpt)
            {
                return string.Format("Conversion function threw an exception: {0}!", excpt.ToString());
            }

        }
        #endregion

        #region Fields
        private Dictionary<string, ConversionFunction> conversionFunctions = new Dictionary<string,ConversionFunction>();
        #endregion
    }

    internal static class BaseConversionFunctions
    {
        #region Methods
        public static void RegisterConversionFunctions(DataConverter dataConverter)
        {
            dataConverter.AddConversionFunction(BaseTypes.Bool.ToString(), BoolToString);
            dataConverter.AddConversionFunction(BaseTypes.Int8.ToString(), Int8ToString);
            dataConverter.AddConversionFunction(BaseTypes.Int16.ToString(), Int16ToString);
            dataConverter.AddConversionFunction(BaseTypes.Int32.ToString(), Int32ToString);
            dataConverter.AddConversionFunction(BaseTypes.Int64.ToString(), Int64ToString);

            dataConverter.AddConversionFunction(BaseTypes.UInt8.ToString(), UInt8ToString);
            dataConverter.AddConversionFunction(BaseTypes.UInt16.ToString(), UInt16ToString);
            dataConverter.AddConversionFunction(BaseTypes.UInt32.ToString(), UInt32ToString);
            dataConverter.AddConversionFunction(BaseTypes.UInt64.ToString(), UInt64ToString);

            dataConverter.AddConversionFunction(BaseTypes.Float32.ToString(), Float32ToString);
            dataConverter.AddConversionFunction(BaseTypes.Float64.ToString(), Float64ToString);

            dataConverter.AddConversionFunction(BaseTypes.ANSIString.ToString(), ANSIStringToString);
            dataConverter.AddConversionFunction(BaseTypes.UTF8String.ToString(), UTF8StringToString);
            dataConverter.AddConversionFunction(BaseTypes.HEXPattern.ToString(), HEXPatternToString);
        }

        public static object BoolToString(WatchMemoryObject watchMemoryObject)
        {
            return watchMemoryObject.Data[0] > 0 ? "true" : "false";
        }

        public static object Int8ToString(WatchMemoryObject watchMemoryObject)
        {
            return ((SByte)watchMemoryObject.Data[0]).ToString();
        }

        public static object Int16ToString(WatchMemoryObject watchMemoryObject)
        {
            return BitConverter.ToInt16(watchMemoryObject.Data, 0).ToString();
        }

        public static object Int32ToString(WatchMemoryObject watchMemoryObject)
        {
            return BitConverter.ToInt32(watchMemoryObject.Data, 0).ToString();
        }

        public static object Int64ToString(WatchMemoryObject watchMemoryObject)
        {
            return BitConverter.ToInt64(watchMemoryObject.Data, 0).ToString();
        }

        public static object UInt8ToString(WatchMemoryObject watchMemoryObject)
        {
            return watchMemoryObject.Data[0].ToString();
        }

        public static object UInt16ToString(WatchMemoryObject watchMemoryObject)
        {
            return BitConverter.ToUInt16(watchMemoryObject.Data, 0).ToString();
        }

        public static object UInt32ToString(WatchMemoryObject watchMemoryObject)
        {
            return BitConverter.ToUInt32(watchMemoryObject.Data, 0).ToString();
        }

        public static object UInt64ToString(WatchMemoryObject watchMemoryObject)
        {
            return BitConverter.ToUInt64(watchMemoryObject.Data, 0).ToString();
        }

        public static object Float32ToString(WatchMemoryObject watchMemoryObject)
        {
            return BitConverter.ToSingle(watchMemoryObject.Data, 0).ToString();
        }

        public static object Float64ToString(WatchMemoryObject watchMemoryObject)
        {
            return BitConverter.ToDouble(watchMemoryObject.Data, 0).ToString();
        }

        public static object ANSIStringToString(WatchMemoryObject watchMemoryObject)
        {
            return Encoding.Default.GetString(watchMemoryObject.Data, 0, (int)watchMemoryObject.MaxSize);
        }

        public static object UTF8StringToString(WatchMemoryObject watchMemoryObject)
        {
            return Encoding.UTF8.GetString(watchMemoryObject.Data, 0, (int)watchMemoryObject.MaxSize);
        }

        public static object HEXPatternToString(WatchMemoryObject watchMemoryObject)
        {
            string hex = "";

            foreach (byte b in watchMemoryObject.Data)
                hex += string.Format("{0:x2} ", b);

            return hex;
        }
        #endregion
    }
}
