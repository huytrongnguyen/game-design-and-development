namespace M17.Net;

public readonly record struct ReadResult(ReadStatus Status, string Text = "");
