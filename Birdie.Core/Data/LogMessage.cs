using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Birdie.Data
{
    public enum MessageOrigins
    {
        Client,
        Network,
        Birdie
    }

    /// <summary>
    /// This class represents log messages, either from a client or from Birdie components themselves.
    /// </summary>
    public class LogMessage
    {
        #region Properties
        public string Message { get; set; }
        public string Filter { get; set; }
        public MessageOrigins MessageOrigin { get; internal set; }
        #endregion
    }
}
