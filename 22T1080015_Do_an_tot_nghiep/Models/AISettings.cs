namespace _22T1080015_Do_an_tot_nghiep.Models
{
    public class AISettings
    {
        public string Provider { get; set; } = "Gemini";

        public string? GeminiApiKey { get; set; }

        public string GeminiEmbeddingModel { get; set; } = "gemini-embedding-001";

        public string? OpenAIApiKey { get; set; }

        public string OpenAIEmbeddingModel { get; set; } = "text-embedding-3-small";
        public bool UseMockEmbedding { get; set; } = false;

        public int MockEmbeddingDimensions { get; set; } = 768;
        public bool UseMockChat { get; set; } = true;

        public string GeminiChatModel { get; set; } = "gemini-2.0-flash";
    }

    public class WeatherSettings
    {
        public string? WindyApiKey { get; set; }
    }
}