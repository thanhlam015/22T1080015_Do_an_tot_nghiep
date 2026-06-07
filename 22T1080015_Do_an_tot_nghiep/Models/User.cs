using System;
using System.Collections.Generic;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? PasswordHash { get; set; }
    public string Status { get; set; } = "Active";

    public bool IsLocked { get; set; }

    public string? LockReason { get; set; }

    public DateTime? LockedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public bool EmailConfirmed { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<SavedAccommodation> SavedAccommodations { get; set; } = new List<SavedAccommodation>();
}
