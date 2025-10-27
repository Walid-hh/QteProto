using Godot;
using System;

[GlobalClass]
public partial class CombatantStats : Resource
{
    [Export]
    public int health { get; set; }
    [Export]
    public int attack { get; set; }
    [Export]
    public int speed { get; set; }
    [Export]
    public int defense { get; set; }
    [Export]
    public int luck { get; set; }
}