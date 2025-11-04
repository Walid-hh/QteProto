class_name Player extends CharacterBody2D
var player_name: String 
var level: int
var experience: int
var max_health: int
var attack: int
var defense: int
var speed: int
var player_info: Resource

func _ready() -> void:
    if player_info != null:
        player_name = player_info.player_name
        level = player_info.level
        experience = player_info.experience
        max_health = player_info.max_health
        attack = player_info.attack
        defense = player_info.defense
        speed = player_info.speed




