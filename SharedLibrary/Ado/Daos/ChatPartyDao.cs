using Common.Models;
using MySql.Data.MySqlClient;

namespace Ado.Daos;

public class ChatPartyDao
{
    private readonly DbFactory mDbFactory;
    
    public ChatPartyDao(DbFactory dbFactory)
    {
        mDbFactory = dbFactory;
    }

    public async Task<List<string>> FindAllPartyIdAsync(CancellationToken token)
    {
        await using var conn = mDbFactory.CreateConnection();
        await conn.OpenAsync(token);
        
        await using var cmd = new MySqlCommand("SELECT partyId FROM chatParty", conn);
        
        await using var reader = await cmd.ExecuteReaderAsync(token);
        return await ChatParty.FromDbDataReaderToPartyIdListAsync(reader, token);
    }
    
    // TODO 존재 여부 검색하는 부분 필요,,, 
    
    // TODO 삭제 : 공지 후 나가지는 무언가가 필요
}