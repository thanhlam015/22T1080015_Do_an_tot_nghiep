using _22T1080015_Do_an_tot_nghiep.Models;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace _22T1080015_Do_an_tot_nghiep.Services
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AISettings _settings;

        public EmbeddingService(
            IHttpClientFactory httpClientFactory,
            IOptions<AISettings> settings)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
        }

        public string GetModelName()
        {
            if (_settings.UseMockEmbedding)
            {
                return $"mock-local-{_settings.MockEmbeddingDimensions}";
            }

            return _settings.Provider == "OpenAI"
                ? _settings.OpenAIEmbeddingModel
                : _settings.GeminiEmbeddingModel;
        }

        public async Task<float[]> CreateEmbeddingAsync(string text)
        {
            if (_settings.UseMockEmbedding)
            {
                return CreateMockEmbedding(text, _settings.MockEmbeddingDimensions);
            }

            if (_settings.Provider == "OpenAI")
            {
                return await CreateOpenAIEmbeddingAsync(text);
            }

            return await CreateGeminiEmbeddingAsync(text);
        }

        private async Task<float[]> CreateOpenAIEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(_settings.OpenAIApiKey))
            {
                throw new InvalidOperationException("Chưa cấu hình OpenAI API key.");
            }

            var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.OpenAIApiKey);

            var body = new
            {
                model = _settings.OpenAIEmbeddingModel,
                input = text
            };

            var response = await client.PostAsync(
                "https://api.openai.com/v1/embeddings",
                new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"
                )
            );

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("OpenAI embedding lỗi: " + json);
            }

            using var doc = JsonDocument.Parse(json);

            var embedding = doc.RootElement
                .GetProperty("data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();

            return embedding;
        }

        private async Task<float[]> CreateGeminiEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(_settings.GeminiApiKey))
            {
                throw new InvalidOperationException("Chưa cấu hình Gemini API key.");
            }

            var client = _httpClientFactory.CreateClient();

            var model = _settings.GeminiEmbeddingModel;

            var url =
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:embedContent";

            var body = new
            {
                model = $"models/{model}",
                content = new
                {
                    parts = new[]
                    {
                        new { text }
                    }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", _settings.GeminiApiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Gemini embedding lỗi: " + json);
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("embedding", out var embeddingElement) &&
                embeddingElement.TryGetProperty("values", out var values))
            {
                return values.EnumerateArray()
                    .Select(x => x.GetSingle())
                    .ToArray();
            }

            if (root.TryGetProperty("embeddings", out var embeddings))
            {
                var first = embeddings[0];

                if (first.TryGetProperty("values", out var values2))
                {
                    return values2.EnumerateArray()
                        .Select(x => x.GetSingle())
                        .ToArray();
                }
            }

            throw new InvalidOperationException("Không đọc được embedding từ Gemini response.");
        }
        private float[] CreateMockEmbedding(string text, int dimensions)
        {
            if (dimensions <= 0)
            {
                dimensions = 768;
            }

            var vector = new float[dimensions];

            using var sha = SHA256.Create();

            var input = string.IsNullOrWhiteSpace(text)
                ? "empty"
                : text;

            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));

            for (int i = 0; i < dimensions; i++)
            {
                byte value = hash[i % hash.Length];

                vector[i] = ((value / 255f) * 2f) - 1f;
            }

            float length = (float)Math.Sqrt(vector.Sum(x => x * x));

            if (length > 0)
            {
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] /= length;
                }
            }

            return vector;
        }
    }
}