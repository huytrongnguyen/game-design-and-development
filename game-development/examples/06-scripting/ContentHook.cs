namespace Course.Scripting;

/// <summary>
/// A content hook: a named piece of game-specific logic that the engine calls at a defined moment
/// (a quest completes, an item is used). The engine owns the call site; content owns the body.
/// </summary>
public delegate void ContentHook(HookContext context);
