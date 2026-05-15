using System.Text;
using System.Text.Json;

namespace VelvetCakes.Api.Services;

public class YooKassaPaymentResponse
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public YooKassaConfirmation? Confirmation { get; set; }
}

public class YooKassaConfirmation
{
    public string Type { get; set; } = string.Empty;
    public string ConfirmationUrl { get; set; } = string.Empty;
}

public class CreatePaymentRequest
{
    public YooKassaAmount Amount { get; set; } = new();
    public string? Description { get; set; }
    public YooKassaConfirmationData Confirmation { get; set; } = new();
    public string? Capture { get; set; }
}

public class YooKassaAmount
{
    public string Value { get; set; } = string.Empty;
    public string Currency { get; set; } = "RUB";
}

public class YooKassaConfirmationData
{
    public string Type { get; set; } = "redirect";
    public string ReturnUrl { get; set; } = string.Empty;
}

public interface IYooKassaService
{
    Task<YooKassaPaymentResponse?> CreatePaymentAsync(decimal amount, string description, string returnUrl);
}

public class YooKassaService : IYooKassaService
{
    private readonly HttpClient _httpClient;
    private readonly string _shopId;
    private readonly string _secretKey;
    private readonly ILogger<YooKassaService> _logger;

    public YooKassaService(IConfiguration config, ILogger<YooKassaService> logger)
    {
        _shopId = config["YooKassa:ShopId"] ?? "";
        _secretKey = config["YooKassa:SecretKey"] ?? "";
        _logger = logger;

        _httpClient = new HttpClient();
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_shopId}:{_secretKey}"));
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {auth}");
        _httpClient.BaseAddress = new Uri("https://api.yookassa.ru/v3/");
    }

    public async Task<YooKassaPaymentResponse?> CreatePaymentAsync(decimal amount, string description, string returnUrl)
    {
        try
        {
            var request = new CreatePaymentRequest
            {
                Amount = new YooKassaAmount { Value = amount.ToString("F2"), Currency = "RUB" },
                Description = description,
                Confirmation = new YooKassaConfirmationData { Type = "redirect", ReturnUrl = returnUrl },
                Capture = "true"
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("payments", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<YooKassaPaymentResponse>(json);
            }

            _logger.LogError($"YooKassa error: {response.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "YooKassa payment creation failed");
            return null;
        }
    }
}