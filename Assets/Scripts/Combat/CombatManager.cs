using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public enum TurnState
{
    PLAYER_TURN,
    PLAYER_UNIT_SELECTED,
    PLAYER_SKILL_SELECTED,
    PLAYER_TARGET_SELECTED,
    PLAYER_ACTION_PERFORMING,
    ENEMY_TURN,
    ENEMY_UNIT_SELECTED,
    ENEMY_SKILL_SELECTED,
    ENEMY_TARGET_SELECTED,
    ENEMY_ACTION_PERFORMING,
    DISENGAGING_UNITS,
}

public class CombatManager : MonoBehaviour
{
    [Header("Initilize Connections")]
    public CombatUIChannel uiChannel;
    public CombatEvent combatEvent;
    public ActionQueueManager actionQueueManager;

    [Header("Combat Positioning")]
    public Transform combatCenterLocation;
    public float unitPlacementOffset = 1f;
    public float perspectiveScaleFactor = 0.2f;
    public float perspectivePositionFactor = 0.5f;


    [Header("Ignore")]
    public int currentRound = 1;
    public TurnState CurrTurnState { get; private set; }
    public SkillSO selectedSkill;
    public Unit selectedUnit = null;
    public List<Unit> playerUnits;
    public List<Unit> enemyUnits;
    List<Unit> selectedTargets = new();
    bool wasPlayerTurn;

    public void StartCombat(List<Unit> units)
    {
        CurrTurnState = TurnState.PLAYER_TURN;
        selectedSkill = null;
        selectedUnit = null;
        selectedTargets.Clear();
        SplitUnits(units);
        ArrangeCharacters(playerUnits, -1, units.Count);
        ArrangeCharacters(enemyUnits, 1, units.Count);
        SetupEvents();
        Level_UI_Manager.instance.DisableUI();
    }

    private void SplitUnits(List<Unit> units)
    {
        playerUnits.Clear();
        enemyUnits.Clear();
        foreach (var unit in units)
        {
            if (unit.IsEnemy)
                enemyUnits.Add(unit);
            else
                playerUnits.Add(unit);
        }
    }

    private void ArrangeCharacters(List<Unit> unitList, int side, int totalCount)
    {
        float startX = combatCenterLocation.position.x + side * 5f; // Starting X position for left (-5) or right (+5)
        float startY = combatCenterLocation.position.y; // Starting Y position
        float currentX = startX;

        for (int i = 0; i < unitList.Count; i++)
        {
            Unit unit = unitList[i];
            SpriteRenderer spriteRenderer = unit.GetComponent<SpriteRenderer>();

            // Calculate target position with perspective adjustment
            float perspectiveFactor = Mathf.Lerp(1f, 0.6f, (totalCount - (float)i) / totalCount); // Smooth scaling
            float targetY = startY - i * perspectivePositionFactor * perspectiveFactor;

            // Calculate scale with perspective adjustment
            float scaleFactor = 1f - perspectiveScaleFactor * (1f - perspectiveFactor);

            // Apply position and scale
            unit.transform.position = new Vector3(currentX, targetY, 0);
            unit.transform.localScale = new Vector3(unit.transform.localScale.x > 0 ? scaleFactor : -scaleFactor, scaleFactor, 1f);

            currentX += side * (scaleFactor + unitPlacementOffset);

            spriteRenderer.sortingOrder = -(int)(targetY * 10);
        }
    }

    void SetupEvents()
    {
        if (MouseManager.Instance != null)
        {
            MouseManager.Instance.OnUnitHover += HandleUnitHover;
            MouseManager.Instance.OnUnitSelect += HandleUnitSelect;
        }

        if (CombatEvent.Instance != null)
        {
            CombatEvent.Instance.ActionsCompleted += HandleActionsCompleted;
            CombatEvent.Instance.UnitDeath += HandleOnDeath;
        }

        if (uiChannel != null)
        {
            uiChannel.SkillSelected += HandleSkillSelected;
            uiChannel.PotionSelected += HandlePotionSelected;
        }

        uiChannel.AssignEnemies(enemyUnits);
        uiChannel.OnTurnChange(CurrTurnState, null);
        uiChannel.OnNewTurn(currentRound);
    }

