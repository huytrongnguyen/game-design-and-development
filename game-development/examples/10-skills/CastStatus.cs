namespace Course.Skills;

/// <summary>The outcome of a cast request. Expected failures are values, not exceptions.</summary>
public enum CastStatus
{
    Started,
    UnknownSkill,
    NotInStance,
    AlreadyCasting,
    OnCooldown,
    NotEnoughSp,
    NoTarget,
    OutOfRange,
}
