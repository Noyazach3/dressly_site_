﻿namespace ClassLibrary1.Models
{
    public class ResetPasswordRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
