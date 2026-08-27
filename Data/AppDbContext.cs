using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PROJECT2106.Models;

namespace PROJECT2106.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Follow> Follows { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Place> Places { get; set; }
    public DbSet<PostMedia> PostMedia { get; set; }
    public DbSet<PlaceRating> PlaceRatings { get; set; }
    public DbSet<CustomCategory> CustomCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Follow>()
            .HasOne(f => f.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Follow>()
            .HasOne(f => f.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(f => f.FollowingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Follow>()
            .HasIndex(f => new { f.FollowerId, f.FollowingId })
            .IsUnique();

        builder.Entity<Like>()
            .HasIndex(l => new { l.UserId, l.PostId })
            .IsUnique();

        builder.Entity<Tag>()
            .HasIndex(t => t.Name)
            .IsUnique();

        builder.Entity<Place>()
            .HasOne(p => p.CreatedByUser)
            .WithMany(u => u.CreatedPlaces)
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CustomCategory>()
            .HasOne(c => c.CreatedByUser)
            .WithMany(u => u.CreatedCustomCategories)
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Place>()
            .HasOne(p => p.CustomCategory)
            .WithMany(c => c.Places)
            .HasForeignKey(p => p.CustomCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Place>()
            .HasMany(p => p.Posts)
            .WithOne(p => p.Place)
            .HasForeignKey(p => p.PlaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Place>()
            .ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Places_Latitude",
                    "\"Latitude\" >= -90 AND \"Latitude\" <= 90");

                t.HasCheckConstraint(
                    "CK_Places_Longitude",
                    "\"Longitude\" >= -180 AND \"Longitude\" <= 180");

                t.HasCheckConstraint(
                    "CK_Places_Name_NotBlank",
                    "length(btrim(\"Name\")) > 0");

                t.HasCheckConstraint(
                    "CK_Places_Category_ExactlyOne",
                    "(\"SystemCategory\" IS NOT NULL AND \"CustomCategoryId\" IS NULL) OR " +
                    "(\"SystemCategory\" IS NULL AND \"CustomCategoryId\" IS NOT NULL)");

                t.HasCheckConstraint(
                    "CK_Places_SystemCategory_Range",
                    "\"SystemCategory\" IS NULL OR (\"SystemCategory\" >= 0 AND \"SystemCategory\" <= 8)");
            });

        builder.Entity<PostMedia>()
            .HasOne(m => m.Post)
            .WithMany(p => p.Media)
            .HasForeignKey(m => m.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PostMedia>()
            .HasIndex(m => m.PostId);

        builder.Entity<PostMedia>()
            .ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_PostMedia_SizeBytes_Positive",
                    "\"SizeBytes\" > 0");

                t.HasCheckConstraint(
                    "CK_PostMedia_MediaType_Range",
                    "\"MediaType\" >= 0 AND \"MediaType\" <= 1");
            });

        builder.Entity<Post>()
            .HasMany(p => p.Tags)
            .WithMany(t => t.Posts)
            .UsingEntity(j => j.ToTable("PostTags"));

        builder.Entity<PlaceRating>()
        .HasOne(r => r.Place)
        .WithMany(p => p.Ratings)
        .HasForeignKey(r => r.PlaceId)
        .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PlaceRating>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PlaceRating>()
            .HasIndex(r => new { r.UserId, r.PlaceId })
            .IsUnique();

        builder.Entity<PlaceRating>()
        .ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_PlaceRatings_Value_Range",
                "\"Value\" >= 1 AND \"Value\" <= 5");
        });
    }
}
