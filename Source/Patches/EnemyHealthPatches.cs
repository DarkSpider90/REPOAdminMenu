using AdminMenu.Features;
using HarmonyLib;

namespace AdminMenu.Patches;

/// <summary>
/// One-shot enemies: any hit becomes exactly as much damage as the enemy has left.
///
/// Enemies belong to the host, so this one patch on the host covers the whole lobby and every
/// player benefits without having the mod. The damage is raised to the enemy's remaining
/// health rather than to a large number, so the game's own death handling runs normally.
/// </summary>
[HarmonyPatch(typeof(EnemyHealth))]
internal static class EnemyHealthPatches
{
    /// <summary>Used when the enemy's health has not been read yet and reads as zero or less.</summary>
    private const int FallbackDamage = 100;

    [HarmonyPatch("Hurt")]
    [HarmonyPrefix]
    private static void Hurt_Prefix(ref int _damage, int ___healthCurrent)
    {
        if (!AdminFlags.OneShotEnemies)
        {
            return;
        }

        _damage = ___healthCurrent > 0 ? ___healthCurrent : FallbackDamage;
    }
}
