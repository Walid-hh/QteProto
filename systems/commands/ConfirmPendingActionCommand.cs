using Godot;

public class ConfirmPendingActionCommand : ICombatCommand
{
	public string Id { get; } = "action.confirm_pending";
	public string DisplayName { get; } = "Confirm";
	public bool EndsTurn => true;

	public bool CanExecute(BattleContext context)
	{
		if (context == null)
		{
			return false;
		}

		if (context.PendingAction == null)
		{
			return false;
		}

		return context.PendingAction.CanExecute(context);
	}

	public void Execute(BattleContext context)
	{
		if (!CanExecute(context))
		{
			GD.Print("ConfirmPendingActionCommand cannot execute without a valid pending action and target.");
			return;
		}

		var pending = context.PendingAction;
		context.PendingAction = null;
		context.CommandToResolve = pending;
		context.CommandManager.ClearHistory();
		context.InitializeMenuState(BattleMenuState.ActionMenu);
	}

	public bool CanUndo(BattleContext context)
	{
		return false;
	}

	public void Undo(BattleContext context)
	{
	}
}
