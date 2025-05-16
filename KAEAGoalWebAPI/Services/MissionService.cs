using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ImageMagick;
using KAEAGoalWebAPI.Data;
using KAEAGoalWebAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QRCoder;

namespace KAEAGoalWebAPI.Services
{
    public class MissionService : IMissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MissionService> _logger;
        private readonly IConfiguration _configuration;

        public MissionService(ApplicationDbContext context, ILogger<MissionService> logger, IConfiguration configuration)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _context = context;
            _logger = logger;
            _configuration = configuration;

            _logger.LogInformation("Configuration injected into MissionService.");
        }

        private DateTime GetBangkokTime()
        {
            var bangkokTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bangkokTimeZone);
        }

        private string GenerateQRCode(string missionId)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(missionId, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);

                var base64QRCode = Convert.ToBase64String(qrCodeBytes);
                return $"data:image/png;base64,{base64QRCode}";
            }
        }

        private string GenerateCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVYXZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 5)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public MissionService(ApplicationDbContext context, ILogger<MissionService> logger)
        {
            _context = context;
            _logger = logger;
        }



        public async Task<Guid> CreateMissionAsync(Guid userId, CreateMissionModel model)

        {
            if (model == null || string.IsNullOrWhiteSpace(model.MISSION_NAME))
                throw new ArgumentException("Invalid mission details.");

            if (model.Expire_Date <= model.Start_Date)
                throw new ArgumentException("Expire date must be later than start date.");

            if (!new[] { "Code", "QR", "Photo", "Video", "Text" }.Contains(model.MISSION_TYPE))
                throw new ArgumentException("Invalid mission type.");

            if (!new[] { "All", "AUOF", "AUFC", "AUBR" }.Contains(model.Participate_Type))
                throw new ArgumentException("Invalid participate type");
            var bangkokTime = GetBangkokTime();

            var mission = new Mission
            {
                MISSION_ID = Guid.NewGuid(),
                MISSION_NAME = model.MISSION_NAME,
                MISSION_TYPE = model.MISSION_TYPE,
                Coin_Reward = model.Coin_Reward,
                Mission_Point = model.Mission_Point,
                Start_Date = model.Start_Date,
                Expire_Date = model.Expire_Date,
                Description = model.Description,
                Is_Limited = model.Is_Limited,
                Created_At = bangkokTime,
                Participate_Type = model.Participate_Type,
                Missioner = userId,
                MISSION_IMAGES = model.ImageUrls.Select(url => new MissionImage
                {
                    IMAGE_ID = Guid.NewGuid(),
                    ImageUrl = url,
                    Uploaded_Date = bangkokTime
                }).ToList() ?? new List<MissionImage>(),
                Accept_limit = model.Accept_limit,
                MISSION_Buffer = model.MISSION_Buffer,
                MISSION_TypeCoin = model.MISSION_TypeCoin ?? 0,
                Is_Public = model.Is_Public,
                Is_Winners = model.Is_Winners,
                WinnerSt = model.WinnerSt,
                WinnerNd = model.WinnerNd,
                WinnerRd = model.WinnerRd,
                WinnerStCoin = model.WinnerStCoin,
                WinnerNdCoin = model.WinnerNdCoin,
                WinnerRdCoin = model.WinnerRdCoin,

            };

            _context.MISSIONS.Add(mission);
            await _context.SaveChangesAsync();

            if (model.MISSION_TYPE == "Code")
            {
                var codeMission = new CodeMission
                {
                    MISSION_ID = mission.MISSION_ID,
                    Code_Mission_Code = model.Code_Mission_Code
                };

                _context.CODE_MISSIONS.Add(codeMission);
                await _context.SaveChangesAsync(); // Save child
            }
            else if (model.MISSION_TYPE == "QR")
            {
                var qrCode = GenerateQRCode(mission.MISSION_ID.ToString());

                var qrMission = new QrCodeMission
                {
                    QR_MISSION_ID = Guid.NewGuid(),
                    MISSION_ID = mission.MISSION_ID,
                    QRCode = qrCode,
                    QRCodeText = mission.MISSION_ID.ToString()
                };

                _context.QR_CODE_MISSIONS.Add(qrMission);
                await _context.SaveChangesAsync();
            }

            return mission.MISSION_ID;
        }
        public async Task UpdateMissionAsync(Guid missionId, CreateMissionModel model)
        {
            var mission = await _context.MISSIONS
                .Include(m => m.MISSION_IMAGES)
                .FirstOrDefaultAsync(m => m.MISSION_ID == missionId);

            if (mission == null)
                throw new ArgumentException("Mission not found.");

            if (model == null || string.IsNullOrWhiteSpace(model.MISSION_NAME))
                throw new ArgumentException("Invalid mission details.");

            if (model.Expire_Date <= model.Start_Date)
                throw new ArgumentException("Expire date must be later than start date.");

            // -------------------------------
            // จัดการรูปภาพ ถ้ามีการอัปโหลดรูปใหม่
            // -------------------------------
            if (model.Images != null && model.Images.Any())
            {
                var allowedFileTypes = new[] {
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "image/bmp", "image/tiff", "image/svg+xml",
            "image/heif", "image/heic"
        };
                long maxFileSize = 50 * 1024 * 1024; // 50MB
                var uploadDirectory = _configuration["AppSettings:ImageUploadPath"];
                var newImageEntities = new List<MissionImage>();

                foreach (var file in model.Images)
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

                    var imagePath = _configuration["AppSettings:ImagePath"];
                    var fileUrl = Path.Combine(imagePath, uniqueFileName);

                    newImageEntities.Add(new MissionImage
                    {
                        IMAGE_ID = Guid.NewGuid(),
                        MISSION_ID = missionId, // **💬 ใส่ MISSION_ID ให้รูปใหม่**
                        ImageUrl = fileUrl,
                        Uploaded_Date = GetBangkokTime()
                    });
                }

                // 🔥 ลบรูปเก่าก่อน
                var oldImages = await _context.MISSION_IMAGES
                    .Where(img => img.MISSION_ID == missionId)
                    .ToListAsync();

                if (oldImages.Any())
                {
                    _context.MISSION_IMAGES.RemoveRange(oldImages);
                    await _context.SaveChangesAsync(); // Save หลังลบ
                }

                // 🔥 เพิ่มรูปใหม่
                await _context.MISSION_IMAGES.AddRangeAsync(newImageEntities);
                await _context.SaveChangesAsync(); // Save หลังเพิ่ม
            }

            // -------------------------------
            // อัปเดตข้อมูล Mission ทั่วไป
            // -------------------------------
            mission.MISSION_NAME = model.MISSION_NAME;
            mission.MISSION_TYPE = model.MISSION_TYPE;
            mission.Coin_Reward = model.Coin_Reward;
            mission.Mission_Point = model.Mission_Point;
            mission.Start_Date = model.Start_Date;
            mission.Expire_Date = model.Expire_Date;
            mission.Description = model.Description;
            mission.Is_Limited = model.Is_Limited;
            mission.Participate_Type = model.Participate_Type;
            mission.Accept_limit = model.Accept_limit;
            mission.MISSION_Buffer = model.MISSION_Buffer;
            mission.MISSION_TypeCoin = model.MISSION_TypeCoin ?? 0;
            mission.Is_Public = model.Is_Public;
            mission.Is_Winners = model.Is_Winners;
            mission.WinnerSt = model.WinnerSt;
            mission.WinnerNd = model.WinnerNd;
            mission.WinnerRd = model.WinnerRd;
            mission.WinnerStCoin = model.WinnerStCoin;
            mission.WinnerNdCoin = model.WinnerNdCoin;
            mission.WinnerRdCoin = model.WinnerRdCoin;

            await _context.SaveChangesAsync(); // Save การอัปเดตข้อมูล
        }




        public async Task<List<MissionViewModel>> GetAllMissionsAsync()
        {
            var missions = await _context.MISSIONS
                //.Where(m => m.Expire_Date < DateTime.UtcNow)
                .Include(m => m.MISSION_IMAGES)   // Include images if necessary
                .Include(m => m.CodeMission)      // Include the related CodeMission if necessary
                .Include(m => m.QrCodeMission)
                .ToListAsync();

            var model = missions.Select(m => new MissionViewModel
            {
                MISSION_ID = m.MISSION_ID,
                MISSION_NAME = m.MISSION_NAME,
                MISSION_TYPE = m.MISSION_TYPE,
                Coin_Reward = m.Coin_Reward,
                Mission_Point = m.Mission_Point,
                Start_Date = m.Start_Date,
                Expire_Date = m.Expire_Date,
                Description = m.Description,
                Is_Limited = m.Is_Limited,
                MissionImages = m.MISSION_IMAGES.Select(img => img.ImageUrl).ToList(),  // Map images to URLs
                CodeMission = m.CodeMission?.Code_Mission_Code, // If you have a CodeMission, map it
                QrMission = m.QrCodeMission?.QRCode,
                Accept_limit = m.Accept_limit ?? 0,
                Current_Accept = _context.USER_MISSIONS.Count(um => um.MISSION_ID == m.MISSION_ID && um.Submitted_At != null),
                Participate_Type = m.Participate_Type,
                MISSION_Buffer = m.MISSION_Buffer,
                MISSION_TypeCoin = m.MISSION_TypeCoin ?? 0,
                Is_Public = m.Is_Public,
                WinnerSt = m.WinnerSt,
                WinnerNd = m.WinnerNd,
                WinnerRd = m.WinnerRd,
                WinnerStCoin = m.WinnerStCoin,
                WinnerNdCoin = m.WinnerNdCoin,
                WinnerRdCoin = m.WinnerRdCoin,
            }).ToList();

            return model;
        }


        public async Task<List<MissionViewModel>> GetUnacceptedMissionsAsync(Guid userId)
        {
            var bangkokTime = GetBangkokTime();


            var user = await _context.USERS
                .FirstOrDefaultAsync(u => u.A_USER_ID == userId);

            // Fetch missions the user has already accepted
            var acceptedMissionIds = await _context.USER_MISSIONS
                .Where(um => um.A_USER_ID == userId)
                .Select(um => um.MISSION_ID)
                .ToListAsync();

            // Fetch missions that are not in the accepted list
            var missions = await _context.MISSIONS
                .Where(m => !acceptedMissionIds.Contains(m.MISSION_ID) && m.Expire_Date >= bangkokTime && (m.Participate_Type == "All" || m.Participate_Type == user.BranchCode||user.IsAdmin==9)) // Filter out accepted missions
                .Include(m => m.MISSION_IMAGES)   // Include images if necessary
                .Include(m => m.CodeMission)      // Include the related CodeMission if necessary
                .ToListAsync();

            // Map to view model
            var model = missions.Select(m => new MissionViewModel
            {
                MISSION_ID = m.MISSION_ID,
                MISSION_NAME = m.MISSION_NAME,
                MISSION_TYPE = m.MISSION_TYPE,
                Coin_Reward = m.Coin_Reward,
                Mission_Point = m.Mission_Point,
                Start_Date = m.Start_Date,
                Expire_Date = m.Expire_Date,
                Description = m.Description,
                Is_Limited = m.Is_Limited,
                MissionImages = m.MISSION_IMAGES.Select(img => img.ImageUrl).ToList(),  // Map images to URLs
                CodeMission = m.CodeMission?.Code_Mission_Code,
                Accept_limit = (int)m.Accept_limit,
                Current_Accept = _context.USER_MISSIONS.Count(um => um.MISSION_ID == m.MISSION_ID&&um.Submitted_At!=null),
                Participate_Type = m.Participate_Type,
                MISSION_Buffer = m.MISSION_Buffer,
                MISSION_TypeCoin = m.MISSION_TypeCoin ?? 0,
                Is_Public = m.Is_Public,
            }).ToList();

            return model;
        }

        //        public async Task AcceptMissionAsync(Guid userId, AcceptMissionModel model)
        //{
        //    var bangkokTime = GetBangkokTime();

        //    using (var transaction = await _context.Database.BeginTransactionAsync())
        //    {
        //        try
        //        {
        //            var user = await _context.USERS
        //                .FirstOrDefaultAsync(u => u.A_USER_ID == userId);

        //            var mission = await _context.MISSIONS
        //                .FirstOrDefaultAsync(m => m.MISSION_ID == model.MISSION_ID);

        //            if (mission == null)
        //                throw new Exception("Mission not found.");

        //            if (bangkokTime >= mission.Expire_Date)
        //                throw new Exception("Cannot accept the mission as it has already expired.");

        //            if (bangkokTime < mission.Start_Date)
        //                throw new Exception("Cannot accept the mission as it not started.");

        //            if (mission.Accept_limit.HasValue && 
        //                (await _context.USER_MISSIONS.CountAsync(um => um.MISSION_ID == mission.MISSION_ID)) >= mission.Accept_limit.Value)
        //                throw new Exception("This mission has reached acceptance limit");

        //            if (mission.Participate_Type != "All" && mission.Participate_Type != user.Site)
        //                throw new Exception("You are not eligible to participate in this mission.");

        //            var existingUserMission = await _context.USER_MISSIONS
        //                .FirstOrDefaultAsync(um => um.A_USER_ID == model.A_USER_ID && um.MISSION_ID == model.MISSION_ID);

        //            if (existingUserMission != null)
        //                throw new Exception("You have already accepted this mission.");

        //            var userMission = new UserMission
        //            {
        //                USER_MISSION_ID = Guid.NewGuid(),
        //                A_USER_ID = model.A_USER_ID,
        //                MISSION_ID = model.MISSION_ID,
        //                Verification_Status = "In progress",
        //                Accepted_Date = bangkokTime,
        //                Is_Collect = false
        //            };

        //            _context.USER_MISSIONS.Add(userMission);

        //            mission.Current_Accept = (mission.Current_Accept ?? 0) + 1;

        //            await _context.SaveChangesAsync();

        //            // ✅ Commit transaction เมื่อทุกอย่างผ่าน
        //            await transaction.CommitAsync();
        //        }
        //        catch (Exception ex)
        //        {
        //            // ❌ Rollback transaction เมื่อเกิดข้อผิดพลาด
        //            await transaction.RollbackAsync();
        //            throw new Exception("Error updating mission acceptance: " + ex.Message);
        //        }
        //    }
        //}

        public async Task AcceptMissionAsync(Guid userId, AcceptMissionModel model)
        {
            var bangkokTime = GetBangkokTime();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // เริ่มต้นการอ่านข้อมูลผู้ใช้
                    var user = await _context.USERS
                        .FirstOrDefaultAsync(u => u.A_USER_ID == userId);

                    // ใช้ SQL Locking สำหรับการดึงข้อมูล mission โดยใช้ UPDLOCK และ ROWLOCK
                    var mission = await _context.MISSIONS
                        .FromSqlRaw("SELECT * FROM MISSIONS WITH (UPDLOCK, ROWLOCK) WHERE MISSION_ID = {0}", model.MISSION_ID)
                        .FirstOrDefaultAsync();

                    if (mission == null)
                        throw new Exception("Mission not found.");

                    // ตรวจสอบว่า Mission หมดอายุหรือยัง
                    if (bangkokTime >= mission.Expire_Date)
                        throw new Exception("Cannot accept the mission as it has already expired.");

                    if (bangkokTime < mission.Start_Date)
                        throw new Exception("Cannot accept the mission as it has not started.");

                    // ตรวจสอบจำนวนการยอมรับการเข้าร่วม Mission หากถึง limit แล้วให้ไม่สามารถยอมรับได้
                    if (mission.Accept_limit.HasValue &&
    //(await _context.USER_MISSIONS.CountAsync(um => um.MISSION_ID == mission.MISSION_ID)) >= mission.Accept_limit.Value)
    //_context.USER_MISSIONS.Count(um => um.MISSION_ID == m.MISSION_ID && um.Submitted_At != null)
    (await _context.USER_MISSIONS.CountAsync(um => um.MISSION_ID == mission.MISSION_ID && um.Submitted_At != null) >= mission.Accept_limit.Value))

                        throw new Exception("This mission has reached acceptance limit");

                    // ตรวจสอบว่า user นี้มีการยอมรับ mission นี้แล้วหรือไม่
                    var existingUserMission = await _context.USER_MISSIONS
                        .FirstOrDefaultAsync(um => um.A_USER_ID == model.A_USER_ID && um.MISSION_ID == model.MISSION_ID);

                    if (existingUserMission != null)
                        throw new Exception("You have already accepted this mission.");

                    // สร้างการยอมรับ mission ใหม่
                    var userMission = new UserMission
                    {
                        USER_MISSION_ID = Guid.NewGuid(),
                        A_USER_ID = model.A_USER_ID,
                        MISSION_ID = model.MISSION_ID,
                        Verification_Status = "In progress",
                        Accepted_Date = bangkokTime,
                        Is_Collect = false
                    };

                    // เพิ่มข้อมูลการยอมรับ mission ของ user
                    _context.USER_MISSIONS.Add(userMission);

                    // อัพเดตจำนวนการยอมรับของ mission
                    //mission.Current_Accept = (mission.Current_Accept ?? 0) + 1;

                    // บันทึกข้อมูล
                    await _context.SaveChangesAsync();

                    // ✅ Commit transaction เมื่อทุกอย่างผ่าน
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    // ❌ Rollback transaction เมื่อเกิดข้อผิดพลาด
                    await transaction.RollbackAsync();
                    throw new Exception("Error updating mission acceptance: " + ex.Message);
                }
            }
        }


        public async Task<List<UserMissionViewModel>> GetUserMissionsAsync(Guid userId)
        {
            return await _context.USER_MISSIONS
                .Where(um => um.A_USER_ID == userId && um.Completed_Date == null)
                .Include(um => um.User)
                .Include(um => um.Mission)
                .ThenInclude(m => m.MISSION_IMAGES)
                .Select(um => new UserMissionViewModel
                {
                    A_USER_ID = um.A_USER_ID,
                    LOGON_NAME = um.User.LOGON_NAME,
                    USER_MISSION_ID = um.USER_MISSION_ID,
                    MISSION_ID = um.MISSION_ID,
                    Coin_Reward = um.Mission.Coin_Reward,
                    Point_Reward = um.Mission.Mission_Point,
                    MISSION_TypeCoin = um.Mission.MISSION_TypeCoin,
                    Mission_Name = um.Mission.MISSION_NAME,
                    Mission_Type = um.Mission.MISSION_TYPE,
                    Description = um.Mission.Description,
                    Expire_Date = um.Mission.Expire_Date,
                    Mission_Image = um.Mission.MISSION_IMAGES.Select(img => img.ImageUrl).ToList(),
                    Verification_Status = um.Verification_Status,
                    Accepted_Date = um.Accepted_Date,
                    Submitted_At = um.Submitted_At,
                    Completed_Date = um.Completed_Date,
                    Is_Collect = um.Is_Collect, 
                    Accept_limit = um.Mission.Accept_limit ?? 0,
                    Current_Accept = _context.USER_MISSIONS.Count(uum=>uum.MISSION_ID==um.MISSION_ID&&uum.Submitted_At!=null),
                    Accepted_Desc = um.Accepted_Desc,
                    Is_Public = um.Mission.Is_Public,
                })
                .ToListAsync();
        }

        public async Task<List<UserMissionViewModel>> GetCompletedMissionsAsync(Guid userId)
        {
            var completedMission = await _context.USER_MISSIONS
                .Where(um => um.A_USER_ID == userId && um.Completed_Date != null)  // Check if Completed_Date is not null
                .Include(um => um.Mission)   // Include the related Mission data
                .ThenInclude(m => m.MISSION_IMAGES)   // Include Mission Images
                .ToListAsync();

            var model = completedMission.Select(um => new UserMissionViewModel
            {
                A_USER_ID = um.A_USER_ID,
                USER_MISSION_ID = um.USER_MISSION_ID,
                MISSION_ID = um.MISSION_ID,
                Mission_Name = um.Mission.MISSION_NAME,
                Mission_Type = um.Mission.MISSION_TYPE,
                Description = um.Mission.Description,
                Coin_Reward = um.Mission.Coin_Reward,
                Point_Reward = um.Mission.Mission_Point,
                MISSION_TypeCoin = um.Mission.MISSION_TypeCoin,
                Mission_Image = um.Mission.MISSION_IMAGES.Select(img => img.ImageUrl).ToList(),  // Map images to URLs
                Verification_Status = um.Verification_Status,
                Accepted_Date = um.Accepted_Date,
                Completed_Date = um.Completed_Date,
                Is_Collect = um.Is_Collect,
                Is_Public = um.Mission.Is_Public,
            }).ToList();

            return model;
        }

        public async Task<List<UserMissionViewModel>> GetAllUserMissionsAsync()
        {
            var model = await _context.USER_MISSIONS
                .Include(um => um.Mission)
                .ThenInclude(m => m.MISSION_IMAGES)
                .Select(um => new UserMissionViewModel
                {
                    A_USER_ID = um.A_USER_ID,
                    LOGON_NAME = um.User.LOGON_NAME,
                    USER_MISSION_ID = um.USER_MISSION_ID,
                    MISSION_ID = um.MISSION_ID,
                    Coin_Reward = um.Mission.Coin_Reward,
                    Point_Reward = um.Mission.Mission_Point,
                    MISSION_TypeCoin = um.Mission.MISSION_TypeCoin,
                    Mission_Name = um.Mission.MISSION_NAME,
                    Mission_Type = um.Mission.MISSION_TYPE,
                    Mission_Image = um.Mission.MISSION_IMAGES.Select(img => img.ImageUrl).ToList(),
                    Verification_Status = um.Verification_Status,
                    Current_Accept = _context.USER_MISSIONS.Count(x => x.MISSION_ID == um.MISSION_ID && x.Submitted_At != null),
                    Accepted_Date = um.Accepted_Date,
                    Completed_Date = um.Completed_Date,
                    Is_Collect = um.Is_Collect
                })
                .ToListAsync();

            return model;
        }

        public async Task<string> ExecuteCodeMissionAsync(Guid userId, ExecuteCodeMissionModel model)
        {
            var bangkokTime = GetBangkokTime();

            _logger.LogInformation($"User {userId} is attempting to execute mission {model.MissionId}.");

            var mission = await _context.MISSIONS
                .Include(m => m.CodeMission)
                .FirstOrDefaultAsync(m => m.MISSION_ID == model.MissionId);

            if (mission == null)
            {
                _logger.LogWarning($"Mission {model.MissionId} not found for User {userId}.");
                throw new Exception("Mission not found.");
            }

            if (bangkokTime >= mission.Expire_Date)
            {
                _logger.LogWarning($"Mission {model.MissionId} is expired.");
                throw new Exception("Mission is expired.");
            }

            if (bangkokTime < mission.Start_Date)
            {
                _logger.LogWarning($"Mission {model.MissionId} is not started yet.");
                throw new Exception("Mission is not started.");
            }

            if (mission.CodeMission == null ||
    !mission.CodeMission.Code_Mission_Code
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(c => c.Trim())
        .Contains(model.MissionCode.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Invalid code for Mission {model.MissionId} by User {userId}.");
                throw new Exception("Invalid mission code.");
            }


            // Check if user already accepted and completed this mission
            var userMission = await _context.USER_MISSIONS
                .FirstOrDefaultAsync(um => um.A_USER_ID == userId && um.MISSION_ID == model.MissionId);

            if (userMission == null)
            {
                _logger.LogWarning($"Mission {model.MissionId} was not accepted by User {userId}.");
                throw new Exception("Mission not accepted by the user.");
            }

            if (userMission.Completed_Date != null)
            {
                _logger.LogWarning($"Mission {model.MissionId} is already completed by User {userId}.");
                throw new Exception("Mission already completed.");
            }

            var existingCodeMission = await _context.USER_CODE_MISSIONS
                .FirstOrDefaultAsync(ucm => ucm.A_USER_ID == userId && ucm.MISSION_ID == model.MissionId);

            if (existingCodeMission != null)
            {
                throw new Exception("User has already submitted a code for this mission.");
            }

            //var bangkokTime = GetBangkokTime();

            // Update mission status
            userMission.Completed_Date = bangkokTime;
            userMission.Submitted_At = bangkokTime;
            userMission.Verification_Status = "Completed";

            //await _context.SaveChangesAsync();

            var userCodeMission = new UserCodeMission
            {
                USER_CODE_MISSION_ID = Guid.NewGuid(),
                A_USER_ID = userId,
                MISSION_ID = model.MissionId,
                Code = model.MissionCode,
                Submit_At = bangkokTime
            };

            _context.USER_CODE_MISSIONS.Add(userCodeMission);
            //await _context.SaveChangesAsync();

            var cointype = mission.MISSION_TypeCoin ?? 0;
            var pointTransaction_coin = new CoinTransaction
            {
                COIN_TRANSACTION_ID = Guid.NewGuid(),
                Amount = mission.Mission_Point,
                Transaction_Date = bangkokTime,
                Transaction_Type = cointype == 0 ? "Mission Reward" : "Receive from Mission",
                Description = $"{(cointype == 0 ? "Mission Reward" : "Receive from Mission")} : {mission.MISSION_NAME}",
                A_USER_ID = userCodeMission.A_USER_ID,
                Coin_Type = cointype == 0 ? CoinType.KaeaCoin : CoinType.ThankCoin
            };
            _context.COIN_TRANSACTIONS.Add(pointTransaction_coin);

            var pointTransaction = new CoinTransaction
            {
                COIN_TRANSACTION_ID = Guid.NewGuid(),
                Amount = mission.Mission_Point,
                Transaction_Date = bangkokTime,
                Transaction_Type = "Mission Reward",
                Description = $"Reward for mission: {userMission.Mission.MISSION_NAME}",
                A_USER_ID = userId,
                Coin_Type = CoinType.MissionPoint
            };

            _context.COIN_TRANSACTIONS.Add(pointTransaction);
            await _context.SaveChangesAsync();

            var pointToAdd = await _context.COIN_TRANSACTIONS
                .Where(c => c.A_USER_ID == userId && c.Coin_Type == CoinType.MissionPoint &&
                            c.Transaction_Date.Month == bangkokTime.Month &&
                            c.Transaction_Date.Year == bangkokTime.Year)
                .SumAsync(c => c.Amount);

            var startOfMonth = new DateTime(bangkokTime.Year, bangkokTime.Month, 1); // First day of the month

            var leaderboardEntry = await _context.LEADERBOARDS
                .FirstOrDefaultAsync(lb => lb.A_USER_ID == userId && lb.MonthYear == startOfMonth); // Compare DateTime correctly

            if (leaderboardEntry == null)
            {
                leaderboardEntry = new Leaderboard
                {
                    LEADERBOARD_ID = Guid.NewGuid(),
                    A_USER_ID = userId,
                    Point = pointToAdd,
                    MonthYear = startOfMonth, // Store DateTime instead of string
                    Create_at = bangkokTime,
                };
                _context.LEADERBOARDS.Add(leaderboardEntry);
            }
            else
            {
                leaderboardEntry.Point = pointToAdd;
                _context.LEADERBOARDS.Update(leaderboardEntry);
            }



            userMission.Is_Collect = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Mission {model.MissionId} executed successfully for User {userId}.");
            return "Mission executed successfully.";
        }

        public async Task<string> ExecuteQRCodeMissionASync(Guid userId, ExecuteQRCodeModel model)
        {
            var bangkokTime = GetBangkokTime();

            _logger.LogInformation($"User {userId} is attempting to execute mission {model.MissionId}.");

            var mission = await _context.MISSIONS
                .Include(m => m.QrCodeMission)
                .FirstOrDefaultAsync(m => m.MISSION_ID == model.MissionId);

            if (mission == null)
            {
                _logger.LogWarning($"Mission {model.MissionId} not found for User {userId}.");
                throw new Exception("Mission not found.");
            }

            if (bangkokTime >= mission.Expire_Date)
            {
                _logger.LogWarning($"Mission {model.MissionId} is expired.");
                throw new Exception("Mission is expired.");
            }

            if (bangkokTime < mission.Start_Date)
            {
                _logger.LogWarning($"Mission {model.MissionId} is not started yet.");
                throw new Exception("Mission is not started.");
            }

            if (mission.QrCodeMission == null || mission.QrCodeMission.QRCodeText != model.QRCode)
            {
                _logger.LogWarning($"Invalid QRCode for Mission {model.MissionId} by User {userId}.");
                throw new Exception("Invalid mission QRCode.");
            }

            // Check if user already accepted and completed this mission
            var userMission = await _context.USER_MISSIONS
                .FirstOrDefaultAsync(um => um.A_USER_ID == userId && um.MISSION_ID == model.MissionId);

            if (userMission == null)
            {
                _logger.LogWarning($"Mission {model.MissionId} was not accepted by User {userId}.");
                throw new Exception("Mission not accepted by the user.");
            }

            if (userMission.Completed_Date != null)
            {
                _logger.LogWarning($"Mission {model.MissionId} is already completed by User {userId}.");
                throw new Exception("Mission already completed.");
            }

            var existingQRCodeMission = await _context.USER_QR_CODE_MISSIONS
                .FirstOrDefaultAsync(uqm => uqm.A_USER_ID == userId && uqm.MISSION_ID == model.MissionId);

            if (existingQRCodeMission != null)
            {
                throw new Exception("User has already scanned for this mission.");
            }


            //var bangkokTime = GetBangkokTime();

            userMission.Verification_Status = "Waiting for Confirmation.";
            userMission.Submitted_At = bangkokTime;
            await _context.SaveChangesAsync();

            var userQRCodemission = new UserQRCodeMission
            {
                USER_QRCODE_MISSION_ID = Guid.NewGuid(),
                A_USER_ID = userId,
                QRCode = model.QRCode,
                MISSION_ID = model.MissionId,
                Scanned_At = bangkokTime,
                USER_MISSION_ID = model.UserMissionId,
            };

            _context.USER_QR_CODE_MISSIONS.Add(userQRCodemission);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Mission {model.MissionId} executed successfully for User {userId}.");
            return "Mission executed successfully.";
        }

        public async Task<string> ExecutePhotoMissionAsync(Guid userId, ExecutePhotoModel model)
        {
            var bangkokTime = GetBangkokTime();

            _logger.LogInformation($"User {userId} is attempting to execute mission {model.missionId}.");

            var mission = await _context.MISSIONS
                .FirstOrDefaultAsync(m => m.MISSION_ID == model.missionId);

            if (mission == null)
            {
                _logger.LogWarning($"Mission {model.missionId} not found for User {userId}.");
                throw new Exception("Mission not found.");
            }

            if (bangkokTime >= mission.Expire_Date)
            {
                _logger.LogWarning($"Mission {model.missionId} is expired.");
                throw new Exception("Mission is expired.");
            }

            if (bangkokTime < mission.Start_Date)
            {
                _logger.LogWarning($"Mission {model.missionId} is not started yet.");
                throw new Exception("Mission is not started.");
            }

            var userMission = await _context.USER_MISSIONS
                .FirstOrDefaultAsync(um => um.A_USER_ID == userId && um.MISSION_ID == model.missionId);

            if (userMission == null)
            {
                _logger.LogWarning($"Mission {model.missionId} was not accepted by User {userId}.");
                throw new Exception("Mission not accepted by the user.");
            }

            if (userMission.Completed_Date != null)
            {
                _logger.LogWarning($"Mission {model.missionId} is already completed by User {userId}.");
                throw new Exception("Mission already completed.");
            }

            var existingPhotoMission = await _context.USER_PHOTO_MISSIONS
                .FirstOrDefaultAsync(uqm => uqm.A_USER_ID == userId && uqm.MISSION_ID == model.missionId);

            if (existingPhotoMission != null)
            {
                throw new Exception("User has already sent photo for this mission.");
            }

            if (model.imageFile == null || !model.imageFile.Any())
                throw new Exception("At least 1 photo required.");

            //var uploadedUrls = new List<string>();
            var allowedFileTypes = new[] {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/bmp",
        "image/tiff",
        "image/svg+xml",
        "image/heif",  // .heif (iPhone, Android)
        "image/heic",   // .heic (iPhone)
        "application/octet-stream"
    };
            long maxFileSize = 10 * 1024 * 1024; // 50MB max file size
            var uploadDirectory = _configuration["AppSettings:ImagePhotoMissionUploadPath"];
            _logger.LogInformation($"ImagePhotoMissionUploadPath: {uploadDirectory}");

            if (string.IsNullOrEmpty(uploadDirectory))
            {
                _logger.LogError("ImagePhotoMissionUploadPath is not set in the configuration.");
                throw new Exception("The upload directory path is not configured.");
            }

            var userPhotoMission = new UserPhotoMission
            {
                USER_PHOTO_MISSION_ID = Guid.NewGuid(),
                MISSION_ID = model.missionId,
                A_USER_ID = userId,
                USER_MISSION_ID = model.userMissionId,
                Uploaded_At = bangkokTime,
                Approve = null,
                Approve_By = null,
                IMAGES = new List<UserPhotoMissionImage>(),
                IsView = true
            };

            foreach (var file in model.imageFile)
            {
                if (file.Length > maxFileSize)
                {
                    throw new Exception($"File {file.FileName} is too large. Maximum size allowed is 3 MB.");
                }

                if (!allowedFileTypes.Contains(file.ContentType))
                {
                    throw new Exception($"Invalid file type for {file.FileName}. Only JPEG, PNG, GIF, HEIF, HEIC images are allowed.");
                }



                if (file.Length > 0)
                {
                    var fileUrl = "";
                    var fileExtension = Path.GetExtension(file.FileName);
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
                    var imagePath = _configuration["AppSettings:ImagePhotoMissionPath"];
                    var newFileName = "";



                    if (fileExtension?.ToString().ToLower() == ".heic" || fileExtension?.ToString().ToLower() == ".heif")
                    {
                        var sanitizedFileName = Path.GetFileNameWithoutExtension(file.FileName)
                                                            .Replace(" ", "_")
                                                            .Replace("/", "_")
                                                            .Replace("\\", "_");

                        newFileName = $"{userId}_{model.missionId}_{sanitizedFileName}.jpg";// สร้าง path ชั่วคราว
                        var tempFolder = Path.Combine(uploadDirectory, "TempFiles");
                        Directory.CreateDirectory(tempFolder);

                        // สร้างชื่อไฟล์ชั่วคราว
                        var heicPath = Path.Combine(tempFolder, Guid.NewGuid() + Path.GetExtension(file.FileName));
                        var jpgPath = Path.Combine(uploadDirectory, newFileName);

                        // บันทึก IFormFile ลงไฟล์ .heic ชั่วคราว
                        using (var stream = new FileStream(heicPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        ConvertHeicToJpg(heicPath, jpgPath);

                        // ลบไฟล์ HEIC ต้นฉบับ
                        if (System.IO.File.Exists(heicPath))
                        {
                            System.IO.File.Delete(heicPath);
                        }
                    }
                    else
                    {
                        var sanitizedFileName = Path.GetFileNameWithoutExtension(file.FileName)
                                                             .Replace(" ", "_")
                                                             .Replace("/", "_")
                                                             .Replace("\\", "_");

                        newFileName = $"{userId}_{model.missionId}_{sanitizedFileName}.jpg";
                        var uploadPath = Path.Combine(uploadDirectory, newFileName);

                        Directory.CreateDirectory(Path.GetDirectoryName(uploadPath) ?? string.Empty);

                        await using (var stream = new FileStream(uploadPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                    }

                    

                    fileUrl = Path.Combine(imagePath, newFileName);  // ใช้ชื่อไฟล์ใหม่
                    //uploadedUrls.Add(fileUrl);


                    userPhotoMission.IMAGES ??= new List<UserPhotoMissionImage>();
                    userPhotoMission.IMAGES.Add(new UserPhotoMissionImage
                    {
                        USER_PHOTO_MISSION_IMAGE_ID = Guid.NewGuid(),
                        ImageUrl = fileUrl,
                    });
                }
            }

            userMission.Verification_Status = "Waiting for Confirmation.";
            userMission.Submitted_At = bangkokTime;
            _context.USER_PHOTO_MISSIONS.Add(userPhotoMission);
            await _context.SaveChangesAsync();

            return "Mission executed successfully";
        }

        public static void ConvertHeicToJpg(string heicPath, string outputPath)
        {
            try
            {
                using (var image = new MagickImage(heicPath))
                {
                    //    image.Resize(new MagickGeometry
                    //    {
                    //        IgnoreAspectRatio = false,
                    //        Greater = true,
                    //        Width = 1024,
                    //        Height = 1024
                    //    });

                    //    image.Format = MagickFormat.Jpeg;
                    //    image.Quality = 85; // ปรับคุณภาพตามต้องการ
                    //    image.Write(outputPath);
                    //} 
                    image.Format = MagickFormat.Jpeg;
                    image.Write(outputPath);
                    Console.WriteLine("แปลงสำเร็จ: " + outputPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("เกิดข้อผิดพลาดในการแปลง: " + ex.Message);
            }
        }





        public async Task<CollectCoinRewardResponse> CollectCoinRewardAsync(CollectCoinRewardRequest model, Guid userId)
        {
            var userMission = await _context.USER_MISSIONS
                .Include(um => um.Mission)
                .FirstOrDefaultAsync(um => um.USER_MISSION_ID == model.UserMissionId && um.A_USER_ID == userId);

            if (userMission == null)
                throw new Exception("Mission not found.");

            if (userMission.Is_Collect)
                throw new Exception("Reward already collected.");

            if (userMission.Completed_Date == null)
                throw new Exception("Mission not completed.");

            var bangkokTime = GetBangkokTime();

            var coinTransaction = new CoinTransaction
            {
                COIN_TRANSACTION_ID = Guid.NewGuid(),
                Amount = userMission.Mission.Coin_Reward,
                Transaction_Date = bangkokTime,
                Transaction_Type = "Mission Reward",
                Description = $"Reward for mission: {userMission.Mission.MISSION_NAME}",
                A_USER_ID = userId,
                Coin_Type = CoinType.KaeaCoin
            };

            _context.COIN_TRANSACTIONS.Add(coinTransaction);

            userMission.Is_Collect = true;

            await _context.SaveChangesAsync();

            return new CollectCoinRewardResponse
            {
                IsSuccess = true,
                Message = "Reward collected successfully.",
                RewardAmount = coinTransaction.Amount,
                TransactionDate = coinTransaction.Transaction_Date
            };
        }

        public async Task<string> ApproveQRCodeMissionsAsync(Guid userId, ApproveQRCodeMissionModel model)
        {
            var bangkokTime = GetBangkokTime();
            // Retrieve the QR Code Mission
            var qrCodeMission = await _context.USER_QR_CODE_MISSIONS
                .Include(uqm => uqm.UserMission)
                .ThenInclude(um => um.Mission)
                .FirstOrDefaultAsync(uqm => uqm.USER_QRCODE_MISSION_ID == model.UserQRCodeMissionId);

            //var missioner = await _context.MISSIONS
            //    .FirstOrDefaultAsync(m => m.MISSION_ID == qrCodeMission.MISSION_ID && m.Missioner == userId);
            //if (missioner == null)
            //    throw new Exception("This user is not own this mission.");

            if (qrCodeMission == null)
                throw new Exception("QR Code Mission not found.");

            // Check if the QR Code Mission has already been reviewed
            if (qrCodeMission.Approve.HasValue)
                throw new Exception("This QR Code Mission has already been reviewed.");

            // Update the approval status
            qrCodeMission.Approve = model.Approve;
            qrCodeMission.Approved_By = userId;
            qrCodeMission.Approve_At = bangkokTime;
            qrCodeMission.UserMission.Verification_Status = model.Approve == true ? "Approved" : "Rejected";
            qrCodeMission.UserMission.Accepted_Desc = model.Accepted_Desc;
            //qrCodeMission.UserMission.Completed_Date = bangkokTime;

            // If approved, process the mission reward
            //if (model.Approve == true)
            //{
            //    //var bangkokTime = GetBangkokTime();

            //    // Add a coin transaction for the user
            //    //var coinTransaction = new CoinTransaction
            //    //{
            //    //    COIN_TRANSACTION_ID = Guid.NewGuid(),
            //    //    Amount = qrCodeMission.UserMission.Mission.Coin_Reward,
            //    //    Transaction_Date = bangkokTime,
            //    //    Transaction_Type = "Mission Reward",
            //    //    Description = $"Reward for mission: {qrCodeMission.UserMission.Mission.MISSION_NAME}",
            //    //    A_USER_ID = qrCodeMission.UserMission.A_USER_ID,
            //    //    Coin_Type = CoinType.KaeaCoin
            //    //};

            //    var pointTransaction = new CoinTransaction
            //    {
            //        COIN_TRANSACTION_ID = Guid.NewGuid(),
            //        Amount = qrCodeMission.Mission.Mission_Point,
            //        Transaction_Date = bangkokTime,
            //        Transaction_Type = "Mission Reward",
            //        Description = $"Reward for mission: {qrCodeMission.Mission.MISSION_NAME}",
            //        A_USER_ID = qrCodeMission.A_USER_ID,
            //        Coin_Type = CoinType.MissionPoint
            //    };

            //    //_context.COIN_TRANSACTIONS.Add(coinTransaction);
            //    _context.COIN_TRANSACTIONS.Add(pointTransaction);

            //    var pointToAdd = await _context.COIN_TRANSACTIONS
            //    .Where(c => c.A_USER_ID == userId && c.Coin_Type == CoinType.MissionPoint && c.Transaction_Date.Month == bangkokTime.Month && c.Transaction_Date.Year == bangkokTime.Year)
            //    .SumAsync(c => c.Amount);

            //    //var currentYearBuddhist = bangkokTime.Year + 543;
            //    //Convert B.E. To B.C.
            //    var startOfMonth = new DateTime(bangkokTime.Year, bangkokTime.Month, 1);
            //    var leaderboardEntry = await _context.LEADERBOARDS
            //        .FirstOrDefaultAsync(lb => lb.A_USER_ID == userId && lb.MonthYear == startOfMonth);

            //    if (leaderboardEntry == null)
            //    {
            //        leaderboardEntry = new Leaderboard
            //        {
            //            LEADERBOARD_ID = Guid.NewGuid(),
            //            A_USER_ID = userId,
            //            Point = pointToAdd,
            //            MonthYear = startOfMonth,
            //            Create_at = bangkokTime,
            //        };
            //        _context.LEADERBOARDS.Add(leaderboardEntry);
            //    }
            //    else
            //    {
            //        leaderboardEntry.Point = pointToAdd;
            //        _context.LEADERBOARDS.Update(leaderboardEntry);
            //    }

            //    // Mark the mission as collected
            //    qrCodeMission.UserMission.Is_Collect = true;
            //    qrCodeMission.UserMission.Completed_Date = bangkokTime;
            //}

            ////var pointTransaction = new CoinTransaction
            ////{
            ////    COIN_TRANSACTION_ID = Guid.NewGuid(),
            ////    Amount = qrCodeMission.Mission.Mission_Point,
            ////    Transaction_Date = bangkokTime,
            ////    Transaction_Type = "Mission Reward",
            ////    Description = $"Reward for mission: {qrCodeMission.Mission.MISSION_NAME}",
            ////    A_USER_ID = userId,
            ////    Coin_Type = CoinType.MissionPoint
            ////};

            ////_context.COIN_TRANSACTIONS.Add(pointTransaction);
            ////await _context.SaveChangesAsync();

            ////var pointToAdd = await _context.COIN_TRANSACTIONS
            ////    .Where(c => c.A_USER_ID == userId && c.Coin_Type == CoinType.MissionPoint && c.Transaction_Date.Month == bangkokTime.Month && c.Transaction_Date.Year == bangkokTime.Year)
            ////    .SumAsync(c => c.Amount);

            ////var currentYearBuddhist = bangkokTime.Year + 543;
            ////Convert B.E. To B.C.
            ////var gregorianCalendar = new System.Globalization.GregorianCalendar();
            ////var monthYear = $"{gregorianCalendar.GetYear(bangkokTime):0000}-{bangkokTime:MM}";
            ////var leaderboardEntry = await _context.LEADERBOARDS
            ////    .FirstOrDefaultAsync(lb => lb.A_USER_ID == userId && lb.MonthYear == monthYear);

            ////if (leaderboardEntry == null)
            ////{
            ////    leaderboardEntry = new Leaderboard
            ////    {
            ////        LEADERBOARD_ID = Guid.NewGuid(),
            ////        A_USER_ID = userId,
            ////        Point = pointToAdd,
            ////        MonthYear = monthYear,
            ////        Create_at = bangkokTime,
            ////    };
            ////    _context.LEADERBOARDS.Add(leaderboardEntry);
            ////}
            ////else
            ////{
            ////    leaderboardEntry.Point = pointToAdd;
            ////    _context.LEADERBOARDS.Update(leaderboardEntry);
            ////}
            //// Save changes
            //await _context.SaveChangesAsync();

            //return model.Approve == true ? "Mission approved successfully." : "Mission rejected successfully.";
            if (model.Approve == true)
            {
                // สร้าง point transaction ใหม่
                var pointTransaction = new CoinTransaction
                {
                    COIN_TRANSACTION_ID = Guid.NewGuid(),
                    Amount = qrCodeMission.Mission.Mission_Point,
                    Transaction_Date = bangkokTime,
                    Transaction_Type = "Mission Reward",
                    Description = $"Reward for mission: {qrCodeMission.Mission.MISSION_NAME}",
                    A_USER_ID = qrCodeMission.A_USER_ID,
                    Coin_Type = CoinType.MissionPoint
                };

                // เพิ่ม point transaction เข้าไปในฐานข้อมูล
                _context.COIN_TRANSACTIONS.Add(pointTransaction);

                // เพิ่ม Coin เข้าไปต่อเลย
                var cointype = qrCodeMission.UserMission.Mission.MISSION_TypeCoin ?? 0; 
                var pointTransaction_coin = new CoinTransaction
                {
                    COIN_TRANSACTION_ID = Guid.NewGuid(),
                    Amount = qrCodeMission.Mission.Mission_Point,
                    Transaction_Date = bangkokTime,
                    Transaction_Type = cointype == 0 ? "Mission Reward" : "Receive from Mission",
                    Description = $"{pointTransaction.Transaction_Type} : {qrCodeMission.Mission.MISSION_NAME}",
                    A_USER_ID = qrCodeMission.A_USER_ID,
                    Coin_Type = cointype == 0 ? CoinType.KaeaCoin : CoinType.ThankCoin
                };
                _context.COIN_TRANSACTIONS.Add(pointTransaction_coin);

                // คำนวณคะแนนรวมที่ได้เพิ่มเข้าไป
                var pointToAdd = await _context.COIN_TRANSACTIONS
                    .Where(c => c.A_USER_ID == qrCodeMission.A_USER_ID && c.Coin_Type == CoinType.MissionPoint &&
                                c.Transaction_Date.Month == bangkokTime.Month && c.Transaction_Date.Year == bangkokTime.Year)
                    .SumAsync(c => c.Amount);

                // บวก amount จาก pointTransaction ลงไปใน pointToAdd
                pointToAdd += pointTransaction.Amount;  // เพิ่มคะแนนใหม่เข้าไป

                // Log ค่า pointToAdd ที่เพิ่มแล้ว
                _logger.LogInformation($"New pointToAdd: {pointToAdd}");

                // คำนวณและอัปเดต leaderboard
                var startOfMonth = new DateTime(bangkokTime.Year, bangkokTime.Month, 1);
                var leaderboardEntry = await _context.LEADERBOARDS
                    .FirstOrDefaultAsync(lb => lb.A_USER_ID == qrCodeMission.A_USER_ID && lb.MonthYear == startOfMonth);

                if (leaderboardEntry == null)
                {
                    leaderboardEntry = new Leaderboard
                    {
                        LEADERBOARD_ID = Guid.NewGuid(),
                        A_USER_ID = qrCodeMission.A_USER_ID,
                        Point = pointToAdd,
                        MonthYear = startOfMonth,
                        Create_at = bangkokTime,
                    };
                    _context.LEADERBOARDS.Add(leaderboardEntry);
                }
                else
                {
                    leaderboardEntry.Point = pointToAdd;
                    _context.LEADERBOARDS.Update(leaderboardEntry);
                }

                // Mark the mission as collected
                qrCodeMission.UserMission.Is_Collect = true;
                qrCodeMission.UserMission.Completed_Date = bangkokTime;
            }
            // บันทึกการเปลี่ยนแปลงในฐานข้อมูลหลังการคำนวณ
            await _context.SaveChangesAsync();

            return model.Approve == true ? "Mission approved successfully" : "Mission rejected successfully";

        }

        public async Task<List<ApproveViewModel>> GetAllQRCodeApproveAsync(string missionowner)
        {

            IQueryable<UserQRCodeMission> query_user = _context.USER_QR_CODE_MISSIONS;

            if (missionowner != "9")
            {
                var missiontype = ConvertMissionOwner(missionowner);
                query_user = query_user.Where(wh => missiontype.Contains(wh.Mission.Participate_Type));
            }

            var model = await query_user
                 .Select(m => new ApproveViewModel
                 {
                     USER_QR_CODE_MISSION_ID = m.USER_QRCODE_MISSION_ID,
                     QRCode = m.QRCode,
                     A_USER_ID = m.A_USER_ID,
                     LOGON_NAME = m.User.LOGON_NAME,
                     USER_NAME = m.UserMission.User.User_Name,
                     BranchCode = m.UserMission.User.BranchCode,
                     Department = m.UserMission.User.Department,
                     MISSION_ID = m.MISSION_ID,
                     MISSION_NAME = m.Mission.MISSION_NAME,
                     USER_MISSION_ID = m.USER_MISSION_ID,
                     Scanned_At = m.Scanned_At,
                     Approve = m.Approve,
                     Approve_By = m.Approved_By,
                     Approve_DATE = m.Approve_At,
                 })
                .ToListAsync();




            //var missiontype = ConvertMisstionOwner(missionowner);
            //var model = await _context.USER_QR_CODE_MISSIONS
            //    .Where(wh => missiontype.Contains(wh.Mission.Participate_Type))
            //    //.Where(m => m.Approve == null)
            //    .Select(m => new ApproveViewModel
            //    {
            //        USER_QR_CODE_MISSION_ID = m.USER_QRCODE_MISSION_ID,
            //        QRCode = m.QRCode,
            //        A_USER_ID = m.A_USER_ID,
            //        LOGON_NAME = m.User.LOGON_NAME,
            //        USER_NAME = m.UserMission.User.User_Name,
            //        BranchCode = m.UserMission.User.BranchCode,
            //        Department = m.UserMission.User.Department,
            //        MISSION_ID = m.MISSION_ID,
            //        MISSION_NAME = m.Mission.MISSION_NAME,
            //        USER_MISSION_ID = m.USER_MISSION_ID,
            //        Scanned_At = m.Scanned_At,
            //        Approve = m.Approve,
            //        Approve_By = m.Approved_By,
            //        Approve_DATE = m.Approve_At,
            //    })
            //    .ToListAsync();

            return model;
        }

        public async Task<List<MissionViewModel>> GetMissionerAsync(Guid userId)
        {
            var user = await _context.USERS
                .FirstOrDefaultAsync(u => u.A_USER_ID == userId);

            if (user == null)
                throw new Exception("User not found.");

            var missions = await _context.MISSIONS
                .Where(m => m.Missioner == user.A_USER_ID)
                .Include(m => m.MISSION_IMAGES)
                .Include(m => m.QrCodeMission)
                .Select(m => new MissionViewModel
                {
                    MISSION_ID = m.MISSION_ID,
                    MISSION_NAME = m.MISSION_NAME,
                    MISSION_TYPE = m.MISSION_TYPE,
                    Coin_Reward = m.Coin_Reward,
                    Mission_Point = m.Mission_Point,
                    Start_Date = m.Start_Date,
                    Expire_Date = m.Expire_Date,
                    Description = m.Description,
                    Is_Limited = m.Is_Limited,
                    MissionImages = m.MISSION_IMAGES.Select(img => img.ImageUrl).ToList(),
                    CodeMission = m.CodeMission != null ? m.CodeMission.Code_Mission_Code : null,// Map images to URLs // If you have a CodeMission, map it
                    QrMission = m.QrCodeMission != null ? m.QrCodeMission.QRCode : null,
                    Accept_limit = m.Accept_limit ?? 0,
                    Current_Accept = _context.USER_MISSIONS.Count(um => um.MISSION_ID == m.MISSION_ID),
                    Participate_Type = m.Participate_Type
                }).ToListAsync();

            return missions;
        }

        public async Task<List<ApprovePhotoViewModel>> GetAllPhotoApproveAsync(Guid userid, string missionowner)
        {

            IQueryable<UserPhotoMission> query_user = _context.USER_PHOTO_MISSIONS;

            if (missionowner != "9")
            {
                var missiontype = ConvertMissionOwner(missionowner);
                query_user = query_user.Where(wh => missiontype.Contains(wh.Mission.Participate_Type));
            }

            var model = await query_user
                  .Select(m => new ApprovePhotoViewModel
                  {
                      USER_PHOTO_MISSION_ID = m.USER_PHOTO_MISSION_ID,
                      A_USER_ID = m.A_USER_ID,
                      LOGON_NAME = m.User.LOGON_NAME,
                      USER_NAME = m.UserMission.User.User_Name,
                      BranchCode = m.UserMission.User.BranchCode,
                      Department = m.UserMission.User.Department,
                      MISSION_ID = m.MISSION_ID,
                      MISSION_NAME = m.Mission.MISSION_NAME,
                      USER_MISSION_ID = m.USER_MISSION_ID,
                      UPLOADED_AT = m.Uploaded_At,
                      Approve = m.Approve,
                      Approve_By = m.Approve_By,
                      Approve_DATE = m.Approve_At,
                      PHOTO = m.IMAGES.Select(img => img.ImageUrl).ToList()
                  })
                .ToListAsync();




            //var missiontype = ConvertMisstionOwner(missionowner);

            //var model = await _context.USER_PHOTO_MISSIONS
            //    .Include(upm => upm.IMAGES)
            //    //.Where(wh=>wh.Mission.Participate_Type== missiontype)
            //    .Where(wh => missiontype.Contains(wh.Mission.Participate_Type))
            //    .Select(m => new ApprovePhotoViewModel
            //    {
            //        USER_PHOTO_MISSION_ID = m.USER_PHOTO_MISSION_ID,
            //        A_USER_ID = m.A_USER_ID,
            //        LOGON_NAME = m.User.LOGON_NAME,
            //        USER_NAME = m.UserMission.User.User_Name,
            //        BranchCode = m.UserMission.User.BranchCode,
            //        Department = m.UserMission.User.Department,
            //        MISSION_ID = m.MISSION_ID,
            //        MISSION_NAME = m.Mission.MISSION_NAME,
            //        USER_MISSION_ID = m.USER_MISSION_ID,
            //        UPLOADED_AT = m.Uploaded_At,
            //        Approve = m.Approve,
            //        Approve_By = m.Approve_By,
            //        Approve_DATE = m.Approve_At,
            //        PHOTO = m.IMAGES.Select(img => img.ImageUrl).ToList()
            //    })
            //    .ToListAsync();

            return model;
        }
        public async Task<(List<ApprovePhotoViewModel> data, int total, int totalPage)> GetPhotoApproveByMissionAsync(Guid missionId, int page, int pageSize, string missionowner, string? searchName)
        {
            IQueryable<UserPhotoMission> query = _context.USER_PHOTO_MISSIONS
                .Include(m => m.Mission)
                .Include(m => m.User)
                .Include(m => m.UserMission)
                .ThenInclude(um => um.User)
                .Include(m => m.IMAGES)
                .Where(m => m.MISSION_ID == missionId);

            if (missionowner != "9")
            {
                var missionTypes = ConvertMissionOwner(missionowner);
                query = query.Where(m => missionTypes.Contains(m.Mission.Participate_Type));
            }
            if (!string.IsNullOrEmpty(searchName))
            {
                var loweredSearch = searchName.ToLower();
                query = query.Where(m => m.UserMission.User.User_Name.ToLower().Contains(loweredSearch));
            }

            var total = await query.CountAsync();
            var totalPage = (int)Math.Ceiling((double)total / pageSize);


            var data = await query
                .OrderBy(m => m.Approve) // Approve = null หรือ false จะมาก่อน
                .ThenByDescending(m => m.Uploaded_At)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new ApprovePhotoViewModel
                {
                    USER_PHOTO_MISSION_ID = m.USER_PHOTO_MISSION_ID,
                    A_USER_ID = m.A_USER_ID,
                    LOGON_NAME = m.User.LOGON_NAME,
                    USER_NAME = m.UserMission.User.User_Name,
                    BranchCode = m.UserMission.User.BranchCode,
                    Department = m.UserMission.User.Department,
                    MISSION_ID = m.MISSION_ID,
                    MISSION_NAME = m.Mission.MISSION_NAME,
                    USER_MISSION_ID = m.USER_MISSION_ID,
                    UPLOADED_AT = m.Uploaded_At,
                    Approve = m.Approve,
                    Approve_By = m.Approve_By,
                    Approve_By_NAME = _context.USERS.Where(u=>u.A_USER_ID==m.Approve_By).Select(u=>u.User_Name).FirstOrDefault(),
                    Approve_DATE = m.Approve_At,
                    PHOTO = m.IMAGES.Select(img => img.ImageUrl).ToList(),
                    Is_View = m.IsView,
                    Reject_Des = m.UserMission.Accepted_Desc,
                })
                .ToListAsync();

            return (data, total, totalPage);
        }

        public async Task<(List<ApproveVideoViewModel> data, int total ,int totalPage)> GetVideoApproveByMissionAsync(
    Guid missionId, int page, int pageSize, string missionowner, string? searchName)
        {
            IQueryable<UserVideoMission> query = _context.USER_VIDEO_MISSIONS
                .Include(m => m.Mission)
                .Include(m => m.User)
                .Include(m => m.UserMission)
                .ThenInclude(um => um.User);

            // Filter ตาม missionId
            query = query.Where(m => m.MISSION_ID == missionId);

            // ถ้าไม่ใช่ admin (missionowner != "9") ให้ filter ตาม Participate_Type
            if (missionowner != "9")
            {
                var missionTypes = ConvertMissionOwner(missionowner);
                query = query.Where(m => missionTypes.Contains(m.Mission.Participate_Type));
            }
            if (!string.IsNullOrEmpty(searchName))
            {
                var loweredSearch = searchName.ToLower();
                query = query.Where(m => m.UserMission.User.User_Name.ToLower().Contains(loweredSearch));
            }

            // นับจำนวนทั้งหมด
            var total = await query.CountAsync();
            var totalPage = (int)Math.Ceiling((double)total / pageSize);

            // ดึงข้อมูลเฉพาะหน้า
            var data = await query
                .OrderBy(m => m.Approve) // Approve = null หรือ false จะมาก่อน
                .ThenByDescending(m => m.Uploaded_At).Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new ApproveVideoViewModel
                {
                    USER_VIDEO_MISSION_ID = m.USER_VIDEO_MISSION_ID,
                    A_USER_ID = m.A_USER_ID,
                    LOGON_NAME = m.User.LOGON_NAME,
                    USER_NAME = m.UserMission.User.User_Name,
                    BranchCode = m.UserMission.User.BranchCode,
                    Department = m.UserMission.User.Department,
                    MISSION_ID = m.MISSION_ID,
                    MISSION_NAME = m.Mission.MISSION_NAME,
                    USER_MISSION_ID = m.USER_MISSION_ID,
                    UPLOADED_AT = m.Uploaded_At,
                    Approve = m.Approve,
                    Approve_By = m.Approve_By,
                    Approve_By_NAME = _context.USERS.Where(u => u.A_USER_ID == m.Approve_By).Select(u => u.User_Name).FirstOrDefault(),
                    Approve_DATE = m.Approve_At,
                    VIDEO = m.VideoUrl,
                    Is_View = m.IsView,
                    Reject_Des = m.UserMission.Accepted_Desc,
                })
                .ToListAsync();

            return (data, total, totalPage);
        }
        public async Task<List<UserSummaryViewModel>> GetUsersInMissionAsync(Guid missionId, string type)
        {
            switch (type.ToLower())
            {
                case "photo":
                    return await _context.USER_PHOTO_MISSIONS
                        .Where(m => m.MISSION_ID == missionId)
                        .Select(m => new UserSummaryViewModel
                        {
                            A_USER_ID = m.A_USER_ID,
                            LOGON_NAME = m.User.LOGON_NAME,
                            USER_NAME = m.UserMission.User.User_Name,
                            BranchCode = m.UserMission.User.BranchCode,
                            Department = m.UserMission.User.Department
                        })
                        .Distinct()
                        .ToListAsync();

                case "video":
                    return await _context.USER_VIDEO_MISSIONS
                        .Where(m => m.MISSION_ID == missionId)
                        .Select(m => new UserSummaryViewModel
                        {
                            A_USER_ID = m.A_USER_ID,
                            LOGON_NAME = m.User.LOGON_NAME,
                            USER_NAME = m.UserMission.User.User_Name,
                            BranchCode = m.UserMission.User.BranchCode,
                            Department = m.UserMission.User.Department
                        })
                        .Distinct()
                        .ToListAsync();

                case "text":
                    return await _context.USER_TEXT_MISSIONS
                        .Where(m => m.MISSION_ID == missionId)
                        .Select(m => new UserSummaryViewModel
                        {
                            A_USER_ID = m.UserMission.User.A_USER_ID,
                            LOGON_NAME = m.UserMission.User.LOGON_NAME,
                            USER_NAME = m.UserMission.User.User_Name,
                            BranchCode = m.UserMission.User.BranchCode,
                            Department = m.UserMission.User.Department
                        })
                        .Distinct()
                        .ToListAsync();

                default:
                    return new List<UserSummaryViewModel>();
            }
        }



        //public async Task<List<MissionSelectViewModel>> GetMissionNamesByTypeAsync(string missionType)
        //{
        //    IQueryable<Guid> missionIds = missionType.ToLower() switch
        //    {
        //        "photo" => _context.USER_PHOTO_MISSIONS.Select(x => x.MISSION_ID),
        //        "text" => _context.USER_TEXT_MISSIONS.Select(x => x.MISSION_ID),
        //        "video" => _context.USER_VIDEO_MISSIONS.Select(x => x.MISSION_ID),
        //        "qr" => _context.USER_QR_CODE_MISSIONS.Select(x => x.MISSION_ID),
        //        _ => Enumerable.Empty<Guid>().AsQueryable()
        //    };

        //    return await _context.MISSIONS
        //        .Where(m => missionIds.Contains(m.MISSION_ID))
        //        .Select(m => new MissionSelectViewModel
        //        {
        //            MISSION_ID = m.MISSION_ID,
        //            MISSION_NAME = m.MISSION_NAME,
        //            Participate_Type = m.Participate_Type,
        //        })
        //        .ToListAsync();
        //}
        public async Task<List<MissionSelectViewModel>> GetMissionNamesByTypeAsync(string missionType, string missionowner)
        {
            IQueryable<Guid> missionIds = missionType.ToLower() switch
            {
                "photo" => _context.USER_PHOTO_MISSIONS.Select(x => x.MISSION_ID),
                "text" => _context.USER_TEXT_MISSIONS.Select(x => x.MISSION_ID),
                "video" => _context.USER_VIDEO_MISSIONS.Select(x => x.MISSION_ID),
                "qr" => _context.USER_QR_CODE_MISSIONS.Select(x => x.MISSION_ID),
                _ => Enumerable.Empty<Guid>().AsQueryable()
            };

            var query = _context.MISSIONS
                .Where(m => missionIds.Contains(m.MISSION_ID));

            if (missionowner != "9") // ถ้าไม่ใช่แอดมิน
            {
                var missionTypes = ConvertMissionOwner(missionowner);
                query = query.Where(m => missionTypes.Contains(m.Participate_Type));
            }

            return await query
                .Select(m => new MissionSelectViewModel
                {
                    MISSION_ID = m.MISSION_ID,
                    MISSION_NAME = m.MISSION_NAME,
                    Participate_Type = m.Participate_Type,
                    Coin_Reward = m.Coin_Reward,
                    MISSION_TypeCoin = m.MISSION_TypeCoin,
                    Is_Winners = m.Is_Winners,
                    CREATED_AT = m.Created_At,
                    Start_DATE = m.Start_Date,
                    Expire_DATE = m.Expire_Date,
                    Is_Public = m.Is_Public,
                    description = m.Description,
                    
                })
                .ToListAsync();
        }

        public async Task<List<MissionSelectViewModel>> GetAllPublicMissionsAsync()
        {
            return await _context.MISSIONS
                .Where(m => m.Is_Public == true) // ✅ เฉพาะ public
                .Select(m => new MissionSelectViewModel
                {
                    MISSION_ID = m.MISSION_ID,
                    MISSION_NAME = m.MISSION_NAME,
                    MISSION_TYPE = m.MISSION_TYPE,
                    Participate_Type = m.Participate_Type,
                    Coin_Reward = m.Coin_Reward,
                    MISSION_TypeCoin = m.MISSION_TypeCoin,
                    Is_Winners = m.Is_Winners,
                    CREATED_AT = m.Created_At,
                    Start_DATE = m.Start_Date,
                    Expire_DATE = m.Expire_Date,
                    Is_Public = m.Is_Public,
                    description = m.Description,
                })
                .ToListAsync();
        }


        public async Task<List<ApproveVideoViewModel>> GetAllVideoApproveAsync(Guid userid, string missionowner)
        {

            IQueryable<UserVideoMission> query_user = _context.USER_VIDEO_MISSIONS;

            if (missionowner != "9")
            {
                var missiontype = ConvertMissionOwner(missionowner);
                query_user = query_user.Where(wh => missiontype.Contains(wh.Mission.Participate_Type));
            }

            var model = await query_user
                  .Select(m => new ApproveVideoViewModel
                  {
                      USER_VIDEO_MISSION_ID = m.USER_VIDEO_MISSION_ID,
                      A_USER_ID = m.A_USER_ID,
                      LOGON_NAME = m.User.LOGON_NAME,
                      USER_NAME = m.UserMission.User.User_Name,
                      BranchCode = m.UserMission.User.BranchCode,
                      Department = m.UserMission.User.Department,
                      MISSION_ID = m.MISSION_ID,
                      MISSION_NAME = m.Mission.MISSION_NAME,
                      USER_MISSION_ID = m.USER_MISSION_ID,
                      UPLOADED_AT = m.Uploaded_At,
                      Approve = m.Approve,
                      Approve_By = m.Approve_By,
                      Approve_DATE = m.Approve_At,
                       VIDEO= m.VideoUrl,
                  })
                .ToListAsync();


             

            return model;
        }

        private List<string> ConvertMissionOwner(string missionowner)
        {
            return missionowner switch
            {
                "1" => new List<string> { "All", "AUOF", "AUBR", "AUFC" },
                "2" => new List<string> { "All","AUBR" },
                "3" => new List<string> { "All","AUFC" },
                "9" => new List<string> { "All", "AUOF", "AUBR", "AUFC" }, // ✅ Super Admin
                _ => new List<string>()
            };
        }


        //    public async Task<string> ApprovePhotoMissionAsync(Guid userId, ApprovePhotoMissionModel model)
        //    {
        //        var bangkokTime = GetBangkokTime();

        //        var photoMission = await _context.USER_PHOTO_MISSIONS
        //            .Include(pm => pm.UserMission)
        //            .ThenInclude(um => um.Mission)
        //            .FirstOrDefaultAsync(pm => pm.USER_PHOTO_MISSION_ID == model.USER_PHOTO_MISSION_ID);

        //        //var missioner = await _context.MISSIONS
        //        //    .FirstOrDefaultAsync(m => m.MISSION_ID == photoMission.MISSION_ID && m.Missioner == userId);
        //        //if (missioner == null)
        //        //    throw new Exception("This user is not own this mission.");

        //        if (photoMission == null)
        //        {
        //            throw new Exception("Photo mission not found");
        //        }

        //        if (photoMission.Approve.HasValue)
        //        {
        //            throw new Exception("This Photo Mission has already been reviewed.");
        //        }

        //        photoMission.Approve = model.Approve;
        //        photoMission.Approve_By = userId;
        //        photoMission.Approve_At = bangkokTime;
        //        photoMission.UserMission.Verification_Status = model.Approve == true ? "Approved" : "Rejected";

        //        if (model.Approve == true)
        //        {
        //            //var coinTransaction = new CoinTransaction
        //            //{
        //            //    COIN_TRANSACTION_ID = Guid.NewGuid(),
        //            //    Amount = photoMission.Mission.Coin_Reward,
        //            //    Amount = photoMission.Mission.Coin_Reward,
        //            //    Transaction_Date = bangkokTime,
        //            //    Transaction_Type = "Mission Reward",
        //            //    Description = $"Reward for mission: {photoMission.Mission.MISSION_NAME}",
        //            //    A_USER_ID = photoMission.UserMission.A_USER_ID,
        //            //    Coin_Type = CoinType.KaeaCoin
        //            //};

        //            var pointTransaction = new CoinTransaction
        //            {
        //                COIN_TRANSACTION_ID = Guid.NewGuid(),
        //                Amount = photoMission.Mission.Mission_Point,
        //                Transaction_Date = bangkokTime,
        //                Transaction_Type = "Mission Reward",
        //                Description = $"Reward for mission: {photoMission.Mission.MISSION_NAME}",
        //                A_USER_ID = photoMission.A_USER_ID,
        //                Coin_Type = CoinType.MissionPoint
        //            };

        //            //_context.COIN_TRANSACTIONS.Add(coinTransaction);
        //            _context.COIN_TRANSACTIONS.Add(pointTransaction);

        //            //var pointToAdd = await _context.COIN_TRANSACTIONS
        //            //.Where(c => c.A_USER_ID == userId && c.Coin_Type == CoinType.MissionPoint && c.Transaction_Date.Month == bangkokTime.Month && c.Transaction_Date.Year == bangkokTime.Year)
        //            //.SumAsync(c => c.Amount);
        //            var pointToAdd = await _context.COIN_TRANSACTIONS
        //.Where(c => c.A_USER_ID == photoMission.A_USER_ID && c.Coin_Type == CoinType.MissionPoint &&
        //            c.Transaction_Date.Month == bangkokTime.Month && c.Transaction_Date.Year == bangkokTime.Year)
        //.SumAsync(c => c.Amount);


        //            //var currentYearBuddhist = bangkokTime.Year + 543;
        //            //Convert B.E. To B.C.
        //            var startOfMonth = new DateTime(bangkokTime.Year, bangkokTime.Month, 1);
        //            var leaderboardEntry = await _context.LEADERBOARDS
        //                .FirstOrDefaultAsync(lb => lb.A_USER_ID == userId && lb.MonthYear == startOfMonth);

        //            if (leaderboardEntry == null)
        //            {
        //                leaderboardEntry = new Leaderboard
        //                {
        //                    LEADERBOARD_ID = Guid.NewGuid(),
        //                    A_USER_ID = userId,
        //                    Point = pointToAdd,
        //                    MonthYear = startOfMonth,
        //                    Create_at = bangkokTime,
        //                };
        //                _context.LEADERBOARDS.Add(leaderboardEntry);
        //            }
        //            else
        //            {
        //                leaderboardEntry.Point = pointToAdd;
        //                _context.LEADERBOARDS.Update(leaderboardEntry);
        //            }

        //            // Mark the mission as collected
        //            photoMission.UserMission.Is_Collect = true;
        //            photoMission.UserMission.Completed_Date = bangkokTime;
        //        }

        //        //var pointTransaction = new CoinTransaction
        //        //{
        //        //    COIN_TRANSACTION_ID = Guid.NewGuid(),
        //        //    Amount = photoMission.Mission.Mission_Point,
        //        //    Transaction_Date = bangkokTime,
        //        //    Transaction_Type = "Mission Reward",
        //        //    Description = $"Reward for mission: {photoMission.Mission.MISSION_NAME}",
        //        //    A_USER_ID = userId,
        //        //    Coin_Type = CoinType.MissionPoint
        //        //};

        //        //_context.COIN_TRANSACTIONS.Add(pointTransaction);
        //        //await _context.SaveChangesAsync();

        //        //var pointToAdd = await _context.COIN_TRANSACTIONS
        //        //    .Where(c => c.A_USER_ID == userId && c.Coin_Type == CoinType.MissionPoint && c.Transaction_Date.Month == bangkokTime.Month && c.Transaction_Date.Year == bangkokTime.Year)
        //        //    .SumAsync(c => c.Amount);

        //        ////var currentYearBuddhist = bangkokTime.Year + 543;
        //        ////Convert B.E. To B.C.
        //        //var gregorianCalendar = new System.Globalization.GregorianCalendar();
        //        //var monthYear = $"{gregorianCalendar.GetYear(bangkokTime):0000}-{bangkokTime:MM}";
        //        //var leaderboardEntry = await _context.LEADERBOARDS
        //        //    .FirstOrDefaultAsync(lb => lb.A_USER_ID == userId && lb.MonthYear == monthYear);

        //        //if (leaderboardEntry == null)
        //        //{
        //        //    leaderboardEntry = new Leaderboard
        //        //    {
        //        //        LEADERBOARD_ID = Guid.NewGuid(),
        //        //        A_USER_ID = userId,
        //        //        Point = pointToAdd,
        //        //        MonthYear = monthYear,
        //        //        Create_at = bangkokTime,
        //        //    };
        //        //    _context.LEADERBOARDS.Add(leaderboardEntry);
        //        //}
        //        //else
        //        //{
        //        //    leaderboardEntry.Point = pointToAdd;
        //        //    _context.LEADERBOARDS.Update(leaderboardEntry);
        //        //}

        //        await _context.SaveChangesAsync();

        //        return model.Approve == true ? "Mission approved successfully" : "Mission rejected successfully";
        //    }
        public async Task<string> ApprovePhotoMissionAsync(Guid userId, ApprovePhotoMissionModel model)
        {
            var bangkokTime = GetBangkokTime();

            var photoMission = await _context.USER_PHOTO_MISSIONS
                .Include(pm => pm.UserMission)
                .ThenInclude(um => um.Mission)
                .FirstOrDefaultAsync(pm => pm.USER_PHOTO_MISSION_ID == model.USER_PHOTO_MISSION_ID);

            if (photoMission == null)
            {
                throw new Exception("Photo mission not found");
            }

            if (photoMission.Approve.HasValue)
            {
                throw new Exception("This Photo Mission has already been reviewed.");
            }

            photoMission.Approve = model.Approve;
            photoMission.Approve_By = userId;
            photoMission.Approve_At = bangkokTime;
            photoMission.UserMission.Verification_Status = model.Approve == true ? "Approved" : "Rejected";
            photoMission.UserMission.Accepted_Desc = model.Accepted_Desc;

            if (model.Approve == true)
            {
                // สร้าง point transaction ใหม่
                var pointTransaction = new CoinTransaction
                {
                    COIN_TRANSACTION_ID = Guid.NewGuid(),
                    Amount = photoMission.Mission.Mission_Point,
                    Transaction_Date = bangkokTime,
                    Transaction_Type = "Mission Reward",
                    Description = $"Reward for mission: {photoMission.Mission.MISSION_NAME}",
                    A_USER_ID = photoMission.A_USER_ID,
                    Coin_Type = CoinType.MissionPoint 
                }; 
                // เพิ่ม point transaction เข้าไปในฐานข้อมูล
                _context.COIN_TRANSACTIONS.Add(pointTransaction);


                // เพิ่ม Coin เข้าไปต่อเลย
                // สร้าง point transaction ใหม่
                var cointype = photoMission.UserMission.Mission.MISSION_TypeCoin ?? 0;
                var pointTransaction_coin = new CoinTransaction
                {
                    COIN_TRANSACTION_ID = Guid.NewGuid(),
                    Amount = photoMission.Mission.Mission_Point,
                    Transaction_Date = bangkokTime,
                    Transaction_Type = cointype == 0 ? "Mission Reward" : "Receive from Mission",
                    Description = $"{pointTransaction.Transaction_Type} : {photoMission.Mission.MISSION_NAME}",
                    A_USER_ID = photoMission.A_USER_ID,
                    Coin_Type = cointype == 0 ? CoinType.KaeaCoin : CoinType.ThankCoin
                };             
                _context.COIN_TRANSACTIONS.Add(pointTransaction_coin);


                // คำนวณคะแนนรวมที่ได้เพิ่มเข้าไป
                var pointToAdd = await _context.COIN_TRANSACTIONS
                    .Where(c => c.A_USER_ID == photoMission.A_USER_ID && c.Coin_Type == CoinType.MissionPoint &&
                                c.Transaction_Date.Month == bangkokTime.Month && c.Transaction_Date.Year == bangkokTime.Year)
                    .SumAsync(c => c.Amount);

                // บวก amount จาก pointTransaction ลงไปใน pointToAdd
                pointToAdd += pointTransaction.Amount;  // เพิ่มคะแนนใหม่เข้าไป

                // Log ค่า pointToAdd ที่เพิ่มแล้ว
                _logger.LogInformation($"New pointToAdd: {pointToAdd}");

                // คำนวณและอัปเดต leaderboard
                var startOfMonth = new DateTime(bangkokTime.Year, bangkokTime.Month, 1);
                var leaderboardEntry = await _context.LEADERBOARDS
                    .FirstOrDefaultAsync(lb => lb.A_USER_ID == photoMission.A_USER_ID && lb.MonthYear == startOfMonth);

                if (leaderboardEntry == null)
                {
                    leaderboardEntry = new Leaderboard
                    {
                        LEADERBOARD_ID = Guid.NewGuid(),
                        A_USER_ID = photoMission.A_USER_ID,
                        Point = pointToAdd,
                        MonthYear = startOfMonth,
                        Create_at = bangkokTime,
                    };
                    _context.LEADERBOARDS.Add(leaderboardEntry);
                }
                else
                {
                    leaderboardEntry.Point = pointToAdd;
                    _context.LEADERBOARDS.Update(leaderboardEntry);
                }

                // Mark the mission as collected
                photoMission.UserMission.Is_Collect = true;
                photoMission.UserMission.Completed_Date = bangkokTime;

            }
            // บันทึกการเปลี่ยนแปลงในฐานข้อมูลหลังการคำนวณ
            await _context.SaveChangesAsync();

            return model.Approve == true ? "Mission approved successfully" : "Mission rejected successfully";
        }

        //public async Task<string> ApprovePhotoMissionAsync(Guid userId, ApprovePhotoMissionModel model)
        //{
        //    var bangkokTime = GetBangkokTime();

        //    var photoMission = await _context.USER_PHOTO_MISSIONS
        //        .Include(pm => pm.UserMission)
        //        .ThenInclude(um => um.Mission)
        //        .FirstOrDefaultAsync(pm => pm.USER_PHOTO_MISSION_ID == model.USER_PHOTO_MISSION_ID);

        //    if (photoMission == null)
        //    {
        //        throw new Exception("Photo mission not found");
        //    }

        //    if (photoMission.Approve.HasValue)
        //    {
        //        throw new Exception("This Photo Mission has already been reviewed.");
        //    }

        //    photoMission.Approve = model.Approve;
        //    photoMission.Approve_By = userId;
        //    photoMission.Approve_At = bangkokTime;
        //    photoMission.UserMission.Verification_Status = model.Approve == true ? "Approved" : "Rejected";

        //    if (model.Approve == true)
        //    {
        //        // ตรวจสอบว่า IsReward เป็น true แล้วหรือไม่
        //        if (photoMission.IsReward == true)
        //        {
        //            throw new Exception("This mission has already been rewarded.");
        //        }

        //        // สร้าง point transaction ใหม่
        //        var pointTransaction = new CoinTransaction
        //        {
        //            COIN_TRANSACTION_ID = Guid.NewGuid(),
        //            Amount = photoMission.Mission.Mission_Point,
        //            Transaction_Date = bangkokTime,
        //            Transaction_Type = "Mission Reward",
        //            Description = $"Reward for mission: {photoMission.Mission.MISSION_NAME}",
        //            A_USER_ID = photoMission.A_USER_ID,
        //            Coin_Type = CoinType.MissionPoint
        //        };

        //        // เพิ่ม point transaction เข้าไปในฐานข้อมูล
        //        _context.COIN_TRANSACTIONS.Add(pointTransaction);

        //        // อัปเดต IsReward
        //        photoMission.IsReward = true;

        //        // คำนวณคะแนนรวมที่ได้เพิ่มเข้าไป
        //        var pointToAdd = await _context.COIN_TRANSACTIONS
        //            .Where(c => c.A_USER_ID == photoMission.A_USER_ID && c.Coin_Type == CoinType.MissionPoint &&
        //                        c.Transaction_Date.Month == bangkokTime.Month && c.Transaction_Date.Year == bangkokTime.Year)
        //            .SumAsync(c => c.Amount);

        //        pointToAdd += pointTransaction.Amount;  // เพิ่มคะแนนใหม่เข้าไป

        //        // Log ค่า pointToAdd ที่เพิ่มแล้ว
        //        _logger.LogInformation($"New pointToAdd: {pointToAdd}");

        //        // คำนวณและอัปเดต leaderboard
        //        var startOfMonth = new DateTime(bangkokTime.Year, bangkokTime.Month, 1);
        //        var leaderboardEntry = await _context.LEADERBOARDS
        //            .FirstOrDefaultAsync(lb => lb.A_USER_ID == photoMission.A_USER_ID && lb.MonthYear == startOfMonth);

        //        if (leaderboardEntry == null)
        //        {
        //            leaderboardEntry = new Leaderboard
        //            {
        //                LEADERBOARD_ID = Guid.NewGuid(),
        //                A_USER_ID = photoMission.A_USER_ID,
        //                Point = pointToAdd,
        //                MonthYear = startOfMonth,
        //                Create_at = bangkokTime,
        //            };
        //            _context.LEADERBOARDS.Add(leaderboardEntry);
        //        }
        //        else
        //        {
        //            leaderboardEntry.Point = pointToAdd;
        //            _context.LEADERBOARDS.Update(leaderboardEntry);
        //        }

        //        // Mark the mission as collected
        //        photoMission.UserMission.Is_Collect = true;
        //        photoMission.UserMission.Completed_Date = bangkokTime;
        //    }

        //    // บันทึกการเปลี่ยนแปลงในฐานข้อมูลหลังการคำนวณ
        //    await _context.SaveChangesAsync();

        //    return model.Approve == true ? "Mission approved and reward added successfully" : "Mission rejected successfully";
        //}


        public async Task<string> ApproveVideoMissionAsync(Guid userId, ApproveVideoMissionModel model)
        {
            var bangkokTime = GetBangkokTime();

            var videoMission = await _context.USER_VIDEO_MISSIONS
                .Include(pm => pm.UserMission)
                .ThenInclude(um => um.Mission)
                .FirstOrDefaultAsync(pm => pm.USER_VIDEO_MISSION_ID == model.USER_VIDEO_MISSION_ID);

            if (videoMission == null)
            {
                throw new Exception("Video mission not found");
            }

            if (videoMission.Approve.HasValue)
            {
                throw new Exception("This Video Mission has already been reviewed.");
            }

            videoMission.Approve = model.Approve;
            videoMission.Approve_By = userId;
            videoMission.Approve_At = bangkokTime;
            videoMission.UserMission.Verification_Status = model.Approve == true ? "Approved" : "Rejected";
            videoMission.UserMission.Accepted_Desc = model.Accepted_Desc;


            if (model.Approve == true)
            {
                // สร้าง point transaction ใหม่
                var pointTransaction = new CoinTransaction
                {
                    COIN_TRANSACTION_ID = Guid.NewGuid(),
                    Amount = videoMission.Mission.Mission_Point,
                    Transaction_Date = bangkokTime,
                    Transaction_Type = "Mission Reward",
                    Description = $"Reward for mission: {videoMission.Mission.MISSION_NAME}",
                    A_USER_ID = videoMission.A_USER_ID,
                    Coin_Type = CoinType.MissionPoint
                };
                // เพิ่ม point transaction เข้าไปในฐานข้อมูล
                _context.COIN_TRANSACTIONS.Add(pointTransaction);


                // เพิ่ม Coin เข้าไปต่อเลย
                // สร้าง point transaction ใหม่
                var cointype = videoMission.UserMission.Mission.MISSION_TypeCoin ?? 0;
                var pointTransaction_coin = new CoinTransaction
                {
                    COIN_TRANSACTION_ID = Guid.NewGuid(),
                    Amount = videoMission.Mission.Mission_Point,
                    Transaction_Date = bangkokTime,
                    Transaction_Type = cointype == 0 ? "Mission Reward" : "Receive from Mission",
                    Description = $"{pointTransaction.Transaction_Type} : {videoMission.Mission.MISSION_NAME}",
                    A_USER_ID = videoMission.A_USER_ID,
                    Coin_Type = cointype == 0 ? CoinType.KaeaCoin : CoinType.ThankCoin
                };
                _context.COIN_TRANSACTIONS.Add(pointTransaction_coin);


                // คำนวณคะแนนรวมที่ได้เพิ่มเข้าไป
                var pointToAdd = await _context.COIN_TRANSACTIONS
                    .Where(c => c.A_USER_ID == videoMission.A_USER_ID && c.Coin_Type == CoinType.MissionPoint &&
                                c.Transaction_Date.Month == bangkokTime.Month && c.Transaction_Date.Year == bangkokTime.Year)
                    .SumAsync(c => c.Amount);

                // บวก amount จาก pointTransaction ลงไปใน pointToAdd
                pointToAdd += pointTransaction.Amount;  // เพิ่มคะแนนใหม่เข้าไป

                // Log ค่า pointToAdd ที่เพิ่มแล้ว
                _logger.LogInformation($"New pointToAdd: {pointToAdd}");

                // คำนวณและอัปเดต leaderboard
                var startOfMonth = new DateTime(bangkokTime.Year, bangkokTime.Month, 1);
                var leaderboardEntry = await _context.LEADERBOARDS
                    .FirstOrDefaultAsync(lb => lb.A_USER_ID == videoMission.A_USER_ID && lb.MonthYear == startOfMonth);

                if (leaderboardEntry == null)
                {
                    leaderboardEntry = new Leaderboard
                    {
                        LEADERBOARD_ID = Guid.NewGuid(),
                        A_USER_ID = videoMission.A_USER_ID,
                        Point = pointToAdd,
                        MonthYear = startOfMonth,
                        Create_at = bangkokTime,
                    };
                    _context.LEADERBOARDS.Add(leaderboardEntry);
                }
                else
                {
                    leaderboardEntry.Point = pointToAdd;
                    _context.LEADERBOARDS.Update(leaderboardEntry);
                }

                // Mark the mission as collected
                videoMission.UserMission.Is_Collect = true;
                videoMission.UserMission.Completed_Date = bangkokTime;

            }
            // บันทึกการเปลี่ยนแปลงในฐานข้อมูลหลังการคำนวณ
            await _context.SaveChangesAsync();

            return model.Approve == true ? "Mission approved successfully" : "Mission rejected successfully";
        }

        public async Task<string> MissionerAddWinnerCoinRewardPhotoMissionAsync(Guid userId, AddCoinWinnerMission model)
        {
            var bangkokTime = GetBangkokTime();

            var missioner = await _context.MISSIONS
                .FirstOrDefaultAsync(m => m.Missioner == userId);

            if (missioner == null)
                throw new Exception("User not found.");

            var mission = await _context.MISSIONS
                .FirstOrDefaultAsync(m => m.MISSION_ID == model.MISSION_ID);

            if (mission == null)
                throw new Exception("Mission not found.");

            var sentMission = await _context.USER_PHOTO_MISSIONS
                .FirstOrDefaultAsync(sm => sm.MISSION_ID == model.MISSION_ID && sm.A_USER_ID == model.A_USER_ID && sm.Approve == true);

            if (sentMission == null)
                throw new Exception("User not sent mission yet");

            var coinTransaction = new CoinTransaction
            {
                COIN_TRANSACTION_ID = Guid.NewGuid(),
                A_USER_ID = model.A_USER_ID,
                Coin_Type = CoinType.KaeaCoin,
                Amount = model.Amount,
                Transaction_Type = "Mission winner Reward",
                Transaction_Date = bangkokTime,
                Description = $"Mission winner reward for mission: {mission.MISSION_NAME}"
            };
            sentMission.IsReward = true;
            _context.COIN_TRANSACTIONS.Add(coinTransaction);
            await _context.SaveChangesAsync();

            return "Coin reward added successfully.";
        }

        public async Task<string> MissionerAddWinnerCoinAllMissionAsync_list(Guid userId, AddCoinWinnerMission model, string missionowner)
        {
            var bangkokTime = GetBangkokTime();
 
            var list_missioner = await _context.MISSIONS.ToListAsync();
            var missioner = list_missioner.Where(m => m.MISSION_ID == model.MISSION_ID).ToList();


            if (missionowner != "9")
            {
                var missiontype = ConvertMissionOwner(missionowner);
                missioner = missioner.Where(wh => missiontype.Contains(wh.Participate_Type)).ToList();
            }
            if (missioner == null)
                throw new Exception("User not found.");

            //var mission = await _context.MISSIONS
            //    .FirstOrDefaultAsync(m => m.MISSION_ID == model.MISSION_ID);

            //if (mission == null)
            //    throw new Exception("Mission not found.");

            //var sentMission = await _context.USER_PHOTO_MISSIONS
            //    .FirstOrDefaultAsync(sm => sm.MISSION_ID == model.MISSION_ID && sm.A_USER_ID == model.A_USER_ID && sm.Approve == true);

            var sentMission = await _context.USER_PHOTO_MISSIONS
                .FirstOrDefaultAsync(sm => sm.MISSION_ID == model.MISSION_ID && sm.Approve == true && model.A_USER_ID_list.Contains(sm.A_USER_ID));


            if (sentMission == null)
                throw new Exception("User not sent mission yet");



            var RankCoin = 0;
            using var transaction = await _context.Database.BeginTransactionAsync(); // เริ่มต้น transaction

            try
            {
                foreach (var m_userid in model.A_USER_ID_list)
                {
                    Console.WriteLine($"User ID: {userId}");

                    if (model.Rank == 1)
                    {
                        RankCoin = missioner.FirstOrDefault().WinnerStCoin ?? 0;

                    }
                    else if (model.Rank == 2)
                    {
                        RankCoin = missioner.FirstOrDefault().WinnerNdCoin ?? 0;

                    }
                    else if (model.Rank == 3)
                    {
                        RankCoin = missioner.FirstOrDefault().WinnerRdCoin ?? 0;

                    }

                    var missionPointTransaction = new CoinTransaction
                    {
                        COIN_TRANSACTION_ID = Guid.NewGuid(),
                        A_USER_ID = m_userid,
                        Coin_Type = CoinType.MissionPoint,
                        Amount = RankCoin,
                        Transaction_Type = $"Mission winner Reward Rank {model.Rank}",
                        Transaction_Date = bangkokTime,
                        Description = $"Mission winner  Rank {model.Rank} reward for mission: {missioner.FirstOrDefault().MISSION_NAME}"
                    };
                    // สร้างรายการ CoinTransaction
                    var coinTransaction = new CoinTransaction
                    {
                        COIN_TRANSACTION_ID = Guid.NewGuid(),
                        A_USER_ID = m_userid,
                        Coin_Type = CoinType.KaeaCoin,
                        Amount = RankCoin,
                        Transaction_Type = $"Mission winner Reward Rank {model.Rank}",
                        Transaction_Date = bangkokTime,
                        Description = $"Mission winner  Rank {model.Rank} reward for mission: {missioner.FirstOrDefault().MISSION_NAME}"
                    };
                    ;

                    // ทำการกำหนดค่าให้กับ sentMission (ควรระวังว่า sentMission ไม่ควรเป็น null)
                    //sentMission.IsReward = true;

                    // เพิ่มรายการ CoinTransaction ลงในฐานข้อมูล
                    _context.COIN_TRANSACTIONS.Add(coinTransaction);

                    // บันทึกข้อมูลแต่ละรายการ
                    var result = await _context.SaveChangesAsync();

                    if (result > 0) // ถ้าบันทึกข้อมูลสำเร็จ
                    {
                        Console.WriteLine($"CoinTransaction added for User: {userId}");

                        // อัพเดทสถานะการมอบรางวัลใน `sentMission`
                        sentMission.IsReward = true;

                        // บันทึกสถานะการเปลี่ยนแปลงของ sentMission
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        Console.WriteLine($"Failed to add CoinTransaction for User: {userId}");
                        throw new Exception($"Failed to process transaction for User: {userId}"); // ข้อผิดพลาดจะทำให้ Rollback
                    }
                }

                // หากไม่มีข้อผิดพลาดเกิดขึ้นทั้งหมด ให้ commit การเปลี่ยนแปลง
                await transaction.CommitAsync();

                return "Coin reward added successfully.";

            }
            catch (Exception ex)
            {
                // หากเกิดข้อผิดพลาดในระหว่างกระบวนการ ให้ Rollback
                Console.WriteLine($"Error during processing: {ex.Message}");


                await transaction.RollbackAsync(); // Rollback ทุกการบันทึก


                return $"Error during processing: {ex.Message}";
            }



        }
         
        public async Task<string> MissionerAddWinnerCoinAllMissionAsync(Guid userId, AddCoinWinnerMission model, string missionowner)
        {
            try
            {
                var bangkokTime = GetBangkokTime();

                // เฉพาะเจ้าของ Mission ถึงจะมีสิทธิ์
                //var missioner = await _context.MISSIONS.FirstOrDefaultAsync(m => m.Missioner == userId);

                //if (missioner == null)
                //    throw new Exception("User not found.");


                // IQueryable<Mission> query_user = _context.MISSIONS;
                var list_missioner = await _context.MISSIONS.ToListAsync();
                var missioner = list_missioner.Where(m => m.MISSION_ID == model.MISSION_ID).ToList();


                if (missionowner != "9")
                {
                    var missiontype = ConvertMissionOwner(missionowner);
                    missioner = missioner.Where(wh => missiontype.Contains(wh.Participate_Type)).ToList();
                }
                if (missioner == null)
                    throw new Exception("User not found.");

                //var mission = await _context.MISSIONS
                //    .FirstOrDefaultAsync(m => m.MISSION_ID == model.MISSION_ID);

                //if (mission == null)
                //    throw new Exception("Mission not found.");

                object sentMission =
    await _context.USER_PHOTO_MISSIONS
        .FirstOrDefaultAsync(m => m.MISSION_ID == model.MISSION_ID && m.A_USER_ID == model.A_USER_ID && m.Approve == true);

                if (sentMission == null)
                {
                    sentMission = await _context.USER_TEXT_MISSIONS
                        .FirstOrDefaultAsync(m => m.MISSION_ID == model.MISSION_ID && m.A_USER_ID == model.A_USER_ID && m.Approve == true);
                }

                if (sentMission == null)
                {
                    sentMission = await _context.USER_VIDEO_MISSIONS
                        .FirstOrDefaultAsync(m => m.MISSION_ID == model.MISSION_ID && m.A_USER_ID == model.A_USER_ID && m.Approve == true);
                }

                if (sentMission == null)
                    throw new Exception("User not sent mission yet");

                // 🔒 ป้องกันการให้รางวัลซ้ำ
                bool alreadyRewarded = false;
                if (sentMission is UserPhotoMission photo && photo.IsReward == true) alreadyRewarded = true;
                if (sentMission is UserTextMission text && text.IsReward == true) alreadyRewarded = true;
                if (sentMission is UserVideoMission video && video.isReward == true) alreadyRewarded = true;

                if (alreadyRewarded)
                    throw new Exception("This user already received reward for this mission.");

                // ✅ ยังไม่เคยได้รางวัล: Mark ว่าได้แล้ว
                if (sentMission is UserPhotoMission p) p.IsReward = true;
                if (sentMission is UserTextMission t) t.IsReward = true;
                if (sentMission is UserVideoMission v) v.isReward = true;

                // อย่าลืม SaveChanges หลังจาก mark isReward
                await _context.SaveChangesAsync();




                var RankCoin = 0;
                using var transaction = await _context.Database.BeginTransactionAsync(); // เริ่มต้น transaction

           
                var m_userid = model.A_USER_ID;
                 
                Console.WriteLine($"User ID: {userId}");

                if (model.Rank == 1)
                {
                    RankCoin = missioner.FirstOrDefault().WinnerStCoin ?? 0;

                }
                else if (model.Rank == 2)
                {
                    RankCoin = missioner.FirstOrDefault().WinnerNdCoin ?? 0;

                }
                else if (model.Rank == 3)
                {
                    RankCoin = missioner.FirstOrDefault().WinnerRdCoin ?? 0;

                }


                // สร้างรายการ CoinTransaction
                var coinTransaction = new CoinTransaction
                {
                    COIN_TRANSACTION_ID = Guid.NewGuid(),
                    A_USER_ID = m_userid,
                    Coin_Type = CoinType.KaeaCoin,
                    Amount = RankCoin,
                    Transaction_Type = $"Mission winner Reward Rank {model.Rank}",
                    Transaction_Date = bangkokTime,
                    Description = $"Mission winner  Rank {model.Rank} reward for mission: {missioner.FirstOrDefault().MISSION_NAME}"
                };
                ;

                // ทำการกำหนดค่าให้กับ sentMission (ควรระวังว่า sentMission ไม่ควรเป็น null)
                //sentMission.IsReward = true;

                // เพิ่มรายการ CoinTransaction ลงในฐานข้อมูล
                _context.COIN_TRANSACTIONS.Add(coinTransaction); 
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();


                return "Coin reward added successfully.";

            }
            catch (Exception ex)
            { 
                return $"Error during processing: {ex.Message}";
            } 
        }
         
        public async Task<string> MissionerAddWinnerCoinRewardQRMissionAsync(Guid userId, AddCoinWinnerMission model)
        {
            var bangkokTime = GetBangkokTime();

            var missioner = await _context.MISSIONS
                .FirstOrDefaultAsync(m => m.Missioner == userId);

            if (missioner == null)
                throw new Exception("User not found.");

            var mission = await _context.MISSIONS
                .FirstOrDefaultAsync(m => m.MISSION_ID == model.MISSION_ID);

            if (mission == null)
                throw new Exception("Mission not found.");

            var sentMission = await _context.USER_QR_CODE_MISSIONS
                .FirstOrDefaultAsync(sm => sm.MISSION_ID == model.MISSION_ID && sm.A_USER_ID == model.A_USER_ID && sm.Approve == true);

            if (sentMission == null)
                throw new Exception("User not sent mission yet");

            var coinTransaction = new CoinTransaction
            {
                COIN_TRANSACTION_ID = Guid.NewGuid(),
                A_USER_ID = model.A_USER_ID,
                Coin_Type = CoinType.KaeaCoin,
                Amount = model.Amount,
                Transaction_Type = "Mission winner Reward",
                Transaction_Date = bangkokTime,
                Description = $"Mission winner reward for mission: {mission.MISSION_NAME}"
            };
            sentMission.IsReward = true;

            _context.COIN_TRANSACTIONS.Add(coinTransaction);
            await _context.SaveChangesAsync();

            return "Coin reward added successfully.";
        }

        public async Task<string> MissionerAddBatchPhotoMissionRewardAsync(Guid userId, Guid missionId, int Amount)
        {
            var bangkokTime = GetBangkokTime();

            var mission = await _context.MISSIONS
                .FirstOrDefaultAsync(m => m.MISSION_ID == missionId);
            if (mission == null) throw new Exception("Mission not found.");

            //var approvedUsers = await _context.USER_PHOTO_MISSIONS
            //    //.Where(upm => (upm.Approve ?? false) == true && (upm.IsReward ?? false) != true)
            //    .Where(upm => (upm.Approve == true && upm.Approve != null) && (upm.IsReward != true))
            //    //.Select(upm => upm.A_USER_ID)
            //    .ToListAsync();
            //        var approvedUsers = await _context.USER_PHOTO_MISSIONS
            //.Where(upm => upm.Approve == true && upm.IsReward == null)
            //.ToListAsync();
            var approvedUsers = await _context.USER_PHOTO_MISSIONS
            .Where(upm => upm.Approve == true && (upm.IsReward == null || upm.IsReward == false))
            .ToListAsync();

            // ถ้า approvedUsers เท่ากับ 0 ให้โยน Exception
            if (!approvedUsers.Any())
                throw new Exception("No users have approved this mission.");

            // Check if there are any users with approve = null or approve = false
            if (approvedUsers.Any(upm => upm.Approve == null || upm.Approve == false))
            {
                throw new Exception("Some users have not approved this mission.");
            }


            if (mission.Winners > 0)
            {
                var topWinnerUsers = await _context.USER_PHOTO_MISSIONS
                    .Where(tw => tw.MISSION_ID == missionId && tw.IsReward == true)
                    .Select(tw => tw.A_USER_ID)
                    .ToListAsync();

                if (topWinnerUsers.Count < mission.Winners)
                    throw new Exception("Top winners have not been rewarded yet Reward the winners first.");

                approvedUsers = approvedUsers.Where(upm => !topWinnerUsers.Contains(upm.A_USER_ID)).ToList();

                if (!approvedUsers.Any()) throw new Exception("All eligible users have already received a reward.");
            }

            foreach (var userMission in approvedUsers)
            {
                var coinTransaction = new CoinTransaction
                {
                    COIN_TRANSACTION_ID = Guid.NewGuid(),
                    A_USER_ID = userMission.A_USER_ID,
                    Coin_Type = CoinType.KaeaCoin,
                    Amount = Amount,
                    Transaction_Date = bangkokTime,
                    Transaction_Type = "Mission Reward",
                    Description = $"Reward for completed mission: {mission.MISSION_NAME}"
                };

                userMission.IsReward = true;
                _context.COIN_TRANSACTIONS.Add(coinTransaction);
            }

            await _context.SaveChangesAsync();
            return $"Coin reward of {Amount} added to {approvedUsers.Count} user.";
        }

        public async Task<string> MissionerAddBatchQRCodeMissionRewardAsync(Guid userId, Guid missionId, int Amount)
        {
            var bangkokTime = GetBangkokTime();

            var mission = await _context.MISSIONS
                .FirstOrDefaultAsync(m => m.MISSION_ID == missionId);
            if (mission == null) throw new Exception("Mission not found.");

            var approvedUsers = await _context.USER_QR_CODE_MISSIONS
                .Where(upm => upm.Approve == true && upm.IsReward != true)
                //.Select(upm => upm.A_USER_ID)
                .ToListAsync();

            if (!approvedUsers.Any()) throw new Exception("No users have approved this missions.");

            if (mission.Winners > 0)
            {
                var topWinnerUsers = await _context.USER_QR_CODE_MISSIONS
                    .Where(tw => tw.MISSION_ID == missionId && tw.IsReward == true)
                    .Select(tw => tw.A_USER_ID)
                    .ToListAsync();

                if (topWinnerUsers.Count < mission.Winners)
                    throw new Exception("Top winners have not been rewarded yet Reward the winners first.");

                approvedUsers = approvedUsers.Where(upm => !topWinnerUsers.Contains(upm.A_USER_ID)).ToList();

                if (!approvedUsers.Any()) throw new Exception("All eligible users have already received a reward.");
            }

            foreach (var userMission in approvedUsers)
            {
                var coinTransaction = new CoinTransaction
                {
                    COIN_TRANSACTION_ID = Guid.NewGuid(),
                    A_USER_ID = userMission.A_USER_ID,
                    Coin_Type = CoinType.KaeaCoin,
                    Amount = Amount,
                    Transaction_Date = bangkokTime,
                    Transaction_Type = "Mission Reward",
                    Description = $"Reward for completed mission: {mission.MISSION_NAME}"
                };

                userMission.IsReward = true;
                _context.COIN_TRANSACTIONS.Add(coinTransaction);
            }

            await _context.SaveChangesAsync();
            return $"Coin reward of {Amount} added to {approvedUsers.Count} user.";
        }

        //public async Task<string> ExecuteVideoMissionAsync_V_BK(Guid userId, ExecuteVideoMissionModel model)
        //{
        //    var bangkokTime = GetBangkokTime();

        //    var user = await _context.USERS
        //        .FirstOrDefaultAsync(u => u.A_USER_ID == userId);
        //    if (user == null) return "User not found.";

        //    var mission = await _context.MISSIONS
        //        .FirstOrDefaultAsync(m => m.MISSION_ID == model.MissionId && m.MISSION_TYPE == "Video");
        //    if (mission == null) return "Mission not found.";

        //    var userMission = await _context.USER_MISSIONS
        //        .FirstOrDefaultAsync(um => um.USER_MISSION_ID == model.UserMissionId);
        //    if (userMission == null) return "User mission not found.";

        //    //if (user == null) return "User not found.";

        //    if (model.VideoFile == null || model.VideoFile.Length == 0)
        //        return "A video file is required.";

        //    var allowedFileTypes = new[] { "video/mp4", "video/avi", "video/mov" };
        //    long maxFileSize = 50 * 1024 * 1024;
        //    var uploadDirectory = _configuration["AppSettings:VideoMissionUploadPath"];

        //    if (!allowedFileTypes.Contains(model.VideoFile.ContentType))
        //        return "Only MP4, AVI, MOV Video formats are allowed.";

        //    if (model.VideoFile.Length > maxFileSize)
        //        return "File too Large. Maximum size allowed is 50MB.";

        //    var uploadStart = DateTime.UtcNow;
        //    var uniqueFileName = $"{Guid.NewGuid()}_{model.VideoFile.FileName}";
        //    //var uploadPath = Path.Combine(uploadDirectory, uniqueFileName);
        //    var originalFilePath = Path.Combine(uploadDirectory, "original", uniqueFileName);
        //    var compressedFilePath = Path.Combine(uploadDirectory, "compressed", uniqueFileName);

        //    Directory.CreateDirectory(Path.GetDirectoryName(originalFilePath) ?? string.Empty);
        //    Directory.CreateDirectory(Path.GetDirectoryName(compressedFilePath) ?? string.Empty);

        //    //Directory.CreateDirectory(Path.GetDirectoryName(uploadPath) ?? string.Empty);

        //    //await using (var stream = new FileStream(uploadPath, FileMode.Create))
        //    //{
        //    //    await model.VideoFile.CopyToAsync(stream);
        //    //}

        //    await using (var stream = new FileStream(originalFilePath, FileMode.Create))
        //    {
        //        await model.VideoFile.CopyToAsync(stream);
        //    }

        //    var uploadEnd = DateTime.UtcNow;
        //    Console.WriteLine($"Upload Time: {(uploadEnd - uploadStart).TotalSeconds} seconds");
        //    var compressStart = DateTime.UtcNow;

        //    if (!File.Exists(originalFilePath))
        //        return "Fail: Video file not saved properly.";

        //    bool compressionSuccess = await CompressVideoAsync(originalFilePath, compressedFilePath);
        //    if (!compressionSuccess)
        //        return "Failed to compress video.";
        //    var compressEnd = DateTime.UtcNow;
        //    Console.WriteLine($"Compression Time: {(compressEnd - compressStart).TotalSeconds} seconds");

        //    var videoUrl = Path.Combine(_configuration["AppSettings:VideoMissionPath"], "compressed", uniqueFileName);
        //    //var videoUrl = Path.Combine(_configuration["AppSettings:VideoMissionPath"], uniqueFileName);

        //    var userVideoMission = new UserVideoMission
        //    {
        //        USER_VIDEO_MISSION_ID = Guid.NewGuid(),
        //        MISSION_ID = model.MissionId,
        //        A_USER_ID = userId,
        //        USER_MISSION_ID = model.UserMissionId,
        //        VideoUrl = videoUrl,
        //        Uploaded_At = bangkokTime,
        //        isReward = false
        //    };

        //    _context.USER_VIDEO_MISSIONS.Add(userVideoMission);
        //    await _context.SaveChangesAsync();

        //    return "Video uploaded successfully.";
        //}

        private async Task<bool> CompressVideoAsync(string inputFilePath, string outputFilePath)
        {
            try
            {
                string ffmpegPath = "C:\\Users\\Tulipe\\AppData\\Local\\Microsoft\\WinGet\\Links\\ffmpeg.exe"; // Use absolute path

                var startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-i \"{inputFilePath}\" -vf scale=1280:-1 -b:v 800k -preset fast -c:a aac -b:a 128k \"{outputFilePath}\" -y",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    // ✅ Read logs in background (Prevents blocking)
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync(); // ✅ Does not block UI

                    string output = await outputTask;
                    string error = await errorTask;

                    Console.WriteLine($"FFmpeg Output: {output}");
                    Console.WriteLine($"FFmpeg Error: {error}");

                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error compressing video: {ex.Message}");
                return false;
            }
        }

        public async Task<string> ExecuteTextMissionAsync(Guid userId, ExecuteTextMissionModel model)
        {
            var bangkokTime = GetBangkokTime();

            var user = await _context.USERS
                .FirstOrDefaultAsync(u => u.A_USER_ID == userId);
            if (user == null)
                throw new Exception("User not found");

            //var missioner = await _context.MISSIONS
            //    .FirstOrDefaultAsync(m => m.MISSION_ID == model.MISSION_ID && m.Missioner == userId);
            //if (missioner == null)
            //    throw new Exception("This user is not own this mission.");

            if (string.IsNullOrWhiteSpace(model.Text))
                throw new Exception("Please insert Text");

            var mission = await _context.USER_MISSIONS
                .Include(m => m.Mission)
                //.ThenInclude(m => m.Expire_Date)
                .FirstOrDefaultAsync(um => um.USER_MISSION_ID == model.USER_MISSION_ID && um.MISSION_ID == model.MISSION_ID);
            if (mission == null) throw new Exception("User mission not found.");

            if (bangkokTime < mission.Mission.Start_Date)
                throw new Exception("Mission not started.");

            if (mission.Mission.Expire_Date <= bangkokTime)
                throw new Exception("Mission has expired.");

            var existingMission = await _context.USER_TEXT_MISSIONS
                .FirstOrDefaultAsync(u => u.MISSION_ID == mission.MISSION_ID && u.A_USER_ID == userId);

            if (existingMission != null)
            {
                if (existingMission.Approve == null)
                    throw new Exception("You have already submitted this mission and it's awaiting approval.");

                if (existingMission.Approve == true)
                    throw new Exception($"You already completed the mission: {mission.Mission.MISSION_NAME}.");
            }

            var userTextMission = new UserTextMission
            {
                USER_TEXT_MISSION_ID = Guid.NewGuid(),
                A_USER_ID = userId,
                MISSION_ID = model.MISSION_ID,
                USER_MISSION_ID = model.USER_MISSION_ID,
                USER_TEXT = model.Text,
                Submitted_At = bangkokTime,
                Approve = null,
                IsView = true,
                
            };

            mission.Submitted_At = bangkokTime;
            mission.Verification_Status = "Waiting For confirmation.";
            _context.USER_TEXT_MISSIONS.Add(userTextMission);
            await _context.SaveChangesAsync();

            return "Executed Mission successfully.";
        }

        public async Task<(List<ApproveTextViewModel> data, int total,int totalPage)> GetTextApproveByMissionAsync(
    Guid missionId, int page, int pageSize, string missionowner, string? searchName)
        {
            IQueryable<UserTextMission> query = _context.USER_TEXT_MISSIONS
                .Include(m => m.UserMission)
                    .ThenInclude(um => um.User)
                .Include(m => m.UserMission.Mission);

            // Filter ตาม missionId
            query = query.Where(m => m.MISSION_ID == missionId);

            // Filter ตาม missionowner (ยกเว้น Admin ที่ missionowner == "9")
            if (missionowner != "9")
            {
                var missionTypes = ConvertMissionOwner(missionowner);
                query = query.Where(m => missionTypes.Contains(m.Mission.Participate_Type));
            }

            if (!string.IsNullOrEmpty(searchName))
            {
                var loweredSearch = searchName.ToLower();
                query = query.Where(m => m.UserMission.User.User_Name.ToLower().Contains(loweredSearch));
            }


            // นับจำนวนทั้งหมดก่อน paging
            var total = await query.CountAsync();
            var totalPage = (int)Math.Ceiling((double)total / pageSize);

            // ดึงข้อมูลตามหน้า
            var data = await query
                .OrderBy(m => m.Approve) // Approve = null หรือ false จะมาก่อน
                .ThenByDescending(m => m.Submitted_At).Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new ApproveTextViewModel
                {
                    USER_TEXT_MISSION_ID = m.USER_TEXT_MISSION_ID,
                    A_USER_ID = m.A_USER_ID,
                    LOGON_NAME = m.UserMission.User.LOGON_NAME,
                    USER_NAME = m.UserMission.User.User_Name,
                    BranchCode = m.UserMission.User.BranchCode,
                    Department = m.UserMission.User.Department,
                    MISSION_ID = m.MISSION_ID,
                    MISSION_NAME = m.UserMission.Mission.MISSION_NAME,
                    USER_MISSION_ID = m.USER_MISSION_ID,
                    SUBMIT_DATE = m.Submitted_At,
                    Approve = m.Approve,
                    Approve_DATE = m.Approve_At,
                    Approve_By = m.Approve_By,
                    Approve_By_NAME = _context.USERS.Where(u => u.A_USER_ID == m.Approve_By).Select(u => u.User_Name).FirstOrDefault(),
                    TEXT = new List<string> { m.USER_TEXT },
                    Is_View = m.IsView,
                    Reject_Des = m.UserMission.Accepted_Desc,
                })
                .ToListAsync();

            return (data, total, totalPage);
        }

        public async Task<List<ApproveTextViewModel>> GetAllTextApproveAsync(string missionowner)
        {
             
            IQueryable<UserTextMission> query_user = _context.USER_TEXT_MISSIONS;

            if (missionowner != "9")
            {
                var missiontype = ConvertMissionOwner(missionowner);
                query_user = query_user.Where(wh => missiontype.Contains(wh.Mission.Participate_Type));
            }

            var model = await query_user
                .Select(m => new ApproveTextViewModel
                {
                    USER_TEXT_MISSION_ID = m.USER_TEXT_MISSION_ID,
                    A_USER_ID = m.A_USER_ID,
                    LOGON_NAME = m.UserMission.User.LOGON_NAME, // ชื่อของคนที่อัปโหลดข้อความ
                    USER_NAME = m.UserMission.User.User_Name,
                    BranchCode = m.UserMission.User.BranchCode,
                    Department = m.UserMission.User.Department,
                    MISSION_ID = m.MISSION_ID,
                    MISSION_NAME = m.UserMission.Mission.MISSION_NAME,
                    USER_MISSION_ID = m.USER_MISSION_ID,
                    SUBMIT_DATE = m.Submitted_At,
                    Approve = m.Approve,
                    Approve_DATE = m.Approve_At,

                    Approve_By = m.Approve_By, // ID ของคนที่อนุมัติ
                    TEXT = new List<string> { m.USER_TEXT } // ข้อความที่ผู้ใช้ส่งมา
                })
                .ToListAsync();


           // var missiontype = ConvertMisstionOwner(missionowner);

            //var model = await _context.USER_TEXT_MISSIONS
            //    .Where(wh => missiontype.Contains(wh.Mission.Participate_Type))
            //    //.Where(wh => missiontype == 0 || missiontype.Contains(wh.Mission.Participate_Type))
            //    .Select(m => new ApproveTextViewModel
            //    {
            //        USER_TEXT_MISSION_ID = m.USER_TEXT_MISSION_ID,
            //        A_USER_ID = m.A_USER_ID,
            //        LOGON_NAME = m.UserMission.User.LOGON_NAME, // ชื่อของคนที่อัปโหลดข้อความ
            //        USER_NAME = m.UserMission.User.User_Name,
            //        BranchCode = m.UserMission.User.BranchCode,
            //        Department = m.UserMission.User.Department,
            //        MISSION_ID = m.MISSION_ID,
            //        MISSION_NAME = m.UserMission.Mission.MISSION_NAME,
            //        USER_MISSION_ID = m.USER_MISSION_ID,
            //        SUBMIT_DATE = m.Submitted_At,
            //        Approve = m.Approve,
            //        Approve_DATE = m.Approve_At,

            //        Approve_By = m.Approve_By, // ID ของคนที่อนุมัติ
            //        TEXT = new List<string> { m.USER_TEXT } // ข้อความที่ผู้ใช้ส่งมา
            //    })
            //    .ToListAsync();

            return model;
        }


        public async Task<string> MissionerApproveTextMissionAsync(Guid userId, ApproveTextMissionModel model)

        {
            var bangkokTime = GetBangkokTime();

            var userTextMission = await _context.USER_TEXT_MISSIONS
                .Include(pm => pm.UserMission)
               .ThenInclude(um => um.Mission)
                .FirstOrDefaultAsync(utm => utm.USER_MISSION_ID == model.USER_TEXT_MISSION_ID);

            if (userTextMission == null)
                throw new Exception("User mission not found.");

            //var missioner = await _context.MISSIONS
            //    .FirstOrDefaultAsync(m => m.MISSION_ID == userTextMission.MISSION_ID && m.Missioner == userId);

            //if (missioner == null)
            //    throw new Exception("You do not have permission to approve this mission.");

            if (userTextMission.Approve.HasValue)
                throw new Exception("This Text mission has already been reviewed");

            userTextMission.Approve = model.Approve;
            userTextMission.Approve_By = userId;
            userTextMission.Approve_At = bangkokTime;
            userTextMission.UserMission.Verification_Status = model.Approve == true ? "Approved" : "Rejected";
            userTextMission.UserMission.Accepted_Desc = model.Accepted_Desc;


            //    if (model.Approve == true)
            //    {
            //        //var coinTransaction = new CoinTransaction
            //        //{
            //        //    COIN_TRANSACTION_ID = Guid.NewGuid(),
            //        //    Amount = userTextMission.Mission.Coin_Reward,
            //        //    Transaction_Date = bangkokTime,
            //        //    Transaction_Type = "Mission Reward",
            //        //    Description = $"Reward for mission: {userTextMission.Mission.MISSION_NAME}",
            //        //    A_USER_ID = userTextMission.UserMission.A_USER_ID,
            //        //    Coin_Type = CoinType.KaeaCoin
            //        //};

            //        var pointTransaction = new CoinTransaction
            //        {
            //            COIN_TRANSACTION_ID = Guid.NewGuid(),
            //            Amount = userTextMission.Mission.Mission_Point,
            //            Transaction_Date = bangkokTime,
            //            Transaction_Type = "Mission Reward",
            //            Description = $"Reward for mission: {userTextMission.Mission.MISSION_NAME}",
            //            A_USER_ID = userTextMission.A_USER_ID,
            //            Coin_Type = CoinType.MissionPoint
            //        };

            //        //_context.COIN_TRANSACTIONS.Add(coinTransaction);
            //        _context.COIN_TRANSACTIONS.Add(pointTransaction);

            //        var pointToAdd = await _context.COIN_TRANSACTIONS
            //        .Where(c => c.A_USER_ID == userId && c.Coin_Type == CoinType.MissionPoint && c.Transaction_Date.Month == bangkokTime.Month && c.Transaction_Date.Year == bangkokTime.Year)
            //        .SumAsync(c => c.Amount);


            //        var startOfMonth = new DateTime(bangkokTime.Year, bangkokTime.Month, 1);
            //        var leaderboardEntry = await _context.LEADERBOARDS
            //            .FirstOrDefaultAsync(lb => lb.A_USER_ID == userId && lb.MonthYear == startOfMonth);

            //        if (leaderboardEntry == null)
            //        {
            //            leaderboardEntry = new Leaderboard
            //            {
            //                LEADERBOARD_ID = Guid.NewGuid(),
            //                A_USER_ID = userId,
            //                Point = pointToAdd,
            //                MonthYear = startOfMonth,
            //                Create_at = bangkokTime,
            //            };
            //            _context.LEADERBOARDS.Add(leaderboardEntry);
            //        }
            //        else
            //        {
            //            leaderboardEntry.Point = pointToAdd;
            //            _context.LEADERBOARDS.Update(leaderboardEntry);
            //        }

            //        // Mark the mission as collected
            //        userTextMission.UserMission.Is_Collect = true;
            //        userTextMission.UserMission.Completed_Date = bangkokTime;
            //    }

            //    await _context.SaveChangesAsync();

            //    return model.Approve == true ? "Mission approved successfully" : "Mission rejected successfully";
            //}
            if (model.Approve == true)
            {
                // สร้าง point transaction ใหม่
                var pointTransaction = new CoinTransaction
                {
                    COIN_TRANSACTION_ID = Guid.NewGuid(),
                    Amount = userTextMission.Mission.Mission_Point,
                    Transaction_Date = bangkokTime,
                    Transaction_Type = "Mission Reward",
                    Description = $"Reward for mission: {userTextMission.Mission.MISSION_NAME}",
                    A_USER_ID = userTextMission.A_USER_ID,
                    Coin_Type = CoinType.MissionPoint
                };

                // เพิ่ม point transaction เข้าไปในฐานข้อมูล
                _context.COIN_TRANSACTIONS.Add(pointTransaction); 

                // เพิ่ม Coin ไปต่อ
                var cointype = userTextMission.UserMission.Mission.MISSION_TypeCoin ?? 0;
                var pointTransaction_coin = new CoinTransaction
                {
                    COIN_TRANSACTION_ID = Guid.NewGuid(),
                    Amount = userTextMission.Mission.Mission_Point,
                    Transaction_Date = bangkokTime,
                    Transaction_Type = cointype == 0 ? "Mission Reward" : "Receive from Mission",
                    Description = $"{pointTransaction.Transaction_Type} : {userTextMission.Mission.MISSION_NAME}",
                    A_USER_ID = userTextMission.A_USER_ID,
                    Coin_Type = cointype == 0 ? CoinType.KaeaCoin : CoinType.ThankCoin
                };
                _context.COIN_TRANSACTIONS.Add(pointTransaction_coin);

                // คำนวณคะแนนรวมที่ได้เพิ่มเข้าไป
                var pointToAdd = await _context.COIN_TRANSACTIONS
                    .Where(c => c.A_USER_ID == userTextMission.A_USER_ID && c.Coin_Type == CoinType.MissionPoint &&
                                c.Transaction_Date.Month == bangkokTime.Month && c.Transaction_Date.Year == bangkokTime.Year)
                    .SumAsync(c => c.Amount);

                // บวก amount จาก pointTransaction ลงไปใน pointToAdd
                pointToAdd += pointTransaction.Amount;  // เพิ่มคะแนนใหม่เข้าไป

                // Log ค่า pointToAdd ที่เพิ่มแล้ว
                _logger.LogInformation($"New pointToAdd: {pointToAdd}");

                // คำนวณและอัปเดต leaderboard
                var startOfMonth = new DateTime(bangkokTime.Year, bangkokTime.Month, 1);
                var leaderboardEntry = await _context.LEADERBOARDS
                    .FirstOrDefaultAsync(lb => lb.A_USER_ID == userTextMission.A_USER_ID && lb.MonthYear == startOfMonth);

                if (leaderboardEntry == null)
                {
                    leaderboardEntry = new Leaderboard
                    {
                        LEADERBOARD_ID = Guid.NewGuid(),
                        A_USER_ID = userTextMission.A_USER_ID,
                        Point = pointToAdd,
                        MonthYear = startOfMonth,
                        Create_at = bangkokTime,
                    };
                    _context.LEADERBOARDS.Add(leaderboardEntry);
                }
                else
                {
                    leaderboardEntry.Point = pointToAdd;
                    _context.LEADERBOARDS.Update(leaderboardEntry);
                }

                // Mark the mission as collected
                userTextMission.UserMission.Is_Collect = true;
                userTextMission.UserMission.Completed_Date = bangkokTime;


            }
            // บันทึกการเปลี่ยนแปลงในฐานข้อมูลหลังการคำนวณ
            await _context.SaveChangesAsync();

            return model.Approve == true ? "Mission approved successfully" : "Mission rejected successfully";
        }


        public async Task<string> MissionerAddWinnerCoinRewardTextMissionAsync(Guid userId, AddCoinWinnerMission model)
        {
            var bangkokTime = GetBangkokTime();

            //var missioner = await _context.MISSIONS
            //    .FirstOrDefaultAsync(m => m.Missioner == userId);

            //if (missioner == null)
            //    throw new Exception("User not found.");

            //var mission = await _context.MISSIONS
            //    .FirstOrDefaultAsync(m => m.MISSION_ID == model.MISSION_ID);

            //if (mission == null)
            //    throw new Exception("Mission not found.");

            //var sentMission = await _context.USER_TEXT_MISSIONS
            //    .FirstOrDefaultAsync(sm => sm.MISSION_ID == model.MISSION_ID && sm.A_USER_ID == model.A_USER_ID && sm.Approve == true);

            //if (sentMission == null)
            //    throw new Exception("User not sent mission yet");

            //var coinTransaction = new CoinTransaction
            //{
            //    COIN_TRANSACTION_ID = Guid.NewGuid(),
            //    A_USER_ID = model.A_USER_ID,
            //    Coin_Type = CoinType.KaeaCoin,
            //    Amount = model.Amount,
            //    Transaction_Type = "Mission winner Reward",
            //    Transaction_Date = bangkokTime,
            //    Description = $"Mission winner reward for mission: {mission.MISSION_NAME}"
            //};
            //sentMission.IsReward = true;

            //_context.COIN_TRANSACTIONS.Add(coinTransaction);
            //await _context.SaveChangesAsync();


            return "Coin reward added successfully.";
        }

        public async Task<string> MissionerAddBatchTexTMissionRewardAsync(Guid userId, Guid missionId, int Amount)
        {
            var bangkokTime = GetBangkokTime();

            var mission = await _context.MISSIONS
                .FirstOrDefaultAsync(m => m.MISSION_ID == missionId);
            if (mission == null) throw new Exception("Mission not found.");

            var approvedUsers = await _context.USER_TEXT_MISSIONS
                .Where(upm => upm.Approve == true && upm.IsReward != true)
                //.Select(upm => upm.A_USER_ID)
                .ToListAsync();

            if (!approvedUsers.Any()) throw new Exception("No users have approved this missions.");

            if (mission.Winners > 0)
            {
                var topWinnerUsers = await _context.USER_TEXT_MISSIONS
                    .Where(tw => tw.MISSION_ID == missionId && tw.IsReward == true)
                    .Select(tw => tw.A_USER_ID)
                    .ToListAsync();

                if (topWinnerUsers.Count < mission.Winners)
                    throw new Exception("Top winners have not been rewarded yet Reward the winners first.");

                approvedUsers = approvedUsers.Where(upm => !topWinnerUsers.Contains(upm.A_USER_ID)).ToList();

                if (!approvedUsers.Any()) throw new Exception("All eligible users have already received a reward.");
            }

            foreach (var userMission in approvedUsers)
            {
                var coinTransaction = new CoinTransaction
                {
                    COIN_TRANSACTION_ID = Guid.NewGuid(),
                    A_USER_ID = userMission.A_USER_ID,
                    Coin_Type = CoinType.KaeaCoin,
                    Amount = Amount,
                    Transaction_Date = bangkokTime,
                    Transaction_Type = "Mission Reward",
                    Description = $"Reward for completed mission: {mission.MISSION_NAME}"
                };

                userMission.IsReward = true;
                _context.COIN_TRANSACTIONS.Add(coinTransaction);
            }

            await _context.SaveChangesAsync();
            return $"Coin reward of {Amount} added to {approvedUsers.Count} user.";
        }




        public async Task<string> ExecuteVideoMissionAsync(Guid userId, ExecuteVideoModel model)
        {
            var bangkokTime = GetBangkokTime();

            _logger.LogInformation($"User {userId} is attempting to execute mission {model.missionId}.");

            var mission = await _context.MISSIONS
                .FirstOrDefaultAsync(m => m.MISSION_ID == model.missionId);

            if (mission == null)
            {
                _logger.LogWarning($"Mission {model.missionId} not found for User {userId}.");
                throw new Exception("Mission not found.");
            }

            if (bangkokTime >= mission.Expire_Date)
            {
                _logger.LogWarning($"Mission {model.missionId} is expired.");
                throw new Exception("Mission is expired.");
            }

            if (bangkokTime < mission.Start_Date)
            {
                _logger.LogWarning($"Mission {model.missionId} is not started yet.");
                throw new Exception("Mission is not started.");
            }

            // userId = Guid.Parse("40E05D2E-C31E-4940-80F5-48575C9B96F7");
            //model.missionId = Guid.Parse("729DB737-5521-480E-87DA-0086AA01B1A9");

            var userMission = await _context.USER_MISSIONS
                 .FirstOrDefaultAsync(um => um.A_USER_ID == userId && um.MISSION_ID == model.missionId);

            //return "Step 0";
            if (userMission == null)
            {
                _logger.LogWarning($"Mission {model.missionId} was not accepted by User {userId}.");
                throw new Exception("Mission not accepted by the user.");
            }

            if (userMission.Completed_Date != null)
            {
                _logger.LogWarning($"Mission {model.missionId} is already completed by User {userId}.");
                throw new Exception("Mission already completed.");
            }

            var existingPhotoMission = await _context.USER_VIDEO_MISSIONS
                 .AsNoTracking()
                .FirstOrDefaultAsync(uqm => uqm.A_USER_ID == userId && uqm.MISSION_ID == model.missionId);

            //var existingPhotoMission = await _context.USER_VIDEO_MISSIONS
            //    .Where(uqm => uqm.A_USER_ID == userId && uqm.MISSION_ID == model.missionId).ToListAsync();

            if (existingPhotoMission != null)
            {
                throw new Exception("User has already sent video for this mission.");
            }

           // return "Step 0xxx";
            if (model.videoFile == null)
                throw new Exception("At least 1 video required.");

            //var uploadedUrls = new List<string>();
            var allowedFileTypes = new[] {
                "video/mp4",
                "video/quicktime",
                "video/hevc",
                "video/h265",
            };

            long maxFileSize = 100 * (1024 * 1024); // 50MB max file size
            var uploadDirectory = _configuration["AppSettings:VideoMissionUploadPath"];
            _logger.LogInformation($"VideoMissionUploadPath: {uploadDirectory}");

            if (string.IsNullOrEmpty(uploadDirectory))
            {
                _logger.LogError("VideoMissionUploadPath is not set in the configuration.");
                throw new Exception("The upload directory path is not configured.");
            }


            var file = model.videoFile;


            if (file.Length > maxFileSize)
            {
                throw new Exception($"File {file.FileName} is too large. Maximum size allowed is 100 MB.");
            }

            if (!allowedFileTypes.Contains(file.ContentType))
            {
                throw new Exception($"Invalid file type for {file.FileName}. Only MP4, MOV, HEVC Videos are allowed.");
            }


            //return "Step 1";
            var USER_VIDEO_MISSION_ID = Guid.NewGuid();
            var fileUrl = "";
            var fileExtension = Path.GetExtension(file.FileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
            var imagePath = _configuration["AppSettings:VideoMissionPath"];
            var newFileName = "";

            var uploadsPath = Path.Combine(uploadDirectory, "uploads");
            Directory.CreateDirectory(uploadsPath);


            var inputFilePath = Path.Combine(uploadsPath, Path.GetFileName(file.FileName));
            //var outputFilePath = Path.ChangeExtension(inputFilePath, ".mp4");
            var outputFilePath = Path.Combine(uploadsPath, $"{USER_VIDEO_MISSION_ID}.mp4");


            using (var stream = new FileStream(inputFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var success = await ConvertHevcToMp4Async(inputFilePath, outputFilePath);

            //var success = true;
            if (success[0] == "0")
            {
                throw new Exception($"Invalid file type for {file.FileName}. Only  MP4, MOV, HEVC Videos are allowed.");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(outputFilePath);

            // ลบไฟล์ต้นฉบับและ output หากต้องการ
            System.IO.File.Delete(inputFilePath);


            var userVideoMission = new UserVideoMission
            {
                USER_VIDEO_MISSION_ID = USER_VIDEO_MISSION_ID,
                MISSION_ID = model.missionId,
                A_USER_ID = userId,
                USER_MISSION_ID = model.userMissionId,
                Uploaded_At = bangkokTime,
                Approve = null,
                Approve_By = null,
                VideoUrl = Path.Combine("uploads/uploads", $"{USER_VIDEO_MISSION_ID}.mp4").Replace("\\", "/"),
                IsView=true,
            };

            //fileUrl = Path.Combine(imagePath, newFileName);  // ใช้ชื่อไฟล์ใหม่
           // uploadedUrls.Add(fileUrl);


             userMission.Verification_Status = "Waiting for Confirmation.";
            userMission.Submitted_At = bangkokTime;
            _context.USER_VIDEO_MISSIONS.Add(userVideoMission);
                await _context.SaveChangesAsync();

                return $"Mission executed successfully {inputFilePath} ,{outputFilePath}";
             
        }

  
        public async Task<List<string>> ConvertHevcToMp4Async(string inputFilePath, string outputFilePath)
        {
            List<string> result = new List<string>();

            try
            { 
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
                var ffmpegPath = $@"{uploadsPath}\ffmpeg.exe"; // หาก ffmpeg ถูกเพิ่มไว้ใน PATH แล้ว

                // ปรับปรุงคำสั่งเพิ่มเติมสำหรับการบีบอัดไฟล์
                //var args = $"-i \"{inputFilePath}\" -c:v libx264 -preset fast -crf 23 \"{outputFilePath}\"";
                var args = $"-i \"{inputFilePath}\" -c:v libx264 -preset fast -crf 35 -c:a aac -b:a 96k \"{outputFilePath}\"";


                var processStartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();

                    // อ่าน error เผื่อใช้ debug
                    string stderr = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        // ถ้ามีข้อผิดพลาด ให้แสดงข้อความจาก stderr 
                        result.Add("0");
                        result.Add($"Error during conversion: {stderr}"); 
                    }

                    // หากทำงานเสร็จสมบูรณ์
                    result.Add("1");
                    result.Add($"Conversion successful!");  
                }
                 
            }
            catch (Exception ex)
            {
                // log error ได้ที่นี่
                result.Add("0");
                result.Add($"Error during conversion: {ex.Message}");
                //Console.WriteLine($"Error during conversion: {ex.Message}"); 
            }

            return result;
        }
        public async Task<(List<FeedViewModel> data, int total, int totalPage)> GetMissionFeedAsync(Guid userId, int page, int pageSize, string? type = null, string? missionName = null, string? displayName = null)
        {
            var feedItems = new List<FeedViewModel>();

            var photoResults = await _context.USER_PHOTO_MISSIONS
                .Include(p => p.UserMission).ThenInclude(um => um.User)
                .Include(p => p.Mission)
                .Include(p => p.IMAGES)
                .Where(p => p.Approve == true && p.Mission.Is_Public == true && p.IsView == true)
                .Select(p => new FeedViewModel
                {
                    Type = "photo",
                    USER_MISSION_ID = p.USER_PHOTO_MISSION_ID,
                    USER_ID = p.A_USER_ID,
                    USER_NAME = p.UserMission.User.User_Name,
                    Display_NAME = p.UserMission.User.DisplayName,
                    ImageURL = p.UserMission.User.ImageUrls,
                    LOGON_NAME = p.UserMission.User.LOGON_NAME,
                    BranchCode = p.UserMission.User.BranchCode,
                    Department = p.UserMission.User.Department,
                    MISSION_ID = p.MISSION_ID,
                    MISSION_NAME = p.Mission.MISSION_NAME,
                    SUBMIT_DATE = p.Uploaded_At,
                    CONTENT_URLS = p.IMAGES.Select(img => img.ImageUrl).ToList(),
                }).ToListAsync();

            feedItems.AddRange(photoResults);

            // 2. Video Missions
            var videoResults = await _context.USER_VIDEO_MISSIONS
                .Include(v => v.UserMission).ThenInclude(um => um.User)
                .Include(v => v.Mission)
                .Where(v => v.Approve == true && v.Mission.Is_Public == true && v.IsView == true)
                .Select(v => new FeedViewModel
                {
                    Type = "video",
                    USER_MISSION_ID = v.USER_VIDEO_MISSION_ID,
                    USER_ID = v.A_USER_ID,
                    USER_NAME = v.UserMission.User.User_Name,
                    Display_NAME = v.UserMission.User.DisplayName,
                    ImageURL = v.UserMission.User.ImageUrls,
                    LOGON_NAME = v.UserMission.User.LOGON_NAME,
                    BranchCode = v.UserMission.User.BranchCode,
                    Department = v.UserMission.User.Department,
                    MISSION_ID = v.MISSION_ID,
                    MISSION_NAME = v.Mission.MISSION_NAME,
                    SUBMIT_DATE = v.Uploaded_At,
                    CONTENT_URLS = new List<string> { v.VideoUrl }
                }).ToListAsync();

            feedItems.AddRange(videoResults);

            // 3. Text Missions
            var textResults = await _context.USER_TEXT_MISSIONS
                .Include(t => t.UserMission).ThenInclude(um => um.User)
                .Include(t => t.UserMission.Mission)
                .Where(t => t.Approve == true && t.Mission.Is_Public == true && t.IsView == true)
                .Select(t => new FeedViewModel
                {
                    Type = "text",
                    USER_MISSION_ID = t.USER_TEXT_MISSION_ID,
                    USER_ID = t.A_USER_ID,
                    USER_NAME = t.UserMission.User.User_Name,
                    Display_NAME = t.UserMission.User.DisplayName,
                    ImageURL = t.UserMission.User.ImageUrls,
                    LOGON_NAME = t.UserMission.User.LOGON_NAME,
                    BranchCode = t.UserMission.User.BranchCode,
                    Department = t.UserMission.User.Department,
                    MISSION_ID = t.MISSION_ID,
                    MISSION_NAME = t.UserMission.Mission.MISSION_NAME,
                    SUBMIT_DATE = t.Submitted_At,
                    CONTENT_URLS = new List<string> { t.USER_TEXT },
                }).ToListAsync();

            feedItems.AddRange(textResults);

            // 4. Apply filter before pagination
            if (!string.IsNullOrEmpty(type))
                feedItems = feedItems.Where(f => f.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!string.IsNullOrEmpty(missionName))
                feedItems = feedItems.Where(f => f.MISSION_NAME.Contains(missionName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!string.IsNullOrEmpty(displayName))
                feedItems = feedItems.Where(f => f.Display_NAME.Contains(displayName, StringComparison.OrdinalIgnoreCase)).ToList();

            // 5. Get all USER_MISSION_IDs
            var missionIds = feedItems.Select(f => f.USER_MISSION_ID).ToList();

            // 6. Like Counts (batch)
            var likeCounts = await _context.Feed_Likes
                .Where(l => missionIds.Contains(l.USER_MISSION_ID) && l.IS_LIKE == true)
                .GroupBy(l => l.USER_MISSION_ID)
                .Select(g => new { USER_MISSION_ID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.USER_MISSION_ID, x => x.Count);

            // 7. IsLiked by current user (batch)
            var likedByUser = await _context.Feed_Likes
                .Where(l => missionIds.Contains(l.USER_MISSION_ID) && l.A_USER_ID == userId && l.IS_LIKE == true)
                .Select(l => l.USER_MISSION_ID)
                .ToListAsync();

            // 8. Map back to feedItems
            foreach (var item in feedItems)
            {
                item.LIKE_COUNT = likeCounts.ContainsKey(item.USER_MISSION_ID) ? likeCounts[item.USER_MISSION_ID] : 0;
                item.IS_LIKE = likedByUser.Contains(item.USER_MISSION_ID);
            }

            // 9. Pagination
            var total = feedItems.Count;
            var totalPage = (int)Math.Ceiling((double)total / pageSize);
            var pagedData = feedItems
                .OrderByDescending(f => f.SUBMIT_DATE)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (pagedData, total, totalPage);
        }

        public async Task<List<UserLikeInfo>> GetLikesForMissionAsync(Guid userMissionId)
        {
            var likedUsers = await _context.Feed_Likes
                .Where(l => l.USER_MISSION_ID == userMissionId && l.IS_LIKE == true)
                .Join(_context.USERS, l => l.A_USER_ID, u => u.A_USER_ID, (l, u) => new UserLikeInfo
        {
                    UserId = u.A_USER_ID,
                    DisplayName = u.DisplayName,
                    ProfileImageUrl = u.ImageUrls, // Assuming ImageUrls is the profile image field
                    bracnhCode = u.BranchCode, // Assuming BranchCode is the field for branch
                    department = u.Department // Assuming Department is the field for department
                })
                .ToListAsync();

            return likedUsers;
        }



        public async Task<string> LikeMissionAsync(FeedLikeReq request)
        {
            // ค้นหาข้อมูลว่า User ได้กด Like ไว้หรือยัง
            var existing = await _context.Feed_Likes
                .FirstOrDefaultAsync(x => x.USER_MISSION_ID == request.UserMissionId && x.A_USER_ID == request.UserId);

            var bangkokTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var bangkokTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bangkokTimeZone);

            // ถ้าผู้ใช้เคยกด Like แล้ว (IS_LIKE == true), ให้ทำการ "Unlike"
            if (existing != null && existing.IS_LIKE == true)
            {
                existing.IS_LIKE = false;  // เปลี่ยนสถานะเป็น "Unlike"
                existing.UPDATED_AT = bangkokTime;
                _context.Feed_Likes.Update(existing);
                await _context.SaveChangesAsync();
                return "Unliked successfully.";
            }

            // ถ้ายังไม่เคยกด Like (existing == null) หรือกดแล้วแต่เป็น "Unlike", ทำการ "Like"
            if (existing == null)
            {
                var newLike = new FEEDLIKE
                {
                    LIKE_ID = Guid.NewGuid(),
                    USER_MISSION_ID = request.UserMissionId,
                    MISSION_ID = request.MissionId,
                    A_USER_ID = request.UserId,
                    TYPE = request.Type,
                    IS_LIKE = true,  // ตั้งค่าเป็น "Like"
                    CREATED_AT = bangkokTime,
                    UPDATED_AT = bangkokTime,
                };

                _context.Feed_Likes.Add(newLike);
                await _context.SaveChangesAsync();
                return "Liked successfully.";
            }
            else
            {
                // ถ้าผู้ใช้เคยกด Unlike หรือยังไม่ได้กดเลย ก็ทำการ "Like" ใหม่
                existing.IS_LIKE = true;  // เปลี่ยนสถานะเป็น "Like"
                existing.UPDATED_AT = bangkokTime;
                _context.Feed_Likes.Update(existing);
                await _context.SaveChangesAsync();
                return "Liked successfully.";
            }
        }


        public async Task<int> GetLikeCountAsync(Guid userMissionId)
        {
            return await _context.Feed_Likes
                .Where(f => f.USER_MISSION_ID == userMissionId && f.IS_LIKE == true)
                .CountAsync();
        }

        public async Task<string> SetIsViewAsync(IsViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.MissionType))
                throw new ArgumentException("MissionType is required.");

            switch (model.MissionType.ToLower())
            {
                case "text":
                    var text = await _context.USER_TEXT_MISSIONS.FindAsync(model.UserMissionId);
                    if (text == null) throw new Exception("Text mission not found.");
                    text.IsView = model.IsView;
                    break;

                case "photo":
                    var photo = await _context.USER_PHOTO_MISSIONS.FindAsync(model.UserMissionId);
                    if (photo == null) throw new Exception("Photo mission not found.");
                    photo.IsView = model.IsView;
                    break;

                case "video":
                    var video = await _context.USER_VIDEO_MISSIONS.FindAsync(model.UserMissionId);
                    if (video == null) throw new Exception("Video mission not found.");
                    video.IsView = model.IsView;
                    break;

                default:
                    throw new ArgumentException("Invalid mission type. Must be one of: text, photo, video.");
            }

            await _context.SaveChangesAsync();
            return "Is_View updated successfully.";
        }







    }

}