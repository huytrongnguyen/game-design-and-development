namespace Course.Economy;

/// <summary>
/// What actually sits in an inventory slot: a definition id and a count. Only unique items
/// (max stack 1) carry an <see cref="InstanceId"/>, because only they can hold their own
/// state, such as a reinforcement level.
/// </summary>
public sealed record ItemStack(string ItemId, int Count, long? InstanceId = null, int Reinforce = 0);
