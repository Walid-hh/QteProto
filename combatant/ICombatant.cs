using Godot;
using System;

public interface ICombatant {
    
    string CombatantName { get; }
    int Health { get; set; }
    int Attack { get; }
    int Speed { get; }
    int Defense { get; }
    int Luck { get; }
    CombatantStats CombatantStats { get; }
    BattleSide Side { get; }
    void TakeDamage(int amount);
    void TakeTurn();
    bool IsAlive();
    void SetBattleSide(BattleSide side);
} 
