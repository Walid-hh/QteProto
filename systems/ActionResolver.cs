using Godot;
using System;

public partial class ActionResolver : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	public void ResolveBasicAttack(ICombatant attacker, ICombatant defender)
	{
		if (attacker == null || defender == null)
		{
			GD.PrintErr("Cannot resolve attack without valid attacker and defender.");
			return;
		}

		int rawDamage = Math.Max(1, attacker.Attack - defender.Defense);
		defender.TakeDamage(rawDamage);

		string attackerName = string.IsNullOrEmpty(attacker.CombatantName) ? "Attacker" : attacker.CombatantName;
		string defenderName = string.IsNullOrEmpty(defender.CombatantName) ? "Defender" : defender.CombatantName;

		GD.Print($"{attackerName} attacks {defenderName} for {rawDamage} damage.");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
