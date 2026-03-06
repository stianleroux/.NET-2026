// QR Code endpoints for generating scannable codes
// Demonstrates: Minimal APIs, service injection, binary responses
using CloudPizza.Infrastructure.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CloudPizza.Api.Features.QrCode;

/// <summary>
/// Extension methods to register QR code endpoints.
/// </summary>
public static class QrCodeEndpoints
{
    public static RouteGroupBuilder MapQrCodeEndpoints(this RouteGroupBuilder group)
    {
        var qr = group.MapGroup("/qr")
            .WithTags("QR Code");

        // GET /api/qr?url=... - Generate QR code
        qr.MapGet("/", GenerateQrCodeAsync)
            .WithName("GenerateQrCode")
            .WithSummary("Generate a QR code")
            .WithDescription("Generates a QR code image for the provided URL. Returns PNG image.")
            .Produces(StatusCodes.Status200OK, contentType: "image/png")
            .ProducesValidationProblem();

        // GET /api/qr/base64?url=... - Generate QR code as Base64
        qr.MapGet("/base64", GenerateQrCodeBase64Async)
            .WithName("GenerateQrCodeBase64")
            .WithSummary("Generate a QR code as Base64")
            .WithDescription("Generates a QR code as Base64 string for the provided URL.")
            .Produces<QrCodeResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        return group;
    }

    /// <summary>
    /// Generate QR code as PNG image.
    /// Demonstrates: Binary file responses, validation.
    /// </summary>
    private static Results<FileContentHttpResult, ValidationProblem> GenerateQrCodeAsync(
        string? url,
        QrCodeService qrCodeService,
        int pixelsPerModule = 20)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["url"] = new[] { "URL is required" }
            });
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["url"] = new[] { "Invalid URL format" }
            });
        }

        if (pixelsPerModule < 1 || pixelsPerModule > 50)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["pixelsPerModule"] = new[] { "Pixels per module must be between 1 and 50" }
            });
        }

        var qrCodeBytes = qrCodeService.GenerateQrCode(url, pixelsPerModule);
        return TypedResults.File(qrCodeBytes, contentType: "image/png", fileDownloadName: "qrcode.png");
    }

    /// <summary>
    /// Generate QR code as Base64 string for HTML embedding.
    /// </summary>
    private static Results<Ok<QrCodeResponse>, ValidationProblem> GenerateQrCodeBase64Async(
        string? url,
        QrCodeService qrCodeService,
        int pixelsPerModule = 20)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["url"] = new[] { "URL is required" }
            });
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["url"] = new[] { "Invalid URL format" }
            });
        }

        var base64 = qrCodeService.GenerateQrCodeBase64(url, pixelsPerModule);
        return TypedResults.Ok(new QrCodeResponse
        {
            Url = url,
            Base64Image = base64,
            ImageDataUrl = $"data:image/png;base64,{base64}"
        });
    }
}

/// <summary>
/// Response DTO for Base64 QR code.
/// </summary>
public sealed record QrCodeResponse
{
    public required string Url { get; init; }
    public required string Base64Image { get; init; }
    public required string ImageDataUrl { get; init; }
}
