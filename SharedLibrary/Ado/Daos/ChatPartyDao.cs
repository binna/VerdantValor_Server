using Common.Models;
using MySql.Data.MySqlClient;

namespace Ado.Daos;

public class ChatPartyDao : IChatPartyDao
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

    public async Task<string?> FindPartyIdByOwnerUserIdAsync(ulong ownerUserId, CancellationToken token)
    {
        await using var conn = mDbFactory.CreateConnection();
        await conn.OpenAsync(token);
        
        await using var cmd = new MySqlCommand(
            "SELECT partyId FROM chatParty WHERE ownerUserId = @ownerUserId", conn);
        
        cmd.Parameters.Add("@ownerUserId", MySqlDbType.UInt64).Value = ownerUserId;
        
        await using var reader = await cmd.ExecuteReaderAsync(token);
        return await ChatParty.FromDbDataReaderToPartyIdAsync(reader, token);
    }
    
    public async Task<string?> FindPartyIdByMemberUserIdAsync(ulong userId, CancellationToken token)
    {
        await using var conn = mDbFactory.CreateConnection();
        await conn.OpenAsync(token);
        
        await using var cmd = new MySqlCommand(
            "SELECT partyId FROM chatPartyMember WHERE userId = @userId", conn);
        
        cmd.Parameters.Add("@userId", MySqlDbType.UInt64).Value = userId;
        
        await using var reader = await cmd.ExecuteReaderAsync(token);
        return await ChatPartyMember.FromDbDataReaderToPartyIdAsync(reader, token);
    }

    public async Task<ChatParty?> FindByPartyIdAsync(string partyId, CancellationToken token)
    {
        await using var conn = mDbFactory.CreateConnection();
        await conn.OpenAsync(token);

        await using var cmd = new MySqlCommand(
            "SELECT partyId, name, ownerUserId FROM chatParty WHERE partyId = @partyId", conn);

        cmd.Parameters.Add("@partyId", MySqlDbType.VarChar).Value = partyId;

        await using var reader = await cmd.ExecuteReaderAsync(token);
        return await ChatParty.FromDbDataReaderAsync(reader, token);
    }
}