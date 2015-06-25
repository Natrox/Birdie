using System;

namespace Birdie.Interop
{
    internal static class Privileges
    {
        #region Methods
        /// <summary>
        /// Sets the appropriate privileges to use functions like ReadProcess.
        /// </summary>
        public static void SetPrivileges()
        {
            // Sets SeDebugPrivilege for the current thread
            System.Diagnostics.Process.EnterDebugMode();
        }
        #endregion
    }
}
