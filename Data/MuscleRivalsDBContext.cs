

using Microsoft.EntityFrameworkCore;
using MuscleRivalsBackend.Models.Entities;

namespace MuscleRivalsBackend.Data;

public class MuscleRivalsDBContext(DbContextOptions<MuscleRivalsDBContext> options) : DbContext(options)
{

    public DbSet<UserEntity> Users { get; set; }
    public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }

    public DbSet<VerificationTokenEntity> VerificationTokens { get; set; }

    public DbSet<PasswordResetTokenEntity> PasswordResetTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>().HasMany(u => u.VerificationTokens).WithOne(rt => rt.User).HasForeignKey(vt => vt.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UserEntity>().HasMany(u => u.PasswordResetTokens).WithOne(rt => rt.User).HasForeignKey(rp => rp.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UserEntity>().HasMany(u => u.RefreshTokens).WithOne(rt => rt.User).HasForeignKey(rf => rf.UserId).OnDelete(DeleteBehavior.Cascade);
        base.OnModelCreating(modelBuilder);
    }
}