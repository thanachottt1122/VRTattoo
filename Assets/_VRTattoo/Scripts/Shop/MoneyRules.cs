using System;
using UnityEngine;
using Random = UnityEngine.Random;

// What a finished tattoo pays: the design's price scaled by quality and the fast bonus,
// plus a random tip when the customer is thrilled (3 stars).
[Serializable]
public struct MoneyRules
{
    [Tooltip("Price of an easy / medium / hard design at quality 100 ($)")]
    public int easyPrice;
    public int mediumPrice;
    public int hardPrice;

    [Tooltip("Tip as a random fraction of the design's price, only for 3 stars")]
    public Vector2 tipFraction;

    public static MoneyRules Default => new MoneyRules
    {
        easyPrice = 50,
        mediumPrice = 80,
        hardPrice = 120,
        tipFraction = new Vector2(0.1f, 0.3f),
    };

    public int Price(StencilDifficulty difficulty) => difficulty switch
    {
        StencilDifficulty.Medium => mediumPrice,
        StencilDifficulty.Hard => hardPrice,
        _ => easyPrice,
    };

    // quality 0-100, fastBonus 0..0.25 (PatienceScoring.Bonus)
    public int Pay(StencilDifficulty difficulty, int quality, float fastBonus) =>
        Mathf.RoundToInt(Price(difficulty) * Mathf.Clamp01(quality / 100f) * (1f + fastBonus));

    public int Tip(StencilDifficulty difficulty) =>
        Mathf.Max(1, Mathf.RoundToInt(Price(difficulty) * Random.Range(tipFraction.x, tipFraction.y)));
}
