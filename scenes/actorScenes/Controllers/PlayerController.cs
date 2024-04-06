using Godot;
using MMOTest.scripts.Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using static Godot.TextServer;

public partial class PlayerController : AbstractController
{
    //model node paths
    private AbstractModel Model;
    private AnimationPlayer ModelAnimation;

    private Node3D CameraVerticalRotationPoint;
    private RayCast3D RayCast;
    private Camera3D Camera;
    private Node3D CastingPoint;

    [Export] private float HorizontalMouseSensitivity = 0.2f;
    [Export] private float VerticalMouseSensitivity = 0.2f;
    [Export] private float ZoomedMouseSensitivityModifier = 0.5f;
    [Export] private float CameraScrollSensitivity = 0.25f;
    [Export] private float CameraScrollMaxIn = 4f;
    [Export] private float CameraScrollMaxOut = 10f;
    [Export] private float CameraZValue = 4f;
    [Export] private float CameraDefaultFOV = 75;
    [Export] private float CameraZoomedFOV = 40;
    private bool isZoomed = false;

    // Used if position needs to be halted for animations
    bool IsPositionLocked = false;

    // Physics "constants"
    Vector3 gravity = (Vector3)ProjectSettings.GetSetting("physics/3d/default_gravity_vector") * (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

    private float airDensity = 1.293f; // Should probably come from project settings
    private float waterDensity = 998f; // Should probably come from project settings
    private float fluidDensity; // 1.293 for air, 998 for water.

    private float dragCoefficient = 1.15f; // Ranges between 1.0 and 1.3 for a person. https://en.wikipedia.org/wiki/Drag_coefficient
    // Should be like 1, 0.9. High for game feel for now.
    private float staticFrictionCoefficient = 1f; // best guess. Leather on wood, with the grain is 0.61. So a leather shoe on stone or dirt? idk. a bit higher. // Needs a setter and surface detection of some kind? so we can switch to an ice coefficient? Also, kinetic friction at some point? https://www.engineeringtoolbox.com/friction-coefficients-d_778.html 
    private float kineticFrictionCoefficient = 0.9f; // Also a guess 

    // Physics exports
    // These are about the character itself
    [Export] private float mass = 80f; // kilograms. Default for all characters.
    [Export] private float realMass = 100f; // Total mass with armor, TODO: set this from stats or something somehow
    [Export] private float modelProjectedArea = 0.7f; // Used for air resistance, https://www.ntnu.no/documents/11601816/b830b9bd-d256-42c4-9dfc-5726c0ae3596
    [Export] private float modelVolume = 0.075f; // cubic meters, surprisingly.

    // These are about the desired behavior of the character
    [Export] private float jumpHeight = 3f; // 3 meters is way higher than people can jump but 0.3 feels bad because you cant pick up your legs to clear a fence.
    [Export] private float maxSprintSpeed = 7f; // 10 meters a second. Ballpark of olympic athletes in 200m races.
    [Export] private float maxSwimSpeed = 2.2f; // 2.2 Meters per second. https://www.wired.com/2012/08/olympics-physics-swimming/
    [Export] private float maxFlySpeed = 7f; //Terminal velocity?  // People cant fly. Should be zero, but having it at 10 helps a bit.The air thrust force needs to be calculated differently. Drag doesnt make sense.
    [Export] private float angleOfAttack = (float)(Math.PI / 4f); // How much you glide while falling;

    // These are derived from the exported values and are actually used in calculations
    private float jumpVelocity;
    private float jumpForce;
    private float angleOfAttackThrustForce; // Derived from angle of attack and drag
    private float airThrustForce = 500; // Force generated to fly, like a bird.
    private float swimThrustForce; // Force generated to swim.
    private float thrustForce; // total Thrust force

    // Forces
    // Internal Force
    private Vector2 inputDirection = new Vector2(); // Captures movement key presses
    private Vector3 currentRunningSpeedVector = new Vector3(); // How the character is running in a given direction
    private Vector3 internalForceVector = new Vector3(); // Represents the force the character is exerting inorder to move in a certain direction.
    private Vector3 thrustForceVector = new Vector3(); // Force used to move in a given direction in a fluid (air, water) and not on the ground. Internal force = thrust force when off the ground.
    private Vector3 runningForceVector = new Vector3(); // thrustForce On ground counterpart. Used to move in a given direction on the ground.
    
    // External Force 
    private Vector3 externalForceVector = new Vector3(); // Stuff that pushes on the model. Explosions, Wind, etc. (Wind would technically impact drag too)

    // Resistance Forces
    private Vector3 dragForceVector = new Vector3(); // Friction while moving through a fluid (off the ground)
    private Vector3 frictionForceVector = new Vector3(); // Dampens movement of objects
    private float normalForce; // Used to calculate friction
    private Vector3 movementResistanceForceVector = new Vector3(); // Represents forces from the environment that slow the character down. Friction and drag.
    private Vector3 buoyantForceVector = new Vector3(); // Used to float in fluids (or sink)

    // Total
    private Vector3 totalForceVector = new Vector3(); // Accumulates all forces and applies it to the model.

    // This is an animation thing
    [Export] private float turnSpeed = 10f;
    private Vector2 locomotionBlendPositionVector = new Vector2();
    [Export]
    private float locomotionBlendPositionSpeed = 3.5f;

    // Jetpack stuff, mage only
    [Export] private float JetpackMaxFuel = 10f;
    private float JetPackFuel = 10f;
    [Export] private float JetpackFuelConsumptionRate = 0.1f;
    [Export] private float JetpackFuelRefillRate = 0.5f;
    [Export] private float jetPackForce = 1200f; // Arbitrary. Might turn into a calculation later. Give it a better handle
    [Export] private float propulsionThrustForce = 300f;

    public bool WaterFlag { get; private set; }

    public override void _EnterTree()
    {
        
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        CameraVerticalRotationPoint = GetNode<Node3D>("CameraVerticalRotationPoint");
        Camera = GetNode<Camera3D>("CameraVerticalRotationPoint/Camera3D");
        RayCast = GetNode<RayCast3D>("CameraVerticalRotationPoint/Camera3D/RayCast3D");

        // So this whole bit feels a little backwards. The forces are ultimately what moves the model, we are inferring what those forces should be from exported variables that are more intuitive to set.
        jumpVelocity = (float)Math.Sqrt(jumpHeight * 2 * -gravity.Y); // This one is a little bit of a doozy. Has to do with velocity averages and calculating time to max height
        jumpForce = jumpVelocity * mass * 60; // 60 for 60fps. This will be multiplied by delta later, so the 60 is here to cancel it out.
        swimThrustForce = 0.5f * waterDensity * maxSwimSpeed * maxSwimSpeed * dragCoefficient * modelProjectedArea; // Needs to be equal to drag at max speed.
        
        // TODO: Check before setting 
        SetWaterFlag(false);
    }

    public void AttachModel(AbstractModel model)
    {
        this.Model = model;
        this.ModelAnimation = model.GetNode<AnimationPlayer>("AnimationPlayer");
        this.CastingPoint = this.Model.GetCastPoint();
        Model.AttachController(this);
    }

    public void InitializeUI(int ActorID)
    {
        PlayerUI p = GD.Load<PackedScene>("res://scenes/PlayerUI/PlayerUI.tscn").Instantiate<PlayerUI>();
        p.initialize(ActorID);
        this.AddChild(p);
    }

    public override void _Input(InputEvent @event)
    {
        InputEventMouseMotion motion = @event as InputEventMouseMotion;
        if (motion != null && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            if (isZoomed)
            {
                this.RotateY(Mathf.DegToRad(-motion.Relative.X * HorizontalMouseSensitivity * ZoomedMouseSensitivityModifier));
                CameraVerticalRotationPoint.RotateX(Mathf.DegToRad(-motion.Relative.Y * VerticalMouseSensitivity * ZoomedMouseSensitivityModifier));
            }
            else
            {
                this.RotateY(Mathf.DegToRad(-motion.Relative.X * HorizontalMouseSensitivity));
                CameraVerticalRotationPoint.RotateX(Mathf.DegToRad(-motion.Relative.Y * VerticalMouseSensitivity));
            }

            if (CameraVerticalRotationPoint.Rotation.X > Mathf.DegToRad(90))
            {
                CameraVerticalRotationPoint.RotateX(Mathf.DegToRad(90) - CameraVerticalRotationPoint.Rotation.X);
            }
            if (CameraVerticalRotationPoint.Rotation.X < Mathf.DegToRad(-90))
            {
                CameraVerticalRotationPoint.RotateX(Mathf.DegToRad(-90) - CameraVerticalRotationPoint.Rotation.X);
            }
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("pause"))
        {
            if (Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Model == null) { return; }

        if (Model.IsDead) 
        {
            totalForceVector = Vector3.Zero;
            externalForceVector = Vector3.Zero;
            internalForceVector = Vector3.Zero;
            return;
        }

        this.GlobalPosition = this.Model.GlobalPosition;

        if (Input.IsActionJustPressed("aim"))
        {
            isZoomed = true;
            Tween tween = GetTree().CreateTween();
            tween.TweenProperty(this.Camera, "fov", this.CameraZoomedFOV, 0.3f).SetTrans(Tween.TransitionType.Sine);
            tween.Play();
        }

        if (Input.IsActionJustReleased("aim"))
        {
            isZoomed = false;
            Tween tween = GetTree().CreateTween();
            tween.TweenProperty(this.Camera, "fov", this.CameraDefaultFOV, 0.3f).SetTrans(Tween.TransitionType.Sine);
            tween.Play();
        }

        if (Input.IsActionJustPressed("zoomIn"))
        {
            if ((CameraZValue - CameraScrollSensitivity) > CameraScrollMaxIn)
            {
                CameraZValue -= CameraScrollSensitivity;
                Tween tween = GetTree().CreateTween();
                Vector3 targetValue = new Vector3(Camera.Position.X, Camera.Position.Y, this.CameraZValue);
                tween.TweenProperty(this.Camera, "position", targetValue, 0.1f).SetTrans(Tween.TransitionType.Sine);
                tween.Play();
            }
        }

        if (Input.IsActionJustPressed("zoomOut"))
        {
            if ((CameraZValue + CameraScrollSensitivity) < CameraScrollMaxOut)
            {
                CameraZValue += CameraScrollSensitivity;
                Tween tween = GetTree().CreateTween();
                Vector3 targetValue = new Vector3(Camera.Position.X, Camera.Position.Y, this.CameraZValue);
                tween.TweenProperty(this.Camera, "position", targetValue, 0.1f).SetTrans(Tween.TransitionType.Sine);
                tween.Play();
            }
        }

        if (Input.IsActionJustPressed("shoot_throw"))
        {
            Vector3 target;
            if (RayCast.IsColliding())
            {
                target = RayCast.GetCollisionPoint();
            }
            else
            {
                target = (RayCast.GlobalPosition - Camera.GlobalPosition).Normalized() * 1000f; // Arbitrary "Max distance"
            }
            Vector3 fireballTrajectory = (target - CastingPoint.GlobalPosition).Normalized();

            JObject job = new JObject
            {
                { "spell", "Fireball"},
                { "type", "cast"},
                { "posx", CastingPoint.GlobalPosition.X},
                { "posy", CastingPoint.GlobalPosition.Y},
                { "posz", CastingPoint.GlobalPosition.Z},
                { "velx", fireballTrajectory.X},
                { "vely", fireballTrajectory.Y},
                { "velz", fireballTrajectory.Z},
                { "SourceID", Model.GetActorID()}
            };
            MessageQueue.GetInstance().RpcId(1, "AddMessage", JsonConvert.SerializeObject(job));
            //this.GetParent<MainLevel>().RpcId(1,"SendMessage", job.ToString());
        }

        // Get button presses and convert to a vector that points the right way in the world.
        inputDirection = Input.GetVector("left", "right", "forward", "backward");
        Vector3 transformedInputDirectionVector = Transform.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y).Normalized();

        // Update locomotion animation with the new button key inputs
        locomotionBlendPositionVector = locomotionBlendPositionVector.MoveToward(inputDirection, locomotionBlendPositionSpeed * (float)delta);
        this.Model.GetAnimationTree().Set("parameters/movement/blend_position", new Vector2(locomotionBlendPositionVector.X, -locomotionBlendPositionVector.Y));
        // Rotate the model to reflect the change

        
        if (transformedInputDirectionVector != Vector3.Zero)
        {
            //Transform3D tr = Model.Transform.LookingAt(Model.GlobalPosition + -(transformedInputDirectionVector));
            Transform3D tr = Model.Transform.LookingAt(Model.GlobalPosition + (Transform.Basis * new Vector3(0,0,1)));
            this.Model.Transform = this.Model.Transform.InterpolateWith(tr, turnSpeed * (float)delta);
        }
        
        
        // Set internal force vector to a normalized version of the direction buttons are being pressed in the world. 
        internalForceVector = transformedInputDirectionVector.Normalized();
        
        if (Model.IsOnFloor())
        {
            // Logic for running
            currentRunningSpeedVector = internalForceVector * maxSprintSpeed;

            currentRunningSpeedVector.Y = Model.Velocity.Y; // Setting equal here means that the y component of the velocity won't be considered when calculating attempted acceleration
            Vector3 attemptedAcceleration = (currentRunningSpeedVector - Model.Velocity) / (float)delta;
            currentRunningSpeedVector.Y = 0; // Set back to zero after comparison
            runningForceVector = realMass * attemptedAcceleration;
            
            // Capped here by physical human limitations
            if (runningForceVector.Length() > 3000) // Magic number from here: https://www.ncbi.nlm.nih.gov/pmc/articles/PMC9446565/
            {
                runningForceVector = runningForceVector.Normalized() * 3000;
            }

            normalForce = Model.GetFloorNormal().Normalized().Y * (realMass * Math.Abs(gravity.Y));
            float maxStaticFrictionForce = staticFrictionCoefficient * normalForce;
            if (Math.Abs(runningForceVector.Length()) > maxStaticFrictionForce)
            {
                runningForceVector = runningForceVector.Normalized() * (kineticFrictionCoefficient * normalForce); // Max we can get from friction. Should "slip" from trying to run too fast on ice
            }

            // Helps stop
            if (currentRunningSpeedVector == Vector3.Zero && runningForceVector.Length() < (kineticFrictionCoefficient * normalForce))
            {
                runningForceVector = runningForceVector.Normalized() * (kineticFrictionCoefficient * normalForce); // We are stopping, but aren't using the full force available to us to do so.

                // Not DRY, but we are reversing the extra force to help stopping so our estimation here is correct
                float estimatedStoppingForce = realMass * Model.Velocity.Length() / (float)delta;
                float estimatedDotProduct = internalForceVector.Normalized().Dot(Model.Velocity.Normalized());
                estimatedDotProduct = (estimatedDotProduct / -2f) + 1.5f; // Magic numbers to convert range of [-1,1] to [1,2] but reversed
                estimatedStoppingForce /= estimatedDotProduct;
                
                if (estimatedStoppingForce < runningForceVector.Length())
                {
                    runningForceVector = runningForceVector.Normalized() * estimatedStoppingForce;
                }
            }

            internalForceVector = runningForceVector;
            movementResistanceForceVector = Vector3.Zero; // Turns out that friction and sliding is fully encapsulated by running force vector.
        }
        else
        {
            //Logic for moving through a fluid (air, water)
            
            // Drag equation
            dragForceVector = -Model.Velocity.Normalized() * (0.5f * fluidDensity * Model.Velocity.Length() * Model.Velocity.Length() * dragCoefficient * modelProjectedArea);
            
            // If there is input, convert some of the drag into horizontal "lift", according to the players angle of attack.
            if (inputDirection.Y != 0 || inputDirection.X != 0)
            {
                angleOfAttackThrustForce = Math.Abs(dragForceVector.Y) * (float)Math.Cos((double)angleOfAttack);
                dragForceVector.Y *= (float)Math.Sin(angleOfAttack);
            }
            
            movementResistanceForceVector = dragForceVector;

            Vector3 flySpeedCapDampeningVector = new Vector3();
            // Adjust thrust to enforce fly speed cap
            if (!WaterFlag)
            {
                // TODO: If thrust would accelerate us past our max fly speed, scale down thrust to match
                Vector2 horizontalModelVelocity = new Vector2(this.Model.Velocity.X, this.Model.Velocity.Z);
                if (horizontalModelVelocity.Length() > maxFlySpeed)
                {
                    Vector2 horizontalInternalForceVector = new Vector2(internalForceVector.X, internalForceVector.Z);
                    float dampenScalar = horizontalInternalForceVector.Normalized().Dot(horizontalModelVelocity.Normalized());
                    if (dampenScalar > 0)
                    {
                        flySpeedCapDampeningVector = (-internalForceVector * dampenScalar * (thrustForce + angleOfAttackThrustForce + propulsionThrustForce));
                    }
                }
            }

            // Because we aren't considering pressure, this works the same in air and water. 
            thrustForceVector = (internalForceVector * (thrustForce + angleOfAttackThrustForce + propulsionThrustForce)) + flySpeedCapDampeningVector;
            internalForceVector = thrustForceVector;
        }

        // Jump
        if (Input.IsActionJustPressed("jump_dodge") & Model.IsOnFloor())
        {
            internalForceVector += Vector3.Up * jumpForce; // Vertical jump
            // internalForceVector += (Transform.Basis * new Vector3(inputDirection.X, 1, inputDirection.Y).Normalized()) * jumpForce; // Angled Jump
        }

        if (Input.IsActionPressed("movementAbility"))
        {
            // Change logic to reflect kit. For now -> mage hover
            Vector3 hoverForce = Vector3.Up * jetPackForce;
            // Vector3 hoverForce = (Transform.Basis * new Vector3(inputDirection.X, 1, inputDirection.Y).Normalized()) * jetPackForce; // Force is angled according to input.
            internalForceVector += hoverForce;
            
            // Do fuel and stuff
        }

        buoyantForceVector = Vector3.Up * (-1 * fluidDensity * gravity.Y * modelVolume);
        externalForceVector += buoyantForceVector;

        // NOT PHYSICAL, ADDED FOR GAME FEEL
        // Internal force is scaled to be up to twice as strong if it is in a direction opposite current velocity.
        float dotProduct = internalForceVector.Normalized().Dot(Model.Velocity.Normalized());
        dotProduct = (dotProduct / -2f) + 1.5f; // Magic numbers to convert range of [-1,1] to [1,2] but reversed. Could be higher for game feel
        internalForceVector *= dotProduct;

        // Sum up all forces. Internal, external, and friction
        totalForceVector += internalForceVector;
        totalForceVector += externalForceVector;
        externalForceVector = Vector3.Zero; // Reset to begin accumulation again.
        totalForceVector += movementResistanceForceVector;

        // Update Model velocity. V_next = v_current + (time * acceleration). Acceleration = force / mass. Gravity is an acceleration value, so it is added to acceleration.
        Model.Velocity = Model.Velocity + ((float)delta * ((totalForceVector / realMass) + gravity));
        totalForceVector = Vector3.Zero; // Reset force to recalculate next frame.
        Model.MoveAndSlide(); // Move the model according to its new velocity.

        // Update animations for being in the air or not
        if (Model.IsOnFloor())
        {
            this.Model.GetAnimationTree().Set("parameters/Blended/Floating/blend_amount", 0f);
        }
        else
        {
            this.Model.GetAnimationTree().Set("parameters/Blended/Floating/blend_amount", 1f);
        }

        //if (Model.IsOnFloor())
        //{
        //    AppliedGravity = Vector3.Zero;
        //    CalculatedVelocity = new Vector3(CalculatedVelocity.X, 0, CalculatedVelocity.Z);
        //    if (JetPackFuel < JetpackMaxFuel)
        //    {
        //        JetPackFuel += JetpackFuelRefillRate;
        //    }
        //}
    }

    public override void ApplyImpulse(Vector3 vec)
    {
        externalForceVector += vec;
    }

    // Set water flag on enter
    public void SetWaterFlag(bool flag)
    {
        this.WaterFlag = flag;
        if (flag)
        {
            fluidDensity = waterDensity;
            thrustForce = swimThrustForce;
        }
        else
        {
            fluidDensity = airDensity;
            thrustForce = airThrustForce;
        }
    }
}
