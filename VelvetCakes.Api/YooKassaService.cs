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
    public string ConfirmationToken { get; set; } = string.Empty;
}

public class CreatePaymentRequest
{
    public YooKassaAmount Amount { get; set; } = new();
    public string? Description { get; set; }
    public YooKassaConfirmationData Confirmation { get; set; } = new();
    public string? Capture { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
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
    Task<YooKassaPaymentResponse?> CreatePaymentAsync(decimal amount, string description, string returnUrl, string confirmationType = "redirect", int orderId = 0);
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

    public async Task<YooKassaPaymentResponse?> CreatePaymentAsync(decimal amount, string description, string returnUrl, string confirmationType = "redirect", int orderId = 0)
    {
        try
        {
            _logger.LogInformation($"Creating payment: amount={amount}, description={description}, returnUrl={returnUrl}, type={confirmationType}, orderId={orderId}");

            var request = new CreatePaymentRequest
            {
                Amount = new YooKassaAmount { Value = amount.ToString("F2"), Currency = "RUB" },
                Description = description,
                Confirmation = new YooKassaConfirmationData
                {
                    Type = confirmationType,
                    ReturnUrl = returnUrl
                },
                Capture = "true",
                Metadata = new Dictionary<string, string>
                {
                    { "orderId", orderId.ToString() }
                }
            };

            var jsonRequest = JsonSerializer.Serialize(request);
            _logger.LogInformation($"Request JSON: {jsonRequest}");

            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("payments", content);

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"YooKassa response status: {response.StatusCode}");
            _logger.LogInformation($"YooKassa response body: {responseBody}");

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<YooKassaPaymentResponse>(responseBody);
                return result;
            }

            _logger.LogError($"YooKassa error: {response.StatusCode}, Body: {responseBody}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "YooKassa payment creation failed");
            return null;
        }
    }
}