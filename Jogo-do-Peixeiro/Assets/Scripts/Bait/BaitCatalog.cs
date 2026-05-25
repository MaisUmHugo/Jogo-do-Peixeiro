using System.Collections.Generic;
using UnityEngine;

public static class BaitCatalog
{
    private static BaitData[] defaultBaits;

    public static BaitData[] GetDefaultBaits()
    {
        if (defaultBaits != null && defaultBaits.Length > 0)
            return defaultBaits;

        defaultBaits = LoadResourceBaits();

        if (defaultBaits != null && defaultBaits.Length > 0)
            return defaultBaits;

        defaultBaits = new[]
        {
            CreateRuntimeBait(
                "worms",
                "Minhoca",
                "Ponteiro do skill check 15% mais lento.",
                20,
                1,
                1f,
                1f,
                0.85f,
                1f,
                1f,
                false
            ),
            CreateRuntimeBait(
                "insects",
                "Inseto",
                "Ponteiro do skill check 30% mais lento.",
                40,
                1,
                1f,
                1f,
                0.7f,
                1f,
                1f,
                false
            ),
            CreateRuntimeBait(
                "master_bait",
                "Isca Mestre",
                "Ponteiro do skill check 50% mais lento e todo acerto vira crítico.",
                120,
                1,
                1f,
                1f,
                0.5f,
                1f,
                1f,
                true
            )
        };

        return defaultBaits;
    }

    public static void ReloadDefaultBaits()
    {
        defaultBaits = null;
    }

    public static BaitData[] GetBaitsOrDefault(BaitData[] _configuredBaits)
    {
        List<BaitData> validBaits = new List<BaitData>();

        if (_configuredBaits != null)
        {
            for (int i = 0; i < _configuredBaits.Length; i++)
            {
                if (_configuredBaits[i] != null && !validBaits.Contains(_configuredBaits[i]))
                    validBaits.Add(_configuredBaits[i]);
            }
        }

        return validBaits.Count > 0 ? validBaits.ToArray() : GetDefaultBaits();
    }

    public static BaitData FindDefaultById(string _baitId)
    {
        if (string.IsNullOrWhiteSpace(_baitId))
            return null;

        BaitData[] baits = GetDefaultBaits();

        for (int i = 0; i < baits.Length; i++)
        {
            BaitData bait = baits[i];

            if (bait != null && BaitIdMatches(bait, _baitId))
                return bait;
        }

        return null;
    }

    public static bool BaitIdMatches(BaitData _bait, string _baitId)
    {
        if (_bait == null || string.IsNullOrWhiteSpace(_baitId))
            return false;

        return string.Equals(_bait.SaveId, _baitId, System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(_bait.name, _baitId, System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(_bait.BaitName, _baitId, System.StringComparison.OrdinalIgnoreCase);
    }

    private static BaitData[] LoadResourceBaits()
    {
        BaitData[] loadedBaits = Resources.LoadAll<BaitData>("Baits");

        if (loadedBaits == null || loadedBaits.Length == 0)
            return null;

        List<BaitData> validBaits = new List<BaitData>();

        for (int i = 0; i < loadedBaits.Length; i++)
        {
            if (loadedBaits[i] != null && !validBaits.Contains(loadedBaits[i]))
                validBaits.Add(loadedBaits[i]);
        }

        validBaits.Sort(CompareBaitsByDefaultOrder);
        return validBaits.ToArray();
    }

    private static int CompareBaitsByDefaultOrder(BaitData _left, BaitData _right)
    {
        return GetDefaultBaitOrder(_left).CompareTo(GetDefaultBaitOrder(_right));
    }

    private static int GetDefaultBaitOrder(BaitData _bait)
    {
        if (_bait == null)
            return int.MaxValue;

        string id = _bait.SaveId;

        if (string.Equals(id, "worms", System.StringComparison.OrdinalIgnoreCase))
            return 0;

        if (string.Equals(id, "insects", System.StringComparison.OrdinalIgnoreCase))
            return 1;

        if (string.Equals(id, "master_bait", System.StringComparison.OrdinalIgnoreCase))
            return 2;

        return 100;
    }

    private static BaitData CreateRuntimeBait(
        string _saveId,
        string _baitName,
        string _description,
        int _purchasePrice,
        int _purchaseQuantity,
        float _biteDelayMultiplier,
        float _catchProgressMultiplier,
        float _skillCheckIndicatorSpeedMultiplier,
        float _skillCheckSuccessZoneMultiplier,
        float _directionChangeIntervalMultiplier,
        bool _forcePerfectSkillCheckHits)
    {
        BaitData bait = ScriptableObject.CreateInstance<BaitData>();
        bait.InitializeRuntime(
            _saveId,
            _baitName,
            _description,
            _purchasePrice,
            _purchaseQuantity,
            _biteDelayMultiplier,
            _catchProgressMultiplier,
            _skillCheckIndicatorSpeedMultiplier,
            _skillCheckSuccessZoneMultiplier,
            _directionChangeIntervalMultiplier,
            _forcePerfectSkillCheckHits
        );

        return bait;
    }
}
