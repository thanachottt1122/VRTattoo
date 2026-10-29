using System;
using UnityEngine;

// How happy a customer is with the finished tattoo. Shown as an emote (CustomerEmote) and voiced
// (CustomerVoice) after the job is submitted.
public enum CustomerMood { Angry, Neutral, Happy, Ecstatic }

// Score ranges for each mood (quality 0-100). Below `neutral` the customer is angry.
[Serializable]
public struct CustomerMoodThresholds
{
    [Tooltip("This quality and above: very happy (heart eyes)")]
    [Range(0, 100)] public int ecstatic;

    [Tooltip("This quality and above: smiling")]
    [Range(0, 100)] public int happy;

    [Tooltip("This quality and above: neutral / not quite satisfied. Below it: angry")]
    [Range(0, 100)] public int neutral;

    public static CustomerMoodThresholds Default => new CustomerMoodThresholds { ecstatic = 80, happy = 55, neutral = 30 };

    public CustomerMood For(int quality)
    {
        if (quality >= ecstatic) return CustomerMood.Ecstatic;
        if (quality >= happy) return CustomerMood.Happy;
        if (quality >= neutral) return CustomerMood.Neutral;
        return CustomerMood.Angry;
    }
}
