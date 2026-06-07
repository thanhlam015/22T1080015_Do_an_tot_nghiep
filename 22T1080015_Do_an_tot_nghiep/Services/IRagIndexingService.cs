namespace _22T1080015_Do_an_tot_nghiep.Services
{
    public interface IRagIndexingService
    {
        Task<int> SyncAllAsync();

        Task<bool> SyncAccommodationAsync(int accommodationId);
    }
}