using Godot;

public class FleeCommand : ICombatCommand
{
	public string Id { get; } = "action.flee";
	public string DisplayName { get; } = "Flee";
	public bool EndsTurn => true;

	public bool CanExecute(BattleContext context)
	{
		return context != null && context.ActiveActor != null && context.BattleManager != null;
	}

	public void Execute(BattleContext context)
	{
		if (!CanExecute(context))
		{
			GD.Print("FleeCommand cannot execute without an active actor and battle manager.");
			return;
		}

		context.CommandManager.ClearHistory();
		context.PendingAction = null;
		context.BattleManager.HandleFleeAttempt(context.ActiveActor);
	}

	public bool CanUndo(BattleContext context)
	{
		return false;
	}

	public void Undo(BattleContext context)
	{
	}
}
