namespace Course.Combat;

/// <summary>How much each primary stat contributes to one derived value. A row of data, not code.</summary>
public sealed record StatWeights(double Strength = 0, double Agility = 0, double Vitality = 0, double Intellect = 0)
{
    public double Apply(PrimaryStats s) =>
        s.Strength * Strength + s.Agility * Agility + s.Vitality * Vitality + s.Intellect * Intellect;
}
