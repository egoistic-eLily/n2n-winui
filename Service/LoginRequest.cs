using N2N_Saenai.Initialization;
using N2N_Saenai.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace N2N_Saenai.Service
{
    public static class Login {

        public static N2NUserData? userdata { get; private set; }

        public static async Task<bool> UploadRequest(string username,string password) {
            try
            {
                Debug.WriteLine($"username-{username}-password-{password}");
                var post = new Initialization.LoginRequest
                {
                    userid = username,
                    password = password
                };
                Debug.WriteLine($"class-{post}");
                var client = Initialization.Initialize._client;
                using var request = new HttpRequestMessage(HttpMethod.Post, Initialization.Initialize._config.APIURL);
                string json = JsonConvert.SerializeObject(post);

                Debug.WriteLine("发送 JSON：");
                Debug.WriteLine(json);
                var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
                request.Content = content;
                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }
                var body = await response.Content.ReadAsStringAsync();
                var apiResult = JsonConvert.DeserializeObject<Initialization.LoginResponse>(body);
                if (apiResult == null || !apiResult.success || apiResult.data == null) return false;
                userdata = apiResult.data;
                Debug.WriteLine($"data-{userdata.supernode_ip}");
                return true;
            }
            catch(Exception ex) {
                Debug.WriteLine(ex);

                return false;
            }
            
        }
    }
}
