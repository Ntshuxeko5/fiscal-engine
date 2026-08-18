using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Fiscal.Infrastructure.MraEis
{
    /// <summary>
    /// Real IFiscalClient implementation connecting directly to the MRA EIS API.
    ///
    /// Endpoint: POST /api/v1/sales/submit-sales-transaction
    ///
    /// Uses a pre-obtained Authorization Token (activation was completed
    /// separately by the team, not by this app). If a call comes back 401,
    /// this class does NOT attempt to re-activate - there is no TAC available
    /// to do so, and re-activation must be a deliberate, tracked action.
    /// It returns a clear failure instead, so a human can obtain a fresh token.
    /// </summary>
    public class MraEisClient : IFiscalClient
    {
        private readonly HttpClient _httpClient;
        private readonly MraEisConfig _config;

        public MraEisClient(HttpClient httpClient, MraEisConfig config)
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
                HttpMethod.Post,
                $"{_config.BaseUrl}/api/v1/sales/submit-sales-transaction");

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", _config.Token);

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (TaskCanceledException)
            {
                return Failure("Request to MRA EIS timed out.");
            }
            catch (HttpRequestException ex)
            {
                return Failure($"Network error contacting MRA EIS: {ex.Message}");
            }

            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return Failure(
                    "MRA EIS rejected the token as unauthorized (401). " +
                    "The token may have expired. This app cannot re-activate " +
                    "automatically - a fresh token must be obtained by the " +
                    "team and updated in the local credentials file.");
            }

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

            return result;
        }

        private static DynamicRecord ParseErrorResponse(
            HttpResponseMessage response,
            string responseBody)
        {
            string errorMessage = ExtractErrorMessage(responseBody)
                ?? $"Fiscal device returned HTTP {(int)response.StatusCode} " +
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

                if (root.TryGetProperty("remark", out var remarkElement))
                {
                    string remark = remarkElement.GetString() ?? "Validation failed.";

                    if (root.TryGetProperty("errors", out var errorsArray) &&
                        errorsArray.ValueKind == JsonValueKind.Array)
                    {
                        var details = new List<string>();
                        foreach (JsonElement err in errorsArray.EnumerateArray())
                        {
                            string field = GetStringOrNull(err, "fieldName") ?? "";
                            string msg = GetStringOrNull(err, "errorMessage") ?? "";
                            details.Add($"{field}: {msg}");
                        }

                        if (details.Count > 0)
                        {
                            return $"{remark} - {string.Join("; ", details)}";
                        }
                    }

                    return remark;
                }

                if (root.TryGetProperty("message", out var messageElement))
                {
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

                return null;
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
