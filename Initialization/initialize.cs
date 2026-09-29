using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Text.Json;

namespace N2N_Saenai.Initialization
{
    public static class Initialize {
        public static HttpClient _client;
        public static Initialization.LauncherConfig _config;
        public static void Init() {
            HttpInit();
        }

        private static void HttpInit() {
            _client = new HttpClient();

            _config = new()
            {
                APIURL = "https://saenai.asia:8443/login"
            };
        }
    }
}
