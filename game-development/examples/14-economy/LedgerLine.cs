namespace Course.Economy;

/// <summary>One signed change to one asset (a currency such as "gold", or an item such as "item:potion") of one account.</summary>
public readonly record struct LedgerLine(string Account, string Asset, long Delta);
