using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item
{
    [SerializeField]Item_Stats stats = null;
    public Dictionary<string, int> baseStats = new Dictionary<string, int>();
    public Dictionary<string, int> currentStatsFiltered = new Dictionary<string, int>();
    public string name;
    public string description;
    public Sprite pixelArt;
    public Sprite image;
    public string rarityName;
    public string type;
    public int rarityTier;

    public Item(Item_Stats stats) 
    {
        this.stats = stats;
        checkValues();
    }
    private void Start()
    {
        
    }
    private void Update()
    {
        
    }
    public void checkValues()
    {
        this.name = stats.name;
        this.description = stats.description;
        this.pixelArt = stats.pixelArt;
        this.image = stats.image;
        this.rarityName = stats.rarity.ToString();
        this.rarityTier = (int)stats.rarity;
        this.type = stats.itemType.ToString();

        baseStats.Add("health", stats.health * rarityTier);
        baseStats.Add("healthRegeneration", stats.healthRegenerationRate * rarityTier);
        baseStats.Add("mana", stats.mana * rarityTier);
        baseStats.Add("manaRegeneration", stats.healthRegenerationRate * rarityTier);
        baseStats.Add("attackBonus", stats.attackBonus);
        baseStats.Add("shield", stats.shield * rarityTier);
        baseStats.Add("criticalChance", stats.criticalChance);

        foreach (var item in baseStats)
        {
            if (item.Value != 0)
            {
                currentStatsFiltered.Add(item.Key, item.Value);
            }
        }
    }
}
