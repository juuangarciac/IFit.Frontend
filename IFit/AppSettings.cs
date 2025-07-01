using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit
{
    class AppSettings
    {
        public static string BaseAddress = DeviceInfo.Platform == DevicePlatform.Android ? "http://192.168.1.52:8080" : "http://192.168.1.52:8080";
    }
}
