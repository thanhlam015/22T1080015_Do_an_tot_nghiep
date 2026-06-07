using System;
using System.Collections.Generic;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class Employee
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string? Role { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? AvatarUrl { get; set; }
}
