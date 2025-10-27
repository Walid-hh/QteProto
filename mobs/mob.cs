using Godot;
using System;

public partial class Mob : Node, ICombatant
{
    public string CombatantName { get; set; }
    public int Health { get; set; }
    public int Attack { get; private set; }
    public int Speed { get; private set; }
    public int Defense { get; private set; }
    public int Luck { get; private set; }
    public CombatantStats CombatantStats { get; private set; }
    public BattleSide Side { get; private set; } = BattleSide.Enemy;

    public Mob(CombatantStats stats, string name)
    {
        CombatantName = name;
        if (stats != null)
        {
            InitializeFromStats(stats);
        }
    }
    public void SetBattleSide(BattleSide side)
    {
        Side = side;
    }
    public override void _Ready()
    {
        if (CombatantStats != null)
        {
            InitializeFromStats(CombatantStats);
        }
    }
    public void SetCombatantStats(CombatantStats stats)
    {
        if (stats == null)
        {
            return;
        }

        InitializeFromStats(stats);
    }
    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health < 0)
        {
            Health = 0;
        }
    }
    private void InitializeFromStats(CombatantStats stats)
    {
        CombatantStats = stats;
        Health = stats.health;
        Attack = stats.attack;
        Speed = stats.speed;
        Defense = stats.defense;
        Luck = stats.luck;
    }

    public void TakeTurn()
    {
        // Implement mob's turn logic here
    }

    public bool IsAlive()
    {
        return Health > 0;
    }
}
