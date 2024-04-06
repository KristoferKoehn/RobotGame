extends CharacterBody3D
@onready var pivot = $pivot
@onready var tilt = $pivot/tilt
@onready var body = $foot_l
@onready var antree = $AnimationTree
@onready var gunarm = $foot_l/Skeleton3D/BoneAttachment3D/playerarm
@onready var aimer = $foot_l/Skeleton3D/BoneAttachment3D/aimer
@onready var raycaster = $pivot/tilt/RayCast3D

@onready var handspawner = $foot_l/Skeleton3D/BoneAttachment3D/playerarm/spawner
@onready var missilespawner = $foot_l/Skeleton3D/BoneAttachment3D/missile/spawner
@onready var rifle = $foot_l/Skeleton3D/BoneAttachment3D/playerarm/rifle
@onready var machinegun = $foot_l/Skeleton3D/BoneAttachment3D/playerarm/machinegun
@onready var shotgun = $foot_l/Skeleton3D/BoneAttachment3D/playerarm/shotgun
@onready var bazooka = $foot_l/Skeleton3D/BoneAttachment3D/playerarm/bazooka

var strafe_dir = [0.0,0.0]
var input_dir = [0.0,0.0]


func _ready():
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
# Called when the node enters the scene tree for the first time.
func _unhandled_input(event):
	if Input.is_action_just_pressed("ui_cancel"):
		get_tree().quit()
	if event is InputEventMouseMotion:
		pivot.rotate_y(-event.relative.x*0.005)
		tilt.rotate_x(event.relative.y*0.005)
		tilt.rotation.x = clamp(tilt.rotation.x, -PI/4, PI/4)
		
func movement():
	body.rotation.y = lerp_angle(body.rotation.y,pivot.rotation.y,0.4)
	input_dir = Input.get_vector("left", "right", "up", "down")
	var direction = (transform.basis * Vector3(input_dir[0], 0, input_dir[1])).normalized()
	strafe_dir[0] = move_toward(strafe_dir[0],input_dir[0],0.2)
	strafe_dir[1] = move_toward(strafe_dir[1],input_dir[1],0.2)
	direction = direction.rotated(Vector3.UP,pivot.rotation.y)
	velocity.x = move_toward(velocity.x,(-direction.x * 10.0),4)
	velocity.z = move_toward(velocity.z,(-direction.z * 10.0),4)
	antree.set("parameters/movement/blend_position",Vector2(strafe_dir[0],-strafe_dir[1]))
	if is_on_floor():
		if Input.is_action_just_pressed("space"):
			velocity.y = 20.0
	else:
		velocity.y = move_toward(velocity.y,-5,1.0)

	
# Called every frame. 'delta' is the elapsed time since the previous frame.
func _physics_process(delta):
	movement()
	
	move_and_slide()
