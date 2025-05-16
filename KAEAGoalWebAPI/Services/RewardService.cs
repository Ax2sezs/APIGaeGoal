using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using KAEAGoalWebAPI.Data;
using KAEAGoalWebAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KAEAGoalWebAPI.Services
{
    public class RewardService : IRewardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public RewardService (ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            //_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _context = context;
            _configuration = configuration;
        }

        private DateTime GetBangkokTime()
        {
            var bangkokTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bangkokTimeZone);
        }

        public async Task<string> CreateRewardAsync(CreateRewardModel model)
        {
            var bangkokTime = GetBangkokTime();

            //if (model.ImageFile == null || !model.ImageFile.Any())
            //{
            //    var defaultImagePath = _configuration["AppSettings:DefaultMissionImage"];
            //    var defaultImageStream = new FileStream(defaultImagePath, FileMode.Open);
            //    var defaultImageFile = new FormFile(defaultImageStream, 0, defaultImageStream.Length, "DefaultImage", Path.GetFileName(defaultImagePath))
            //    {
            //        Headers = new HeaderDictionary(),
            //        ContentType = "image/png" // or appropriate content type
            //    };

            //    model.ImageFile = new List<IFormFile> { defaultImageFile };
            //}

            if (model.ImageFile == null || !model.ImageFile.Any())
            {
                throw new Exception("No image files were provided.");
            }

            var uploadedUrls = new List<string>();
            var allowedFileTypes = new[] {"image/jpeg", 
                "image/png", 
                "image/gif", 
                "image/webp", 
                "image/bmp", 
                "image/tiff", 
                "image/svg+xml", 
                "image/heif", 
                "image/heic" };
            long maxFileSize = 50 * 1024 * 1024;
            var uploadDirectory = _configuration["AppSettings:ImageRewardUploadPath"];

            var reward = new Reward
            {
                REWARD_ID = Guid.NewGuid(),
                REWARD_NAME = model.REWARD_NAME,
                PRICE = model.REWARD_PRICE,
                QUANTITY = model.QUANTITY,
                DESCRIPTION = model.DESCRIPTION,
                REWARD_IMAGES = new List<RewardImage>(),
                REWARDCate_Id = model.REWARDCate_Id,
            };

            foreach (var file in model.ImageFile)
            {
                if (file.Length > maxFileSize)
                {
                    throw new Exception($"File {file.FileName} is too large. Maximum size allowed is 50MB.");
                }

                if (!allowedFileTypes.Contains(file.ContentType))
                {
                    throw new Exception($"Invalid file type for {file.FileName}. Only JPEG, PNG GIF images are allowed.");
                }

                if (file.Length > 0)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var uploadPath = Path.Combine(uploadDirectory, uniqueFileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(uploadPath) ?? string.Empty);

                    await using (var stream = new FileStream(uploadPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var imagePath = _configuration["AppSettings:ImageRewardPath"];
                    var fileUrl = Path.Combine(imagePath, uniqueFileName);
                    uploadedUrls.Add(fileUrl);

                    reward.REWARD_IMAGES ??= new List<RewardImage>();
                    reward.REWARD_IMAGES.Add(new RewardImage
                    {
                        REWARD_IMAGE_ID = Guid.NewGuid(),
                        ImageUrls = fileUrl,
                        Uploaded_At = bangkokTime,
                    });
                }
            }

            _context.REWARDS.Add(reward);
            await _context.SaveChangesAsync();

            return "Reward registered";
        }

        public async Task<string> UpdateRewardAsync(Guid rewardId, CreateRewardModel model)
        {
            // ดึง Reward ที่ต้องการอัปเดต
            var reward = await _context.REWARDS
                .Include(r => r.REWARD_IMAGES)
                .FirstOrDefaultAsync(r => r.REWARD_ID == rewardId);

            if (reward == null)
                throw new ArgumentException("Reward not found.");

            if (model == null || string.IsNullOrWhiteSpace(model.REWARD_NAME))
                throw new ArgumentException("Invalid reward details.");

            // -------------------------------
            // จัดการรูปภาพ ถ้ามีการอัปโหลดรูปใหม่
            // -------------------------------
            if (model.ImageFile != null && model.ImageFile.Any())
            {
                var allowedFileTypes = new[] {
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "image/bmp", "image/tiff", "image/svg+xml",
            "image/heif", "image/heic"
        };
                long maxFileSize = 50 * 1024 * 1024; // 50MB
                var uploadDirectory = _configuration["AppSettings:ImageRewardUploadPath"];
                var newImageEntities = new List<RewardImage>();

                // ตรวจสอบไฟล์และอัปโหลด
                foreach (var file in model.ImageFile)
                {
                    if (file.Length > maxFileSize)
                        throw new ArgumentException($"File {file.FileName} is too large.");

                    if (!allowedFileTypes.Contains(file.ContentType))
                        throw new ArgumentException($"Invalid file type for {file.FileName}.");

                    var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var uploadPath = Path.Combine(uploadDirectory, uniqueFileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(uploadPath) ?? string.Empty);

                    await using (var stream = new FileStream(uploadPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var imagePath = _configuration["AppSettings:ImageRewardPath"];
                    var fileUrl = Path.Combine(imagePath, uniqueFileName);

                    newImageEntities.Add(new RewardImage
                    {
                        REWARD_IMAGE_ID = Guid.NewGuid(),
                        REWARD_ID = rewardId,  // เชื่อมโยงกับ Reward ที่กำลังอัปเดต
                        ImageUrls = fileUrl,
                        Uploaded_At = GetBangkokTime() // ใช้เวลาในกรุงเทพ
                    });
                }

                // 🔥 ลบรูปเก่า
                var oldImages = await _context.REWARD_IMAGES
                    .Where(img => img.REWARD_ID == rewardId)
                    .ToListAsync();

                if (oldImages.Any())
                {
                    _context.REWARD_IMAGES.RemoveRange(oldImages);
                    await _context.SaveChangesAsync(); // Save หลังลบ
                }

                // 🔥 เพิ่มรูปใหม่
                await _context.REWARD_IMAGES.AddRangeAsync(newImageEntities);
                await _context.SaveChangesAsync(); // Save หลังเพิ่ม
            }

            // -------------------------------
            // อัปเดตข้อมูล Reward ทั่วไป
            // -------------------------------
            reward.REWARD_NAME = model.REWARD_NAME;
            reward.PRICE = model.REWARD_PRICE;
            reward.QUANTITY = model.QUANTITY;
            reward.DESCRIPTION = model.DESCRIPTION;
            reward.REWARDCate_Id = model.REWARDCate_Id;

            await _context.SaveChangesAsync(); // Save การอัปเดตข้อมูล
            return "Reward updated successfully.";
        }




        public async Task<List<RewardCategoryViewModel>> GetAllRewardCategoryAsync()
        {
            var reward = await _context.REWARDS_Category 
                .ToListAsync();
             
            var model = reward.Select(m => new RewardCategoryViewModel
            {
                REWARDSCate_Id = m.REWARDSCate_Id,
                REWARDSCate_Name = m.REWARDSCate_Name,
                REWARDSCate_NameEn = m.REWARDSCate_NameEn, 

            }).ToList();

            return model;
        }
        public async Task<List<RewardViewModel>> GetAllRewardAsync()
        {
            var reward = await _context.REWARDS
                .Include(r => r.REWARD_IMAGES)
                .Include(r => r.REWARD_CATEGORY) // รวม Category เข้าไป
                .ToListAsync();

             

            var rewardQuantities = await _context.USER_REWARDS
                .GroupBy(r => r.REWARD_ID)
                .Select(g => new { RewardId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.RewardId, g => g.Count);




            var model = reward.Select(m => new RewardViewModel
            {
                Reward_Id = m.REWARD_ID,
                Reward_Name = m.REWARD_NAME,
                Reward_Description = m.DESCRIPTION,
                Reward_price = m.PRICE,
                Reward_quantity = m.QUANTITY - (rewardQuantities.ContainsKey(m.REWARD_ID) ? rewardQuantities[m.REWARD_ID] : 0),
                Reward_Image = m.REWARD_IMAGES.Select(img => img.ImageUrls).ToList(),
                REWARDCate_Id = m.REWARD_CATEGORY.REWARDSCate_Id,
                REWARDSCate_Name = m.REWARD_CATEGORY.REWARDSCate_Name,
                REWARDSCate_NameEn  = m.REWARD_CATEGORY.REWARDSCate_NameEn,
                Reward_Total = m.QUANTITY ,
                Reward_TotalRedeem = (rewardQuantities.ContainsKey(m.REWARD_ID) ? rewardQuantities[m.REWARD_ID] : 0),

            }).ToList();

            return model;
        }

        public async Task<string> RedeemRewardAsync(Guid userId, RedeemRewardModel model)
        {
            var bangkokTime = GetBangkokTime();

            //var redeemableMonth = int.Parse(_configuration["AppSettings:RedeemableMonth"]);
            //var currentMonth = bangkokTime.Month;

            //if (currentMonth != redeemableMonth)
            //    throw new Exception("Reward can only be redeemed in December.");

            var rewards = await _context.REWARDS
                .FirstOrDefaultAsync(r => r.REWARD_ID == model.Reward_Id);

            var kaeaCoinBalance = await _context.COIN_TRANSACTIONS
                .Where(c => c.A_USER_ID == userId && c.Coin_Type == CoinType.KaeaCoin)
                .SumAsync(c => c.Amount);

            if (rewards == null)
                throw new Exception("Reward not found.");

            if (kaeaCoinBalance == null)
                throw new Exception("User Coin Balance not found");

            if (kaeaCoinBalance < rewards.PRICE)
                throw new Exception("Insufficient Coin");

            var redeemedCount = _context.USER_REWARDS.Count(ur => ur.REWARD_ID == rewards.REWARD_ID);

            if (redeemedCount >= rewards.QUANTITY)
            {
                throw new Exception("This reward has reached the limit for redemption.");
            }

            var userReward = new UserReward
            {
                USER_REWARD_ID = Guid.NewGuid(),
                A_USER_ID = userId,
                REWARD_ID = model.Reward_Id,
                REDEEMED_AT = bangkokTime,
                STATUS = "Redeemed"
            };

            _context.USER_REWARDS.Add(userReward);

            var coinTransaction = new CoinTransaction
            {
                COIN_TRANSACTION_ID = Guid.NewGuid(),
                Amount = -rewards.PRICE,
                Transaction_Date = bangkokTime,
                Transaction_Type = "Redeem",
                Description = $"Redeemed Reward: {rewards.REWARD_NAME}.",
                A_USER_ID = userId,
                Coin_Type = CoinType.KaeaCoin
            };

            _context.COIN_TRANSACTIONS.Add(coinTransaction);
            await _context.SaveChangesAsync();

            return "Redeemed successfully";
        }

        public async Task<List<UserRewardViewModel>> GetUserRewardAsync(Guid userId)
        {
            var userReward = await _context.USER_REWARDS
                .Where(ur => ur.A_USER_ID == userId)
                .Include(r => r.Reward)
                .ThenInclude(r => r.REWARD_IMAGES)
                .Include(r => r.Reward)
                .ThenInclude(r => r.REWARD_CATEGORY)
                .Select(ur => new  UserRewardViewModel
                {
                    User_reward_Id = ur.USER_REWARD_ID,
                    Reward_Name = ur.Reward.REWARD_NAME,
                    Reward_Description = ur.Reward.DESCRIPTION,
                    Reward_Price = ur.Reward.PRICE,
                    Reward_Status = ur.STATUS,
                    Redeem_Date = ur.REDEEMED_AT,
                    OnDelivery_Date = ur.OnDelivery_AT,
                    Delivered_Date = ur.Delivered_AT,
                    Collect_Date = ur.COLLECT_AT,
                    Image = ur.Reward.REWARD_IMAGES.Select(img => img.ImageUrls).ToList(),
                    REWARDCate_Id = ur.Reward.REWARD_CATEGORY.REWARDSCate_Id,
                    REWARDCate_Name = ur.Reward.REWARD_CATEGORY.REWARDSCate_Name,
                    User_Firstname = ur.User.FirstName,
                    User_SurName = ur.User.LastName,
                    DepartmentCode = ur.User.DepartmentCode,
                    Department = ur.User.Department,
                })
                .ToListAsync();

            return (userReward);
        }

        public async Task<string> ChangeStatusUserRewardAsync(Guid userId, ChangeStatusUserReward model)
        {
            var userRewards = await _context.USER_REWARDS
                .Include(r => r.Reward)
                .ThenInclude(r => r.REWARD_IMAGES)
                .FirstOrDefaultAsync(r => r.USER_REWARD_ID == model.USER_REWARD_ID);

            if (userRewards == null)
                throw new Exception("User reward not found.");

            userRewards.STATUS = model.STATUS;
            await _context.SaveChangesAsync();

            return "Changed status successfully";
        }

        public async Task<List<UserRewardViewModel>> GetAllUserRewardsAsync()
        {
            var userReward = await _context.USER_REWARDS
                .Include(r => r.Reward)
                .ThenInclude(r => r.REWARD_IMAGES)
                .Include(r => r.Reward)
                .ThenInclude(r => r.REWARD_CATEGORY)  
                .Select(ur => new UserRewardViewModel
                {
                    User_reward_Id = ur.USER_REWARD_ID,
                    Reward_Name = ur.Reward.REWARD_NAME,
                    Reward_Description = ur.Reward.DESCRIPTION,
                    Reward_Price = ur.Reward.PRICE,
                    Reward_Status = ur.STATUS,
                    Redeem_Date = ur.REDEEMED_AT,
                    Collect_Date = ur.COLLECT_AT,
                    Image = ur.Reward.REWARD_IMAGES.Select(img => img.ImageUrls).ToList(),
                    REWARDCate_Id = ur.Reward.REWARD_CATEGORY.REWARDSCate_Id,
                    REWARDCate_Name = ur.Reward.REWARD_CATEGORY.REWARDSCate_Name, 
                    USER_NAME = ur.User.LOGON_NAME,
                    User_Firstname = ur.User.FirstName,
                    User_SurName = ur.User.LastName,
                    Department = ur.User.Department, 
                    DepartmentCode = ur.User.DepartmentCode,
                })
                .ToListAsync();

            return (userReward);
        }

        public async Task<string> ConfirmToPickup(Guid userId, CollectRewardModel model)
        {
            var bangkokTime = GetBangkokTime();

            var user = await _context.USERS
                .FirstOrDefaultAsync(u => u.A_USER_ID == userId);

            if (user == null)
                throw new Exception("User not found");

            var userReward = await _context.USER_REWARDS
                .FirstOrDefaultAsync(ur => ur.A_USER_ID == user.A_USER_ID && ur.USER_REWARD_ID == model.USER_REWARD_ID);

            if (userReward == null)
                throw new Exception("User reward not found.");

            if (userReward.IsCollect != null)
                throw new Exception("This reward have already collected.");

            if (!string.Equals(userReward.STATUS, "Ready to pickup.", StringComparison.OrdinalIgnoreCase)) //Status string must be "Ready to pickup."
                return "Reward is not ready for pickup.";

            userReward.COLLECT_AT = bangkokTime;
            userReward.IsCollect = true;
            userReward.STATUS = "Collected";

            _context.SaveChanges();
            await _context.SaveChangesAsync();

            return "Confirm pickup reward successfully.";
        }
    }
}
