using System.Text.Json.Serialization;

namespace SimpleWebhooks.Embeds
{
    public struct DiscordEmbedImage
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("proxy_url")]
        public string ProxyUrl { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        public DiscordEmbedImage WithUrl(string url, string proxyUrl = null)
        {
            Url = url;
            ProxyUrl = proxyUrl;

            return this;
        }

        public DiscordEmbedImage WithResolution(int width, int height)
        {
            Height = height;
            Width = width;

            return this;
        }

        public static DiscordEmbedImage Create(string url, string proxy = null, int? height = null, int? width = null)
        {
            var result = new DiscordEmbedImage();

            result.Url = url;
            result.ProxyUrl = proxy;
            result.Height = height;
            result.Width = width;

            return result;
        }
    }

}
