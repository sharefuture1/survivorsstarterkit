using Godot;
using System.Collections.Generic;

internal class EnemyManager
{
    public const int EnemySpawnRange = 30;

    private const float SeparationRadius = 0.9f;
    private const float MinSpawnRate = 0.1f;

    public float SpawnRate { get; private set; } = 1;

    public float SpawnDelay => 1f / SpawnRate;

    private readonly GameManager _gameManager;
    private readonly Dictionary<EnemyClass, PackedScene> _enemyPrefabs;

    public List<Enemy> Enemies { get; } = new();

    // BONUSES
    private readonly List<EnemyClass> _enemyClasses = new() { EnemyClass.Minion };
    private int _lifepointsBonus = 0;
    private uint _damageBonus = 0;
    private float _movespeedBonus = 0;

    public EnemyManager(GameManager gameManager)
    {
        _gameManager = gameManager;

        _enemyPrefabs = new()
        {
            { EnemyClass.Minion, (PackedScene)GD.Load("res://Prefabs/Enemies/enemy_minion.tscn") },
            { EnemyClass.Warrior, (PackedScene)GD.Load("res://Prefabs/Enemies/enemy_warrior.tscn") },
            { EnemyClass.Archer, (PackedScene)GD.Load("res://Prefabs/Enemies/enemy_archer.tscn") },
            { EnemyClass.Mage, (PackedScene)GD.Load("res://Prefabs/Enemies/enemy_mage.tscn") },
            { EnemyClass.Boss, (PackedScene)GD.Load("res://Prefabs/Enemies/enemy_boss.tscn") },
        };
    }

    internal Enemy SpawnEnemy() => SpawnEnemy(_enemyClasses[GD.RandRange(0, _enemyClasses.Count - 1)]);

    internal Enemy SpawnEnemy(EnemyClass enemyClass)
    {
        var enemy = _enemyPrefabs[enemyClass].Instantiate<Enemy>();
        enemy.Name = enemyClass.ToString();
        enemy.Lifepoints = Mathf.RoundToInt(enemy.Lifepoints * _gameManager.EnemyHealthMultiplier) + _lifepointsBonus;
        enemy.Damages += _damageBonus;
        enemy.MovementSpeed += _movespeedBonus;
        enemy.Position = _gameManager.GetRandomPosOnRing(EnemySpawnRange);
        enemy.Died += OnEnemyDied;
        enemy.TreeExiting += () => Enemies.Remove(enemy);
        enemy.OnEnemyHit += _gameManager.EnemyHit;
        _gameManager.GetTree().CurrentScene.AddChild(enemy);
        Enemies.Add(enemy);
        return enemy;
    }

    internal Enemy SpawnBoss() => SpawnEnemy(EnemyClass.Boss);

    private void OnEnemyDied(Enemy enemy) => _gameManager.EnemyKilled(enemy.Experience);

    // Cheap pairwise separation so enemies don't collapse into a single stack.
    internal void ApplySeparation()
    {
        const float radiusSq = SeparationRadius * SeparationRadius;
        for (int i = 0; i < Enemies.Count; i++)
        {
            var a = Enemies[i];
            Vector3 aPos = a.GlobalPosition;
            for (int j = i + 1; j < Enemies.Count; j++)
            {
                var b = Enemies[j];
                Vector3 diff = aPos - b.GlobalPosition;
                diff.Y = 0;
                float distanceSq = diff.LengthSquared();
                if (distanceSq >= radiusSq || distanceSq < 0.0001f) continue;

                float distance = Mathf.Sqrt(distanceSq);
                Vector3 push = diff / distance * ((SeparationRadius - distance) * 0.5f);
                aPos += push;
                b.GlobalPosition -= push;
            }
            a.GlobalPosition = aPos;
        }
    }

    internal void Upgrade(EnemyPowerup enemyPowerup)
    {
        switch (enemyPowerup.Type)
        {
            case EnemyPowerupType.UnlockClassWarrior:
                _enemyClasses.Add(EnemyClass.Warrior);
                break;
            case EnemyPowerupType.UnlockClassMage:
                _enemyClasses.Add(EnemyClass.Mage);
                break;
            case EnemyPowerupType.UnlockClassArcher:
                _enemyClasses.Add(EnemyClass.Archer);
                break;
            case EnemyPowerupType.BossSpawn:
                SpawnBoss();
                break;
            case EnemyPowerupType.Lifepoints:
                _lifepointsBonus += (int)((StatEnemyPowerup)enemyPowerup).Value;
                break;
            case EnemyPowerupType.Damages:
                _damageBonus += (uint)((StatEnemyPowerup)enemyPowerup).Value;
                break;
            case EnemyPowerupType.Movespeed:
                _movespeedBonus += ((StatEnemyPowerup)enemyPowerup).Value;
                break;
            case EnemyPowerupType.SpawnRate:
                SpawnRate = Mathf.Max(MinSpawnRate, SpawnRate + ((StatEnemyPowerup)enemyPowerup).Value);
                break;
            default:
                GD.PrintErr($"{enemyPowerup.Type} is not handled");
                break;
        }
    }

    internal double GetFinalValue(EnemyPowerup enemyPowerup) => enemyPowerup.Type switch
    {
        EnemyPowerupType.Lifepoints => _lifepointsBonus + (int)((StatEnemyPowerup)enemyPowerup).Value,
        EnemyPowerupType.Damages => _damageBonus + (uint)((StatEnemyPowerup)enemyPowerup).Value,
        EnemyPowerupType.Movespeed => (double)(_movespeedBonus + ((StatEnemyPowerup)enemyPowerup).Value),
        EnemyPowerupType.SpawnRate => (double)(1f / (SpawnRate + ((StatEnemyPowerup)enemyPowerup).Value)),
        _ => default,
    };
}
