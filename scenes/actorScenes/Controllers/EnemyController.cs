using Godot;
using MMOTest.scripts.Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using MMOTest.Backend;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

public partial class EnemyController : AbstractController
{
    //model node paths
    private AbstractModel Model;
    private AnimationPlayer ModelAnimation;
    private Node3D CastingPoint;
    
    // Sensors
    private RayCastPair NorthRayCasts;
    private RayCastPair NorthEastRayCasts;
    private RayCastPair EastRayCasts;
    private RayCastPair SouthEastRayCasts;
    private RayCastPair SouthRayCasts;
    private RayCastPair SouthWestRayCasts;
    private RayCastPair WestRayCasts;
    private RayCastPair NorthWestRayCasts;

    // Used if position needs to be halted for animations
    bool IsPositionLocked = false;

    // Physics "constants"
    Vector3 gravity = (Vector3)ProjectSettings.GetSetting("physics/3d/default_gravity_vector") * (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

    private float airDensity = 1.293f; // Should probably come from project settings
    private float waterDensity = 998f; // Should probably come from project settings
    private float fluidDensity; // 1.293 for air, 998 for water.

    private float dragCoefficient = 1.15f; // Ranges between 1.0 and 1.3 for a person. https://en.wikipedia.org/wiki/Drag_coefficient
    // Should be like 1, 0.9. High for game feel for now.
    private float staticFrictionCoefficient = 2f; // best guess. Leather on wood, with the grain is 0.61. So a leather shoe on stone or dirt? idk. a bit higher. // Needs a setter and surface detection of some kind? so we can switch to an ice coefficient? Also, kinetic friction at some point? https://www.engineeringtoolbox.com/friction-coefficients-d_778.html 
    private float kineticFrictionCoefficient = 1.9f; // Also a guess 

    // Physics exports
    // These are about the character itself
    [Export] private float mass = 20000f; // kilograms. Default for all characters.
    [Export] private float realMass = 20000f; // Total mass with armor, TODO: set this from stats or something somehow
    [Export] private float modelProjectedArea = 11.2f; // Used for air resistance, https://www.ntnu.no/documents/11601816/b830b9bd-d256-42c4-9dfc-5726c0ae3596
    [Export] private float modelVolume = 4.8f; // cubic meters, surprisingly.

    // These are about the desired behavior of the character
    [Export] private float jumpHeight = 12f; // 3 meters is way higher than people can jump but 0.3 feels bad because you cant pick up your legs to clear a fence.
    [Export] private float maxSprintSpeed = 15f; // 10 meters a second. Ballpark of olympic athletes in 200m races.
    [Export] private float maxSwimSpeed = 4.5f; // 2.2 Meters per second. https://www.wired.com/2012/08/olympics-physics-swimming/
    [Export] private float maxFlySpeed = 15f; //Terminal velocity?  // People cant fly. Should be zero, but having it at 10 helps a bit.The air thrust force needs to be calculated differently. Drag doesnt make sense.
    [Export] private float angleOfAttack = (float)(Math.PI / 4f); // How much you glide while falling;

    // These are derived from the exported values and are actually used in calculations
    private float jumpVelocity;
    private float jumpForce;
    private float angleOfAttackThrustForce; // Derived from angle of attack and drag
    private float airThrustForce = 100000; // Force generated to fly, like a bird.
    private float swimThrustForce; // Force generated to swim.
    private float thrustForce; // total Thrust force

    // Forces
    // Internal Force
    private Vector2 inputDirection = new Vector2(); // Captures movement 'key presses'
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
    [Export] private float jetPackForce = 240000f; // Arbitrary. Might turn into a calculation later. Give it a better handle
    [Export] private float propulsionThrustForce = 60000f;

    public bool WaterFlag { get; private set; }

    public float BulletSpeed = 60f;
    private double shootCoolDown = 1f;
    public float WeaponMaxRange = 60f;
    public float WeaponMinRange = 5f;
    public List<Actor> actorsInDetectionRange = new List<Actor>();
    public List<Actor> quarries = new List<Actor>();
    public Actor ActiveQuarry = null;
    public Vector3 focusPosition;
    [Export] private float personalSpaceRadius = 3.5f;
    public bool BumpedFlag = false;
    RayCast3D lineOfSight;
    [Export] private float maxFollowDistance = 100f;
    private bool shootOnCoolDownFlag = false;

    public override void _EnterTree()
    {
        
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        RayCast3D UpperRayCast = GetNode<RayCast3D>("NorthRayCasts/UpperRayCast3D");
        RayCast3D lowerRayCast = GetNode<RayCast3D>("NorthRayCasts/LowerRayCast3D");
        NorthRayCasts = new RayCastPair(UpperRayCast, lowerRayCast, personalSpaceRadius);

        UpperRayCast = GetNode<RayCast3D>("NorthEastRayCasts/UpperRayCast3D");
        lowerRayCast = GetNode<RayCast3D>("NorthEastRayCasts/LowerRayCast3D");
        NorthEastRayCasts = new RayCastPair(UpperRayCast, lowerRayCast, personalSpaceRadius);
        
        UpperRayCast = GetNode<RayCast3D>("EastRayCasts/UpperRayCast3D");
        lowerRayCast = GetNode<RayCast3D>("EastRayCasts/LowerRayCast3D");
        EastRayCasts = new RayCastPair(UpperRayCast, lowerRayCast, personalSpaceRadius);

        UpperRayCast = GetNode<RayCast3D>("SouthEastRayCasts/UpperRayCast3D");
        lowerRayCast = GetNode<RayCast3D>("SouthEastRayCasts/LowerRayCast3D");
        SouthEastRayCasts = new RayCastPair(UpperRayCast, lowerRayCast, personalSpaceRadius);

        UpperRayCast = GetNode<RayCast3D>("SouthRayCasts/UpperRayCast3D");
        lowerRayCast = GetNode<RayCast3D>("SouthRayCasts/LowerRayCast3D");
        SouthRayCasts = new RayCastPair(UpperRayCast, lowerRayCast, personalSpaceRadius);

        UpperRayCast = GetNode<RayCast3D>("SouthWestRayCasts/UpperRayCast3D");
        lowerRayCast = GetNode<RayCast3D>("SouthWestRayCasts/LowerRayCast3D");
        SouthWestRayCasts = new RayCastPair(UpperRayCast, lowerRayCast, personalSpaceRadius);

        UpperRayCast = GetNode<RayCast3D>("WestRayCasts/UpperRayCast3D");
        lowerRayCast = GetNode<RayCast3D>("WestRayCasts/LowerRayCast3D");
        WestRayCasts = new RayCastPair(UpperRayCast, lowerRayCast, personalSpaceRadius);

        UpperRayCast = GetNode<RayCast3D>("NorthWestRayCasts/UpperRayCast3D");
        lowerRayCast = GetNode<RayCast3D>("NorthWestRayCasts/LowerRayCast3D");
        NorthWestRayCasts = new RayCastPair(UpperRayCast, lowerRayCast, personalSpaceRadius);

        lineOfSight = GetNode<RayCast3D>("LineOfSight");

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
        this.CastingPoint = this.Model.GetCastPoint();
        Model.AttachController(this);
        this.focusPosition = this.Model.GlobalPosition;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {

    }

    public override void _PhysicsProcess(double delta)
    {
        if (Model == null) { return; }
        if (Model.IsQueuedForDeletion()) {
            QueueFree();
            return;
        }


        if (Model.IsDead) 
        {
            totalForceVector = Vector3.Zero;
            externalForceVector = Vector3.Zero;
            internalForceVector = Vector3.Zero;
            return;
        }

        this.GlobalPosition = this.Model.GlobalPosition;

        // Do logic for updating data about quarries
        foreach (Actor actor in actorsInDetectionRange)
        {
            Vector3 actorRelativeDirection = (actor.ClientModelReference.GlobalPosition - this.GlobalPosition).Normalized() * Basis;
            if (actorRelativeDirection.Z < 0) // Is in front of us
            {
                bool los = checkLineOfSight(actor);
                if (los)
                {
                    quarries.Add(actor);
                }
            }
            else if (actor != ActiveQuarry)
            {
                quarries.Remove(actor); // They might not be in here. But why check before removing.
            }
        }

        // Do logic for setting activeQuarry if any
        if (ActiveQuarry == null && quarries.Count > 0)
        {
            Actor newQuarry = quarries[0];
            float nearestDistance = float.MaxValue;
            foreach (Actor quarry in quarries)
            {
                float distance = (quarry.ClientModelReference.GlobalPosition - this.GlobalPosition).Length();
                if (distance <= nearestDistance)
                {
                    newQuarry = quarry;
                    nearestDistance = distance;
                }
            }
            ActiveQuarry = newQuarry;
        }

        // Try to set null
        if (ActiveQuarry != null)
        {
            DeathManager deathManager = DeathManager.GetInstance();
            bool isDead = deathManager.IsActorDead(ActiveQuarry.ActorID);
            if (isDead)
            {
                ActiveQuarry = null;
            }

            float activeQuarryDistance = (ActiveQuarry.ClientModelReference.GlobalPosition - this.GlobalPosition).Length(); ;
            if (activeQuarryDistance > maxFollowDistance)
            {
                ActiveQuarry = null;
            }
        }

        // Do logic for setting focusPosition
        if (ActiveQuarry != null)
        {
            this.focusPosition = ActiveQuarry.ClientModelReference.GlobalPosition;
            lineOfSight.TargetPosition = (ActiveQuarry.ClientModelReference.GlobalPosition - this.GlobalPosition) + new Vector3(0, 4, 0);
            lineOfSight.ForceRaycastUpdate();
        }
        else
        {
            if (((focusPosition - this.GlobalPosition).Length() < 4f) || BumpedFlag) // Within range of focus position, or bumped into something
            {
                RNGSingleton rng = RNGSingleton.GetInstance();
                this.focusPosition = new Vector3(this.GlobalPosition.X + ((rng.GetRandomNumber() % 200f) - 100f), 0f, this.GlobalPosition.Z + ((rng.GetRandomNumber() % 200f) - 100f));
            }
        }

        // Rotate the model to look at focus position
        Transform3D tr = Model.Transform.LookingAt(-focusPosition);
        this.Model.Transform = this.Model.Transform.InterpolateWith(tr, turnSpeed * (float)delta);
        //this.Transform = this.Model.Transform;

        // Figure out what "buttons" the enemy is pressing
        
        // Move away from other objects
        Basis modelBasis = this.Model.Transform.Basis;
        inputDirection = new Vector2();
        BumpedFlag = false;
        inputDirection += NorthRayCasts.GetInputVector(modelBasis);
        inputDirection += NorthEastRayCasts.GetInputVector(modelBasis);
        inputDirection += EastRayCasts.GetInputVector(modelBasis);
        inputDirection += SouthEastRayCasts.GetInputVector(modelBasis);
        inputDirection += SouthRayCasts.GetInputVector(modelBasis);
        inputDirection += SouthWestRayCasts.GetInputVector(modelBasis);
        inputDirection += WestRayCasts.GetInputVector(modelBasis);
        inputDirection += NorthWestRayCasts.GetInputVector(modelBasis);
        inputDirection = inputDirection.Normalized();

        if (inputDirection.Length() > 0)
        {
            BumpedFlag = true;
        }

        // Move towards target
        if (ActiveQuarry != null)
        {
            float distanceToQuarry = (ActiveQuarry.ClientModelReference.GlobalPosition - this.GlobalPosition).Length();
            if (distanceToQuarry > this.WeaponMaxRange)
            {
                inputDirection += new Vector2(0, -1); // Forward
            }
            else if (distanceToQuarry < this.WeaponMinRange)
            {
                inputDirection += new Vector2(0, 1); // Backward
            }
            else // In range, strafe for line of sight, shoot if you have it.
            {
                bool ActiveQuarryLOS = checkLineOfSight(ActiveQuarry);
                if (ActiveQuarryLOS)
                {
                    Shoot();
                }
                else // Strafe
                {
                    Vector3 colliderNormal = lineOfSight.GetCollisionNormal();
                    Vector2 horizontalColliderNormal = new Vector2(colliderNormal.X, colliderNormal.Z).Normalized();
                    Vector2 rotateLeft = horizontalColliderNormal.Rotated((float)Math.PI / 4f);
                    Vector2 rotateRight = horizontalColliderNormal.Rotated(-(float)Math.PI / 4f);
                    Vector3 lineOfSightVector = lineOfSight.TargetPosition - lineOfSight.GlobalPosition;
                    Vector2 horizontalLineOfSightVector = new Vector2(lineOfSightVector.X, lineOfSightVector.Z).Normalized();
                    float strafingRightDotProduct = horizontalLineOfSightVector.Dot(rotateLeft);
                    float strafingLeftDotProduct = horizontalLineOfSightVector.Dot(rotateRight);

                    Vector3 localizedInputDirection;
                    if (Math.Abs(strafingLeftDotProduct) < Math.Abs(strafingRightDotProduct))
                    {
                        localizedInputDirection = new Vector3(rotateRight.X, 0, rotateRight.Y);
                    }
                    else
                    {
                        localizedInputDirection = new Vector3(rotateLeft.X, 0, rotateLeft.Y);
                    }

                    localizedInputDirection = localizedInputDirection * Basis;
                    localizedInputDirection.Y = 0;
                    localizedInputDirection = localizedInputDirection.Normalized();
                    inputDirection += new Vector2(localizedInputDirection.X, localizedInputDirection.Z);
                }
            }
        }
        else
        {
            inputDirection += new Vector2(0, -1); // Forward, toward destination
        }

        //convert to a vector that points the right way in the world.
        Vector3 transformedInputDirectionVector = Transform.Basis * new Vector3(inputDirection.X, 0, inputDirection.Y).Normalized();

        // Update locomotion animation with the new button key inputs
        locomotionBlendPositionVector = locomotionBlendPositionVector.MoveToward(inputDirection, locomotionBlendPositionSpeed * (float)delta);
        this.Model.GetAnimationTree().Set("parameters/Blended/Locomotion/blend_position", new Vector2(locomotionBlendPositionVector.X, -Math.Abs(locomotionBlendPositionVector.Y)));

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
            
            //// Capped here by physical human limitations
            //if (runningForceVector.Length() > 3000) // Magic number from here: https://www.ncbi.nlm.nih.gov/pmc/articles/PMC9446565/
            //{
            //    runningForceVector = runningForceVector.Normalized() * 3000;
            //}

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

        //// Jump
        //if (Input.IsActionJustPressed("jump_dodge") & Model.IsOnFloor())
        //{
        //    internalForceVector += Vector3.Up * jumpForce; // Vertical jump
        //    // internalForceVector += (Transform.Basis * new Vector3(inputDirection.X, 1, inputDirection.Y).Normalized()) * jumpForce; // Angled Jump
        //}

        //if (Input.IsActionPressed("movementAbility"))
        //{
        //    // Change logic to reflect kit. For now -> mage hover
        //    Vector3 hoverForce = Vector3.Up * jetPackForce;
        //    // Vector3 hoverForce = (Transform.Basis * new Vector3(inputDirection.X, 1, inputDirection.Y).Normalized()) * jetPackForce; // Force is angled according to input.
        //    internalForceVector += hoverForce;
            
        //    // Do fuel and stuff
        //}

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

    public bool checkLineOfSight(Actor actor)
    {
        lineOfSight.TargetPosition = ToLocal(actor.ClientModelReference.GlobalPosition) + new Vector3(0, 4, 0);
        lineOfSight.ForceRaycastUpdate();
        
        if (lineOfSight.IsColliding())
        {
            GodotObject collider = lineOfSight.GetCollider();
            // If collider is not actor
            if (collider != (GodotObject)actor.ClientModelReference && collider != (GodotObject)actor.ClientModelReference)
            {
                return false;
            }

            return true;
        }
        else
        {
            // error?
            return false;
        }
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

    public void ShootCoolDownOver()
    {
        shootOnCoolDownFlag = false;
    }

    public void Shoot()
    {
        if (shootOnCoolDownFlag)
        {
            return;
        }

        shootOnCoolDownFlag = true;

        Timer t = new Timer();
        t.Timeout += ShootCoolDownOver;
        t.Timeout += t.QueueFree;
        this.AddChild(t);
        t.Start(shootCoolDown);

        // Rough Math
        Vector3 target = ActiveQuarry.ClientModelReference.GlobalPosition + new Vector3(0, 4, 0);
        float HorizontalDistance = (ActiveQuarry.ClientModelReference.GlobalPosition - CastingPoint.GlobalPosition).Length();
        float travelTime = HorizontalDistance / BulletSpeed;
        float bulletDrop = (((float)ProjectSettings.GetSetting("physics/3d/default_gravity") * travelTime) / 2f) * travelTime;
        target += new Vector3(0, -bulletDrop, 0);
        Vector3 lead = ActiveQuarry.ClientModelReference.Velocity * travelTime;
        target += lead;
        RNGSingleton rng = RNGSingleton.GetInstance();
        Vector3 inaccuracy = new Vector3((rng.GetRandomNumber() % 2) - 1f, (rng.GetRandomNumber() % 2) - 1f, (rng.GetRandomNumber() % 2) - 1f);
        target += inaccuracy;

        Vector3 fireballTrajectory = (target - CastingPoint.GlobalPosition).Normalized() * BulletSpeed;

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

    private class RayCastPair
    {
        public RayCast3D upper;
        public RayCast3D lower;
        public float personalSpaceRadius;
        private bool upperCollided;
        private bool lowerCollided;

        public RayCastPair(RayCast3D upper, RayCast3D lower, float personalSpaceRadius)
        {
            this.upper = upper;
            this.lower = lower;
            this.personalSpaceRadius = personalSpaceRadius;
        }

        public Vector2 GetInputVector(Basis basis)
        {
            Vector3 inputVector = new Vector3();
            upperCollided = false;
            lowerCollided = false;
            Vector3 upperTarget = getCollision(upper);
            Vector3 lowerTarget = getCollision(lower);

            if (personalSpaceRadius > 1000f)
            {
                throw new Exception("personalSpaceRadius cant be greater than ray collision's 'max distance' of 1000");
            }

            if ((upperTarget - upper.GlobalPosition).Length() < this.personalSpaceRadius)
            {
                upperCollided = true;
                inputVector += getCollisionNormal(upper);
            }

            if ((lowerTarget - lower.GlobalPosition).Length() < this.personalSpaceRadius)
            {
                lowerCollided = true;
                inputVector += getCollisionNormal(lower);
            }

            inputVector = inputVector.Normalized();
            inputVector = (inputVector * basis).Normalized();
            return new Vector2(inputVector.X, inputVector.Z);
        }

        public bool GetJump()
        {
            if (lowerCollided && !upperCollided)
            {
                return true;
            }

            return false;
        }

        private Vector3 getCollision(RayCast3D ray)
        {
            Vector3 target;
            if (ray.IsColliding())
            {
                target = ray.GetCollisionPoint();
            }
            else
            {
                Vector3 unitVector = (ray.TargetPosition - ray.Position);
                unitVector = ray.Transform.Basis * unitVector;
                target = unitVector.Normalized() * 1000f; // Arbitrary "Max distance"
            }
            return target;
        }

        private Vector3 getCollisionNormal(RayCast3D ray)
        {
            Vector3 collisionNormal = ray.GetCollisionNormal();

            // Get cardinal vector closest to normal?
            collisionNormal.Y = 0f;
            return collisionNormal;
        }
    }

    public void _on_player_detector_body_entered(Node3D body)
    {  
        ConcreteModel concreteModel = body as ConcreteModel;
        if (concreteModel != null)
        {
            GD.Print("added to potential target list");

            int actorID = concreteModel.ActorID;
            Actor actor = ActorManager.GetInstance().GetActor(actorID);
            if (actor != null)
            {
                int teamAffiliation = (int)actor.stats.GetStat(StatType.CTF_TEAM);
                if (teamAffiliation == 0)
                {
                    actorsInDetectionRange.Add(actor);
                }
            }
        }
    }

    public void _on_player_detector_body_exited(Node3D body)
    {
        ConcreteModel concreteModel = body as ConcreteModel;
        if (concreteModel != null)
        {

            GD.Print("removed from potential target list");

            int actorID = concreteModel.ActorID;
            Actor actor = ActorManager.GetInstance().GetActor(actorID);
            if (actor != null)
            {
                actorsInDetectionRange.Remove(actor);
            }
        }
    }
}
