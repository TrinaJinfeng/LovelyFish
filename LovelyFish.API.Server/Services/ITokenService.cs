using LovelyFish.API.Server.Models;
using Microsoft.AspNetCore.Identity;

namespace LovelyFish.API.Server.Services
{
    public interface ITokenService
    {
        Task<string> GenerateToken(ApplicationUser user, UserManager<ApplicationUser> userManager);
    }
}