namespace CloudBurger.Api.Features.QrCode;

/// <summary>
/// Response DTO for Base64 QR code.
/// </summary>
public sealed record QrCodeResponse
{
    public required string Url { get; init; }
    public required string Base64Image { get; init; }
    public required string ImageDataUrl { get; init; }
}
