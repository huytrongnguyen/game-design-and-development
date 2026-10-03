namespace Course.Topology;

/// <summary>What a session token says: who the account is and when the token stops being valid.</summary>
public sealed record TokenClaims(string AccountId, long ExpiresAtSeconds);