    void OnDisable()
    {
        if (MouseManager.Instance != null)
        {
            MouseManager.Instance.OnUnitHover -= HandleUnitHover;
            MouseManager.Instance.OnUnitSelect -= HandleUnitSelect;
        }

        if (CombatEvent.Instance != null)
        {
            CombatEvent.Instance.ActionsCompleted -= HandleActionsCompleted;
            CombatEvent.Instance.UnitDeath -= HandleOnDeath;
        }

        if (uiChannel != null)
        {
            uiChannel.SkillSelected -= HandleSkillSelected;
        }
    }

    void HandleActionsCompleted()
    {
        switch (CurrTurnState)
        {
            case TurnState.PLAYER_ACTION_PERFORMING:
                CurrTurnState = TurnState.DISENGAGING_UNITS;
                wasPlayerTurn = true;
                ActionQueueManager.EnqueueDisengageUnitsAction();
                break;
            case TurnState.ENEMY_ACTION_PERFORMING:
                CurrTurnState = TurnState.DISENGAGING_UNITS;
                wasPlayerTurn = false;
                ActionQueueManager.EnqueueDisengageUnitsAction();
                break;
            case TurnState.DISENGAGING_UNITS:
                AdvanceTurn();
                break;
        }

    }

    private void HandleOnDeath(Unit arg0, Unit arg1)
    {
        if (arg1.IsEnemy)
        {
            foreach (var c in enemyUnits)
            {
                if (c.IsDead)
                    c.gameObject.SetActive(false);
            }
            enemyUnits.RemoveAll(c => c.IsDead);
        }
        else
        {
            foreach (var c in playerUnits)
            {
                if (c.IsDead)
                    c.gameObject.SetActive(false);
            }
            playerUnits.RemoveAll(c => c.IsDead);
        }

        if (playerUnits.Count == 0 || enemyUnits.Count == 0)
            HandleGameEnd();
    }

    private void HandleGameEnd()
    {
        var playerWon = playerUnits.Count > 0;
        CombatEnemyManager.Instance.ClearEnemyList();
        CombatEnemyManager.Instance.OnCombatEnd(playerWon);
        playerUnits.Clear();
        enemyUnits.Clear();
        Scene_Manager.Instance.UnloadAdditiveScene();
        Level_UI_Manager.instance.EnableUI();
        Level_UI_Manager.instance.UpdateUI();
    }

    private void HandlePotionSelected(Potion potion)
    {
        selectedUnit.UsePotion(potion); 
        uiChannel.UpdatePotions();
        uiChannel.OnUpdateStats(selectedUnit);
    }

    public void HandleSkillSelected(SkillSO skill)
    {
        CurrTurnState = selectedUnit.IsEnemy ? TurnState.ENEMY_SKILL_SELECTED : TurnState.PLAYER_SKILL_SELECTED;

        selectedSkill = skill;

        switch (selectedSkill.targetType)
        {
            case TargetType.UNIT_ALL:
                List<Unit> allUnits = new(enemyUnits.Count + playerUnits.Count);
                allUnits.AddRange(enemyUnits.FindAll(u => u != selectedUnit));
                allUnits.AddRange(playerUnits.FindAll(u => u != selectedUnit));
                ExecuteSelectedSkill(allUnits);
                break;
            case TargetType.SELF:
                ExecuteSelectedSkill(selectedUnit);
                break;
            case TargetType.PLAYER_UNIT_ALL:
                ExecuteSelectedSkill(playerUnits.ToList().FindAll(u => u != selectedUnit));
                break;
            case TargetType.ENEMY_UNIT_ALL:
                ExecuteSelectedSkill(enemyUnits.ToList().FindAll(u => u != selectedUnit));
                break;
            case TargetType.SKIP:
                wasPlayerTurn = true;
                AdvanceTurn();
                break;
            default:
                CombatEvent.OnTurnChanged(this);
                break;
        }
    }

