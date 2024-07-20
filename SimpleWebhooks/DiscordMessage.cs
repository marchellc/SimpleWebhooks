using System.Collections.Generic;
using System.Linq;

using System.Text.Json;
using System.Text.Json.Serialization;

using SimpleWebhooks.Embeds;
using SimpleWebhooks.Mentions;

using System.Net.Http;
using System.Net.Http.Headers;

using System;
using System.Threading.Tasks;
using System.Text.Json.Nodes;

namespace SimpleWebhooks
{
    public struct DiscordMessage
    {
        private static readonly MediaTypeHeaderValue _jsonHeader = MediaTypeHeaderValue.Parse("application/json");
        private static readonly HttpMethod _patchMethod = new HttpMethod("PATCH");

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

            json.Headers.ContentType = _jsonHeader;

            multipart.Add(json, "payload_json");
            return multipart;
        }

        public async Task<HttpResponseMessage> PostToWebhookAsync(string webhookToken, ulong webhookId, HttpClient client = null)
            => await PostToWebhookAsync(GetPostUrl(webhookToken, webhookId), client);

        public async Task<HttpResponseMessage> PostToWebhookAndWaitAsync(string webhookToken, ulong webhookId, HttpClient client = null)
            => await PostToWebhookAndWaitAsync(GetPostUrl(webhookToken, webhookId), client);

        public async Task<ulong> PostToWebhookAndGetMessageIdAsync(string webhookUrl, HttpClient client = null)
            => await ExtractMessageIdAsync(await PostToWebhookAndWaitAsync(webhookUrl, client));

        public async Task<ulong> PostToWebhookAndGetMessageIdAsync(string webhookToken, ulong webhookId, HttpClient client = null)
            => await ExtractMessageIdAsync(await PostToWebhookAndWaitAsync(GetPostUrl(webhookToken, webhookId), client));

        public async Task<HttpResponseMessage> EditWebhookAsync(string webhookToken, ulong webhookId, ulong messageId, HttpClient client = null)
            => await EditWebhookAsync(GetEditUrl(webhookToken, webhookId, messageId), client);

        public async Task<HttpResponseMessage> PostToWebhookAsync(string webhookUrl, HttpClient client = null)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
                throw new ArgumentNullException(nameof(webhookUrl));

            if (client is null)
                client = new HttpClient();

            return await client.PostAsync(webhookUrl, ToWebhookHttpContent());
        }

        public async Task<HttpResponseMessage> PostToWebhookAndWaitAsync(string webhookUrl, HttpClient client = null)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
                throw new ArgumentNullException(nameof(webhookUrl));

            if (client is null)
                client = new HttpClient();

            return await client.PostAsync($"{webhookUrl}?wait=true", ToWebhookHttpContent());
        }

        public async Task<HttpResponseMessage> EditWebhookAsync(string webhookUrl, HttpClient client = null)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
                throw new ArgumentNullException(nameof(webhookUrl));

            if (client is null)
                client = new HttpClient();

            using (var request = new HttpRequestMessage(_patchMethod, webhookUrl))
            {
                request.Content = ToWebhookHttpContent();
                return await client.SendAsync(request);
            }
        }

        public static string GetEditUrl(string webhookToken, ulong webhookId, ulong messageId)
            => $"https://discord.com/api/webhooks/{webhookId}/{webhookToken}/messages/{messageId}";

        public static string GetPostUrl(string webhookToken, ulong webhookId)
            => $"https://discord.com/api/webhooks/{webhookId}/{webhookToken}";

        public static async Task<ulong> ExtractMessageIdAsync(HttpResponseMessage responseMessage)
        {
            if (responseMessage is null)
                throw new ArgumentNullException(nameof(responseMessage));

            responseMessage.EnsureSuccessStatusCode();

            var data = await responseMessage.Content.ReadAsStringAsync();
            var message = JsonSerializer.Deserialize<JsonObject>(data);

            foreach (var pair in message)
            {
                if (pair.Key == "id")
                    return ulong.Parse(pair.Value.Deserialize<string>());
            }

            throw new Exception($"Failed to find message ID node!");
        }

        public static DiscordMessage FromJson(string json)
            => JsonSerializer.Deserialize<DiscordMessage>(json);
    }
}