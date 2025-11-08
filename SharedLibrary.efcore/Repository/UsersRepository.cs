using Microsoft.EntityFrameworkCore;
using SharedLibrary.Models;

namespace SharedLibrary.Efcore.Repository;

public class UsersRepository : IUsersRepository
{
    private readonly AppDbContext mAppDbContext;

    public UsersRepository(AppDbContext appDbContext)
    {
        mAppDbContext = appDbContext;
    }

    public async Task<Users?> FindUserByEmailAsync(string email)
    {
        return await mAppDbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
    
    public async Task<bool> ExistsUserAsync(string email)
    {
        return await mAppDbContext.Users.AnyAsync(u => u.Email == email);
    }

    public async Task AddAsync(string email, string nickname, string password)
    {
        await mAppDbContext.Users.AddAsync(new Users(nickname, email, password));
    }
    
    public async Task<int> SaveAsync()
    {
        return await mAppDbContext.SaveChangesAsync();
    }
}