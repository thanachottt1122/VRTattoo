using UnityEngine;

public enum CustomerVoiceLine { Greeting, Pain, Ecstatic, Happy, Neutral, Angry }

// A set of voice clips for one kind of customer (male / female / one character). Every slot can
// hold any number of clips, one is picked at random. Empty slots simply play nothing.
[CreateAssetMenu(menuName = "VRTattoo/Customer Voice Set", fileName = "Voice_")]
public class CustomerVoiceSet : ScriptableObject
{
    [Tooltip("When the customer walks in through the door")]
    public AudioClip[] greeting;

    [Tooltip("Now and then while being tattooed")]
    public AudioClip[] pain;

    [Header("Reaction to the finished tattoo")]
    public AudioClip[] ecstatic;
    public AudioClip[] happy;
    public AudioClip[] neutral;
    public AudioClip[] angry;

    [Header("Playback")]
    [Range(0f, 1f)] public float volume = 1f;

    [Tooltip("Random pitch range so repeated clips sound less alike")]
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    public AudioClip[] Clips(CustomerVoiceLine line) => line switch
    {
        CustomerVoiceLine.Greeting => greeting,
        CustomerVoiceLine.Pain => pain,
        CustomerVoiceLine.Ecstatic => ecstatic,
        CustomerVoiceLine.Happy => happy,
        CustomerVoiceLine.Neutral => neutral,
        _ => angry,
    };

    // A random non-null clip from the slot, avoiding `previous` when there is a choice. Null if empty.
    public AudioClip Pick(CustomerVoiceLine line, AudioClip previous = null)
    {
        var clips = Clips(line);
        if (clips == null || clips.Length == 0)
            return null;

        int count = 0;
        foreach (var clip in clips)
            if (clip != null && clip != previous)
                count++;
        if (count == 0)
            return previous != null && System.Array.IndexOf(clips, previous) >= 0 ? previous : null;

        int pick = Random.Range(0, count);
        foreach (var clip in clips)
            if (clip != null && clip != previous && pick-- == 0)
                return clip;
        return null;
    }

    public static CustomerVoiceLine LineFor(CustomerMood mood) => mood switch
    {
        CustomerMood.Ecstatic => CustomerVoiceLine.Ecstatic,
        CustomerMood.Happy => CustomerVoiceLine.Happy,
        CustomerMood.Neutral => CustomerVoiceLine.Neutral,
        _ => CustomerVoiceLine.Angry,
    };
}
