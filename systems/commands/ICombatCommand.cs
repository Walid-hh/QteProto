public interface ICombatCommand
{
	string Id { get; }
	string DisplayName { get; }
	bool EndsTurn { get; }
	bool CanExecute(BattleContext context);
	void Execute(BattleContext context);
	bool CanUndo(BattleContext context);
	void Undo(BattleContext context);
}
