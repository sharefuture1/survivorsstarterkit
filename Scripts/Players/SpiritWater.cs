using Godot;
using System.Collections.Generic;

public partial class SpiritWater : Node3D, IUpgradable
{
    private const float MinCooldown = 1.5f;
    private const float MinSpawnRange = 1.5f;

    [Export]
    public uint Damages = 1;

    private uint _damagesBonus = 0;

    public uint TotalDamages => Damages + _damagesBonus;

    [Export]
    public float Duration = 4f;

    private float _durationBonus = 0;

    public float TotalDuration => Duration + _durationBonus;

    [Export]
    public float Cooldown = 5f;

    private float _cooldownBonus = 0;

    public float TotalCooldown => Mathf.Max(MinCooldown, Cooldown - _cooldownBonus);

    [Export]
    public float ProjectileRange = 2;

    [Export]
    public PackedScene ProjectilePrefab;
    private Timer _projectileCooldown;
    private Timer _damageCooldown;

    private readonly List<Enemy> _enemies = new();
    private GameManager _gameManager;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _projectileCooldown = GetNode<Timer>("ProjectileCooldown");
        _projectileCooldown.WaitTime = TotalCooldown;
        _projectileCooldown.Start();
        _projectileCooldown.Timeout += OnAttackReady;

        _damageCooldown = GetNode<Timer>("DamageCooldown");
        _damageCooldown.Start();
        _damageCooldown.Timeout += OnDamageReady;
    }

    private void OnAttackReady()
    {
        _projectileCooldown.Start();
        var projectile = ProjectilePrefab.Instantiate<Area3D>();
        projectile.BodyEntered += OnBodyEntered;
        projectile.BodyExited += OnBodyExited;
        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = _gameManager.GetRandomPosInDisk(ProjectileRange, MinSpawnRange) + new Vector3(0, 0.1f, 0);

        var tweener = GetTree().CreateTween();
        tweener.TweenProperty(projectile.GetNode("Visual"), "scale", new Vector3(0.01f, 0.01f, 0.01f), 1).SetDelay(TotalDuration);
        tweener.Parallel().TweenCallback(Callable.From(() => projectile.SetPhysicsProcess(false))).SetDelay(TotalDuration);
        tweener.TweenCallback(Callable.From(projectile.QueueFree));
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is not Enemy enemy) return;
        _enemies.Remove(enemy);
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not Enemy enemy) return;
        _enemies.Add(enemy);
    }

    private void OnDamageReady()
    {
        _damageCooldown.Start();
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _enemies[i];
            if (!IsInstanceValid(enemy) || enemy.IsDead)
            {
                _enemies.RemoveAt(i);
                continue;
            }
            enemy.TakeDamages(TotalDamages);
        }
    }

    public void Upgrade(Powerup powerup)
    {
        switch (powerup.Type)
        {
            case PowerupType.SpiritWaterDamages:
                _damagesBonus += (uint)powerup.Value;
                break;
            case PowerupType.SpiritWaterDuration:
                _durationBonus += powerup.Value;
                break;
            case PowerupType.SpiritWaterCooldown:
                _cooldownBonus += powerup.Value;
                _projectileCooldown.WaitTime = TotalCooldown;
                break;
            default: break;
        }
    }
}
