namespace CloudPizza.Infrastructure.Services;

using CloudPizza.Shared.Common;
using QRCoder;

/// <summary>
/// Service for generating QR codes.
/// Used to create scannable codes for the public Cloudflare tunnel URL.
/// Demonstrates: Result pattern, pattern matching, functional error handling
/// </summary>
public sealed class QrCodeService
{
    /// <summary>
    /// Generate a QR code as PNG bytes for the given URL.
    /// Uses Result pattern with ValidationFailure for structured error handling.
    /// </summary>
    public static Result<byte[]> GenerateQrCode(string url, int pixelsPerModule = 20)
    {
        // Validate URL using pattern matching
        var urlValidation = url switch
        {
            null or "" or { Length: 0 } => Result<string>.ValidationFailure(
                "URL validation failed",
                new Dictionary<string, string[]> { ["url"] = ["URL is required"] }),
            { } when string.IsNullOrWhiteSpace(url) => Result<string>.ValidationFailure(
                "URL validation failed",
                new Dictionary<string, string[]> { ["url"] = ["URL cannot be empty or whitespace"] }),
            _ => Result<string>.Success(url)
        };

        if (urlValidation.IsFailure)
        {
            return Result<byte[]>.ValidationFailure(urlValidation.Error, urlValidation.ValidationErrors!);
        }

        // Validate pixels per module using pattern matching
        var pixelsValidation = pixelsPerModule switch
        {
            < 1 => Result<int>.ValidationFailure(
                "Pixels per module validation failed",
                new Dictionary<string, string[]> { ["pixelsPerModule"] = ["Pixels per module must be at least 1"] }),
            > 50 => Result<int>.ValidationFailure(
                "Pixels per module validation failed",
                new Dictionary<string, string[]> { ["pixelsPerModule"] = ["Pixels per module cannot exceed 50"] }),
            _ => Result<int>.Success(pixelsPerModule)
        };

        if (pixelsValidation.IsFailure)
        {
            return Result<byte[]>.ValidationFailure(pixelsValidation.Error, pixelsValidation.ValidationErrors!);
        }

        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);

            var bytes = qrCode.GetGraphic(pixelsPerModule);
            return Result<byte[]>.Success(bytes);
        }
        catch (Exception ex)
        {
            // Infrastructure failures use regular Failure, not ValidationFailure
            return Result<byte[]>.Failure($"Failed to generate QR code: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate a QR code as Base64 string for HTML img src.
    /// Uses Result pattern for consistent error handling.
    /// </summary>
    public static Result<string> GenerateQrCodeBase64(string url, int pixelsPerModule = 20)
    {
        var bytesResult = GenerateQrCode(url, pixelsPerModule);

        return bytesResult.IsSuccess
            ? Result<string>.Success(Convert.ToBase64String(bytesResult.Value))
            : Result<string>.Failure(bytesResult.Error);
    }
}
