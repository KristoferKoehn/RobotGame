extends Node3D
@onready var aim = $aim
@onready var missile = $backweapons/missile
@onready var railgun = $backweapons/railcannon
@onready var cannon = $backweapons/shouldercannon
@onready var backspawner = $backweapons/spawner
@onready var antree = $AnimationTree

@onready var  rifle = $shouldercuff_l/Skeleton3D/right/hand/rifle
@onready var smg =$shouldercuff_l/Skeleton3D/right/hand/smg
@onready var zook = $shouldercuff_l/Skeleton3D/right/hand/zook
@onready var handspawner = $shouldercuff_l/Skeleton3D/right/hand/spawner

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass
