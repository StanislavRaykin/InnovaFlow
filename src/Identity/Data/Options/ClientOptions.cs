using System.ComponentModel.DataAnnotations;
namespace Identity.Data.Options;

public class ClientOptions
{
    public string BaseUrl { get; set; } = "";

    public string CallbackPath { get; set; } = "/auth/callback"; 

    public string LoginPath { get; set; } = "/auth/login";

    public string Callback => $"{BaseUrl.TrimEnd('/')}{CallbackPath}";
    public string Login    => $"{BaseUrl.TrimEnd('/')}{LoginPath}";
}
