using Microsoft.EntityFrameworkCore;
using SharedLibrary.Models;

namespace SharedLibrary.Efcore.Repository;

public class UsersRepository : IUsersRepository
{
    private readonly IDbContextFactory<AppDbContext> mDbContextFactory;

    public UsersRepository(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        mDbContextFactory = dbContextFactory;
    }

    public async Task<Users?> FindUserByEmailAsync(string email)
    {
        // 매번 새 커넥션을 쓰는 것처럼 보이지만
        // 드라이브에서 커넥션 풀까지 코드가 짜여 있기 때문에
        // 커넥션 풀 재사용 한다
        await using var dbContext = await mDbContextFactory.CreateDbContextAsync();
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
    
    public async Task<bool> ExistsUserAsync(string email)
    {
        await using var dbContext = await mDbContextFactory.CreateDbContextAsync();
        return await dbContext.Users.AnyAsync(u => u.Email == email);
    }

    public async Task AddAsync(string email, string nickname, string password)
    {
        await using var dbContext = await mDbContextFactory.CreateDbContextAsync();
        await dbContext.Users.AddAsync(new Users(email, nickname, password));
    }
    
    public async Task<int> SaveAsync()
    {
        await using var dbContext = await mDbContextFactory.CreateDbContextAsync();
        return await dbContext.SaveChangesAsync();
    }
}