    void HandleUnitSelect(Unit unit)
    {
        switch (CurrTurnState)
        {
            case TurnState.PLAYER_TURN or TurnState.PLAYER_UNIT_SELECTED:
                HandleMainUnitSelect(unit);
                break;
            case TurnState.PLAYER_SKILL_SELECTED:
                HandleTargetUnitSelect(unit);
                break;
        }
    }

    void HandleMainUnitSelect(Unit unit)
    {
        if (!unit.IsEnemy && unit.HasTurn && unit != selectedUnit)
        {
            uiChannel.OnRemoveSelectors();
            uiChannel.OnUnitSelect(unit);
            CurrTurnState = TurnState.PLAYER_UNIT_SELECTED;
            CombatEvent.OnTurnChanged(this);
            selectedUnit = unit;
            uiChannel.OnAssignStats(unit);
            uiChannel.OnTurnChange(CurrTurnState, new List<Unit> { unit });
        }
    }

    public void HandleEnemyMainUnitSelect(Unit unit)
    {
        selectedUnit = unit;
        CurrTurnState = TurnState.ENEMY_UNIT_SELECTED;
        CombatEvent.OnTurnChanged(this);
    }

    public void HandleEnemyTargetUnitSelect(List<Unit> units)
    {
        selectedTargets.AddRange(units);
        // ExecuteSelectedSkill(selectedTargets);
        Debug.Log($"LIGMA: SELECTED {string.Join(", ", selectedTargets)}");
        Debug.Log($"LIGMA: SELECTED UNIT {selectedUnit}");

        ExecuteSelectedSkill(selectedTargets);
    }

    void HandleTargetUnitSelect(Unit unit)
    {
        switch (selectedSkill.targetType)
        {

            case TargetType.ENEMY_UNIT_SINGLE:
                if (unit.IsEnemy)
                    ExecuteSelectedSkill(unit);
                break;

            case TargetType.ENEMY_UNIT_MULTIPLE:
                if (unit.IsEnemy && !selectedTargets.Contains(unit))
                    selectedTargets.Add(unit);

                if (selectedTargets.Count < selectedSkill.numberOfTargets && selectedTargets.Count < enemyUnits.Count)
                    uiChannel.OnUnitSelect(unit);
                else
                    ExecuteSelectedSkill(selectedTargets);
                break;

            case TargetType.PLAYER_UNIT_SINGLE:
                if (!unit.IsEnemy && unit != selectedUnit)
                    ExecuteSelectedSkill(unit);
                break;

            case TargetType.PLAYER_UNIT_MULTIPLE:
                if (!unit.IsEnemy && unit != selectedUnit && !selectedTargets.Contains(unit))
                    selectedTargets.Add(unit);

                if (selectedTargets.Count < selectedSkill.numberOfTargets && selectedTargets.Count < playerUnits.Count)
                    uiChannel.OnUnitSelect(unit);
                else
                    ExecuteSelectedSkill(selectedTargets);
                break;

        }
    }

    void ExecuteSelectedSkill(List<Unit> units)
    {
        if (selectedSkill.targetType != TargetType.UNIT_ALL && selectedSkill.targetType != TargetType.SELF)
            ActionQueueManager.EnqueueEngageUnitsAction(selectedUnit, units, !selectedUnit.IsEnemy);

        if (selectedUnit.IsEnemy)
            Debug.Log("LIGMA: SKILL " + selectedSkill);

        ActionQueueManager.EnqueueSkillAction(selectedUnit, selectedSkill, units, !selectedUnit.IsEnemy);

        if (selectedUnit.IsEnemy)
            CurrTurnState = TurnState.ENEMY_ACTION_PERFORMING;
        else
        {
            uiChannel.OnRemoveSelectors();
            CurrTurnState = TurnState.PLAYER_ACTION_PERFORMING;
        }
        CombatEvent.OnTurnChanged(this);

    }

