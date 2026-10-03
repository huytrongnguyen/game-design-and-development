namespace M12.Ai;

/// <summary>Utility AI: score every action from 0 to 1 times a role weight, then take the best.</summary>
public static class AllyAi
{
    private const double AttackBase = 0.6;

    public static AllyDecision Decide(
        AllyPreset preset, Unit self, Unit leader,
        IReadOnlyList<Unit> allies, IReadOnlyList<Unit> enemies)
    {
        var best = new AllyDecision(AllyAction.Idle, null, 0);

        // Order matters: on an exact tie the earlier action wins (heal, then attack, then follow).
        Consider(ref best, AllyAction.Heal, ScoreHeal(preset, self, allies, out var healId), healId);
        Consider(ref best, AllyAction.Attack, ScoreAttack(preset, self, enemies, out var enemyId), enemyId);
        Consider(ref best, AllyAction.Follow, ScoreFollow(preset, self, leader), leader.Id);
        return best;
    }

    private static void Consider(ref AllyDecision best, AllyAction action, double score, int? target)
    {
        if (score > best.Score) best = new AllyDecision(action, target, score);
    }

    // Urgency grows as the weakest ally gets lower: hp 0.4 gives 0.6, hp 0.1 gives 0.9.
    private static double ScoreHeal(AllyPreset p, Unit self, IReadOnlyList<Unit> allies, out int? targetId)
    {
        targetId = null;
        if (p.HealWeight <= 0) return 0;
        Unit? worst = null;
        foreach (var a in allies)
        {
            if (!a.IsAlive || self.Position.DistanceTo(a.Position) > p.HealRange) continue;
            if (worst is null || a.HpFraction < worst.HpFraction) worst = a;
        }
        if (worst is null || worst.HpFraction >= p.HealThreshold) return 0;
        targetId = worst.Id;
        return p.HealWeight * (1 - worst.HpFraction);
    }

    private static double ScoreAttack(AllyPreset p, Unit self, IReadOnlyList<Unit> enemies, out int? targetId)
    {
        targetId = null;
        Unit? nearest = null;
        var best = p.AttackRange;
        foreach (var e in enemies)
        {
            if (!e.IsAlive) continue;
            var d = self.Position.DistanceTo(e.Position);
            if (d <= best) { best = d; nearest = e; }
        }
        if (nearest is null) return 0;
        targetId = nearest.Id;
        return p.AttackWeight * AttackBase;
    }

    // Zero inside the follow distance, then grows linearly and caps at the weight.
    private static double ScoreFollow(AllyPreset p, Unit self, Unit leader)
    {
        var d = self.Position.DistanceTo(leader.Position);
        if (d <= p.FollowDistance) return 0;
        return p.FollowWeight * Math.Min(1, (d - p.FollowDistance) / p.FollowDistance);
    }
}
