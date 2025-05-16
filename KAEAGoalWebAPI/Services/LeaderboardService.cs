using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using KAEAGoalWebAPI.Data;
using KAEAGoalWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KAEAGoalWebAPI.Services
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LeaderboardService> _logger;

        public LeaderboardService(ApplicationDbContext context, IConfiguration configuration, ILogger<LeaderboardService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        private DateTime GetBangkokTime()
        {
            var bangkokTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bangkokTimeZone);
        }

        public async Task ResetAndRewardLeaderboardAsync()
        {
            var bangkokTime = GetBangkokTime();
            var lastYear = bangkokTime.Year - 1;
            var startOfLastYear = new DateTime(lastYear, 1, 1);
            var endOfLastYear = new DateTime(lastYear, 12, 31);

            var topUsers = await _context.LEADERBOARDS
                .Where(lb => lb.MonthYear >= startOfLastYear && lb.MonthYear <= endOfLastYear)
                .OrderByDescending(lb => lb.Point)
                .Take(3)
                .ToListAsync();

            if (!topUsers.Any())
            {
                _logger.LogInformation("[LeaderboardResetTask] No leaderboard entries found for the past year. Skipping reset.");
                return;
            }

            _logger.LogInformation("[LeaderboardResetTask] Resetting leaderboard for the new year...");

            _context.LEADERBOARDS.RemoveRange(_context.LEADERBOARDS.Where(lb => lb.MonthYear >= startOfLastYear && lb.MonthYear <= endOfLastYear));
            await _context.SaveChangesAsync();

            _logger.LogInformation("[LeaderboardResetTask] Leaderboard reset completed successfully.");
        }

        //public async Task<List<LeaderboardViewModel>> GetCurrentLeaderboardAsync()
        //{
        //    var bangkokTime = GetBangkokTime();
        //    var startOfMonth = new DateTime(bangkokTime.Year, bangkokTime.Month, 1);
        //    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1); // Last day of the month

        //    //var ranking = await _context.LEADERBOARDS
        //    //    .Include(lb => lb.User)
        //    //    .Where(lb => lb.MonthYear >= startOfMonth && lb.MonthYear <= endOfMonth) // Compare DateTime correctly
        //    //    .OrderByDescending(lb => lb.Point)
        //    //    .ToListAsync();  // Get the data first
        //    //var ranking = await _context.uvw_LEADERBOARDS
        //    //   .Include(lb => lb.User)
        //    //   .Where(lb => lb.MonthYear >= startOfMonth && lb.MonthYear <= endOfMonth) // Compare DateTime correctly
        //    //   .ToListAsync();  // Get the data first
        //    var ranking = await _context.uvw_LEADERBOARDS
        //        .ToListAsync();


        //    // Now assign ranks manually after fetching data
        //    var leaderboardWithRank = ranking
        //        .Select((lb, index) => new LeaderboardViewModel
        //        {
        //            A_USER_ID = lb.A_USER_ID,
        //            FirstName = lb.FirstName,
        //            LastName = lb.LastName,
        //            DisplayName = lb.DisplayName,
        //            User_Name = lb.User_Name,
        //            Point = lb.Point,
        //            ImageUrls = lb.ImageUrls,
        //            Rank = lb.RankNo,
        //            PointThk = lb.PointThk,
        //        })
        //        .ToList();

        //    return leaderboardWithRank;
        //}

        public async Task<List<LeaderboardViewModel>> GetCurrentLeaderboardAsync()
        {
            var bangkokTime = GetBangkokTime();
            var startOfMonth = new DateTime(bangkokTime.Year, bangkokTime.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var ranking = await (from lb in _context.uvw_LEADERBOARDS
                                 join user in _context.USERS on lb.A_USER_ID equals user.A_USER_ID
                                 select new LeaderboardViewModel
                                 {
                                     A_USER_ID = lb.A_USER_ID,
                                     FirstName = lb.FirstName,
                                     LastName = lb.LastName,
                                     DisplayName = lb.DisplayName,
                                     User_Name = lb.User_Name,
                                     Point = lb.Point,
                                     ImageUrls = lb.ImageUrls,
                                     Rank = lb.RankNo,
                                     PointThk = lb.PointThk,
                                     DepartmentCode = user.DepartmentCode,
                                     BranchCode = user.BranchCode
                                 }).ToListAsync();

            return ranking;
        }


        public async Task<List<LeaderboardViewModel>> GetTop10LeaderboardAsync()
        {
            // ดึงข้อมูลทั้งหมดจาก GetCurrentLeaderboardAsync
            var currentLeaderboard = await GetCurrentLeaderboardAsync();

            // เลือกเฉพาะ 10 อันดับแรกจาก currentLeaderboard
            //var top10Leaderboard = currentLeaderboard.Take(10).ToList();
            var top10Leaderboard = currentLeaderboard.Where(rn => rn.Rank <= 10).ToList();

            return top10Leaderboard;
        }


        public async Task<LeaderboardViewModel> GetYourCurrentRankingAsync(Guid userId)
        {
            // ดึงข้อมูล leaderboard ทั้งหมดจาก GetCurrentLeaderboardAsync
            var leaderboard = await GetCurrentLeaderboardAsync();

            // หาข้อมูลของผู้ใช้ใน leaderboard โดยกรองตาม userId
            var userLeaderboard = leaderboard
                .FirstOrDefault(lb => lb.A_USER_ID == userId);  // กรองหาผู้ใช้จาก A_USER_ID

            if (userLeaderboard == null)
            {
                return null; // ถ้าผู้ใช้ไม่อยู่ใน leaderboard
            }

            return userLeaderboard; // ส่งคืนข้อมูลของผู้ใช้พร้อมอันดับ
        }










    }
}
