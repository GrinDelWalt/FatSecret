namespace FatSecret.Domain.Entities.Identity;

public class User : BaseEntity
{
    public string Email { get; set; }
    
    public string Login { get; set; }
    
    public string FirstName { get; set; }
    
    public string LastName { get; set; }
    
    public string Password { get; set; }
    
    public ICollection<UserToken> UserTokens { get; set; }
}