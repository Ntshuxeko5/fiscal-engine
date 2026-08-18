using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Fiscal.Infrastructure.FiscalEdge
{
    /// <summary>
    /// Real IFiscalClient implementation connecting to Fiscal Edge middleware,
    /// which forwards transactions to MRA EIS and returns the fiscalized
    /// invoice information.
    ///
    /// Endpoint: POST /mw/Invoice
    /// Auth: X-API-Key and X-TerminalId headers (not Bearer/Basic)
    ///
    /// The payload passed in should already match Fiscal Edge's documented
    /// request shape (TerminalId, CustomerTin, InvoiceNumber, invoiceItems[]
    /// etc.) via the JSON config's field mappings - this class only handles
    /// HTTP transport and response mapping.
    /// </summary>
    public class FiscalEdgeClient : IFiscalClient
    {
        private readonly HttpClient _httpClient;
        private readonly FiscalEdgeConfig _config;

        public FiscalEdgeClient(HttpClient httpClient, FiscalEdgeConfig config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<DynamicRecord> FiscalizeAsync(
            DynamicRecord payload,
            FiscalContext context)
        {
            string json = JsonSerializer.Serialize(
                payload,
                new JsonSerializerOptions
                {
                    Converters = { new DynamicRecordJsonConverter() }
                });

            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"{_config.BaseUrl}/mw/Invoice");

            request.Headers.Add("X-API-Key", _config.ApiKey);
            request.Headers.Add("X-TerminalId", _config.TerminalId);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (TaskCanceledException)
            {
                return Failure("Request to Fiscal Edge timed out.");
            }
            catch (HttpRequestException ex)
            {
                return Failure($"Network error contacting Fiscal Edge: {ex.Message}");
            }

            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return Failure(
                    "Fiscal Edge rejected the API key (401). Verify " +
                    "X-API-Key and X-TerminalId are correct.");
            }

            Console.WriteLine("[DEBUG] Outgoing Fiscal Edge payload:");
            Console.WriteLine(json);
            Console.WriteLine();

            return response.IsSuccessStatusCode
                ? ParseSuccessResponse(responseBody)
                : ParseErrorResponse(response, responseBody);
        }

        private static DynamicRecord Failure(string message)
        {
            var result = new DynamicRecord();
            result.Set("Success", false);
            result.Set("ErrorMessage", message);
            return result;
        }

        /// <summary>
        /// Maps the Fiscal Edge success shape:
        /// { success:true, message, data: { clientInvoiceNumber,
        ///   fiscalInvoiceNumber, receiptCounter, terminalDate, terminalId,
        ///   verificationUrl, signature } }
        /// </summary>
        private static DynamicRecord ParseSuccessResponse(string responseBody)
        {
            using JsonDocument doc = JsonDocument.Parse(responseBody);
            JsonElement root = doc.RootElement;

            JsonElement dataElement = root.TryGetProperty("data", out var nested)
                ? nested
                : root;

            var result = new DynamicRecord();
            result.Set("Success", true);
            result.Set("FiscalReceiptNumber",
                GetStringOrNull(dataElement, "fiscalInvoiceNumber"));
            result.Set("ClientInvoiceNumber",
                GetStringOrNull(dataElement, "clientInvoiceNumber"));
            result.Set("FiscalTimestamp",
                GetStringOrNull(dataElement, "terminalDate")
                    ?? DateTime.UtcNow.ToString("o"));
            result.Set("VerificationUrl",
                GetStringOrNull(dataElement, "verificationUrl"));
            result.Set("Signature",
                GetStringOrNull(dataElement, "signature"));

            if (dataElement.TryGetProperty("receiptCounter", out var counterEl) &&
                counterEl.ValueKind == JsonValueKind.Number)
            {
                result.Set("ReceiptCounter", counterEl.GetInt32());
            }

            return result;
        }

        /// <summary>
        /// Maps the Fiscal Edge error shape:
        /// { success:false, httpCode, message, errorCode,
        ///   errors: { field: [messages] } }
        /// </summary>
        private static DynamicRecord ParseErrorResponse(
            HttpResponseMessage response,
            string responseBody)
        {
            string errorMessage = ExtractErrorMessage(responseBody)
                ?? $"Fiscal Edge returned HTTP {(int)response.StatusCode} " +
                   $"with no parseable error message.";

            var result = new DynamicRecord();
            result.Set("Success", false);
            result.Set("ErrorMessage", errorMessage);
            result.Set("HttpStatusCode", (int)response.StatusCode);
            result.Set("RawResponse", responseBody);

            return result;
        }

        private static string? ExtractErrorMessage(string responseBody)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(responseBody);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("message", out var messageElement))
                {
                    return null;
                }

                string message = messageElement.GetString() ?? "Request failed.";

                if (root.TryGetProperty("errors", out var errorsObj) &&
                    errorsObj.ValueKind == JsonValueKind.Object)
                {
                    var details = new List<string>();
                    foreach (JsonProperty prop in errorsObj.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement msg in prop.Value.EnumerateArray())
                            {
                                details.Add($"{prop.Name}: {msg.GetString()}");
                            }
                        }
                    }

                    if (details.Count > 0)
                    {
                        return $"{message} - {string.Join("; ", details)}";
                    }
                }

                return message;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? GetStringOrNull(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) &&
                   prop.ValueKind == JsonValueKind.String
                ? prop.GetString()
                : null;
        }
    }
}
