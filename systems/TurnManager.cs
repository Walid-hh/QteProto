using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class TurnManager : Node
{
	private List<ICombatant> combatantTurnOrder;
	private ICombatant currentCombatant;
	private BattleContext battleContext;
	private int currentTurnIndex;
	private bool turnCycleActive;

	public event Action<ICombatant> TurnStarted;
	public event Action<ICombatant> TurnEnded;

	public bool IsTurnCycleActive => turnCycleActive;

	public override void _Ready()
	{
		combatantTurnOrder = new List<ICombatant>();
	}

	public void SetBattleContext(BattleContext context)
	{
		if (battleContext != null)
		{
			battleContext.RosterChanged -= HandleBattleRosterChanged;
		}

		battleContext = context;

		if (battleContext != null)
		{
			battleContext.RosterChanged += HandleBattleRosterChanged;
			UpdateTurnOrder();
		}
		else
		{
			combatantTurnOrder = new List<ICombatant>();
		}
	}

	public void RegisterCombatant(ICombatant combatant)
	{
		if (battleContext == null)
		{
			GD.PrintErr("TurnManager requires a BattleContext before registering combatants.");
			return;
		}

		if (!battleContext.RegisterCombatant(combatant))
		{
			return;
		}

		UpdateTurnOrder();
	}

	public void UnregisterCombatant(ICombatant combatant)
	{
		if (battleContext == null)
		{
			GD.PrintErr("TurnManager requires a BattleContext before unregistering combatants.");
			return;
		}

		if (!battleContext.UnregisterCombatant(combatant))
		{
			return;
		}

		UpdateTurnOrder();
	}

	public void StartTurnCycle()
	{
		if (battleContext == null)
		{
			GD.PrintErr("TurnManager requires a BattleContext before starting turns.");
			return;
		}

		UpdateTurnOrder();
		if (combatantTurnOrder.Count == 0)
		{
			GD.Print("TurnManager: No combatants available to start turn cycle.");
			return;
		}

		turnCycleActive = true;
		currentTurnIndex = 0;
	}

	public bool BeginTurn()
	{
		return StartCurrentTurn();
	}

	public void AbortBattle()
	{
		if (battleContext != null)
		{
			battleContext.ResetTurnState();
		}

		turnCycleActive = false;
		currentCombatant = null;
		combatantTurnOrder.Clear();
	}

	public override void _Process(double delta)
	{
	}

	public void CompleteCurrentTurn()
	{
		if (!turnCycleActive)
		{
			return;
		}

		EndCurrentTurn();
	}

	private bool StartCurrentTurn()
	{
		if (!turnCycleActive || combatantTurnOrder.Count == 0)
		{
			return false;
		}

		if (battleContext == null)
		{
			GD.PrintErr("TurnManager cannot start a turn without a BattleContext.");
			return false;
		}

		if (currentTurnIndex >= combatantTurnOrder.Count)
		{
			currentTurnIndex = 0;
		}

		int attempts = combatantTurnOrder.Count;
		currentCombatant = combatantTurnOrder[currentTurnIndex];

		while (attempts-- > 0 && (currentCombatant == null || !currentCombatant.IsAlive()))
		{
			AdvanceTurnIndex();
			currentCombatant = combatantTurnOrder[currentTurnIndex];
		}

		if (currentCombatant == null || !currentCombatant.IsAlive())
		{
			return false;
		}

		TurnStarted?.Invoke(currentCombatant);

		if (battleContext.ActionResolver == null)
		{
			GD.PrintErr("TurnManager requires an ActionResolver before starting turns.");
			currentCombatant.TakeTurn();
			return false;
		}

		battleContext.ActiveActor = currentCombatant;
		battleContext.SelectedTarget = null;

		if (currentCombatant is ICommandSource)
		{
			// Command handling delegated to BattleCommandController.
		}
		else
		{
			currentCombatant.TakeTurn();
			return false;
		}

		return true;
	}

	private void EndCurrentTurn()
	{
		TurnEnded?.Invoke(currentCombatant);

		battleContext?.ResetTurnState();
		currentCombatant = null;

		UpdateTurnOrder();

		if (!turnCycleActive || combatantTurnOrder.Count == 0)
		{
			turnCycleActive = false;
			return;
		}

		AdvanceTurnIndex();
	}

	private void AdvanceTurnIndex()
	{
		if (combatantTurnOrder.Count == 0)
		{
			currentTurnIndex = 0;
			return;
		}

		currentTurnIndex++;
		if (currentTurnIndex >= combatantTurnOrder.Count)
		{
			currentTurnIndex = 0;
		}
	}

	public void UpdateTurnOrder()
	{
		if (battleContext == null)
		{
			combatantTurnOrder = new List<ICombatant>();
			return;
		}

		combatantTurnOrder = battleContext.Combatants
			.Where(combatant => combatant != null && combatant.IsAlive())
			.OrderByDescending(combatant => combatant.Speed)
			.ToList();
	}

	private void HandleBattleRosterChanged()
	{
		UpdateTurnOrder();

		if (!turnCycleActive || combatantTurnOrder.Count != 0)
		{
			return;
		}

		turnCycleActive = false;
		currentCombatant = null;
	}

}
