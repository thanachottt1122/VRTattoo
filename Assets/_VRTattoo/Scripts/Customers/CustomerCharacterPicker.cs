using System.Collections.Generic;
using UnityEngine;

// On the Customer prefab: picks a random character model when the customer is created and hands it
// to AnimatorCustomerVisual. Never the same model as the previous customer, and if possible not one
// that is already in the shop.
[RequireComponent(typeof(AnimatorCustomerVisual))]
public class CustomerCharacterPicker : MonoBehaviour
{
    [Tooltip("Character prefabs (Prefabs/Characters) with a CustomerCharacter on the root")]
    public CustomerCharacter[] characters;

    static int lastIndex = -1;
    static readonly List<CustomerCharacterPicker> inShop = new List<CustomerCharacterPicker>();

    public int CharacterIndex { get; private set; } = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        lastIndex = -1;
        inShop.Clear();
    }

    void Awake()
    {
        if (characters == null || characters.Length == 0)
        {
            Debug.LogWarning($"[CustomerCharacterPicker] {name}: no characters assigned");
            return;
        }

        CharacterIndex = Pick();
        lastIndex = CharacterIndex;

        var character = Instantiate(characters[CharacterIndex], transform);
        character.name = characters[CharacterIndex].name;
        GetComponent<AnimatorCustomerVisual>().SetCharacter(character);
    }

    void OnEnable() => inShop.Add(this);
    void OnDisable() => inShop.Remove(this);

    int Pick()
    {
        var fresh = new List<int>();
        var notLast = new List<int>();
        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null || i == lastIndex)
                continue;
            notLast.Add(i);
            if (!inShop.Exists(p => p != this && p.CharacterIndex == i))
                fresh.Add(i);
        }

        var pool = fresh.Count > 0 ? fresh : notLast;
        return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : Mathf.Max(0, lastIndex);
    }
}
