using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace HelpDeskSystem.Integrations.Twilio;

public class TwilioService : ITwilioService
{
    private readonly HttpClient _httpClient;
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioService> _logger;

    private const string ApiBaseUrl = "https://api.twilio.com/2010-04-01/Accounts";

    public TwilioService(IOptions<TwilioOptions> options, ILogger<TwilioService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = new HttpClient();
        
        // Set up basic authentication
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.AccountSid}:{_options.AuthToken}"));
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
    }

    public async Task<string> SendSmsAsync(string to, string message, string fromNumber = null)
    {
        try
        {
            var from = fromNumber ?? _options.DefaultPhoneNumber;
            var encodedMessage = HttpUtility.UrlEncode(message);
            
            var requestBody = $"To={HttpUtility.UrlEncode(to)}&From={HttpUtility.UrlEncode(from)}&Body={encodedMessage}";
            var content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/{_options.AccountSid}/Messages.json", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
            
            _logger.LogInformation("SMS sent successfully. SID: {Sid}", result["sid"]);
            return result["sid"].ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {To}", to);
            throw;
        }
    }

    public async Task<string> InitiateCallAsync(string to, string fromNumber = null, string url = null)
    {
        try
        {
            var from = fromNumber ?? _options.DefaultPhoneNumber;
            var webhookUrl = url ?? $"{_options.WebhookBaseUrl}/api/twilio/call/incoming";
            
            var requestBody = $"To={HttpUtility.UrlEncode(to)}&From={HttpUtility.UrlEncode(from)}&Url={HttpUtility.UrlEncode(webhookUrl)}";
            var content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/{_options.AccountSid}/Calls.json", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
            
            _logger.LogInformation("Call initiated successfully. SID: {Sid}", result["sid"]);
            return result["sid"].ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate call to {To}", to);
            throw;
        }
    }

    public async Task<TwilioWebhookEvent> ProcessIncomingWebhookAsync(Dictionary<string, string> formData)
    {
        try
        {
            var eventType = formData.ContainsKey("MessageSid") ? "sms" : "voice";
            
            var webhookEvent = new TwilioWebhookEvent
            {
                EventType = eventType,
                MessageSid = formData.GetValueOrDefault("MessageSid"),
                CallSid = formData.GetValueOrDefault("CallSid"),
                From = formData.GetValueOrDefault("From"),
                To = formData.GetValueOrDefault("To"),
                Body = formData.GetValueOrDefault("Body"),
                Status = formData.GetValueOrDefault("SmsStatus") ?? formData.GetValueOrDefault("CallStatus"),
                Timestamp = DateTime.UtcNow,
                AdditionalData = formData.Where(kvp => 
                    !new[] { "MessageSid", "CallSid", "From", "To", "Body", "SmsStatus", "CallStatus" }.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            _logger.LogInformation("Processed {EventType} webhook from {From}", eventType, webhookEvent.From);
            return webhookEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Twilio webhook");
            throw;
        }
    }

    public async Task<bool> ValidateWebhookSignatureAsync(string signature, string url, Dictionary<string, string> formData)
    {
        try
        {
            var sortedData = formData.OrderBy(kvp => kvp.Key)
                .Select(kvp => $"{kvp.Key}{kvp.Value}")
                .Aggregate((a, b) => $"{a}{b}");

            var computedSignature = ComputeHmacSha1(_options.AuthToken, $"{url}{sortedData}");
            var expectedSignature = $"sha1={computedSignature}";

            return signature == expectedSignature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Twilio webhook signature");
            return false;
        }
    }

    private string ComputeHmacSha1(string key, string data)
    {
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }

    public async Task<TwilioPhoneNumber[]> GetAvailablePhoneNumbersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/{_options.AccountSid}/AvailablePhoneNumbers/US/Local.json?SmsEnabled=true&VoiceEnabled=true");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
            
            var phoneNumbers = new List<TwilioPhoneNumber>();
            if (result.ContainsKey("available_phone_numbers"))
            {
                var numbers = JsonSerializer.Deserialize<JsonElement[]>(result["available_phone_numbers"].ToString());
                foreach (var number in numbers)
                {
                    phoneNumbers.Add(new TwilioPhoneNumber
                    {
                        PhoneNumber = number.GetProperty("phone_number").GetString(),
                        FriendlyName = number.GetProperty("friendly_name").GetString(),
                        CountryCode = number.GetProperty("iso_country").GetString(),
                        Capabilities = $"{number.GetProperty("capabilities").GetProperty("sms")},{number.GetProperty("capabilities").GetProperty("voice")}",
                        IsAvailable = true,
                        Price = 1.00m // Default price, actual price would need lookup
                    });
                }
            }

            return phoneNumbers.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available phone numbers");
            throw;
        }
    }

    public async Task<bool> PurchasePhoneNumberAsync(string phoneNumber)
    {
        try
        {
            var requestBody = $"PhoneNumber={HttpUtility.UrlEncode(phoneNumber)}";
            var content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpClient.PostAsync($"{ApiBaseUrl}/{_options.AccountSid}/IncomingPhoneNumbers.json", content);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully purchased phone number: {PhoneNumber}", phoneNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to purchase phone number: {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public async Task<TwilioCallLog[]> GetCallLogsAsync(DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (from.HasValue) queryParams.Add($"StartTime>={from.Value:yyyy-MM-dd}");
            if (to.HasValue) queryParams.Add($"StartTime<={to.Value:yyyy-MM-dd}");
            
            var queryString = queryParams.Any() ? $"?{string.Join("&", queryParams)}" : "";
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/{_options.AccountSid}/Calls.json{queryString}");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
            
            var callLogs = new List<TwilioCallLog>();
            if (result.ContainsKey("calls"))
            {
                var calls = JsonSerializer.Deserialize<JsonElement[]>(result["calls"].ToString());
                foreach (var call in calls)
                {
                    callLogs.Add(new TwilioCallLog
                    {
                        CallSid = call.GetProperty("sid").GetString(),
                        From = call.GetProperty("from").GetString(),
                        To = call.GetProperty("to").GetString(),
                        Status = call.GetProperty("status").GetString(),
                        StartTime = DateTime.Parse(call.GetProperty("date_created").GetString()),
                        EndTime = call.TryGetProperty("date_updated", out var endTime) ? DateTime.Parse(endTime.GetString()) : null,
                        DurationSeconds = call.TryGetProperty("duration", out var duration) ? int.Parse(duration.GetString()) : 0,
                        Price = call.TryGetProperty("price", out var price) && price.ValueKind != JsonValueKind.Null ? decimal.Parse(price.GetString()) : 0,
                        Direction = call.GetProperty("direction").GetString()
                    });
                }
            }

            return callLogs.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get call logs");
            throw;
        }
    }

    public async Task<TwilioMessageLog[]> GetMessageLogsAsync(DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (from.HasValue) queryParams.Add($"DateSent>={from.Value:yyyy-MM-dd}");
            if (to.HasValue) queryParams.Add($"DateSent<={to.Value:yyyy-MM-dd}");
            
            var queryString = queryParams.Any() ? $"?{string.Join("&", queryParams)}" : "";
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/{_options.AccountSid}/Messages.json{queryString}");
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
            
            var messageLogs = new List<TwilioMessageLog>();
            if (result.ContainsKey("messages"))
            {
                var messages = JsonSerializer.Deserialize<JsonElement[]>(result["messages"].ToString());
                foreach (var message in messages)
                {
                    messageLogs.Add(new TwilioMessageLog
                    {
                        MessageSid = message.GetProperty("sid").GetString(),
                        From = message.GetProperty("from").GetString(),
                        To = message.GetProperty("to").GetString(),
                        Body = message.GetProperty("body").GetString(),
                        Status = message.GetProperty("status").GetString(),
                        DateCreated = DateTime.Parse(message.GetProperty("date_created").GetString()),
                        DateSent = message.TryGetProperty("date_sent", out var dateSent) && dateSent.ValueKind != JsonValueKind.Null ? DateTime.Parse(dateSent.GetString()) : null,
                        NumSegments = message.TryGetProperty("num_segments", out var segments) ? int.Parse(segments.GetString()) : 1,
                        Price = message.TryGetProperty("price", out var price) && price.ValueKind != JsonValueKind.Null ? decimal.Parse(price.GetString()) : 0,
                        Direction = message.GetProperty("direction").GetString()
                    });
                }
            }

            return messageLogs.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message logs");
            throw;
        }
    }
}

public class TwilioOptions
{
    public string AccountSid { get; set; }
    public string AuthToken { get; set; }
    public string DefaultPhoneNumber { get; set; }
    public string WebhookBaseUrl { get; set; }
}
