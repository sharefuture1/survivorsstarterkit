using Godot;

public enum EnemyClass
{
    Minion = 0,
    Warrior = 1,
    Archer = 2,
    Mage = 3,

    Boss = 100
}

public partial class Enemy : AnimatableBody3D
{
    private const float AttackRange = 2f;

    [Export]
    public int Lifepoints { get; set; } = 10;

    [Export]
    public uint Damages { get; set; } = 5;

    [Export]
    public float MovementSpeed = 4;

    [Export]
    public int Experience { get; private set; } = 1;

    public bool IsDead { get; private set; }

    private GameManager _gameManager;
    private GpuParticles3D _damageParticles;
    private Timer _attackCooldown;

    [Signal]
    public delegate void OnEnemyHitEventHandler(Enemy enemy, int damages);

    [Signal]
    public delegate void DiedEventHandler(Enemy enemy);

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/GameManager");

        _attackCooldown = GetNode<Timer>("AttackCooldown");
        _damageParticles = GetNode<GpuParticles3D>("DamageParticles");
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        var player = _gameManager.Player;
        if (player == null) return;

        Vector3 toPlayer = player.GlobalPosition - GlobalPosition;
        toPlayer.Y = 0;

        GlobalPosition += (float)delta * MovementSpeed * toPlayer.LimitLength();

        float distanceSq = toPlayer.LengthSquared();
        if (distanceSq > 0.0001f)
            LookAt(GlobalPosition + toPlayer, Vector3.Up, true);

        if (_attackCooldown.TimeLeft <= 0 && distanceSq <= AttackRange * AttackRange)
        {
            _attackCooldown.Start();
            player.TakeDamages(Damages);
        }
    }

    internal void TakeDamages(uint damages = 1)
    {
        if (IsDead) return;

        _damageParticles.Restart();
        EmitSignal(SignalName.OnEnemyHit, this, (int)damages);

        Lifepoints -= (int)damages;
        if (Lifepoints > 0) return;

        IsDead = true;
        EmitSignal(SignalName.Died, this);
        QueueFree();
    }
}
