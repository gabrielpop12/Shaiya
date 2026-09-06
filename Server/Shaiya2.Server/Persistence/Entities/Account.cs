namespace Shaiya2.Server.Persistence.Entities;

public sealed class Account
{
    public long Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string NormalizedUsername { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public AccountStatus Status { get; set; }

    public ICollection<Character> Characters { get; set; }
        = new List<Character>();
}