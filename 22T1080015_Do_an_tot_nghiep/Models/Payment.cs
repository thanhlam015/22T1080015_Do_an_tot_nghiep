using System;
using System.Collections.Generic;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int BookingId { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? TransactionId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public virtual Booking Booking { get; set; } = null!;
}
