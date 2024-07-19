using System.Collections.Generic;
using System.Linq;

using System.Text.Json.Serialization;
using System.Text.Json;

using SimpleWebhooks.Embeds;
using SimpleWebhooks.Mentions;

using System.Net.Http;
using System.Net.Http.Headers;

using System;
using System.Threading.Tasks;

namespace SimpleWebhooks
{
    public struct DiscordMessage
    {
        private static readonly MediaTypeHeaderValue _jsonHeader = MediaTypeHeaderValue.Parse("application/json");

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("tts")]
        public bool IsTextToSpeech { get; set; }

        [JsonPropertyName("embeds")]
        public DiscordEmbed[] Embeds { get; set; }

        [JsonPropertyName("allowed_mentions")]
        public DiscordMessageAllowedMentions? Mentions { get; set; }

        public DiscordMessage WithContent(string content, bool isTts = false)
        {
            Content = content;
            IsTextToSpeech = isTts;
            return this;
        }

        public DiscordMessage WithTextToSpeech(bool isTts = true)
        {
            IsTextToSpeech = isTts;
            return this;
        }

        public DiscordMessage WithEmbeds(params DiscordEmbed[] embeds)
        {
            if (Embeds != null && Embeds.Any())
            {
                var list = new List<DiscordEmbed>(Embeds);

                list.AddRange(embeds);

                Embeds = list.ToArray();
                return this;
            }

            Embeds = embeds;
            return this;
        }

        public DiscordMessage WithMentions(DiscordMessageAllowedMentions? discordMessageAllowedMentions)
        {
            Mentions = discordMessageAllowedMentions;
            return this;
        }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(Content) && Content.Length >= 1900)
                Content = Content.Substring(0, 1900) + " ...";

            if (Embeds != null && Embeds.Any())
            {
                var embeds = Embeds;

                for (int i = 0; i < embeds.Length; i++)
                {
                    var discordEmbed = embeds[i];

                    if (!string.IsNullOrWhiteSpace(discordEmbed.Description) && discordEmbed.Description.Length >= 1900)
                    {
                        var description = discordEmbed.Description;

                        description = description.Substring(0, 1900) + " ...";
                        discordEmbed.WithDescription(description);
                    }
                }
            }

            return JsonSerializer.Serialize(this);
        }

        public HttpContent ToWebhookHttpContent()
        {
            var boundary = "------------------------" + DateTime.Now.Ticks.ToString("x");
            var multipart = new MultipartFormDataContent(boundary);
            var json = new StringContent(ToString());

            multipart.Add(json, "payload_json");

            return multipart;
        }

        public async Task<HttpResponseMessage> SendToWebhookAsync(string webhookUrl, HttpClient client = null)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
                throw new ArgumentNullException(nameof(webhookUrl));

            if (client is null)
                client = new HttpClient();

            return await client.PostAsync(webhookUrl, ToWebhookHttpContent());
        }

        public static DiscordMessage FromJson(string json)
            => JsonSerializer.Deserialize<DiscordMessage>(json);
    }
}