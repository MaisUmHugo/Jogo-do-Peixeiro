using System.Collections.Generic;
using UnityEngine;

public static class BaitCatalog
{
    private static BaitData[] defaultBaits;

    public static BaitData[] GetDefaultBaits()
    {
        if (defaultBaits != null && defaultBaits.Length > 0)
            return defaultBaits;

        defaultBaits = new[]
        {
            CreateRuntimeBait(
                "worms",
                "Minhoca",
                "Diminui um pouco o tempo ate a mordida.",
                20,
                1,
                0.85f,
                1f,
                1f,
                1.05f,
                1f
            ),
            CreateRuntimeBait(
                "insects",
                "Insetos",
                "Ajuda o peixe a morder mais rapido e facilita a captura.",
                40,
                1,
                0.75f,
                1.08f,
                0.95f,
                1.1f,
                1f
            ),
            CreateRuntimeBait(
                "master_bait",
                "Isca mestre",
                "Bonus forte para mordida, progresso e skill check.",
                120,
                1,
                0.65f,
                1.15f,
                0.9f,
                1.2f,
                1.15f
            )
        };

        return defaultBaits;
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
        float _directionChangeIntervalMultiplier)
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
            _directionChangeIntervalMultiplier
        );

        return bait;
    }
}
