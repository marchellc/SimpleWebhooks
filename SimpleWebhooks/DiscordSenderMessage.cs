namespace SimpleWebhooks
{
    public struct DiscordSenderMessage
    {
        public readonly DiscordMessage Message;

        public readonly bool IsEdit;

        public readonly string Token;

        public readonly ulong WebhookId;
        public readonly ulong MessageId;

        public readonly object Value;

        public DiscordSenderMessage(DiscordMessage message, bool isEdit, string token, ulong webhookId, ulong messageId, object value)
        {
            Message = message;
            IsEdit = isEdit;
            Token = token;
            Value = value;
            WebhookId = webhookId;
            MessageId = messageId;
        }
    }
}
