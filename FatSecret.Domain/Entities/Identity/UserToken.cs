using FatSecret.Domain.Models;

namespace FatSecret.Domain.Entities.Identity;

public class UserToken : BaseEntity
{
    public int UserId { get; set; }
    
    public User User { get; set; }
    
    public string Token { get; set; }
    
    public string Value { get; set; }

    public string HashedToken { get; set; }
    
    public DateTimeOffset? ExpirationAt { get; set; }
}