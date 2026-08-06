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
    
    public async Task<(EResponseResult Code, string? PartyId)> AddUserToPartyAsync(ulong userId)
    {
        var partyId = await mChatPartyDao.FindPartyIdByMemberUserIdAsync(userId, mToken);
        
        if (partyId is null)
            return (EResponseResult.NotIn, null);
        
        if (!mParties.TryGetValue(partyId, out var party))
            return (EResponseResult.NoneSelected, null);
        
        var code = party.TryAdd(userId, 0) ? EResponseResult.Success : EResponseResult.AlreadyIn;
        return (code, partyId);
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
    
    public async Task<EResponseResult> CreatePartyAsync(string partyId)
    {
        var chatParty = await mChatPartyDao.FindByPartyIdAsync(partyId, mToken);

        if (chatParty is null)
            return EResponseResult.NoneSelected;

        return mParties.TryAdd(partyId, []) ? EResponseResult.Success : EResponseResult.AlreadyIn;
    }

    public async Task<EResponseResult> DeletePartyAsync(string partyId)
    {
        var chatParty = await mChatPartyDao.FindByPartyIdAsync(partyId, mToken);

        if (chatParty is not null)
            return EResponseResult.PartyNotYetDeleted;
        
        // TODO 다른 세션에 CurrentParty 삭제 방법 고려하기

        return mParties.TryRemove(partyId, out _) ? EResponseResult.Success : EResponseResult.UnexpectedError;
    }
    
    // TODO 월드 추가 삭제
    // TODO 파티도 같은 월드끼리 가능해야 함
}
