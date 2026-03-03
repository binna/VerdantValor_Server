namespace Common.Manager;

public class BannedManager
{
    private static HashSet<string> mBannedSet = [];
    
    private static bool mIsLoaded;
    
    public static void Load()
    {
        if (mIsLoaded)
            return;

        mIsLoaded = true;

        mBannedSet.Add("admin");
    }

    public static bool ContainsBannedWord(string text)
    {
        return mBannedSet.Any(word => 
            text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}