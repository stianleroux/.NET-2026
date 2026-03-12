
using CloudBurger.Infrastructure.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CloudBurger.Api.Features.QrCode;
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
    /// Demonstrates: Binary file responses, Result pattern, pattern matching, structured validation
    /// </summary>
    private static Results<FileContentHttpResult, ValidationProblem> GenerateQrCodeAsync(string? url, QrCodeService qrCodeService, int pixelsPerModule = 20)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["url"] = ["URL is required"]
            });
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["url"] = ["Invalid URL format"]
            });
        }

        // Use Result pattern from service
        var qrCodeResult = QrCodeService.GenerateQrCode(url, pixelsPerModule);

        // Handle result using pattern matching - map ValidationErrors if present
        return qrCodeResult switch
        {
            { IsSuccess: true } => TypedResults.File(qrCodeResult.Value, contentType: "image/png", fileDownloadName: "qrcode.png"),
            { ValidationErrors: not null } => TypedResults.ValidationProblem(qrCodeResult.ValidationErrors),
            _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["qrCode"] = [qrCodeResult.Error]
            })
        };
    }

    /// <summary>
    /// Generate QR code as Base64 string for HTML embedding.
    /// Demonstrates: Result pattern, functional composition, structured validation
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
                ["url"] = ["URL is required"]
            });
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["url"] = ["Invalid URL format"]
            });
        }

        // Use Result pattern from service
        var base64Result = QrCodeService.GenerateQrCodeBase64(url, pixelsPerModule);

        // Handle result using pattern matching - map ValidationErrors if present
        return base64Result switch
        {
            { IsSuccess: true } => TypedResults.Ok(new QrCodeResponse
            {
                Url = url,
                Base64Image = base64Result.Value,
                ImageDataUrl = $"data:image/png;base64,{base64Result.Value}"
            }),
            { ValidationErrors: not null } => TypedResults.ValidationProblem(base64Result.ValidationErrors),
            _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["qrCode"] = [base64Result.Error]
            })
        };
    }
}
