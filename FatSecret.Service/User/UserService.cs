using Microsoft.AspNetCore.Identity;

namespace FatSecret.Service.User;

public class UserService
{
    private readonly UserManager<Domain.Entities.Identity.User> _userManager;

    public UserService(UserManager<Domain.Entities.Identity.User> userManager)
    {
        _userManager = userManager;
    }
    
    
}