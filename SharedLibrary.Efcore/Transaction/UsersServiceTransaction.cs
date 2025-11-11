using Microsoft.EntityFrameworkCore;
using SharedLibrary.Models;

namespace SharedLibrary.Efcore.Transaction;

public class UsersServiceTransaction : IUsersServiceTransaction
{
    private readonly IDbContextFactory<AppDbContext> mDbContextFactory;

    public UsersServiceTransaction(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        mDbContextFactory = dbContextFactory;
    }

    public async Task<int> CreateUserAsync(string email, string nickname, string password)
    {
        await using var dbContext = await mDbContextFactory.CreateDbContextAsync();
        await dbContext.Users.AddAsync(new Users(nickname, email, password));
        // TODO 예를들면, 추후 유저 생성과 동시에 생성 필요한 데이터들 여기서 직접 추가할 예정
        return await dbContext.SaveChangesAsync();
    }
}