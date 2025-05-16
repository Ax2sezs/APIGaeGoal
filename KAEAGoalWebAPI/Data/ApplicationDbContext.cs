using KAEAGoalWebAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Writers;

namespace KAEAGoalWebAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> USERS { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<CoinTransaction> COIN_TRANSACTIONS { get; set; }
        public DbSet<Mission> MISSIONS { get; set; }
        public DbSet<CodeMission> CODE_MISSIONS { get; set; }
        public DbSet<MissionImage> MISSION_IMAGES { get; set; }
        public DbSet<UserMission> USER_MISSIONS { get; set; }
        public DbSet<UserCodeMission> USER_CODE_MISSIONS { get; set; }
        public DbSet<QrCodeMission> QR_CODE_MISSIONS { get; set; }
        public DbSet<UserQRCodeMission> USER_QR_CODE_MISSIONS { get; set; }
        public DbSet<UserPhotoMission> USER_PHOTO_MISSIONS { get; set; }
        public DbSet<UserPhotoMissionImage> USER_PHOTO_MISSION_IMAGES { get; set; }
        public DbSet<Reward> REWARDS { get; set; }
        public DbSet<Reward_Category> REWARDS_Category { get; set; }
        public DbSet<RewardImage> REWARD_IMAGES { get; set; }
        public DbSet<UserReward> USER_REWARDS { get; set; }
        public DbSet<Leaderboard> LEADERBOARDS { get; set; }
        public DbSet<uvw_Leaderboard> uvw_LEADERBOARDS { get; set; }
        public DbSet<Department> DEPARTMENT { get; set; }
        public DbSet<UserVideoMission> USER_VIDEO_MISSIONS { get; set; }
        public DbSet<UserTextMission> USER_TEXT_MISSIONS { get; set; }
        public DbSet<FEEDLIKE>Feed_Likes { get; set; }
        public DbSet<HomeBanner> HomeBanners { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.USERS)
                .WithMany()
                .HasForeignKey(rt => rt.A_USER_ID)
                .IsRequired();

            // Parent table: Missions
            modelBuilder.Entity<Mission>()
                .ToTable("MISSIONS") // Map to parent table
                .HasKey(m => m.MISSION_ID);

