using System;
using Godot;

public class AttackCommand : ICombatCommand
{
	public string Id { get; } = "attack.basic";
	public string DisplayName { get; } = "Attack";
	public bool EndsTurn => true;

	public bool CanExecute(BattleContext context)
	{
		if (context == null)
		{
			return false;
		}

		return context.ActiveActor != null
			&& context.ActionResolver != null
			&& context.SelectedTarget != null
			&& context.SelectedTarget.IsAlive();
	}

	public void Execute(BattleContext context)
	{
		if (!CanExecute(context))
		{
			GD.Print("Attack command cannot execute without a valid target.");
			return;
		}

		context.ActionResolver.ResolveBasicAttack(context.ActiveActor, context.SelectedTarget);
	}

	public bool CanUndo(BattleContext context)
	{
		return false;
	}

	public void Undo(BattleContext context)
	{
		GD.Print("Attack command cannot be undone after execution.");
	}
}
