namespace CloudBurger.Web.Services;

using System.Text.Json;
using CloudBurger.Shared.Contracts;

/// <summary>
/// Client for communicating with CloudBurger API.
/// Uses primary constructor and HttpClient DI.
/// </summary>
public sealed class ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
{
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Create a new burger order.
    /// </summary>
    public async Task<CreateOrderResponse?> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/orders", request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CreateOrderResponse>(jsonOptions, cancellationToken);
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Failed to create order. Status: {Status}, Error: {Error}", response.StatusCode, error);
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Create order request canceled by caller.");
            return null;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error while creating order.");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Create order request timed out.");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected exception while creating order");
            return null;
        }
    }

    /// <summary>
    /// Get recent orders.
    /// </summary>
    public async Task<List<OrderDto>?> GetOrdersAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await httpClient.GetFromJsonAsync<List<OrderDto>>(
                $"/api/orders?limit={limit}",
                jsonOptions,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Get orders request canceled by caller.");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception getting orders");
            return null;
        }
    }

    /// <summary>
    /// Get QR code as Base64.
    /// </summary>
    public async Task<string?> GetQrCodeAsync(string url, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var response = await httpClient.GetFromJsonAsync<QrCodeResponse>(
                $"/api/qr/base64?url={Uri.EscapeDataString(url)}",
                jsonOptions,
                cancellationToken);

            return response?.ImageDataUrl;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Get QR code request canceled by caller.");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception getting QR code");
            return null;
        }
    }

    /// <summary>
    /// Stream order updates via SSE.
    /// Uses async stream for real-time updates with pattern matching.
    /// </summary>
    public async IAsyncEnumerable<OrderDto> StreamOrdersAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/orders/stream");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? eventType = null;
        string? data = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            // Pattern matching for SSE line parsing
            switch (line)
            {
                case var l when l.StartsWith("event:"):
                    eventType = l[6..].Trim();
                    break;

                case var l when l.StartsWith("data:"):
                    data = l[5..].Trim();
                    break;

                case "" or null when data is not null:
                    // End of message - empty line with data
                    if (eventType == "order-created")
                    {
                        var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(data, jsonOptions);
                        if (evt?.Order is not null)
                        {
                            yield return evt.Order;
                        }
                    }

                    eventType = null;
                    data = null;
                    break;
            }
        }
    }
}

// QR Code response from API
public sealed record QrCodeResponse
{
    public required string Url { get; init; }
    public required string Base64Image { get; init; }
    public required string ImageDataUrl { get; init; }
}
