using Godot;
using System;
using System.Collections.Generic;
using BSM = BattleStateMachine;

public partial class BattleManager : Node
{
	private ActionResolver actionResolver;
	private TurnManager turnManager;
	private UIController uiController;
	private BattleCommandController commandController;

	private BSM battleStateMachine;
	internal BattleContext battleContext;

	public BattleContext Context => battleContext;
	public TurnManager TurnManager => turnManager;
	public BattleCommandController CommandController => commandController;
	public override void _Ready()
	{
		GD.Print("BattleManager is ready.");
		SetupStateMachine();
	}

	public void SetupStateMachine()
	{
		battleStateMachine = new BSM();
		battleStateMachine.Name = "BattleStateMachine";
		AddChild(battleStateMachine);
		BSM.State battleStartState = new BSM.StateBattleStart(battleStateMachine, this);
		BSM.State turnPhaseState = new BSM.StateTurnPhase(battleStateMachine, this);
		BSM.State commandPhaseState = new BSM.StateCommandPhase(battleStateMachine, this);
		BSM.State actionPhaseState = new BSM.StateActionPhase(battleStateMachine, this);
		BSM.State battleEndState = new BSM.StateBattleEnd(battleStateMachine, this);
		battleStateMachine.Transitions = new Dictionary<BSM.State, Dictionary<BSM.Events, BSM.State>>
		{
			[battleStartState] = new Dictionary<BSM.Events, BSM.State>
			{
				[BSM.Events.FINISHED] = turnPhaseState,
			},
			[turnPhaseState] = new Dictionary<BSM.Events, BSM.State>
			{
				[BSM.Events.FINISHED] = commandPhaseState,
				[BSM.Events.BATTLE_END] = battleEndState,
			},
			[commandPhaseState] = new Dictionary<BSM.Events, BSM.State>
			{
				[BSM.Events.FINISHED] = actionPhaseState,
				[BSM.Events.BATTLE_END] = battleEndState,
			},
			[actionPhaseState] = new Dictionary<BSM.Events, BSM.State>
			{
				[BSM.Events.FINISHED] = turnPhaseState,
				[BSM.Events.BATTLE_END] = battleEndState,
			}
		};

		battleStateMachine.Activate(battleStartState);
	}

	public BattleContext CreateBattleContext()
	{
		DisposeBattleContext();
		battleContext = new BattleContext(this);
		SetupSystems(battleContext);
		return battleContext;
	}

	public void DisposeBattleContext()
	{
		if (battleContext == null)
		{
			return;
		}

		turnManager?.SetBattleContext(null);
		commandController?.SetBattleContext(null);
		uiController?.SetCommandController(null);
		battleContext = null;
	}

	public void SetupSystems(BattleContext context)
	{
		if (context == null)
		{
			throw new ArgumentNullException(nameof(context));
		}

		actionResolver = GetNodeOrNull<ActionResolver>("ActionResolver");
		if (actionResolver == null)
		{
			actionResolver = new ActionResolver
			{
				Name = "ActionResolver"
			};
			AddChild(actionResolver);
		}

		uiController = GetNodeOrNull<UIController>("UIController");
		if (uiController == null)
		{
			uiController = new UIController
			{
				Name = "UIController"
			};
			AddChild(uiController);
		}

		turnManager = GetNodeOrNull<TurnManager>("TurnManager");
		if (turnManager == null)
		{
			turnManager = new TurnManager
			{
				Name = "TurnManager"
			};
			AddChild(turnManager);
		}

		commandController = GetNodeOrNull<BattleCommandController>("BattleCommandController");
		if (commandController == null)
		{
			commandController = new BattleCommandController
			{
				Name = "BattleCommandController"
			};
			AddChild(commandController);
		}

		context.ConfigureManagers(actionResolver, turnManager, uiController);
		turnManager.SetBattleContext(context);
		commandController.SetBattleContext(context);
		uiController.SetCommandController(commandController);
	}

	public void LoadCombatants(IEnumerable<ICombatant> players, IEnumerable<ICombatant> mobs)
	{
		if (battleContext == null)
		{
			GD.PrintErr("BattleManager cannot load combatants without an active BattleContext.");
			return;
		}

		battleContext.SetCombatants(players, mobs);
		turnManager?.UpdateTurnOrder();
	}

	public void HandleFleeAttempt(ICombatant combatant)
	{
		if (battleContext == null)
		{
			GD.PrintErr("BattleManager cannot process flee attempts without an active BattleContext.");
			return;
		}

		string combatantName = string.IsNullOrEmpty(combatant?.CombatantName) ? "A combatant" : combatant.CombatantName;
		GD.Print($"{combatantName} fled the battle.");

		turnManager?.AbortBattle();
		DisposeBattleContext();
		battleStateMachine?.TriggerEvent(StateMachine.Events.BATTLE_END);
	}

}
