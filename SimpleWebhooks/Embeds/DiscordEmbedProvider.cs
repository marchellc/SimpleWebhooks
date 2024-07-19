using System.Text.Json.Serialization;

namespace SimpleWebhooks.Embeds
{
    public struct DiscordEmbedProvider
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        public static DiscordEmbedProvider Create(string name, string url)
        {
            var result = new DiscordEmbedProvider();

            result.Name = name;
            result.Url = url;

            return result;
        }
    }
}