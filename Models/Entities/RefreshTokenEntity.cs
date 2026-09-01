using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MuscleRivalsBackend.Models.Entities;


[Table("refresh_tokens", Schema = "public"), Index(nameof(UserId), nameof(HashedToken))]
public class RefreshTokenEntity()
{
    public int Id { get; set; }

    [Required]
    public required int UserId { get; set; }

    public UserEntity User { get; set; } = null!;

    [Required]
    public required string HashedToken { get; set; }

    [Required]
    public required DateTime ExpiresAt { get; set; }

    [Required]
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;


    [Required]
    public required bool IsRevoked { get; set; }


    public static RefreshTokenEntity CreateToken(int userId, string hashedToken, int expiryDays)
    => new() { UserId = userId, HashedToken = hashedToken, ExpiresAt = DateTime.UtcNow.AddDays(expiryDays), IsRevoked = false };

};