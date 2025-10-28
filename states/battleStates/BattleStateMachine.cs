using Godot;

public partial class BattleStateMachine : StateMachine
{
	private BattleManager activeManager;
	private bool turnCycleStarted;
	private BattleOutcome lastOutcome = BattleOutcome.Ongoing;
	private bool turnEndingCommandReceived;
	private bool controllerEventsAttached;

	public BattleOutcome LastOutcome => lastOutcome;

	private BattleContext CurrentContext => activeManager?.Context;
	private TurnManager CurrentTurnManager => activeManager?.TurnManager;
	private BattleCommandController CurrentCommandController => activeManager?.CommandController;

	private void Prepare(BattleManager manager)
	{
		if (controllerEventsAttached && activeManager?.CommandController != null)
		{
			activeManager.CommandController.TurnEndingCommandExecuted -= HandleTurnEndingCommandExecuted;
			controllerEventsAttached = false;
		}

		activeManager = manager;
		turnCycleStarted = false;
		lastOutcome = BattleOutcome.Ongoing;
		turnEndingCommandReceived = false;
		AttachControllerEvents();
	}

	private void AttachControllerEvents()
	{
		if (controllerEventsAttached || CurrentCommandController == null)
		{
			return;
		}

		CurrentCommandController.TurnEndingCommandExecuted += HandleTurnEndingCommandExecuted;
		controllerEventsAttached = true;
	}

	private void DetachControllerEvents()
	{
		if (!controllerEventsAttached || CurrentCommandController == null)
		{
			return;
		}

		CurrentCommandController.TurnEndingCommandExecuted -= HandleTurnEndingCommandExecuted;
		controllerEventsAttached = false;
	}

	private void HandleTurnEndingCommandExecuted(ICombatCommand command)
	{
	turnEndingCommandReceived = true;
	}

	private void EnsureContextExists()
	{
		if (activeManager == null)
		{
			return;
		}

		if (CurrentContext == null)
		{
			activeManager.CreateBattleContext();
		}
	}

	private bool EnsureTurnCycle()
	{
		if (CurrentTurnManager == null || CurrentContext == null)
		{
			return false;
		}

		if (!turnCycleStarted)
		{
			CurrentTurnManager.StartTurnCycle();
			turnCycleStarted = CurrentTurnManager.IsTurnCycleActive;
		}

		return turnCycleStarted;
	}

	private void BeginTurnPhase()
	{
		if (CurrentTurnManager == null || CurrentContext == null)
		{
			turnEndingCommandReceived = true;
			return;
		}

		bool began = CurrentTurnManager.BeginTurn();
		if (!began)
		{
			// No player command phase required (e.g., auto-acting combatant).
			turnEndingCommandReceived = true;
		}
		else
		{
			turnEndingCommandReceived = false;
		}
	}

	private void BeginCommandPhase()
	{
		if (CurrentContext == null)
		{
			turnEndingCommandReceived = true;
			return;
		}

		if (CurrentCommandController == null)
		{
			turnEndingCommandReceived = true;
			return;
		}

		if (CurrentContext.ActiveActor != null)
		{
			CurrentCommandController.BeginCommandPhase(CurrentContext.ActiveActor);
		}
		else
		{
			turnEndingCommandReceived = true;
		}
	}

	private void EndCommandPhase()
	{
		if (CurrentCommandController != null)
		{
			CurrentCommandController.EndCommandPhase();
		}
		turnEndingCommandReceived = false;
	}

	private void ResolveActionPhase()
	{
		if (CurrentContext == null)
		{
			return;
		}

		if (CurrentContext.CommandToResolve != null)
		{
			var command = CurrentContext.CommandToResolve;
			CurrentContext.CommandToResolve = null;
			command.Execute(CurrentContext);
		}
	}

	private void CompleteTurn()
	{
		if (CurrentTurnManager == null)
		{
			return;
		}

		CurrentTurnManager.CompleteCurrentTurn();
	}

	private BattleOutcome AssessOutcome()
	{
		if (CurrentContext == null)
		{
			return lastOutcome == BattleOutcome.Ongoing ? BattleOutcome.Draw : lastOutcome;
		}

		return CurrentContext.EvaluateBattleOutcome();
	}

	public partial class StateBattleStart : TypedState<BattleManager>
	{
		private readonly BattleStateMachine owner;

		public StateBattleStart(BattleStateMachine owner, BattleManager manager) : base("StateBattleStart", manager)
		{
			this.owner = owner;
		}

		public override void Enter()
		{
			owner.Prepare(Context);
			owner.EnsureContextExists();
		}

