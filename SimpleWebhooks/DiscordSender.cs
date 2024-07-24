using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace SimpleWebhooks
{
    public class DiscordSender
    {
        private volatile ConcurrentQueue<DiscordSenderMessage> _queue = new ConcurrentQueue<DiscordSenderMessage>();

        public HttpClient Client { get; set; }

        public int Size => _queue.Count;

        public DiscordSender() { Client = new HttpClient(); }
        public DiscordSender(HttpClient client) { Client = client; }

        public void EnqueuePost(string webhookToken, ulong webhookId, DiscordMessage message, object customValue = null)
        {
            if (string.IsNullOrWhiteSpace(webhookToken))
                throw new ArgumentNullException(webhookToken);

            _queue.Enqueue(new DiscordSenderMessage(message, false, webhookToken, webhookId, 0, customValue));
        }

        public void EnqueueEdit(string webhookToken, ulong webhookId, ulong messageId, DiscordMessage message, object customValue)
        {
            if (string.IsNullOrWhiteSpace(webhookToken))
                throw new ArgumentNullException(webhookToken);

            _queue.Enqueue(new DiscordSenderMessage(message, true, webhookToken, webhookId, messageId, customValue));
        }

        public void UpdateQueue(int messageCount = -1, bool useTask = true, Action<DiscordSenderMessage, Exception, ulong> callback = null)
        {
            if (messageCount < 1 || messageCount == 1)
                InternalSendSingle(useTask, callback);
            else
                InternalSendLimitedCount(messageCount, useTask, callback);
        }

        public void ClearQueue(bool sendCurrent = false, bool useTask = true, Action<DiscordSenderMessage, Exception, ulong> callback = null)
        {
            if (sendCurrent && Size > 0)
                InternalSendLimitedCount(Size, useTask, callback);

            while (_queue.TryDequeue(out _))
                continue;
        }

        private void InternalSendLimitedCount(int count, bool useTask, Action<DiscordSenderMessage, Exception, ulong> callback)
        {
            var index = 0;

            while (index < count && _queue.TryDequeue(out var message))
            {
                InternalSend(message, useTask, callback);
                index++;
            }
        }

        private void InternalSendSingle(bool useTask, Action<DiscordSenderMessage, Exception, ulong> callback)
        {
            if (!_queue.TryDequeue(out var message))
                return;

            InternalSend(message, useTask, callback);
        }

        private void InternalSend(DiscordSenderMessage message, bool useTask, Action<DiscordSenderMessage, Exception, ulong> callback)
        {
            if (useTask)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        if (message.IsEdit)
                        {
                            await message.Message.PostToWebhookAsync(message.Token, message.WebhookId, Client);

                            callback?.Invoke(message, null, 0);
                            return;
                        }
                        else
                        {
                            var id = await message.Message.EditWebhookAsync(message.Token, message.WebhookId, message.MessageId, Client);

                            callback?.Invoke(message, null, message.MessageId);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        callback?.Invoke(message, ex, 0);
                    }
                });
            }
            else
            {
                try
                {
                    if (message.IsEdit)
                    {
                        message.Message.PostToWebhookAsync(message.Token, message.WebhookId, Client).ContinueWith(task => callback?.Invoke(message, task.Exception, 0));
                    }
                    else
                    {
                        message.Message.EditWebhookAsync(message.Token, message.WebhookId, message.MessageId, Client).ContinueWith(async task =>
                        {
                            callback?.Invoke(message, task.Exception, await DiscordMessage.ExtractMessageIdAsync(task.Result));
                        });
                    }
                }
                catch (Exception ex)
                {
                    callback?.Invoke(message, ex, 0);
                }
            }
        }
    }
}