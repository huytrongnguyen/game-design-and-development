namespace Course.Combat;

public class AttackResolverTests
{
    private static AttackResolver Resolver(ulong seed = 1) => new(new CombatRng(seed), new CombatRules());

    // Attacker rank 20 with ATK 200 and a hit rate of 100, so the damage roll is fixed.
    private static AttackerProfile Attacker() => new() { AttackRank = 20, Atk = 200, HitRate = 100 };

    private static DefenderProfile Defender() => new() { DefenseRank = 20, Def = 300 };

    [Fact]
    public void Resolve_Def300Rank20_RemovesFiftyPercent()
    {
        var r = Resolver().Resolve(Attacker(), Defender());
        Assert.Equal(AttackOutcome.Hit, r.Outcome);
        Assert.Equal(100, r.Damage); // 200 * 0.5
    }

    [Fact]
    public void Resolve_Fire60Against25Resist_Adds45BeforeFloor()
    {
        var a = Attacker() with { Elemental = new(Element.Fire, 60) };
        var d = Defender() with { Resists = new(Fire: 25) };
        Assert.Equal(145, Resolver().Resolve(a, d).Damage); // 100 + 60 * 0.75
    }

    [Fact]
    public void Resolve_MatchingArmorType_Multiplies1Point25()
    {
        var a = Attacker() with { Elemental = new(Element.Fire, 60), WeaponGroup = WeaponGroup.Pierce };
        var d = Defender() with { Resists = new(Fire: 25), ArmorType = ArmorType.Light };
        Assert.Equal(181, Resolver().Resolve(a, d).Damage); // 145 * 1.25 = 181.25
    }

    [Fact]
    public void Resolve_DefenderOutranksBy5_DamageIsHalved()
    {
        var d = Defender() with { DefenseRank = 25 };
        Assert.Equal(50, Resolver().Resolve(Attacker(), d).Damage); // 100 * 0.5
    }

    [Fact]
    public void Resolve_AttackerOutranksBy5_DamageIsOnePointFiveTimes()
    {
        var d = Defender() with { DefenseRank = 15 };
        Assert.Equal(150, Resolver().Resolve(Attacker(), d).Damage); // 100 * 1.5
    }

    [Fact]
    public void Resolve_GuaranteedCrit_DoublesTheDamage()
    {
        var a = Attacker() with { Elemental = new(Element.Fire, 60), CritRate = 200 };
        var d = Defender() with { Resists = new(Fire: 25) };
        var r = Resolver().Resolve(a, d);
        Assert.Equal(AttackOutcome.Crit, r.Outcome);
        Assert.Equal(290, r.Damage); // 145 * 2.0
    }

    [Fact]
    public void Resolve_CritRateZero_NeverCrits()
    {
        var a = Attacker() with { CritRate = 0 };
        var resolver = Resolver();
        for (int i = 0; i < 200; i++)
            Assert.Equal(AttackOutcome.Hit, resolver.Resolve(a, Defender()).Outcome);
    }

    [Fact]
    public void Resolve_TinyHitAgainstCappedDefense_StillDealsAtLeastOne()
    {
        var a = Attacker() with { Atk = 2 };
        var d = Defender() with { Def = 100_000 };
        Assert.Equal(1, Resolver().Resolve(a, d).Damage); // 2 * 0.25 = 0.5, raised to the floor
    }

    [Fact]
    public void Resolve_FullEvasion_AlwaysMissesWithZeroDamage()
    {
        var d = Defender() with { Evasion = 100 };
        var r = Resolver().Resolve(Attacker(), d);
        Assert.Equal(new AttackResult(AttackOutcome.Miss, 0), r);
    }

    [Fact]
    public void Resolve_Block40AttackerAhead5_BlocksAboutTwentyNinePercent()
    {
        // effective block = 40 - (20-15)*2 = 30; the roll is "< 30", so 29 of 100 values block.
        var d = Defender() with { DefenseRank = 15, Block = 40 };
        var resolver = Resolver(7);
        int blocks = 0;
        const int n = 20_000;
        for (int i = 0; i < n; i++)
            if (resolver.Resolve(Attacker(), d).Outcome == AttackOutcome.Block) blocks++;
        Assert.InRange(blocks / (double)n, 0.27, 0.31);
    }

    [Fact]
    public void Resolve_LowHitRate_RaisesTheFloorOfTheRollNotTheMissChance()
    {
        // HitRate 60: the base roll is uniform in [120, 200]; never a miss, never below the floor.
        var a = Attacker() with { HitRate = 60 };
        var d = Defender() with { Def = 0 };
        var resolver = Resolver(3);
        for (int i = 0; i < 500; i++)
        {
            var r = resolver.Resolve(a, d);
            Assert.Equal(AttackOutcome.Hit, r.Outcome);
            Assert.InRange(r.Damage, 120, 200);
        }
    }

    [Fact]
    public void Resolve_SameSeedHundredAttacks_ProducesIdenticalLog()
    {
        string first = RunLog(seed: 42);
        string second = RunLog(seed: 42);
        Assert.Equal(first, second);
        Assert.NotEqual(first, RunLog(seed: 43));

        // The mixed profile really exercises every outcome.
        Assert.Contains("Miss", first);
        Assert.Contains("Block", first);
        Assert.Contains("Hit", first);
        Assert.Contains("Crit", first);
    }

    private static string RunLog(ulong seed)
    {
        var resolver = Resolver(seed);
        var a = new AttackerProfile
        {
            AttackRank = 20, Atk = 200, HitRate = 60,
            CritRate = 40, Elemental = new(Element.Frost, 30),
        };
        var d = new DefenderProfile
        {
            DefenseRank = 18, Def = 150, Block = 30, Parry = 10, Evasion = 10,
            Resists = new(Frost: 10),
        };
        var lines = new List<string>(100);
        for (int i = 0; i < 100; i++)
        {
            var r = resolver.Resolve(a, d);
            lines.Add($"{i}:{r.Outcome}:{r.Damage}");
        }
        return string.Join('\n', lines);
    }
}
