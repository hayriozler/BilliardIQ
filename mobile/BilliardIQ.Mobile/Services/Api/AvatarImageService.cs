using System.Collections.Concurrent;

namespace BilliardIQ.Mobile.Services.Api;

public sealed class AvatarImageService(ApiClient api)
{
    private readonly ConcurrentDictionary<int, byte[]> _avatars = new();

    public async Task<ImageSource?> GetAsync(int? avatarId, string? photoUrl)
    {
        if (!string.IsNullOrWhiteSpace(photoUrl))
        {
            return ImageSource.FromUri(new Uri(new Uri(ApiSettings.BaseUrl), photoUrl));
        }

        if (avatarId is not { } id)
        {
            return null;
        }

        try
        {
            if (!_avatars.TryGetValue(id, out var png))
            {
                var svg = await api.GetBytesAsync($"api/mobile/avatars/{id}");
                using var stream = new MemoryStream(svg);
                png = AvatarRenderer.RenderPng(stream, 128);
                if (png.Length == 0)
                {
                    return null;
                }

                _avatars[id] = png;
            }

            return ImageSource.FromStream(() => new MemoryStream(png));
        }
        catch (Exception ex) when (ApiErrorText.IsExpected(ex))
        {
            return null;
        }
    }
}
