namespace M24.Pipeline;

/// <summary>A duelist's balance-relevant numbers. <c>CritPercent</c> is 0 to 100; a crit doubles damage.</summary>
public sealed record Fighter(string Name, int Hp, int Attack, int Defense, int CritPercent);
