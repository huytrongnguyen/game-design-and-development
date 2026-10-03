namespace Course.GameLoop;

/// <summary>
/// A command stamped with the sequence number it was assigned at submission time.
/// Internal: callers only ever see <see cref="ICommand"/>s coming out of
/// <see cref="CommandQueue.DrainOrdered"/>, already sorted.
/// </summary>
internal readonly record struct CommandEnvelope(long Sequence, ICommand Command);
