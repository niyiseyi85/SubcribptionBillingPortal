using Microsoft.AspNetCore.Identity;

namespace VeltrixBookingApp.Domain.Entities
{
    public class AppUser : IdentityUser<Guid>
    {
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string? ProfileImageUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public bool IsDefaultUsername { get; set; } = true;
        public string? Occupation { get; set; }
        public DateTime? DOB { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? Bio { get; set; }
        public string? TimeZoneId { get; set; }
        public string? LastActivity { get; set; }
        public DateTime? LastEmailVerificationSentDate { get; set; }
        public DateTime? LastEmailCommunitySentDate { get; set; }
        public string? FacebookUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? XUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? YoutubeUrl { get; set; }
        public string? TikTokUrl { get; set; }
        public bool? ShowCommunity { get; set; } = true;
        public string? Status { get; set; }
        public bool? AllCookies { get; set; }
        public bool? EssentialCookies { get; set; } = true;
        public int LoginCount { get; set; }
        public bool IsFirstLogin { get; set; } = true;
        public bool HasCookieProfile { get; set; }
        public bool? IsFlagged { get; set; } = false;
    }
}
