using Godot;

public partial class Shooting : Node3D, IUpgradable
{
    private const float BulletLifetime = 3f;

    [Export]
    public uint Damages = 5;

    private uint _damagesBonus = 0;

    [Export]
    public float AttackSpeed = 1;

    private float _attackSpeedBonus = 0;

    public uint TotalDamages => Damages + _damagesBonus;

    public float TotalAttackSpeed => AttackSpeed + _attackSpeedBonus;

    [Export]
    public float BulletSpeed = 2;

    private float _bulletSpeedBonus = 0;

    public float TotalBulletSpeed => BulletSpeed + _bulletSpeedBonus;

    private PackedScene _bulletPrefab;

    private GameManager _gameManager;

    private Timer _timer;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _bulletPrefab = (PackedScene)GD.Load("res://Prefabs/Powerups/bullet.tscn");

        _timer = GetNode<Timer>("Timer");
        _timer.WaitTime = 1f / TotalAttackSpeed;
        _timer.Timeout += Shoot;
        _timer.Start();
    }

    private void Shoot()
    {
        _timer.Start();

        var nearestEnemy = _gameManager.GetNearestEnemy();
        if (nearestEnemy == null) return;

        var bullet = _bulletPrefab.Instantiate<RigidBody3D>();
        bullet.LinearVelocity = TotalBulletSpeed * (nearestEnemy.GlobalPosition - GlobalPosition).Normalized();
        bullet.BodyEntered += (body) => OnBodyEntered(bullet, body);
        GetTree().CurrentScene.AddChild(bullet);
        bullet.GlobalPosition = GlobalPosition + new Vector3(0, 0.5f, 0);

        // Bullets that miss despawn instead of flying forever.
        GetTree().CreateTimer(BulletLifetime).Timeout += () =>
        {
            if (IsInstanceValid(bullet)) bullet.QueueFree();
        };
    }

    private void OnBodyEntered(RigidBody3D bullet, Node body)
    {
        bullet.QueueFree();
        if (body is not Enemy enemy) return;
        enemy.TakeDamages(TotalDamages);
    }

    public void Upgrade(Powerup powerup)
    {
        switch (powerup.Type)
        {
            case PowerupType.ShootingDamages:
                _damagesBonus += (uint)powerup.Value;
                break;
            case PowerupType.ShootingAttackSpeed:
                _attackSpeedBonus += powerup.Value;
                _timer.WaitTime = 1f / TotalAttackSpeed;
                break;
            case PowerupType.ShootingBulletSpeed:
                _bulletSpeedBonus += powerup.Value;
                break;
            default: break;
        }
    }
}
