namespace _22T1080015_Do_an_tot_nghiep.Services
{
    public interface IEmbeddingService
    {
        Task<float[]> CreateEmbeddingAsync(string text);

        string GetModelName();
    }
}