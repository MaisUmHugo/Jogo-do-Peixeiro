using UnityEngine;

public static class BaitSaveResolver
{
    public static BaitData FindBaitById(string _baitId)
    {
        if (string.IsNullOrWhiteSpace(_baitId))
            return null;

        BaitData[] loadedBaits = Resources.FindObjectsOfTypeAll<BaitData>();

        for (int i = 0; i < loadedBaits.Length; i++)
        {
            BaitData bait = loadedBaits[i];

            if (bait != null && BaitCatalog.BaitIdMatches(bait, _baitId))
                return bait;
        }

        return BaitCatalog.FindDefaultById(_baitId);
    }
}
