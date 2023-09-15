namespace azopenAiChatApi.Models
{
    public class Settings
    {
        public string? ApiToken { get; set; }
        public string? SigningSecret { get; set; }
        public string? AppLevelToken { get; set; }
        public string? endpoint { get; set; }
        public string? KeyVault { get; set; }
        public string? SlackBotId { get; set; }

        public string? ModelName { get; set; }
    }
}