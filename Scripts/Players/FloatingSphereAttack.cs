using Godot;
using System.Collections.Generic;

public partial class FloatingSphereAttack : Node3D, IUpgradable
{
    private const float DowntimeSeconds = 2f;

    [Export]
    public uint InitialSpheres = 1;

    [Export]
    public uint Damages = 3;

    private uint _damagesBonus = 0;

    public uint TotalDamages => Damages + _damagesBonus;

    [Export]
    public float RotationSpeed { get; private set; } = 1;

    [Export]
    public float Duration { get; private set; } = 3;

    [Export]
    public float SphereDistance { get; private set; } = 1.4f;

    [Export]
    private PackedScene _spherePrefab;
    private readonly List<Area3D> _spheres = new();
    private Timer _timer;

    public override void _Ready()
    {
        for (int i = 0; i < InitialSpheres; i++)
            AddSphere();

        _timer = GetNode<Timer>("Timer");
        _timer.WaitTime = Duration;
        _timer.Timeout += OnAttackEnd;
        _timer.Start();
    }

    private void OnBodyEntered(Node body)
    {
        if (body is not Enemy enemy) return;
        enemy.TakeDamages(TotalDamages);
    }

    public override void _PhysicsProcess(double delta)
    {
        RotateY((float)delta * RotationSpeed);
    }

    private void AddSphere()
    {
        Area3D area = _spherePrefab.Instantiate<Area3D>();
        area.BodyEntered += OnBodyEntered;
        AddChild(area);
        _spheres.Add(area);

        RepositionSpheres();
    }

    private void RepositionSpheres()
    {
        Vector3 basePosition = new(SphereDistance, 0.3f, 0);
        for (int i = 0; i < _spheres.Count; i++)
        {
            float angle = Mathf.Tau * i / _spheres.Count;
            Basis basis = new(Vector3.Up, angle);
            _spheres[i].Position = basis * basePosition;
        }
    }

    private async void OnAttackEnd()
    {
        HideSpheres();
        // Pauses with the tree (processAlways: false) so the downtime doesn't tick during votes.
        await ToSignal(GetTree().CreateTimer(DowntimeSeconds, processAlways: false), SceneTreeTimer.SignalName.Timeout);
        _timer.Start();
        ShowSpheres();
    }

    private void ShowSpheres()
    {
        foreach (var area in _spheres)
        {
            var particles = area.GetNode<GpuParticles3D>("Particles");
            particles.Restart();
            particles.Emitting = true;
            area.SetPhysicsProcess(true);
            area.Show();
        }
    }

    private void HideSpheres()
    {
        foreach (var area in _spheres)
        {
            area.GetNode<GpuParticles3D>("Particles").Emitting = false;
            area.SetPhysicsProcess(false);
            area.Hide();
        }
    }

    public void Upgrade(Powerup powerup)
    {
        switch (powerup.Type)
        {
            case PowerupType.FloatingSphereCount:
                AddSphere();
                break;
            case PowerupType.FloatingSphereDamages:
                _damagesBonus += (uint)powerup.Value;
                break;
            default: break;
        }
    }
}
