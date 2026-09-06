using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shaiya2.Server.Persistence;
using Shaiya2.Server.Persistence.Entities;

namespace Shaiya2.Server.Authentication;

public sealed class AccountService
{
    private readonly PasswordHasher<Account> _passwordHasher = new();

    public RegisterResult Register(
        string username,
        string password)
    {
        username = username.Trim();

        if (!IsValidUsername(username))
            return RegisterResult.InvalidUsername;

        if (!IsValidPassword(password))
            return RegisterResult.InvalidPassword;

        string normalizedUsername =
            NormalizeUsername(username);

        try
        {
            using var db =
                new ShaiyaDbContext();

            bool alreadyExists =
                db.Accounts.Any(
                    account =>
                        account.NormalizedUsername ==
                        normalizedUsername
                );

            if (alreadyExists)
            {
                return RegisterResult.UsernameAlreadyExists;
            }

            var account = new Account
            {
                Username = username,
                NormalizedUsername = normalizedUsername,
                CreatedAtUtc = DateTime.UtcNow,
                Status = AccountStatus.Active
            };

            account.PasswordHash =
                _passwordHasher.HashPassword(
                    account,
                    password
                );

            db.Accounts.Add(account);

            db.SaveChanges();

            Console.WriteLine(
                $"[Account] Registered | AccountId={account.Id} | Username={account.Username}"
            );

            return RegisterResult.Success;
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine(
                $"[Account] Registration database error: {ex.Message}"
            );

            return RegisterResult.ServerError;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[Account] Registration error: {ex.Message}"
            );

            return RegisterResult.ServerError;
        }
    }

    public LoginResult Login(
        PlayerLoginContext context,
        string username,
        string password)
    {
        if (context.IsAuthenticated)
            return LoginResult.AlreadyAuthenticated;

        username = username.Trim();

        string normalizedUsername =
            NormalizeUsername(username);

        try
        {
            using var db =
                new ShaiyaDbContext();

            Account? account =
                db.Accounts.SingleOrDefault(
                    account =>
                        account.NormalizedUsername ==
                        normalizedUsername
                );

            if (account == null)
                return LoginResult.InvalidCredentials;

            PasswordVerificationResult passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    account,
                    account.PasswordHash,
                    password
                );

            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                return LoginResult.InvalidCredentials;
            }

            switch (account.Status)
            {
                case AccountStatus.Banned:
                    return LoginResult.AccountBanned;

                case AccountStatus.Suspended:
                    return LoginResult.AccountSuspended;
            }

            if (passwordResult ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                account.PasswordHash =
                    _passwordHasher.HashPassword(
                        account,
                        password
                    );
            }

            account.LastLoginAtUtc =
                DateTime.UtcNow;

            db.SaveChanges();

            context.AccountId =
                account.Id;

            context.Username =
                account.Username;

            context.IsAuthenticated =
                true;

            Console.WriteLine(
                $"[Account] Login successful | AccountId={account.Id} | Username={account.Username}"
            );

            return LoginResult.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[Account] Login error: {ex.Message}"
            );

            return LoginResult.ServerError;
        }
    }

    private static string NormalizeUsername(
        string username)
    {
        return username
            .Trim()
            .ToUpperInvariant();
    }

    private static bool IsValidUsername(
        string username)
    {
        if (username.Length < 3 ||
            username.Length > 20)
        {
            return false;
        }

        foreach (char character in username)
        {
            if (!char.IsLetterOrDigit(character) &&
                character != '_')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidPassword(
        string password)
    {
        return password.Length >= 8 &&
               password.Length <= 128;
    }
}