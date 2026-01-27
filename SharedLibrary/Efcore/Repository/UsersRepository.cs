using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Common.Models;

namespace Efcore.Repository;

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
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        await dbContext.Users.AddAsync(new Users(nickname, email, password));
    }
}