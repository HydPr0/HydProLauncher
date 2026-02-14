using System.Text.Json;
using Codexus.Cipher.Entities.WPFLauncher;
using Codexus.Cipher.Utils.Cipher;
using Codexus.Cipher.Utils.Http;
namespace HydPro.Launcher;
public static class AuthenticationExtensions
{
    public static async Task<TResult> Api<TBody, TResult>(this EntityAuthenticationOtp otp, string url, TBody body)
    {
        var http = new HttpWrapper("https://x19apigatewayobt.nie.netease.com");
        var json = JsonSerializer.Serialize(body);
        var response = await http.PostAsync(url, json, builder =>
        {
            builder.AddHeader(TokenUtil.ComputeHttpRequestToken(builder.Url, builder.Body, otp.EntityId, otp.Token));
        });
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResult>(content)!;
    }
}