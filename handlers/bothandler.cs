using SlackNet;
using SlackNet.Events;
using SlackNet.WebApi;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Azure;
using Azure.AI.OpenAI;
using SharpToken;
using Microsoft.Extensions.Options;
using azopenAiChatApi.Models;

namespace azopenAiChatApi.Handlers
{
    public class AzOpenAIHandler : IEventHandler<MessageEvent>
    {

        private readonly ISlackApiClient _slack;
        private readonly OpenAIClient _azOpenAiClient;
        private readonly Settings _settings;

        public AzOpenAIHandler(OpenAIClient azOpenAiClient, ISlackApiClient slack, IOptions<Settings> settings)
        {
            _azOpenAiClient = azOpenAiClient;
            _slack = slack;
            _settings = settings.Value;
        }


        string openAisystemMessage = @"you are a helpful assistant. You can help with writing tasks, translations, code examples etc.";

        public async Task Handle(MessageEvent slackEvent)
        {
            Console.WriteLine($"Removed message content: {slackEvent.Text}");

            // filter out non im messages and messages from the bot itself
            if (slackEvent.ChannelType != "im" || string.IsNullOrEmpty(slackEvent.Text) || slackEvent.User == _settings.SlackBotId)
            {
                return;
            }
            // filter out any im that is not with the bot
            var conv = await _slack.Conversations.Members(slackEvent.Channel);
            if (!conv.Members.Contains(_settings.SlackBotId))
            {
                return;
            }

            var system_message = new ChatMessage(ChatRole.System, openAisystemMessage);
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Temperature = (float)0.7,
                MaxTokens = 1000,
                NucleusSamplingFactor = (float)0.95,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                ChoicesPerPrompt = 1,
                User = slackEvent.User,
            };
            chatCompletionsOptions.Messages.Add(system_message);

            // Adding message history to create context
            if (slackEvent.ThreadTs != null)
            {
                var history = await _slack.Conversations.Replies(channelId: slackEvent.Channel, threadTs: slackEvent.ThreadTs, inclusive: true);

                foreach (var message in history.Messages)
                {
                    if (message.User == _settings.SlackBotId)
                    {
                        chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.Assistant, message.Text));
                    }
                    else
                    {
                        chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, message.Text));
                    }
                }
            }
            // add the users latest input to the conversation
            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, slackEvent.Text));
            // count the number of tokens in the chatCompletionOptions
            // if the number of tokens exceeds the limit, remove the oldest messages until the limit is reached
            int tokens_per_message = 4;
            var gptEncoding = GptEncoding.GetEncodingForModel("gpt-4");
            int token_limit = 3000;
            int num_tokens = tokens_per_message;
            foreach (var message in chatCompletionsOptions.Messages)
            {
                num_tokens += gptEncoding.Encode(message.Content).Count;
            }
            var diff = token_limit - num_tokens;
            while (num_tokens > token_limit)
            {
                chatCompletionsOptions.Messages.RemoveAt(chatCompletionsOptions.Messages.Count - 1);
                // write a log line to the console
                Console.WriteLine($"Removed message from chatCompletionsOptions. {diff} tokens over limit.");
                
                // Update the value of num_tokens after removing the message
                num_tokens = tokens_per_message;
                foreach (var message in chatCompletionsOptions.Messages)
                {
                    num_tokens += gptEncoding.Encode(message.Content).Count;
                }
                diff = num_tokens;

                if (num_tokens <= token_limit)
                {
                    break;
                }
            }
            Response<ChatCompletions> responseWithoutStream = await _azOpenAiClient.GetChatCompletionsAsync(_settings.ModelName, chatCompletionsOptions);
            ChatCompletions completions = responseWithoutStream.Value;
            string response = completions.Choices[0].Message.Content;

            await _slack.Chat.PostMessage(new Message
            {
                Text = response,
                Channel = slackEvent.Channel,
                ThreadTs = slackEvent.Ts,              
            }).ConfigureAwait(false);
        }
    }
}