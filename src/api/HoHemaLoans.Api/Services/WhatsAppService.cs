using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoHemaLoans.Api.Services;

/// <summary>
/// Service for interacting with WhatsApp Business API
/// Handles message sending, media uploads, and webhook verification
/// </summary>
public interface IWhatsAppService
{
    Task<bool> VerifyWebhookAsync(string mode, string token, string challenge);
    Task<WhatsAppWebhookPayload?> ParseWebhookAsync(Stream body);
    Task<bool> SendMessageAsync(string phoneNumber, string messageText);
    Task<bool> SendTemplateMessageAsync(string phoneNumber, string templateName, List<string>? parameters = null);
    Task<bool> SendMediaMessageAsync(string phoneNumber, string mediaUrl, string mediaType, string? caption = null);
}

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(
        HttpClient httpClient,
        IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        // Configure default headers
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.AccessToken}");
    }

    /// <summary>
    /// Verify webhook authenticity with Meta
    /// </summary>
    public Task<bool> VerifyWebhookAsync(string mode, string token, string challenge)
    {
        if (mode != "subscribe" || token != _settings.VerifyToken)
        {
            _logger.LogWarning("Webhook verification failed. Mode: {Mode}, Token match: {TokenMatch}",
                mode, token == _settings.VerifyToken);
            return Task.FromResult(false);
        }

        _logger.LogInformation("Webhook verified successfully");
        return Task.FromResult(true);
    }

    /// <summary>
    /// Parse incoming webhook payload from Meta
    /// </summary>
    public async Task<WhatsAppWebhookPayload?> ParseWebhookAsync(Stream body)
    {
        try
        {
            var payload = await JsonSerializer.DeserializeAsync<WhatsAppWebhookPayload>(body, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payload == null)
            {
                _logger.LogWarning("Failed to deserialize webhook payload");
                return null;
            }

            _logger.LogInformation("Parsed webhook payload. Entry count: {EntryCount}", payload.Entry?.Count ?? 0);
            return payload;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing webhook payload");
            return null;
        }
    }

    /// <summary>
    /// Send a text message via WhatsApp
    /// </summary>
    public async Task<bool> SendMessageAsync(string phoneNumber, string messageText)
    {
        try
        {
            var cleanPhoneNumber = CleanPhoneNumber(phoneNumber);
            var url = $"https://graph.facebook.com/{_settings.ApiVersion}/{_settings.PhoneNumberId}/messages";

            var request = new
            {
                messaging_product = "whatsapp",
                to = cleanPhoneNumber,
                type = "text",
                text = new { body = messageText }
            };

            var response = await _httpClient.PostAsJsonAsync(url, request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Message sent successfully to {PhoneNumber}", cleanPhoneNumber);
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to send message to {PhoneNumber}. Status: {StatusCode}, Error: {Error}",
                cleanPhoneNumber, response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending message to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    /// <summary>
    /// Send a template message via WhatsApp
    /// </summary>
    public async Task<bool> SendTemplateMessageAsync(string phoneNumber, string templateName, List<string>? parameters = null)
    {
        try
        {
            var cleanPhoneNumber = CleanPhoneNumber(phoneNumber);
            var url = $"https://graph.facebook.com/{_settings.ApiVersion}/{_settings.PhoneNumberId}/messages";

            var parameterList = parameters?.Select(p => new { type = "text", text = p }).Cast<object>().ToList() ?? new List<object>();

            var request = new
            {
                messaging_product = "whatsapp",
                to = cleanPhoneNumber,
                type = "template",
                template = new
                {
                    name = templateName,
                    language = new { code = "en_US" },
                    body = new { parameters = parameterList }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Template message '{Template}' sent to {PhoneNumber}", 
                    templateName, cleanPhoneNumber);
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to send template message to {PhoneNumber}. Status: {StatusCode}, Error: {Error}",
                cleanPhoneNumber, response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending template message to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    /// <summary>
    /// Send media (image, document, etc.) via WhatsApp
    /// </summary>
    public async Task<bool> SendMediaMessageAsync(string phoneNumber, string mediaUrl, string mediaType, string? caption = null)
    {
        try
        {
            var cleanPhoneNumber = CleanPhoneNumber(phoneNumber);
            var url = $"https://graph.facebook.com/{_settings.ApiVersion}/{_settings.PhoneNumberId}/messages";

            var mediaTypeUpper = mediaType.ToLower();
            
            var request = new
            {
                messaging_product = "whatsapp",
                to = cleanPhoneNumber,
                type = mediaTypeUpper,
                image = mediaTypeUpper == "image" ? new { link = mediaUrl } : null,
                document = mediaTypeUpper == "document" ? new { link = mediaUrl } : null,
                audio = mediaTypeUpper == "audio" ? new { link = mediaUrl } : null,
                video = mediaTypeUpper == "video" ? new { link = mediaUrl } : null,
                caption = caption
            };

            if (!new[] { "image", "document", "audio", "video" }.Contains(mediaTypeUpper))
            {
                _logger.LogError("Unsupported media type: {MediaType}", mediaType);
                return false;
            }

            var response = await _httpClient.PostAsJsonAsync(url, request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Media message ({MediaType}) sent to {PhoneNumber}", 
                    mediaType, cleanPhoneNumber);
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to send media message to {PhoneNumber}. Status: {StatusCode}, Error: {Error}",
                cleanPhoneNumber, response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending media message to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    /// <summary>
    /// Clean phone number to E.164 format (e.g., +27812345678)
    /// </summary>
    private static string CleanPhoneNumber(string phoneNumber)
    {
        // Remove any non-digit characters
        var digits = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"\D", "");

        // Add country code if not present
        if (!digits.StartsWith("27")) // South Africa
        {
            if (digits.StartsWith("0"))
                digits = "27" + digits[1..];
            else if (!digits.StartsWith("27"))
                digits = "27" + digits;
        }

        return "+" + digits;
    }
}

/// <summary>
/// WhatsApp configuration settings from appsettings.json
/// </summary>
public class WhatsAppSettings
{
    public string AccessToken { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string VerifyToken { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "v18.0";
    public string WebhookUrl { get; set; } = string.Empty;
}

/// <summary>
/// Webhook payload models for deserializing Meta webhook
/// </summary>
public class WhatsAppWebhookPayload
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("entry")]
    public List<WebhookEntry>? Entry { get; set; }
}

public class WebhookEntry
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("changes")]
    public List<WebhookChange>? Changes { get; set; }
}

public class WebhookChange
{
    [JsonPropertyName("field")]
    public string? Field { get; set; }

    [JsonPropertyName("value")]
    public WebhookValue? Value { get; set; }
}

public class WebhookValue
{
    [JsonPropertyName("messaging_product")]
    public string? MessagingProduct { get; set; }

    [JsonPropertyName("metadata")]
    public WebhookMetadata? Metadata { get; set; }

    [JsonPropertyName("contacts")]
    public List<WebhookContact>? Contacts { get; set; }

    [JsonPropertyName("messages")]
    public List<WebhookMessage>? Messages { get; set; }

    [JsonPropertyName("statuses")]
    public List<WebhookStatus>? Statuses { get; set; }
}

public class WebhookMetadata
{
    [JsonPropertyName("display_phone_number")]
    public string? DisplayPhoneNumber { get; set; }

    [JsonPropertyName("phone_number_id")]
    public string? PhoneNumberId { get; set; }
}

public class WebhookContact
{
    [JsonPropertyName("profile")]
    public WebhookProfile? Profile { get; set; }

    [JsonPropertyName("wa_id")]
    public string? WaId { get; set; }
}

public class WebhookProfile
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class WebhookMessage
{
    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("text")]
    public WebhookText? Text { get; set; }

    [JsonPropertyName("image")]
    public WebhookMedia? Image { get; set; }

    [JsonPropertyName("document")]
    public WebhookMedia? Document { get; set; }

    [JsonPropertyName("audio")]
    public WebhookMedia? Audio { get; set; }

    [JsonPropertyName("video")]
    public WebhookMedia? Video { get; set; }

    [JsonPropertyName("button")]
    public WebhookButton? Button { get; set; }

    [JsonPropertyName("interactive")]
    public WebhookInteractive? Interactive { get; set; }
}

public class WebhookText
{
    [JsonPropertyName("body")]
    public string? Body { get; set; }
}

public class WebhookMedia
{
    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; }

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("caption")]
    public string? Caption { get; set; }
}

public class WebhookButton
{
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class WebhookInteractive
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("list_reply")]
    public WebhookListReply? ListReply { get; set; }

    [JsonPropertyName("button_reply")]
    public WebhookButtonReply? ButtonReply { get; set; }
}

public class WebhookListReply
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class WebhookButtonReply
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

public class WebhookStatus
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("recipient_id")]
    public string? RecipientId { get; set; }
}
