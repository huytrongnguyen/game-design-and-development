namespace Course.ActionCombat;

/// <summary>Move the caster <c>DistancePerTick</c> along its facing on each tick in [FromTick, ToTick).</summary>
public sealed record MoveSegment(int FromTick, int ToTick, double DistancePerTick);

/// <summary>
/// A skill as an animation timeline measured in server ticks from the moment it starts:
/// startup (no hit yet), active (hits may happen), recovery (vulnerable, cannot act).
/// </summary>
public sealed record SkillDef
{
    public string Id { get; init; } = "";
    public int StartupTicks { get; init; }
    public int ActiveTicks { get; init; }
    public int RecoveryTicks { get; init; }
    /// <summary>Ticks (from skill start) on which the hit shape is tested. Must lie in the active window.</summary>
    public int[] HitTicks { get; init; } = [];
    public ShapeDef? Shape { get; init; }
    public int Damage { get; init; }
    /// <summary>Another skill may start from tick CancelFromTick (inclusive) to CancelToTick (exclusive). -1 = none.</summary>
    public int CancelFromTick { get; init; } = -1;
    public int CancelToTick { get; init; } = -1;
    public MoveSegment[] Moves { get; init; } = [];
    /// <summary>Invulnerable on ticks in [IFrameFromTick, IFrameToTick).</summary>
    public int IFrameFromTick { get; init; }
    public int IFrameToTick { get; init; }

    public int TotalTicks => StartupTicks + ActiveTicks + RecoveryTicks;
    public bool IsInvulnerableAt(int offset) => offset >= IFrameFromTick && offset < IFrameToTick;
    public bool CanCancelAt(int offset) => CancelFromTick >= 0 && offset >= CancelFromTick && offset < CancelToTick;

    /// <summary>Returns a list of human-readable problems; empty means valid.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (StartupTicks < 0 || ActiveTicks < 0 || RecoveryTicks < 0) errors.Add($"{Id}: negative duration");
        foreach (int t in HitTicks)
            if (t < StartupTicks || t >= StartupTicks + ActiveTicks)
                errors.Add($"{Id}: hit tick {t} is outside the active window [{StartupTicks}, {StartupTicks + ActiveTicks})");
        if (HitTicks.Length > 0 && Shape is null) errors.Add($"{Id}: hit ticks without a shape");
        if (CancelFromTick >= 0 && (CancelToTick <= CancelFromTick || CancelToTick > TotalTicks))
            errors.Add($"{Id}: bad cancel window [{CancelFromTick}, {CancelToTick})");
        if (IFrameToTick < IFrameFromTick || IFrameToTick > TotalTicks)
            errors.Add($"{Id}: bad i-frame window [{IFrameFromTick}, {IFrameToTick})");
        foreach (var m in Moves)
            if (m.FromTick < 0 || m.ToTick > TotalTicks || m.ToTick < m.FromTick)
                errors.Add($"{Id}: bad move segment [{m.FromTick}, {m.ToTick})");
        return errors;
    }
}
