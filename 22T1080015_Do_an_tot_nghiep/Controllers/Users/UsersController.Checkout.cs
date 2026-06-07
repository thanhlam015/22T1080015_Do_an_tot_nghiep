using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _22T1080015_Do_an_tot_nghiep.Controllers
{
    public partial class UsersController
    {
        // Checkout GET
        [Authorize(AuthenticationSchemes = "UserCookie", Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> Checkout(
                int roomId,
                DateTime? checkInDate,
                DateTime? checkOutDate,
                int roomCount = 1,
                int adultCount = 2,
                int childCount = 0
                )
        {
            var vm = await BuildCheckoutVmAsync(
                    roomId,
                    checkInDate,
                    checkOutDate,
                    roomCount,
                    adultCount,
                    childCount,
                    null
                    );

            if (vm == null)
            {
                return NotFound();
            }

            if (vm.StayNights <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ngày nhận và trả phòng trước khi đặt.";
                return RedirectToAction(nameof(Details), new
                {
                    id = vm.AccommodationId,
                    checkInDate,
                    checkOutDate,
                    roomCount,
                    adultCount,
                    childCount
                });
            }

            if (vm.AvailableRooms < vm.RoomCount)
            {
                TempData["ErrorMessage"] = "Số phòng còn trống không đủ. Vui lòng chọn lại.";
                return RedirectToAction(nameof(Details), new
                {
                    id = vm.AccommodationId,
                    checkInDate,
                    checkOutDate,
                    roomCount,
                    adultCount,
                    childCount
                });
            }

            return View(vm);
        }
        // Checkout POST
        [Authorize(AuthenticationSchemes = "UserCookie", Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(UserCheckoutVM vm)
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                TempData["InfoMessage"] = "Vui lòng đăng nhập để đặt phòng.";
                return RedirectToAction(nameof(Auth));
            }

            if (vm.PaymentMethod != "PayAtHotel" && vm.PaymentMethod != "VietQR")
            {
                ModelState.AddModelError(nameof(vm.PaymentMethod), "Phương thức thanh toán không hợp lệ.");
            }

            var rebuiltVm = await BuildCheckoutVmAsync(
                            vm.RoomId,
                            vm.CheckInDate,
                            vm.CheckOutDate,
                            vm.RoomCount,
                            vm.AdultCount,
                            vm.ChildCount,
                            vm);

            if (rebuiltVm == null)
            {
                return NotFound();
            }

            if (rebuiltVm.StayNights <= 0)
            {
                ModelState.AddModelError(string.Empty, "Vui lòng chọn ngày nhận và trả phòng hợp lệ.");
            }

            if (rebuiltVm.AvailableRooms < rebuiltVm.RoomCount)
            {
                ModelState.AddModelError(string.Empty, "Số phòng còn trống không đủ. Vui lòng chọn lại.");
            }

            if (!ModelState.IsValid)
            {
                return View(rebuiltVm);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var booking = new Booking
            {
                UserId = user.UserId,
                RoomId = rebuiltVm.RoomId,
                CheckInDate = rebuiltVm.CheckInDate.Date,
                CheckOutDate = rebuiltVm.CheckOutDate.Date,
                NumberOfRooms = rebuiltVm.RoomCount,
                AdultCount = rebuiltVm.AdultCount,
                ChildCount = rebuiltVm.ChildCount,
                TotalPrice = rebuiltVm.TotalPrice,
                Status = "Pending",
                PaymentMethod = rebuiltVm.PaymentMethod,
                PaymentStatus = "Unpaid",
                FullName = rebuiltVm.FullName.Trim(),
                PhoneNumber = rebuiltVm.PhoneNumber.Trim(),
                Email = rebuiltVm.Email.Trim(),
                Notes = BuildBookingNoteWithPromotion(rebuiltVm),
                CreatedAt = DateTime.Now
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            if (rebuiltVm.PromotionId.HasValue && rebuiltVm.DiscountAmount > 0)
            {
                var promotion = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.Id == rebuiltVm.PromotionId.Value);

                if (promotion != null)
                {
                    promotion.UsedCount += 1;
                    promotion.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }

            if (rebuiltVm.PaymentMethod == "VietQR")
            {
                var payment = new Payment
                {
                    BookingId = booking.Id,
                    PaymentMethod = "VietQR",
                    PaymentStatus = "Pending",
                    Amount = booking.TotalPrice,
                    TransactionId = $"TB{booking.Id}",
                    PaymentDate = null
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Đặt phòng thành công. Vui lòng theo dõi trạng thái đơn.";

            return RedirectToAction(nameof(BookingSuccess), new { id = booking.Id });
        }
        // BookingSuccess
        [Authorize(AuthenticationSchemes = "UserCookie", Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> BookingSuccess(int id)
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return RedirectToAction(nameof(Auth));
            }

            var booking = await _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.Accommodation)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b =>
                    b.Id == id &&
                    b.UserId == user.UserId);

            if (booking == null)
            {
                return NotFound();
            }

            string? qrUrl = null;

            if (booking.PaymentMethod == "VietQR" &&
                booking.PaymentStatus != "Paid")
            {
                qrUrl = BuildVietQrImageUrl(booking);
            }

            var vm = new UserBookingSuccessVM
            {
                BookingId = booking.Id,
                AccommodationName = booking.Room.Accommodation.Name,
                RoomType = booking.Room.RoomType,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                RoomCount = booking.NumberOfRooms,
                AdultCount = booking.AdultCount,
                ChildCount = booking.ChildCount,
                TotalPrice = booking.TotalPrice,
                PaymentMethod = booking.PaymentMethod ?? "PayAtHotel",
                PaymentStatus = booking.PaymentStatus ?? "Unpaid",
                BookingStatus = booking.Status,
                QrImageUrl = qrUrl,
                TransferContent = $"TB{booking.Id}",
                BankAccountName = _configuration["VietQrSettings:AccountName"],
                BankAccountNo = _configuration["VietQrSettings:AccountNo"]
            };

            return View(vm);
        }

        // BuildCheckoutVmAsync
        private async Task<UserCheckoutVM?> BuildCheckoutVmAsync(
                        int roomId,
                        DateTime? checkInDate,
                        DateTime? checkOutDate,
                        int roomCount,
                        int adultCount,
                        int childCount,
                        UserCheckoutVM? input = null,
                        string? promotionCode = null)
        {
            var room = await _context.Rooms
                .Include(r => r.Accommodation)
                    .ThenInclude(a => a.AccommodationImages)
                .Include(r => r.RoomImages)
                .Include(r => r.Bookings)
                .Include(r => r.RoomAvailabilityPricings)
                .FirstOrDefaultAsync(r =>
                    r.Id == roomId &&
                    !r.IsDeleted &&
                    r.Status == "Active");

            if (room == null)
            {
                return null;
            }

            var search = new UserSearchVM
            {
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                RoomCount = roomCount,
                AdultCount = adultCount,
                ChildCount = childCount
            };

            NormalizeUserSearch(search);

            int stayNights = CalculateStayNights(search);

            DateTime? checkIn = stayNights > 0 ? search.CheckInDate!.Value.Date : null;
            DateTime? checkOut = stayNights > 0 ? search.CheckOutDate!.Value.Date : null;

            int availableRooms = CalculateAvailableRooms(
                room,
                search.RoomCount,
                checkIn,
                checkOut);

            decimal originalTotalPrice = stayNights > 0
                ? CalculateRoomTotalPrice(room, search)
                : 0;

            var user = await GetCurrentUserAsync();

            var promotionResult = await CalculateAutoPromotionDiscountAsync(
                room.AccommodationId,
                originalTotalPrice,
                user?.UserId);

            decimal discountAmount = promotionResult.DiscountAmount;
            decimal finalTotalPrice = originalTotalPrice - discountAmount;

            if (finalTotalPrice < 0)
            {
                finalTotalPrice = 0;
            }

            

            var vm = input ?? new UserCheckoutVM();

            vm.RoomId = room.Id;
            vm.AccommodationId = room.AccommodationId;
            vm.AccommodationName = room.Accommodation.Name;
            vm.RoomType = room.RoomType;
            vm.CheckInDate = search.CheckInDate ?? DateTime.Today;
            vm.CheckOutDate = search.CheckOutDate ?? DateTime.Today.AddDays(1);
            vm.StayNights = stayNights;
            vm.RoomCount = search.RoomCount;
            vm.AdultCount = search.AdultCount;
            vm.ChildCount = search.ChildCount;
            vm.AvailableRooms = availableRooms;
            vm.PricePerNight = room.PriceNight;
            vm.OriginalTotalPrice = originalTotalPrice;
            vm.DiscountAmount = promotionResult.DiscountAmount;
            vm.TotalPrice = finalTotalPrice;
            vm.PromotionId = promotionResult.PromotionId;
            vm.PromotionCode = promotionResult.PromotionCode;
            vm.PromotionMessage = promotionResult.Message;

            vm.ImageUrl = room.RoomImages
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder ?? int.MaxValue)
                .Select(i => i.ImageUrl)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(vm.ImageUrl))
            {
                vm.ImageUrl = room.Accommodation.AccommodationImages
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.SortOrder ?? int.MaxValue)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault();
            }

            if (input == null && user != null)
            {
                vm.FullName = user.FullName;
                vm.PhoneNumber = user.PhoneNumber;
                vm.Email = user.Email;
            }

            return vm;
        }
        // BuildVietQrImageUrl
        private string BuildVietQrImageUrl(Booking booking)
        {
            string bankId = _configuration["VietQrSettings:BankId"] ?? "";
            string accountNo = _configuration["VietQrSettings:AccountNo"] ?? "";
            string accountName = _configuration["VietQrSettings:AccountName"] ?? "";
            string template = _configuration["VietQrSettings:Template"] ?? "compact2";

            if (string.IsNullOrWhiteSpace(bankId) ||
                string.IsNullOrWhiteSpace(accountNo))
            {
                return "";
            }

            bankId = bankId.Trim();
            accountNo = accountNo.Trim();
            accountName = accountName.Trim();
            template = template.Trim();

            string amount = ((long)booking.TotalPrice).ToString();
            string addInfo = $"TB{booking.Id}";

            string encodedInfo = Uri.EscapeDataString(addInfo);
            string encodedName = Uri.EscapeDataString(accountName);

            return $"https://img.vietqr.io/image/{bankId}-{accountNo}-{template}.png?amount={amount}&addInfo={encodedInfo}&accountName={encodedName}";
        }
        private async Task<(int? PromotionId, string? PromotionCode, decimal DiscountAmount, string? Message)> CalculateAutoPromotionDiscountAsync(
                    int accommodationId,
                    decimal originalTotalPrice,
                    int? userId)
        {
            DateTime now = DateTime.Now;

            var promotions = await _context.Promotions
                .Include(p => p.PromotionAccommodations)
                .Where(p =>
                    p.PromotionAccommodations.Any(pa => pa.AccommodationId == accommodationId) ||
                    !p.PromotionAccommodations.Any())
                .ToListAsync();

            if (!promotions.Any())
            {
                return (null, null, 0, "Hiện chưa có khuyến mãi áp dụng cho nơi lưu trú này.");
            }

            var expiredPromotion = promotions
                .Where(p => p.EndDate < now)
                .OrderByDescending(p => p.EndDate)
                .FirstOrDefault();

            var exhaustedPromotion = promotions
                .Where(p =>
                    p.UsageLimit.HasValue &&
                    p.UsedCount >= p.UsageLimit.Value)
                .OrderByDescending(p => p.EndDate)
                .FirstOrDefault();

            var validPromotions = promotions
                .Where(p =>
                    p.Status == "Active" &&
                    p.StartDate <= now &&
                    p.EndDate >= now &&
                    (!p.UsageLimit.HasValue || p.UsedCount < p.UsageLimit.Value) &&
                    p.MinBookingAmount <= originalTotalPrice)
                .ToList();

            if (!validPromotions.Any())
            {
                if (exhaustedPromotion != null)
                {
                    return (null, exhaustedPromotion.Code, 0, $"Mã {exhaustedPromotion.Code} đã hết số lượng sử dụng.");
                }

                if (expiredPromotion != null)
                {
                    return (null, expiredPromotion.Code, 0, $"Mã {expiredPromotion.Code} đã hết hạn.");
                }

                return (null, null, 0, "Hiện chưa có mã khuyến mãi phù hợp với đơn đặt phòng này.");
            }

            var bestPromotion = validPromotions
                .Select(p =>
                {
                    decimal discountAmount = 0;

                    if (p.DiscountType == "Percent")
                    {
                        discountAmount = originalTotalPrice * p.DiscountValue / 100;

                        if (p.MaxDiscountAmount.HasValue &&
                            p.MaxDiscountAmount.Value > 0 &&
                            discountAmount > p.MaxDiscountAmount.Value)
                        {
                            discountAmount = p.MaxDiscountAmount.Value;
                        }
                    }
                    else if (p.DiscountType == "Amount")
                    {
                        discountAmount = p.DiscountValue;
                    }

                    if (discountAmount > originalTotalPrice)
                    {
                        discountAmount = originalTotalPrice;
                    }

                    discountAmount = Math.Round(discountAmount, 0);

                    return new
                    {
                        Promotion = p,
                        DiscountAmount = discountAmount
                    };
                })
                .Where(x => x.DiscountAmount > 0)
                .OrderByDescending(x => x.DiscountAmount)
                .ThenBy(x => x.Promotion.EndDate)
                .FirstOrDefault();

            if (bestPromotion == null)
            {
                return (null, null, 0, "Hiện chưa có mã khuyến mãi phù hợp với đơn đặt phòng này.");
            }

            return (
                bestPromotion.Promotion.Id,
                bestPromotion.Promotion.Code,
                bestPromotion.DiscountAmount,
                $"Đã tự động áp dụng mã {bestPromotion.Promotion.Code}, giảm {bestPromotion.DiscountAmount:N0}đ."
            );
        }

        private string? BuildBookingNoteWithPromotion(UserCheckoutVM vm)
        {
            var notes = string.IsNullOrWhiteSpace(vm.Notes)
                ? ""
                : vm.Notes.Trim();

            if (!string.IsNullOrWhiteSpace(vm.PromotionCode) && vm.DiscountAmount > 0)
            {
                var promotionInfo =
                    $"Mã khuyến mãi: {vm.PromotionCode}; " +
                    $"Giá gốc: {vm.OriginalTotalPrice:N0}đ; " +
                    $"Giảm: {vm.DiscountAmount:N0}đ; " +
                    $"Thanh toán: {vm.TotalPrice:N0}đ";

                if (string.IsNullOrWhiteSpace(notes))
                {
                    return promotionInfo;
                }

                return notes + Environment.NewLine + promotionInfo;
            }

            return string.IsNullOrWhiteSpace(notes) ? null : notes;
        }
    }
}