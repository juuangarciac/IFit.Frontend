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
        public static string ApiGatewayBaseUrl = "http://192.168.1.72:8080/ifit/api/v1";
        public static string RefreshTokenEndpoint = "/auth/refresh";

        public static string BaseAddress = DeviceInfo.Platform == DevicePlatform.Android ? ApiGatewayBaseUrl : ApiGatewayBaseUrl;

        public static readonly HttpClient _HttpClient = new HttpClient();

        // SQLite database
        public const string DatabaseFilename = "IFitSQLite.db3";

        public const SQLite.SQLiteOpenFlags Flags =
            // open the database in read/write mode
            SQLite.SQLiteOpenFlags.ReadWrite |
            // create the database if it doesn't exist
            SQLite.SQLiteOpenFlags.Create |
            // enable multi-threaded database access
            SQLite.SQLiteOpenFlags.SharedCache;

        public static string DatabasePath =>
            Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);
    }
}
