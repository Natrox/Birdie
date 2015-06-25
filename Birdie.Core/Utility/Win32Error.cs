using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Birdie.Utility
{
    public static class Win32Error
    {
        public static string GetLastWin32Error()
        {
            return new Win32Exception().Message;
        }
    }
}
