using Godot;
using System;
using System.Collections.Generic;

public record Choice(Powerup Powerup, double PlayerValue, EnemyPowerup EnemyPowerup, double EnemyValue);

public partial class GameManager : Node
{
    private const int MaxEnemies = 200;
    private const float BossSpawnInterval = 120f;
    private const float ChoiceDisplayTime = 1.5f;
    private const float SpawnAccelerationTime = 75f;
    private const float MinSpawnDelay = 0.15f;

    public Player Player;
    public Hud Hud;

    internal EnemyManager EnemyManager { get; private set; }

    public double GameTime { get; private set; }
    public int Kills { get; private set; }
    public int PlayerLevel { get; private set; } = 1;
    public float PlayerXp { get; private set; }
    public int MaxPlayerXp { get; private set; } = 5;

    public bool IsVotePhase { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsPauseMenuOpen { get; private set; }

    private double _enemySpawnTimeLeft = 1;
    private double _nextBossTime = BossSpawnInterval;

    private List<Choice> _currentVotes;
    private readonly List<Powerup> _powerups = new();
    private readonly List<EnemyPowerup> _enemyPowerups = new();
    private readonly Dictionary<PowerupType, int> _powerupsCount = new();
    private readonly Dictionary<EnemyPowerupType, int> _enemyPowerupsCount = new();

    public event Action<Enemy, int> OnEnemyHit;

    public override void _Ready()
    {
        base._Ready();

        ProcessMode = ProcessModeEnum.Always;

        LoadPowerups();
        LoadEnemyPowerups();
        ResetRun();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (IsVotePhase || IsGameOver || IsPauseMenuOpen || Player == null) return;

        // Debug thing
        if (Input.IsActionJustPressed("SpawnBoss"))
        {
            EnemyManager.SpawnBoss();
        }

        GameTime += delta;

        if (GameTime >= _nextBossTime)
        {
            _nextBossTime += BossSpawnInterval;
            EnemyManager.SpawnBoss();
        }

        _enemySpawnTimeLeft -= delta;
        if (_enemySpawnTimeLeft > 0) return;
        _enemySpawnTimeLeft = CurrentSpawnDelay;

        if (EnemyManager.Enemies.Count < MaxEnemies)
        {
            EnemyManager.SpawnEnemy();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (IsVotePhase || IsGameOver || IsPauseMenuOpen || Player == null) return;

        EnemyManager.ApplySeparation();
    }

    // Spawns get faster as the run goes on, on top of the spawn-rate votes.
    private double CurrentSpawnDelay =>
        Mathf.Max(MinSpawnDelay, EnemyManager.SpawnDelay / (1f + (float)GameTime / SpawnAccelerationTime));

    // Enemy base lifepoints scale smoothly with player level and elapsed time.
    internal float EnemyHealthMultiplier =>
        1f + 0.12f * (PlayerLevel - 1) + 0.08f * (float)(GameTime / 60.0);

    private void LoadPowerups()
    {
        foreach (var path in PowerupPaths.PlayerPowerups)
        {
            Powerup powerup = GD.Load<Powerup>(path);
            _powerups.Add(powerup);
            _powerupsCount.Add(powerup.Type, 0);
        }
    }

    private void LoadEnemyPowerups()
    {
        foreach (var path in PowerupPaths.EnemyPowerups)
        {
            EnemyPowerup powerup = GD.Load<EnemyPowerup>(path);
            _enemyPowerups.Add(powerup);
            _enemyPowerupsCount.Add(powerup.Type, 0);
        }
    }

    public void RegisterHud(Hud hud)
    {
        Hud = hud;
        hud.UpgradeView.OnChoose += OnChoose;
    }

    internal void EnemyHit(Enemy enemy, int damages)
    {
        OnEnemyHit?.Invoke(enemy, damages);
    }

    internal void EnemyKilled(int enemyXP)
    {
        if (IsGameOver) return;

        Kills++;
        PlayerXp += enemyXP;
        TryStartLevelUp();
    }

    private void TryStartLevelUp()
    {
        while (!IsVotePhase && !IsGameOver && PlayerXp >= MaxPlayerXp)
        {
            PlayerXp -= MaxPlayerXp;
            PlayerLevel++;
            MaxPlayerXp = GetMaxXPPerLevel(PlayerLevel);

            var votes = BuildChoices();
            if (votes.Count == 0) continue; // every powerup is maxed out, keep leveling silently

            IsVotePhase = true;
            GetTree().Paused = true;
            _currentVotes = votes;
            Hud.UpgradeView.SetChoices(votes);
            return;
        }
    }

    private List<Choice> BuildChoices()
    {
        var playerChoices = new List<Powerup>();
        foreach (var powerup in _powerups)
            if (_powerupsCount[powerup.Type] < powerup.MaxCumul) playerChoices.Add(powerup);

        var enemyChoices = new List<EnemyPowerup>();
        foreach (var powerup in _enemyPowerups)
            if (_enemyPowerupsCount[powerup.Type] < powerup.MaxStack) enemyChoices.Add(powerup);

        Shuffle(playerChoices);
        Shuffle(enemyChoices);

        int count = Mathf.Min(3, Mathf.Max(playerChoices.Count, enemyChoices.Count));
        var votes = new List<Choice>(count);
        for (int i = 0; i < count; i++)
        {
            Powerup powerup = i < playerChoices.Count ? playerChoices[i] : null;
            EnemyPowerup enemyPowerup = i < enemyChoices.Count ? enemyChoices[i] : null;
            double playerValue = powerup == null ? 0 : powerup.Value * (_powerupsCount[powerup.Type] + 1);
            double enemyValue = enemyPowerup == null ? 0 : EnemyManager.GetFinalValue(enemyPowerup);
            votes.Add(new Choice(powerup, playerValue, enemyPowerup, enemyValue));
        }
        return votes;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = GD.RandRange(0, i);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private async void OnChoose(Choice choice)
    {
        Hud.UpgradeView.DisplayChoicePicked(_currentVotes.IndexOf(choice));

        await ToSignal(GetTree().CreateTimer(ChoiceDisplayTime), SceneTreeTimer.SignalName.Timeout);

        if (choice.Powerup != null)
        {
            if (Player is IUpgradable playerUpgradable) playerUpgradable.Upgrade(choice.Powerup);
            foreach (var child in Player.GetChildren())
                if (child is IUpgradable upgradable) upgradable.Upgrade(choice.Powerup);

            _powerupsCount[choice.Powerup.Type]++;
        }

        if (choice.EnemyPowerup != null)
        {
            EnemyManager.Upgrade(choice.EnemyPowerup);
            _enemyPowerupsCount[choice.EnemyPowerup.Type]++;
        }

        IsVotePhase = false;
        GetTree().Paused = false;
        Hud.UpgradeView.Clear();

        TryStartLevelUp();
    }

    internal void PlayerDied()
    {
        if (IsGameOver) return;

        IsGameOver = true;
        GetTree().Paused = true;
        Hud.ShowGameOver(GameTime, Kills, PlayerLevel);
    }

    public void TogglePauseMenu()
    {
        if (IsVotePhase || IsGameOver) return;

        IsPauseMenuOpen = !IsPauseMenuOpen;
        GetTree().Paused = IsPauseMenuOpen;
        Hud.SetPauseMenuVisible(IsPauseMenuOpen);
    }

    public void RestartRun()
    {
        ResetRun();
        GetTree().Paused = false;
        // Deferred: restarting is triggered from UI signal callbacks.
        GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
    }

    private void ResetRun()
    {
        GameTime = 0;
        Kills = 0;
        PlayerLevel = 1;
        PlayerXp = 0;
        MaxPlayerXp = GetMaxXPPerLevel(1);
        IsVotePhase = false;
        IsGameOver = false;
        IsPauseMenuOpen = false;
        _nextBossTime = BossSpawnInterval;
        _currentVotes = null;

        foreach (var powerup in _powerups) _powerupsCount[powerup.Type] = 0;
        foreach (var powerup in _enemyPowerups) _enemyPowerupsCount[powerup.Type] = 0;

        EnemyManager = new EnemyManager(this);
        _enemySpawnTimeLeft = EnemyManager.SpawnDelay;
        Player = null;
    }

    // Random point on a circle of the given radius around the player.
    public Vector3 GetRandomPosOnRing(float range)
    {
        float angle = (float)GD.RandRange(0, Mathf.Tau);
        return Player.Position + range * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
    }

    // Uniformly distributed random point in a ring area around the player.
    public Vector3 GetRandomPosInDisk(float maxRange, float minRange = 0)
    {
        float angle = (float)GD.RandRange(0, Mathf.Tau);
        float radius = Mathf.Sqrt((float)GD.RandRange(minRange * minRange, maxRange * maxRange));
        return Player.Position + radius * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
    }

    internal Enemy GetNearestEnemy()
    {
        if (Player == null) return null;

        Enemy nearest = null;
        float nearestDistanceSq = float.MaxValue;
        Vector3 playerPos = Player.Position;
        foreach (var enemy in EnemyManager.Enemies)
        {
            float distanceSq = playerPos.DistanceSquaredTo(enemy.Position);
            if (distanceSq >= nearestDistanceSq) continue;
            nearestDistanceSq = distanceSq;
            nearest = enemy;
        }
        return nearest;
    }

    internal int GetMaxXPPerLevel(int level) => 5 + 8 * (level - 1) + (level - 1) * (level - 1);
}
