namespace Shaiya2.Server.Authentication;

public sealed class PlayerLoginContext
{
    public bool IsAuthenticated { get; set; }

    public long? AccountId { get; set; }

    public string? Username { get; set; }
}