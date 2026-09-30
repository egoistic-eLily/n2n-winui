using System.Net.Http;
using System;

namespace N2N_Saenai.Initialization;

public static class Initialize
{
    public static HttpClient Client { get; } = new() { Timeout = TimeSpan.FromSeconds(20) };
    public static LauncherConfig Config { get; } = new();
    public static void Init() { }
}
