using System;
using System.Collections.Generic;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class Booking
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int RoomId { get; set; }

    public DateTime CheckInDate { get; set; }

    public DateTime CheckOutDate { get; set; }

    public decimal TotalPrice { get; set; }

    public string Status { get; set; } = null!;

    public string? PaymentMethod { get; set; }

    public string? PaymentStatus { get; set; }

    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Notes { get; set; }
    public int NumberOfRooms { get; set; }

    public int AdultCount { get; set; }

    public int ChildCount { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Room Room { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
