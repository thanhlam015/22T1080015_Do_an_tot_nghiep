using _22T1080015_Do_an_tot_nghiep.Models;

namespace _22T1080015_Do_an_tot_nghiep.Services
{
    public interface IChatbotService
    {
        Task<string> AskAsync(string question);

        Task<string> AskWithContextAsync(string question, string ragContext);

        Task<UserChatbotAnswerVM> AskWithCardsAsync(string question);
    }
}