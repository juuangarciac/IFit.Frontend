using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit
{
    class AppSettings
    {
        public static string AppName = "IFit";

        // Base address for the HTTP client, depending on the platform
        public static string serverAddress = "http://192.168.1.63:8080";
        public static string BaseAddress = DeviceInfo.Platform == DevicePlatform.Android ? serverAddress : serverAddress;
        public static readonly HttpClient _HttpClient = new HttpClient();
    }
}
