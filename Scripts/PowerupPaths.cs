using System.Collections.Generic;

public static class PowerupPaths
{
    public static readonly List<string> PlayerPowerups = new()
    {
        "res://Powerups/FloatingSphereCount.tres",
        "res://Powerups/FloatingSphereDamages.tres",
        "res://Powerups/LifestealCooldown.tres",
        "res://Powerups/LifestealDamages.tres",
        "res://Powerups/PlayerMaxHealth.tres",
        "res://Powerups/PlayerMoveSpeed.tres",
        "res://Powerups/ShootingAttackSpeed.tres",
        "res://Powerups/ShootingBulletSpeed.tres",
        "res://Powerups/ShootingDamages.tres",
        "res://Powerups/SpiritWaterCooldown.tres",
        "res://Powerups/SpiritWaterDamages.tres",
        "res://Powerups/SpiritWaterDuration.tres",
    };

    public static readonly List<string> EnemyPowerups = new()
    {
        "res://Powerups/Enemy/EnemyBossSpawn.tres",
        "res://Powerups/Enemy/EnemyDamages.tres",
        "res://Powerups/Enemy/EnemyLifepoints.tres",
        "res://Powerups/Enemy/EnemyMoveSpeed.tres",
        "res://Powerups/Enemy/EnemySpawnRate.tres",
        "res://Powerups/Enemy/EnemyUnlockArcher.tres",
        "res://Powerups/Enemy/EnemyUnlockMage.tres",
        "res://Powerups/Enemy/EnemyUnlockWarrior.tres",
    };
}
