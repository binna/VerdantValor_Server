using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Common.Models;

namespace Efcore.Repositories;

public class GameUserRepository : IGameUserRepository
{
    private readonly IHttpContextAccessor mHttpContextAccessor;

    public GameUserRepository(IHttpContextAccessor httpContextAccessor)
    {
        mHttpContextAccessor = httpContextAccessor;
    }

    public async Task<GameUser?> FindByEmailAsync(string email)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        return await dbContext.GameUser.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<GameUser?> FindByUserIdAsync(ulong userId)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        return await dbContext.GameUser.FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<bool> ExistsAsync(string email)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        return await dbContext.GameUser.AnyAsync(u => u.Email == email);
    }
    
    public async Task AddAsync(string email, string nickname, string password)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        await dbContext.GameUser.AddAsync(new GameUser(nickname, email, password));
    }

    public async Task AddAsync(GameUser user)
    {
        var dbContext = mHttpContextAccessor.GetAppDbContext();
        await dbContext.GameUser.AddAsync(user);
    }
}