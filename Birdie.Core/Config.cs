using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Birdie
{
    /// <summary>
    /// Data used to initialize Birdie.
    /// </summary>
    public class Config
    {
        #region Methods
        internal bool Validate()
        {
            bool isValid = true;

            isValid = isValid && (ListenPort > 0);
            isValid = isValid && (ChallengeKey > 0);

            return isValid;
        }
        #endregion

        #region Properties
        /// <summary>
        /// The port Birdie will listen on for clients.
        /// </summary>
        public ushort ListenPort { get; set; }

        /// <summary>
        /// A key that is used as a handshake so Birdie knows a valid client is connecting.
        /// </summary>
        public UInt64 ChallengeKey { get; set; }
        #endregion
    }
}
