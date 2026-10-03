namespace Course.Combat;

public readonly record struct ElementResists(int Fire = 0, int Frost = 0, int Shock = 0, int Arcane = 0)
{
    public int Get(Element element) => element switch
    {
        Element.Fire => Fire,
        Element.Frost => Frost,
        Element.Shock => Shock,
        Element.Arcane => Arcane,
        _ => 0,
    };
}
