using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DefenseCase.Web.Services;

public class AzureResponsesClient(HttpClient http, IConfiguration configuration)
{
    public async Task<T> GenerateAsync<T>(string instructions, JsonArray content, JsonObject schema,
        bool legalDraft, CancellationToken cancellationToken)
    {
        string endpoint = configuration["AZURE_OPENAI_ENDPOINT"] ?? "";
        string key = configuration["AZURE_OPENAI_API_KEY"] ?? "";
        string deployment = configuration["AZURE_OPENAI_DEPLOYMENT"] ?? "";
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var address) || address.Scheme != "https" || key.Length == 0 || deployment.Length == 0)
            throw new InvalidOperationException("Azure AI is not configured. The endpoint, key and deployment settings are required.");

        var requestBody = new JsonObject
        {
            ["model"] = deployment, ["instructions"] = instructions, ["store"] = false,
            ["max_output_tokens"] = legalDraft ? 64000 : 32000,
            ["input"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = content }),
            ["text"] = new JsonObject { ["format"] = new JsonObject
            {
                ["type"] = "json_schema", ["name"] = legalDraft ? "legal_argument" : "discovery_analysis",
                ["strict"] = true, ["schema"] = schema
            }}
        };
        // Reasoning models do not accept temperature. This setting is explicit per deployment.
        if (configuration.GetValue("AZURE_OPENAI_REASONING", true))
            requestBody["reasoning"] = new JsonObject { ["effort"] = legalDraft ? "high" : "medium" };
        else
            requestBody["temperature"] = 0;

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.TrimEnd('/') + "/openai/v1/responses");
        request.Headers.Add("api-key", key);
        request.Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Azure AI returned HTTP {(int)response.StatusCode}. Please retry after checking the deployment, quota and document size.");
        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        return Parse<T>(body);
    }

    public static T Parse<T>(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            if (!json.RootElement.TryGetProperty("status", out var status) || status.GetString() != "completed")
            {
                string reason = json.RootElement.TryGetProperty("incomplete_details", out var details) &&
                    details.ValueKind == JsonValueKind.Object && details.TryGetProperty("reason", out var value)
                    ? value.GetString() ?? "" : "";
                throw new InvalidOperationException(reason == "max_output_tokens"
                    ? "Azure AI reached its response limit before completing the draft. No motion was generated; please retry."
                    : "Azure AI did not complete the response. No motion was generated; please retry with a smaller packet.");
            }
            var text = new StringBuilder();
            foreach (var item in json.RootElement.GetProperty("output").EnumerateArray())
            {
                if (item.GetProperty("type").GetString() != "message") continue;
                foreach (var part in item.GetProperty("content").EnumerateArray())
                {
                    if (part.GetProperty("type").GetString() == "refusal")
                        throw new InvalidOperationException("Azure AI declined this request. No motion was generated.");
                    if (part.GetProperty("type").GetString() == "output_text") text.Append(part.GetProperty("text").GetString());
                }
            }
            return JsonSerializer.Deserialize<T>(text.ToString()) ?? throw new JsonException();
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException)
        {
            throw new InvalidOperationException("Azure AI returned an incomplete or invalid structured response. No motion was generated; please retry.", error);
        }
    }

    public static JsonObject InputText(string text) => new() { ["type"] = "input_text", ["text"] = text };
}
