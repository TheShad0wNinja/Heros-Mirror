using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Behaviour_Manager : MonoBehaviour
{
    public static UI_Behaviour_Manager Instance { get; private set; }

    public Dictionary<Potion, int> ownedPotions = new();

    public List<Character> ownedCharacters = new();

    public List<Item_Stats> ownedItemsStats = new();
    public List<Item> ownedItems = new();

    public List<Character> teamAssembleCharacters = new();
    public UnitSO defaultCharacter;

    public int gold = 10000;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Character newCharacter = new (defaultCharacter);
        AddCharacter(newCharacter);
        newCharacter = new (defaultCharacter);
        AddCharacter(newCharacter);
    }

    public void AddPotion(Potion potion)
    {
        if (ownedPotions.ContainsKey(potion))
        {
            ownedPotions[potion]++;
        }
        else
        {
            ownedPotions.Add(potion, 1);
        }
    }
    public void AddCharacter(Character character)
    {
        ownedCharacters.Add(character);
    }
    public void AddItem(Item item)
    {
        ownedItems.Add(item);
    }
    public void AddTeamCharacters(List<Character> characters)
    {
        teamAssembleCharacters = characters;
    }
    public void SetGold(int gold) 
    {
        this.gold = gold;
    }
    public int GetGold()
    {
        return gold;
    }
}