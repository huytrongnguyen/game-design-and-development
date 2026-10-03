namespace Course.Navigation;

/// <summary>
/// The three outcomes a path query can have. Many wrappers report only a found path or a failure;
/// here the "the query gave up, not 'no path exists'" case (<see cref="BudgetExceeded"/>) is an
/// explicit, distinct outcome instead of being folded into failure.
/// </summary>
public enum PathStatus
{
    Found,
    NotFound,
    BudgetExceeded
}