            modelBuilder.Entity<CodeMission>()
                .HasOne(cm => cm.Mission)
                .WithOne(m => m.CodeMission)
                .HasForeignKey<CodeMission>(cm => cm.MISSION_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FEEDLIKE>()
                .ToTable("Feed_Likes")
                .HasKey(f => f.LIKE_ID);

            modelBuilder.Entity<HomeBanner>()
                .ToTable("HomeBanners")
                .HasKey(h => h.Id);

            modelBuilder.Entity<QrCodeMission>()
                .HasOne(qrm => qrm.Mission)
                .WithOne(m => m.QrCodeMission)
                .HasForeignKey<QrCodeMission>(qrm => qrm.MISSION_ID)
                .OnDelete(DeleteBehavior.Cascade);

            // MissionImage Table
            modelBuilder.Entity<MissionImage>()
                .ToTable("MISSION_IMAGES")
                .HasKey(mi => mi.IMAGE_ID);

            modelBuilder.Entity<MissionImage>()
                .HasOne(mi => mi.Mission)
                .WithMany(m => m.MISSION_IMAGES)
                .HasForeignKey(mi => mi.MISSION_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserMission>()
                .HasOne(um => um.Mission)  // Define the relationship with Mission
                .WithMany()  // Optional: Define the reverse relationship if needed
                .HasForeignKey(um => um.MISSION_ID);

            modelBuilder.Entity<UserMission>()
                .HasOne(um => um.User)
                .WithMany()
                .HasForeignKey(um => um.A_USER_ID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure UserCodeMission relationship with User
            modelBuilder.Entity<UserCodeMission>()
                .HasOne(ucm => ucm.User)
                .WithMany() // Assuming no navigation property in User for UserCodeMission
                .HasForeignKey(ucm => ucm.A_USER_ID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure UserCodeMission relationship with Mission
            modelBuilder.Entity<UserCodeMission>()
                .HasOne(ucm => ucm.Mission)
                .WithMany() // Assuming no navigation property in Mission for UserCodeMission
                .HasForeignKey(ucm => ucm.MISSION_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserQRCodeMission>()
                .HasOne(uqm => uqm.Mission)
                .WithMany()
                .HasForeignKey(uqm => uqm.MISSION_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserQRCodeMission>()
                .HasOne(uqm => uqm.User)
                .WithMany()
                .HasForeignKey(uqm => uqm.A_USER_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserQRCodeMission>()
                .HasOne(uqm => uqm.UserMission)
                .WithMany() // Assuming no collection in UserMission
                .HasForeignKey(uqm => uqm.USER_MISSION_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserPhotoMission>()
                .ToTable("USER_PHOTO_MISSIONS")
                .HasKey(upm => upm.USER_PHOTO_MISSION_ID);

            modelBuilder.Entity<UserPhotoMission>()
                .HasOne(upm => upm.Mission)
                .WithMany()
                .HasForeignKey(upm => upm.MISSION_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserPhotoMission>()
                .HasOne(upm => upm.User)
                .WithMany()
                .HasForeignKey(upm => upm.A_USER_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserPhotoMission>()
                .HasOne(upm => upm.UserMission)
                .WithMany()
                .HasForeignKey(upm => upm.USER_MISSION_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserPhotoMissionImage>()
                .ToTable("USER_PHOTO_MISSION_IMAGES")
                .HasKey(upmi => upmi.USER_PHOTO_MISSION_IMAGE_ID);

            modelBuilder.Entity<UserPhotoMissionImage>()
                .HasOne(upm => upm.UserPhotoMission)
                .WithMany(m => m.IMAGES)
                .HasForeignKey(upm => upm.USER_PHOTO_MISSION_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Reward>()
                .ToTable("REWARDS")
                .HasKey(r => r.REWARD_ID);

            modelBuilder.Entity<RewardImage>()
                .ToTable("REWARD_IMAGES")
                .HasKey(ri => ri.REWARD_IMAGE_ID);

            modelBuilder.Entity<RewardImage>()
                .HasOne(ri => ri.Reward)
                .WithMany(r => r.REWARD_IMAGES)
                .HasForeignKey(ri => ri.REWARD_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserReward>()
                .ToTable("USER_REWARDS")
                .HasKey(ur => ur.USER_REWARD_ID);

            modelBuilder.Entity<UserReward>()
                .HasOne(ur => ur.Reward)
                .WithMany()
                .HasForeignKey(ur => ur.REWARD_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserReward>()
                .HasOne(ur => ur.User)
                .WithMany()
                .HasForeignKey(ur => ur.A_USER_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Leaderboard>()
                .ToTable("LEADERBOARDS")
                .HasKey(l => l.LEADERBOARD_ID);

            modelBuilder.Entity<Leaderboard>()
                .HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(u => u.A_USER_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Department>()
                .ToTable("DEPARTMENT")
                .HasKey(d => d.DepartmentID);

            modelBuilder.Entity<UserVideoMission>()
                .ToTable("USER_VIDEO_MISSIONS")
                .HasKey(uvm => uvm.USER_VIDEO_MISSION_ID);

            modelBuilder.Entity<UserVideoMission>()
                .HasOne(uvm => uvm.User)
                .WithMany()
                .HasForeignKey(uvm => uvm.A_USER_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserVideoMission>()
                .HasOne(uvm => uvm.Mission)
                .WithMany()
                .HasForeignKey(uvm => uvm.MISSION_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserVideoMission>()
                .HasOne(uvm => uvm.UserMission)
                .WithMany()
                .HasForeignKey(uvm => uvm.USER_MISSION_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserTextMission>()
                .ToTable("USER_TEXT_MISSIONS")
                .HasKey(utm => utm.USER_TEXT_MISSION_ID);

            modelBuilder.Entity<UserTextMission>()
                .HasOne(utm => utm.User)
                .WithMany()
                .HasForeignKey(utm => utm.A_USER_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserTextMission>()
                .HasOne(utm => utm.Mission)
                .WithMany()
                .HasForeignKey(utm => utm.MISSION_ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserTextMission>()
                .HasOne(utm => utm.UserMission)
                .WithMany()
                .HasForeignKey(utm => utm.USER_MISSION_ID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reward>()
                 .HasOne(r => r.REWARD_CATEGORY) // Reward มี 1 Reward_Category
                .WithMany()
                .HasForeignKey(utm => utm.REWARDCate_Id);

            base.OnModelCreating(modelBuilder);
        }
    }

}
