using Godot;
using System;

public partial class Player : CharacterBody3D, IUpgradable
{
    private const ulong InvulnerabilityMsec = 300;

    [Export]
    public float Speed { get; private set; } = 5.0f;

    public uint MaxLifepoints { get; private set; } = 200;

    public uint Lifepoints { get; private set; }

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    private GameManager _gameManager;
    private Node3D _visual;
    private AnimationTree _animationTree;
    private ulong _invulnerableUntilMsec;

    public override void _Ready()
    {
        base._Ready();

        Lifepoints = MaxLifepoints;

        _gameManager = GetNode<GameManager>("/root/GameManager");
        _gameManager.Player = this;

        _visual = GetNode<Node3D>("Visual");
        _animationTree = GetNode<AnimationTree>("AnimationTree");
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        // Get the input direction and handle the movement/deceleration.
        Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();

        velocity.Y = 0;
        _animationTree.Set("parameters/walking/blend_amount", velocity.LimitLength(1).Length());
        if (Mathf.IsZeroApprox(velocity.Length())) return;

        _visual.LookAt(Position + 10 * velocity, Vector3.Up, true);
    }

    public void TakeDamages(uint damages)
    {
        // Brief invulnerability window so a swarm can't burst the player down in one frame.
        ulong now = Time.GetTicksMsec();
        if (now < _invulnerableUntilMsec) return;
        _invulnerableUntilMsec = now + InvulnerabilityMsec;

        Lifepoints -= Math.Min(damages, Lifepoints);
        if (Lifepoints == 0) _gameManager.PlayerDied();
    }

    internal void Heal(uint amount)
    {
        Lifepoints = Math.Min(Lifepoints + amount, MaxLifepoints);
    }

    public void Upgrade(Powerup powerup)
    {
        switch (powerup.Type)
        {
            case PowerupType.Lifepoints:
                MaxLifepoints += (uint)powerup.Value;
                Heal((uint)powerup.Value);
                break;
            case PowerupType.Speed:
                Speed += powerup.Value;
                break;
            default: break;
        }
    }
}
