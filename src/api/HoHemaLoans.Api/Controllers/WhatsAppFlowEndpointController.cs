using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;
using HoHemaLoans.Api.Services;

namespace HoHemaLoans.Api.Controllers;

/// <summary>
/// Controller that handles Meta WhatsApp Flows data_exchange and completion callbacks.
/// This is the Flow Endpoint that Meta calls when a user interacts with a WhatsApp Flow.
/// 
/// WhatsApp Flows Architecture:
/// 1. User triggers a flow (e.g., APPLY command)
/// 2. Backend sends a flow message via WhatsApp API
/// 3. User interacts with the flow screens on their phone
/// 4. On data_exchange actions, Meta calls THIS endpoint to get the next screen data
/// 5. On complete actions, Meta sends the final payload to THIS endpoint
/// 
/// This enables a rich, native-feeling multi-screen experience within WhatsApp.
/// </summary>
[ApiController]
[Route("api/whatsapp")]
public class WhatsAppFlowEndpointController : ControllerBase
{
    private readonly IWhatsAppFlowOrchestrationService _orchestrationService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<WhatsAppFlowEndpointController> _logger;
    private readonly IConfiguration _configuration;

    public WhatsAppFlowEndpointController(
        IWhatsAppFlowOrchestrationService orchestrationService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<WhatsAppFlowEndpointController> logger,
        IConfiguration configuration)
    {
        _orchestrationService = orchestrationService;
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Flow Endpoint: handles data_exchange and complete actions from WhatsApp Flows.
    /// Meta sends encrypted requests here when a user interacts with a flow.
    /// 
    /// Request format (decrypted):
    /// {
    ///   "version": "3.0",
    ///   "action": "data_exchange" | "INIT" | "BACK",
    ///   "screen": "SCREEN_ID",
    ///   "data": { ... form data ... },
    ///   "flow_token": "user_session_token"
    /// }
    /// 
    /// Response format:
    /// {
    ///   "version": "3.0",
    ///   "screen": "NEXT_SCREEN_ID",
    ///   "data": { ... screen data ... }
    /// }
    /// </summary>
    [HttpPost("flow-endpoint")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleFlowRequest()
    {
        try
        {
            _logger.LogInformation("========== WhatsApp Flow Endpoint Request ==========");

            // Read the raw request body
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            _logger.LogInformation("Flow endpoint raw body length: {Length}", rawBody.Length);

            // In production, the body would be encrypted. For now, handle both encrypted and plain.
            string decryptedBody;
            var privateKey = _configuration["WhatsApp:FlowPrivateKey"];

            if (!string.IsNullOrEmpty(privateKey) && IsEncryptedPayload(rawBody))
            {
                decryptedBody = DecryptFlowPayload(rawBody, privateKey);
            }
            else
            {
                decryptedBody = rawBody;
            }

            var flowRequest = JsonSerializer.Deserialize<FlowEndpointRequest>(decryptedBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (flowRequest == null)
            {
                _logger.LogWarning("Failed to parse flow request");
                return BadRequest(new { error = "Invalid request" });
            }

            _logger.LogInformation("Flow request: Action={Action}, Screen={Screen}, FlowToken={Token}",
                flowRequest.Action, flowRequest.Screen, flowRequest.FlowToken);

            // Extract user ID from flow_token (format: "userId:flowId" or just pass the userId)
            var userId = ExtractUserIdFromToken(flowRequest.FlowToken);
            var flowId = ExtractFlowIdFromToken(flowRequest.FlowToken);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Could not extract user ID from flow token: {Token}", flowRequest.FlowToken);
                return Ok(new FlowEndpointResponse
                {
                    Version = "3.0",
                    Screen = "ERROR",
                    Data = new Dictionary<string, object> { ["error"] = "Session expired. Please start again." }
                });
            }

            // Handle the different actions
            FlowResponse result;

            switch (flowRequest.Action?.ToUpper())
            {
                case "INIT":
                case "PING":
                    // Health check / initialization
                    result = new FlowResponse
                    {
                        Screen = flowRequest.Screen ?? "SCREEN_0",
                        Data = new Dictionary<string, object> { ["status"] = "ok" }
                    };
                    break;

                case "DATA_EXCHANGE":
                    // User submitted a screen — process and return next screen
                    var screenId = flowRequest.Data?.GetValueOrDefault("screen")?.ToString() ?? flowRequest.Screen ?? "";
                    result = await _orchestrationService.HandleFlowDataExchangeAsync(
                        flowId ?? "",
                        screenId,
                        flowRequest.Data ?? new Dictionary<string, object>(),
                        userId);
                    break;

                case "COMPLETE":
                    // Flow finished — process completion
                    await _orchestrationService.HandleFlowCompletionAsync(
                        flowId ?? "",
                        flowRequest.Data ?? new Dictionary<string, object>(),
                        userId);

                    // Return acknowledgement
                    result = new FlowResponse
                    {
                        Screen = "SUCCESS",
                        Data = new Dictionary<string, object> { ["completed"] = true }
                    };
                    break;

                case "BACK":
                    // User pressed back — return previous screen data
                    // For simplicity, re-initialize the current screen
                    result = await _orchestrationService.HandleFlowDataExchangeAsync(
                        flowId ?? "",
                        flowRequest.Screen ?? "",
                        flowRequest.Data ?? new Dictionary<string, object>(),
                        userId);
                    break;

                default:
                    _logger.LogWarning("Unknown flow action: {Action}", flowRequest.Action);
                    result = new FlowResponse
                    {
                        Screen = flowRequest.Screen ?? "ERROR",
                        Data = new Dictionary<string, object> { ["error"] = "Unknown action" }
                    };
                    break;
            }

            var response = new FlowEndpointResponse
            {
                Version = "3.0",
                Screen = result.Screen ?? "ERROR",
                Data = result.Data ?? new Dictionary<string, object>()
            };

            _logger.LogInformation("Flow response: Screen={Screen}, DataKeys={Keys}",
                response.Screen, string.Join(", ", response.Data.Keys));

            // In production, encrypt the response
            if (!string.IsNullOrEmpty(privateKey) && IsEncryptedPayload(rawBody))
            {
                var encryptedResponse = EncryptFlowResponse(JsonSerializer.Serialize(response), privateKey);
                return Content(encryptedResponse, "application/json");
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing flow endpoint request");
            return Ok(new FlowEndpointResponse
            {
                Version = "3.0",
                Screen = "ERROR",
                Data = new Dictionary<string, object>
                {
                    ["error"] = "An error occurred. Please try again."
                }
            });
        }
    }

    /// <summary>
    /// Health check endpoint for the flow endpoint.
    /// Meta may call this to verify the endpoint is reachable.
    /// </summary>
    [HttpGet("flow-endpoint")]
    [AllowAnonymous]
    public IActionResult FlowEndpointHealthCheck()
    {
        return Ok(new { status = "ok", endpoint = "whatsapp-flow-endpoint", timestamp = DateTime.UtcNow });
    }

    // ==================== HELPER METHODS ====================

    private string ExtractUserIdFromToken(string? flowToken)
    {
        if (string.IsNullOrEmpty(flowToken)) return "";

        // Token format: "userId:flowId" or just "userId"
        var parts = flowToken.Split(':');
        return parts[0];
    }

    private string? ExtractFlowIdFromToken(string? flowToken)
    {
        if (string.IsNullOrEmpty(flowToken)) return null;

        var parts = flowToken.Split(':');
        return parts.Length > 1 ? parts[1] : null;
    }

    private bool IsEncryptedPayload(string body)
    {
        // Encrypted payloads from Meta contain encrypted_aes_key and encrypted_flow_data
        return body.Contains("encrypted_aes_key") || body.Contains("encrypted_flow_data");
    }

    private string DecryptFlowPayload(string encryptedBody, string privateKey)
    {
        // TODO: Implement full Meta encryption/decryption
        // See: https://developers.facebook.com/docs/whatsapp/flows/guides/implementingyourflowendpoint
        _logger.LogWarning("Flow encryption not fully implemented. Processing as plain text.");
        return encryptedBody;
    }

    private string EncryptFlowResponse(string responseBody, string privateKey)
    {
        // TODO: Implement full Meta encryption
        _logger.LogWarning("Flow encryption not fully implemented. Returning plain text.");
        return responseBody;
    }
}

// ==================== REQUEST/RESPONSE MODELS ====================

/// <summary>
/// Request received from Meta WhatsApp Flows data_exchange endpoint.
/// </summary>
public class FlowEndpointRequest
{
    public string? Version { get; set; }
    public string? Action { get; set; }
    public string? Screen { get; set; }
    public Dictionary<string, object>? Data { get; set; }
    public string? FlowToken { get; set; }
}

/// <summary>
/// Response sent back to Meta WhatsApp Flows.
/// </summary>
public class FlowEndpointResponse
{
    public string Version { get; set; } = "3.0";
    public string Screen { get; set; } = "";
    public Dictionary<string, object> Data { get; set; } = new();
}
