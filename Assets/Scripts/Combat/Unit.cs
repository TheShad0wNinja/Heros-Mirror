using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Unit : MonoBehaviour
{

    [Header("Unit Information")]
    [SerializeField] UnitSO unitData;
    [SerializeField] public List<SkillSO> skills;
    [SerializeField] public List<PassiveSO> passives;
    [SerializeField] public List<StatusEffect> activeEffects;
    [SerializeField] Transform rangedAttackSpawn;
    [SerializeField] GameObject projectile;
    [SerializeField] float rangeAttackDistance = 4f;
    [SerializeField] float rangeAttackDuration = 0.2f;

    public bool AnimationFinished { get; private set; }
    public new string name => unitData.name;
    public bool IsEnemy => unitData.isEnemy;
    public int Shield { get; set; }
    public bool HasTurn { get; set; }
    public bool IsDead { get; set; }
    // public int CurrentHealth { get ; private set; }
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; }
    public int HealthRegen { get; private set; }
    public int CurrentMana { get; private set; }
    public int MaxMana { get; private set; }
    public int ManaRegen { get; private set; }
    public float AttackBonus { get; set; }
    public float CritChance { get; set; }
    public Sprite image => unitData.pixelArt;
    public Sprite portrait => unitData.portrait;
    public bool IsFlipped => unitData.flipped;
    protected SpriteRenderer sr;
    protected Animator anim;

    public void InitilizeUnit(Character character)
    {
        var stats = character.currentStats;

        MaxHealth = stats["health"];
        CurrentHealth = character.currentHealth;
        HealthRegen = stats["healthRegeneration"];

        CurrentMana = stats["mana"];
        MaxMana = CurrentMana;
        ManaRegen = stats["manaRegeneration"];
        Shield = stats["shield"];
        CritChance = stats["criticalChance"] / 100;
        AttackBonus = stats["attackBonus"] / 100;
        unitData = character.stats;

        if (unitData.flipped)
            transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);

        HasTurn = true;
        AnimationFinished = true;

        SetupEvents();
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    void SetupEvents()
    {
        if (CombatEvent.Instance != null)
            CombatEvent.Instance.NewTurn += TriggerEffects;

        foreach (var passive in passives)
        {
            Debug.Log("Ligma");
            // passive.SubscribeToEvent(null);
        }
    }

    void OnDisable()
    {
        foreach (var passive in passives)
        {
            Debug.Log("BALLS");
            // passive.UnsubscribeToEvent(null);
        }

        if (CombatEvent.Instance != null)
            CombatEvent.Instance.NewTurn -= TriggerEffects;
    }


    public void TriggerEffects(CombatManager m)
    {
        foreach (var effect in activeEffects.ToList())
        {
            Debug.Log("TICKING EFFECT " + effect.name + " : " + effect.duration);
            ActionQueueManager.EnqueueStatusEffectAction(this, effect, StatusEffectAction.ActionType.TICK);

            if (effect.IsExpired)
            {
                Debug.Log("REMOVING " + effect.name);
                ActionQueueManager.EnqueueStatusEffectAction(this, effect, StatusEffectAction.ActionType.REMOVE);
            }
        }
    }

    public void AddStatusEffect(StatusEffect effect)
    {
        activeEffects.Add(effect);
    }

    public void RemoveStatusEffect(StatusEffect effect)
    {
        activeEffects.Remove(effect);
    }

    public void TakeDamage(Unit attacker, int damage)
    {
        int damageLeft = DamageShield(damage);
        if (damageLeft == 0)
            return;

        TakeRawDamage(attacker, damageLeft);
    }

    int DamageShield(int damage)
    {
        if (Shield > 0)
        {
            Shield = Math.Max(0, Shield - damage);
            damage = Math.Max(0, damage - Shield);
            CombatEvent.OnUnitShieldDamage(this, Shield);
            // if (Shield < damage)
            // {
            //     damage -= Shield;
            //     Shield = 0;
            // }
            // else
            // {
            //     CombatEvent.OnUnitShieldDamage(this, damage);
            //     Shield -= damage;
            //     damage = 0;
            // }
        }
        return damage;
    }
    public void TakeRawDamage(Unit attacker, int damage)
    {
        CurrentHealth -= damage;
        CombatEvent.OnUnitDamage(this, damage);
        if (CurrentHealth <= 0)
            Kill(attacker);
    }

    public void ConsumeMana(int amount)
    {
        CurrentMana = Math.Max(0, CurrentMana - amount);
    }

    public void GainMana(int amount)
    {
        CurrentMana = Math.Min(MaxMana, CurrentMana + amount);
    }

    public void UsePotion(Potion potion)
    {
        foreach(var k in potion.currentStatsFiltered)
        {
            switch(k.Key)
            {
                case "health":
                    Heal(k.Value);
                    break;
                case "healthRegeneration":
                    HealthRegen += k.Value;
                    break;
                case "mana":
                    GainMana(k.Value);
                    break;
                case "manaRegeneration":
                    ManaRegen += k.Value;
                    break;
                case "attackBonus":
                    AttackBonus += k.Value / 100;
                    break;
                case "shield":
                    Shield += k.Value;
                    break;
                case "criticalChance":
                    CritChance += k.Value / 100;
                    break;
            }
        }

        UI_Behaviour_Manager.Instance.RemovePotion(potion);
    }

    private void Kill(Unit attacker)
    {
        ActionQueueManager.EnqueueDeathAction(attacker, this);
    }

    public void Heal(int amount)
    {
        CombatEvent.OnUnitHeal(this, amount);
        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
    }

    public IEnumerator AnimateAction(SkillSO skill)
    {
        AnimationFinished = false;

        yield return Helper.WaitForAnimation(anim, 0, skill.animationName);

        anim.Play("Idle");

        AnimationFinished = true;
    }

    public void FinishAnimationEarly()
    {
        AnimationFinished = true;
    }

    public IEnumerator AnimateRangedProjectile()
    {
        AnimationFinished = false;

        Vector3 direction;
        if (IsEnemy)
            direction = new(-1, 0);
        else
            direction = new(1, 0);

        var p = Instantiate(projectile, this.transform.position, projectile.transform.rotation);

        yield return Helper.MoveObject(
            rangedAttackSpawn.position,
            rangedAttackSpawn.position + direction * rangeAttackDistance,
            p.transform, rangeAttackDuration);

        Destroy(p);

        AnimationFinished = true;
    }

    public IEnumerator AnimateHit()
    {
        AnimationFinished = false;

        yield return ParticleManager.TriggerHitEffect(gameObject);

        if (unitData.hasHitAnimation)
        {
            yield return Helper.WaitForAnimation(anim, 0, "Hit");
            anim.Play("Idle");
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 0, IsEnemy ? -16 : 16);
            yield return new WaitForSeconds(0.5f);
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }


        AnimationFinished = true;
        yield return null;
    }

    void OnMouseEnter()
    {
        // Debug.Log("Enter : " + this.name);
        MouseManager.Instance?.RaiseOnUnitHover(this);
    }

    void OnMouseDown()
    {
        MouseManager.Instance?.RaiseOnUnitSelect(this);
    }

    void OnMouseExit()
    {
        // Debug.Log("Exit : " + this.name);
        MouseManager.Instance?.RaiseOnUnitUnhover(this);
    }
}