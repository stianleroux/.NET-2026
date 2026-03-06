// QR Code generation service using QRCoder library
// Demonstrates: Service pattern, single responsibility
using QRCoder;

namespace CloudPizza.Infrastructure.Services;

/// <summary>
/// Service for generating QR codes.
/// Used to create scannable codes for the public Cloudflare tunnel URL.
/// </summary>
public sealed class QrCodeService
{
    /// <summary>
    /// Generate a QR code as PNG bytes for the given URL.
    /// </summary>
    public byte[] GenerateQrCode(string url, int pixelsPerModule = 20)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));

        if (pixelsPerModule < 1 || pixelsPerModule > 50)
            throw new ArgumentException("Pixels per module must be between 1 and 50", nameof(pixelsPerModule));

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        
        return qrCode.GetGraphic(pixelsPerModule);
    }

    /// <summary>
    /// Generate a QR code as Base64 string for HTML img src.
    /// </summary>
    public string GenerateQrCodeBase64(string url, int pixelsPerModule = 20)
    {
        var bytes = GenerateQrCode(url, pixelsPerModule);
        return Convert.ToBase64String(bytes);
    }
}
