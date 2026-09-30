using N2N_Saenai.Initialization;
using System.Net.Http.Json;
using System.Net.Http;
using System.Threading.Tasks;
using N2N_Saenai.Serialization;
using System;

namespace N2N_Saenai.Service;

public static class Login
{
    public static N2NUserData? UserData { get; private set; }
    public static string? LastError { get; private set; }

    public static async Task<bool> UploadRequestAsync(string userId, string password)
    {
        LastError = null;
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
        {
            LastError = "请输入用户名和密码。";
            return false;
        }
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Initialize.Config.ApiUrl)
            {
                Content = JsonContent.Create(new LoginRequest { UserId = userId.Trim(), Password = password },
                    AppJsonContext.Default.LoginRequest)
            };
            using var response = await Initialize.Client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                LastError = $"服务器拒绝了请求（HTTP {(int)response.StatusCode}）。";
                return false;
            }
            var result = await response.Content.ReadFromJsonAsync(AppJsonContext.Default.LoginResponse);
            UserData = result is { Success: true, Data: not null } ? result.Data : null;
            if (UserData is null) LastError = "用户名或密码错误，或服务器返回的数据不完整。";
            return UserData is not null;
        }
        catch (HttpRequestException ex) { LastError = $"无法连接服务器：{ex.Message}"; return false; }
        catch (TaskCanceledException) { LastError = "连接服务器超时，请稍后重试。"; return false; }
        catch (Exception ex) { LastError = $"登录组件出错：{ex.Message}"; return false; }
    }
}
