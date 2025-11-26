using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Models;

namespace SharedLibrary.Efcore.Repository;

public class UsersRepository : IUsersRepository
{
    private readonly IHttpContextAccessor mHttpContextAccessor;

    public UsersRepository(IHttpContextAccessor httpContextAccessor)
    {
        mHttpContextAccessor = httpContextAccessor;
    }

    public async Task<Users?> FindUserByEmailAsync(string email)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
    
    public async Task<bool> ExistsUserAsync(string email)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        return await dbContext.Users.AnyAsync(u => u.Email == email);
    }
    
    public async Task AddAsync(string email, string nickname, string password)
    {
        var dbContext = (AppDbContext)mHttpContextAccessor.HttpContext!.Items["dbContext"]!;
        mHttpContextAccessor.HttpContext!.Items["isChange"] = true; 
        await dbContext.Users.AddAsync(new Users(nickname, email, password));
    }
}