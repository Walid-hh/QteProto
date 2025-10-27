using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using BSM = BattleStateMachine;

public partial class BattleManager : Node
{
	private ActionResolver actionResolver;
	private TurnManager turnManager;
	private UIController uiController;

	private BSM battleStateMachine;
	internal BattleContext battleContext;

	public BattleContext Context => battleContext;
	public override void _Ready()
	{
		GD.Print("BattleManager is ready.");
		SetupStateMachine();
	}

	public void BeginBattle()
	{
		if (battleContext == null)
		{
			GD.PrintErr("BattleManager cannot begin battle without an active BattleContext.");
			return;
		}

		turnManager.StartTurnCycle();
	}

	public void SetupStateMachine()
	{
		battleStateMachine = new BSM();
		battleStateMachine.Name = "BattleStateMachine";
		AddChild(battleStateMachine);
		BSM.State battleStartState = new BSM.StateBattleStart(this);
		BSM.State turnHandleState = new BSM.StateTurnHandler(this);
		battleStateMachine.Transitions = new Dictionary<BSM.State, Dictionary<BSM.Events, BSM.State>>
		{
			[battleStartState] = new Dictionary<BSM.Events, BSM.State>
			{
				[BSM.Events.FINISHED] = turnHandleState,
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

		context.ConfigureManagers(actionResolver, turnManager, uiController);
		turnManager.SetBattleContext(context);
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

}