		public override Events Update(double delta)
		{
			return Events.FINISHED;
		}

		public override void Exit()
		{
			GD.Print("Exiting Battle Start State");
		}
	}

	public partial class StateTurnPhase : TypedState<BattleManager>
	{
		private readonly BattleStateMachine owner;
		private bool turnPrepared;

		public StateTurnPhase(BattleStateMachine owner, BattleManager manager) : base("StateTurnPhase", manager)
		{
			this.owner = owner;
		}

		public override void Enter()
		{
			GD.Print("Entering Turn Phase");
			turnPrepared = false;

			var outcome = owner.AssessOutcome();
			if (outcome != BattleOutcome.Ongoing)
			{
				owner.lastOutcome = outcome;
			}
		}

		public override Events Update(double delta)
		{
			var outcome = owner.AssessOutcome();
			if (outcome != BattleOutcome.Ongoing)
			{
				owner.lastOutcome = outcome;
				return Events.BATTLE_END;
			}

			if (!owner.EnsureTurnCycle())
			{
				return Events.BATTLE_END;
			}

			if (!turnPrepared)
			{
				owner.BeginTurnPhase();
				turnPrepared = true;
				return Events.FINISHED;
			}

			return Events.NONE;
		}

		public override void Exit()
		{
			GD.Print("Exiting Turn Phase");
		}
	}

	public partial class StateCommandPhase : TypedState<BattleManager>
	{
		private readonly BattleStateMachine owner;

		public StateCommandPhase(BattleStateMachine owner, BattleManager manager) : base("StateCommandPhase", manager)
		{
			this.owner = owner;
		}

		public override void Enter()
		{
			GD.Print("Entering Command Phase");
			if (owner.CurrentContext?.ActiveActor is ICommandSource)
			{
				owner.turnEndingCommandReceived = false;
				owner.BeginCommandPhase();
			}
			else
			{
				owner.turnEndingCommandReceived = true;
			}
		}

		public override Events Update(double delta)
		{
			var outcome = owner.AssessOutcome();
			if (outcome != BattleOutcome.Ongoing)
			{
				owner.lastOutcome = outcome;
				return Events.BATTLE_END;
			}

			if (owner.turnEndingCommandReceived)
			{
				return Events.FINISHED;
			}

			return Events.NONE;
		}

		public override void Exit()
		{
			GD.Print("Exiting Command Phase");
			owner.EndCommandPhase();
		}
	}

	public partial class StateActionPhase : TypedState<BattleManager>
	{
		private readonly BattleStateMachine owner;

		public StateActionPhase(BattleStateMachine owner, BattleManager manager) : base("StateActionPhase", manager)
		{
			this.owner = owner;
		}

		public override void Enter()
		{
			GD.Print("Entering Action Phase");
			owner.ResolveActionPhase();
			owner.CompleteTurn();

			var outcome = owner.AssessOutcome();
			if (outcome != BattleOutcome.Ongoing)
			{
				owner.lastOutcome = outcome;
			}
		}

		public override Events Update(double delta)
		{
			var outcome = owner.AssessOutcome();
			if (outcome != BattleOutcome.Ongoing)
			{
				owner.lastOutcome = outcome;
				return Events.BATTLE_END;
			}

			return Events.FINISHED;
		}

		public override void Exit()
		{
			GD.Print("Exiting Action Phase");
		}
	}

	public partial class StateBattleEnd : TypedState<BattleManager>
	{
		private readonly BattleStateMachine owner;

		public StateBattleEnd(BattleStateMachine owner, BattleManager manager) : base("StateBattleEnd", manager)
		{
			this.owner = owner;
		}

		public override void Enter()
		{
			GD.Print("Entering Battle End State");
			var outcome = owner.AssessOutcome();
			if (outcome != BattleOutcome.Ongoing)
			{
				owner.lastOutcome = outcome;
			}

			switch (owner.lastOutcome)
			{
				case BattleOutcome.Victory:
					GD.Print("Battle finished: Victory.");
					break;
				case BattleOutcome.Defeat:
					GD.Print("Battle finished: Defeat.");
					break;
				case BattleOutcome.Draw:
					GD.Print("Battle finished: Draw.");
					break;
				default:
					GD.Print("Battle finished.");
					break;
			}

			owner.DetachControllerEvents();
			owner.turnCycleStarted = false;
			if (Context.Context != null)
			{
				Context.DisposeBattleContext();
			}
		}

		public override Events Update(double delta)
		{
			return Events.NONE;
		}

		public override void Exit()
		{
			GD.Print("Exiting Battle End State");
		}
	}
}