    void ExecuteSelectedSkill(Unit unit)
    {
        if (selectedUnit.IsEnemy)
            CurrTurnState = TurnState.ENEMY_ACTION_PERFORMING;
        else
        {
            uiChannel.OnRemoveSelectors();
            CurrTurnState = TurnState.PLAYER_ACTION_PERFORMING;
        }

        CombatEvent.OnTurnChanged(this);

        if (selectedSkill.targetType != TargetType.UNIT_ALL && selectedSkill.targetType != TargetType.SELF)
            ActionQueueManager.EnqueueEngageUnitsAction(selectedUnit, new List<Unit>() { unit }, !selectedUnit.IsEnemy);

        ActionQueueManager.EnqueueSkillAction(selectedUnit, selectedSkill, unit, !selectedUnit.IsEnemy);

    }

    void HandleUnitHover(Unit unit)
    {
        switch (CurrTurnState)
        {
            case TurnState.PLAYER_TURN or TurnState.PLAYER_UNIT_SELECTED:
                HandleMainUnitHover(unit);
                break;
            case TurnState.PLAYER_SKILL_SELECTED:
                HandleTargetUnitHover(unit);
                break;
        }
    }

    void HandleMainUnitHover(Unit unit)
    {
        if (!unit.IsEnemy && unit.HasTurn && unit != selectedUnit)
        {
            uiChannel.OnUnitHover(unit);
        }
    }

    void HandleTargetUnitHover(Unit unit)
    {
        switch (selectedSkill.targetType)
        {
            case TargetType.PLAYER_UNIT_SINGLE or TargetType.PLAYER_UNIT_MULTIPLE:
                if (!unit.IsEnemy && !selectedTargets.Contains(unit) && unit != selectedUnit)
                    uiChannel.OnUnitHover(unit);
                break;
            case TargetType.ENEMY_UNIT_SINGLE or TargetType.ENEMY_UNIT_MULTIPLE:
                if (unit.IsEnemy && !selectedTargets.Contains(unit))
                    uiChannel.OnUnitHover(unit);
                break;
        }
    }

    void HandleTurnChange()
    {
        switch (CurrTurnState)
        {
            case TurnState.ENEMY_TURN:
                var numOfAvailableEnemyUnits = enemyUnits.Count(u => u.HasTurn);

                if (numOfAvailableEnemyUnits == 0)
                {
                    enemyUnits.ForEach(u => u.HasTurn = true);
                }
                break;

            case TurnState.PLAYER_TURN:
                var numOfAvailablePlayerUnits = playerUnits.Count(u => u.HasTurn);

                if (numOfAvailablePlayerUnits == 0)
                {
                    currentRound++;
                    CombatEvent.OnNewTurn(this);
                    uiChannel.OnNewTurn(currentRound);

                    playerUnits.ForEach(u =>
                    {
                        u.HasTurn = true;
                        u.Heal(u.HealthRegen);
                        u.GainMana(u.ManaRegen);
                    });

                    enemyUnits.ForEach(u =>
                    {
                        u.Heal(u.HealthRegen);
                        u.GainMana(u.ManaRegen);
                    });

                }

                break;
        }
    }

    void AdvanceTurn()
    {
        if (wasPlayerTurn)
        {

            selectedUnit.HasTurn = false;
            selectedUnit = null;
            selectedTargets.Clear();
            selectedSkill = null;

            CurrTurnState = TurnState.ENEMY_TURN;
            uiChannel.OnTurnChange(CurrTurnState, playerUnits);
            CombatEvent.OnTurnChanged(this);
        }
        else
        {
            selectedUnit.HasTurn = false;
            selectedUnit = null;
            selectedTargets.Clear();
            selectedSkill = null;

            CurrTurnState = TurnState.PLAYER_TURN;
            uiChannel.OnTurnChange(CurrTurnState, playerUnits);
            CombatEvent.OnTurnChanged(this);
        }
        HandleTurnChange();
    }
}