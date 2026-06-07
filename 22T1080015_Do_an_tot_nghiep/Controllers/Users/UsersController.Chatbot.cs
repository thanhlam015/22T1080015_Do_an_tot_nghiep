using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _22T1080015_Do_an_tot_nghiep.Controllers
{
    public partial class UsersController
    {
        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AskChatbot([FromBody] UserChatbotRequestVM vm)
        {
            if (vm == null || string.IsNullOrWhiteSpace(vm.Question))
            {
                return Json(new
                {
                    success = false,
                    answer = "Vui lòng nhập câu hỏi."
                });
            }

            try
            {
                var result = await _chatbotService.AskWithCardsAsync(vm.Question.Trim());

                return Json(new
                {
                    success = true,
                    answer = result.Answer,
                    cards = result.Cards
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    answer = "Bot đang gặp lỗi khi xử lý câu hỏi: " + ex.Message
                });
            }
        }
    }
}