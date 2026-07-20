using Godot;

public partial class LifestealAttack : Node3D, IUpgradable
{
    [Export]
    public uint Damages = 1;

    private uint _damagesBonus = 0;

    // How many drain ticks happen per second.
    [Export]
    public float DrainsPerSecond = 5;

    private float _drainRateBonus = 0;

    public uint TotalDamages => Damages + _damagesBonus;
    public float TotalDrainsPerSecond => DrainsPerSecond + _drainRateBonus;

    private Player _player;
    private Timer _stealCooldown;
    private Area3D _area;

    public override void _Ready()
    {
        _player = GetParent<Player>();
        _stealCooldown = GetNode<Timer>("Timer");
        _stealCooldown.WaitTime = 1f / TotalDrainsPerSecond;
        _stealCooldown.Start();
        _area = GetNode<Area3D>("Area3D");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_stealCooldown.TimeLeft > 0) return;

        _stealCooldown.Start();
        if (!_area.HasOverlappingBodies()) return;
        foreach (var body in _area.GetOverlappingBodies())
        {
            if (body is not Enemy enemy || enemy.IsDead) continue;
            enemy.TakeDamages(TotalDamages);
            _player.Heal(TotalDamages);
        }
    }

    public void Upgrade(Powerup powerup)
    {
        switch (powerup.Type)
        {
            case PowerupType.LifestealDamages:
                _damagesBonus += (uint)powerup.Value;
                break;
            case PowerupType.LifestealCooldown:
                _drainRateBonus += powerup.Value;
                _stealCooldown.WaitTime = 1f / TotalDrainsPerSecond;
                break;
            default: break;
        }
    }
}
