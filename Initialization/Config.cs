using System;
using System.Collections.Generic;
using System.Text;

namespace N2N_Saenai.Initialization
{
    public class LoginRequest
    {
        public string userid { get; set; }
        public string password { get; set; }
    }

    public class N2NUserData
    {
        public string? user_id { get; set; }
        public string supernode_ip { get; set; }
        public int supernode_port { get; set; }
        public string community_name { get; set; }
        public string device_name { get; set; }
        public string password { get; set; }
        public string community_key { get; set; }
        public int encrypt_algorithm { get; set; }
    }

    public class LoginResponse
    { 
        public bool success { get; set;  }
        public N2NUserData? data { get; set; }
    }


    public class LauncherConfig
    { 
        public string APIURL { get; set; }
    }
}
