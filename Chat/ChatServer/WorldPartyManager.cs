using System.Collections.Concurrent;
using Ado.Daos;
using Common.KeyValueStore;
using Shared.Types;

namespace ChatServer;

public class WorldPartyManager
{
    private ConcurrentDictionary<string, ConcurrentDictionary<ulong, byte>> mWorlds = [];
    private ConcurrentDictionary<string, ConcurrentDictionary<ulong, byte>> mParties = [];
    
    private readonly IChatPartyDao mChatPartyDao;
    private readonly ISessionKeyValueStore mSessionKeyValueStore;
    private readonly CancellationToken mToken;

    public WorldPartyManager(
        IChatPartyDao chatPartyDao, 
        ISessionKeyValueStore sessionKeyValueStore, 
        CancellationToken token)
    {
        mChatPartyDao = chatPartyDao;
        mSessionKeyValueStore = sessionKeyValueStore;
        mToken = token;
    }
    
    public async Task InitAsync()
    {
        mWorlds["Korea_1"] = [];

        var partyIds = await mChatPartyDao.FindAllPartyIdAsync(mToken);
        foreach (var partyId in partyIds)
        {
            if (!mParties.TryAdd(partyId, []))
            {
                Console.WriteLine($"[Error] Party {partyId} is already");
            }
        }
    }
    
    public bool TryGetWorldMembers(string worldName, out ConcurrentDictionary<ulong, byte>? members) => mWorlds.TryGetValue(worldName, out members);
    public bool TryGetPartyMembers(string partyId, out ConcurrentDictionary<ulong, byte>? members) => mParties.TryGetValue(partyId, out members);
    
    public EResponseResult AddUserToWorld(string worldName, ulong userId)
    {
        if (!mWorlds.TryGetValue(worldName, out var world))
            return EResponseResult.NoneSelected;

        return world.TryAdd(userId, 0) ? EResponseResult.Success : EResponseResult.AlreadyIn;
    }
    
    public async Task<EResponseResult> AddUserToPartyAsync(ulong userId)
    {
        var partyId = await mChatPartyDao.FindPartyIdByMemberUserIdAsync(userId, mToken);
        
        if (partyId is null)
            return EResponseResult.NotIn;
        
        if (!mParties.TryGetValue(partyId, out var party))
            return EResponseResult.NoneSelected;
        
        return party.TryAdd(userId, 0) ? EResponseResult.Success : EResponseResult.AlreadyIn;
    }
    
    public EResponseResult RemoveUserFromWorld(string worldName, ulong userId)
    {
        if (!mWorlds.TryGetValue(worldName, out var world))
            return EResponseResult.NoneSelected;

        return world.TryRemove(userId, out _) ? EResponseResult.Success : EResponseResult.UnexpectedError;
    }
    
    public EResponseResult RemoveUserFromParty(string partyId, ulong userId)
    {
        if (!mParties.TryGetValue(partyId, out var party))
            return EResponseResult.NoneSelected;
        
        return party.TryRemove(userId, out _) ? EResponseResult.Success : EResponseResult.UnexpectedError;
    }
    
    // TODO 파티랑 월드 추가, 파티랑 월드 삭제
    //  월드추가는 레디스에서 확인해서 추가할 예정
    //  파티는 클라에서 쏴줄거야
}
