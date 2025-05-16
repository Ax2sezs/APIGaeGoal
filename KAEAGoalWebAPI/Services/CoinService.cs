using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KAEAGoalWebAPI.Data;
using KAEAGoalWebAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KAEAGoalWebAPI.Services
{
    public class CoinService : ICoinService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;
        private readonly ILogger<CoinService> _logger;

        private DateTime GetBangkokTime()
        {
            var bangkokTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bangkokTimeZone);
        }

        public CoinService(ApplicationDbContext context, IAuthService authService, ILogger<CoinService> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        public async Task AddKaeaCoinToUserAsync(Guid userId, AddCoinModel model)
        {
            var adminUser = await _context.USERS.FirstOrDefaultAsync(u => u.A_USER_ID == userId);
            if (adminUser == null || adminUser.IsAdmin != 9)
            {
                throw new UnauthorizedAccessException("Only an admin can add coins.");
            }

            if (model.Amount <= 0)
            {
                throw new Exception("Please specify a positive amount.");
            }

            var user = await _context.USERS.FirstOrDefaultAsync(u => u.A_USER_ID == model.UserId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            var bangkokTime = GetBangkokTime();

            var pointTransaction = new CoinTransaction
            {
                COIN_TRANSACTION_ID = Guid.NewGuid(),
                Amount = model.Amount,
                Transaction_Date = bangkokTime,
                Transaction_Type = "Mission Reward",
                Description = model.Description,
                A_USER_ID = user.A_USER_ID,
                Coin_Type = CoinType.MissionPoint
            };
            // เพิ่ม point transaction เข้าไปในฐานข้อมูล
            _context.COIN_TRANSACTIONS.Add(pointTransaction);
            var coinTransaction = new CoinTransaction
            {
                COIN_TRANSACTION_ID = Guid.NewGuid(),
                Amount = model.Amount,
                Coin_Type = CoinType.KaeaCoin,  // Set coin type to KAEACoin
                Transaction_Date = bangkokTime,
                Transaction_Type = "Mission Reward",  // Could be "Credit" or another appropriate value
                Description = model.Description,
                A_USER_ID = user.A_USER_ID,
            };

            _context.COIN_TRANSACTIONS.Add(coinTransaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Admin {userId} added {model.Amount} KaeaCoin to User {model.UserId}");
        }

        public async Task AddThankCoinToUserAsync(Guid userId, AddCoinModel model)
        {
            var User = await _context.USERS.FirstOrDefaultAsync(u => u.A_USER_ID == userId);
            if (userId == null || User.IsAdmin != 9)
            {
                throw new UnauthorizedAccessException("Only an admin can add coins.");
            }

            if (model.Amount <= 0)
            {
                throw new Exception("Please specify a positive amount.");
            }

            var user = await _context.USERS.FirstOrDefaultAsync(u => u.A_USER_ID == model.UserId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            var bangkokTime = GetBangkokTime();

            var coinTransaction = new CoinTransaction
            {
                COIN_TRANSACTION_ID = Guid.NewGuid(),
                Amount = model.Amount,
                Coin_Type = CoinType.ThankCoin,  // Set coin type to KAEACoin
                Transaction_Date = bangkokTime,
                Transaction_Type = "Receive from Admin",  // Could be "Credit" or another appropriate value
                Description = model.Description,
                A_USER_ID = user.A_USER_ID,
            };

            _context.COIN_TRANSACTIONS.Add(coinTransaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Admin {userId} added {model.Amount} KaeaCoin to User {model.UserId}");
        }
        public async Task AddAllUserThankCoinAsync(Guid userId, AddCoinModel model)
        {
            var adminUser = await _context.USERS.FirstOrDefaultAsync(u => u.A_USER_ID == userId);
            if (adminUser == null || adminUser.IsAdmin != 9)
            {
                throw new UnauthorizedAccessException("Only an admin can add coins.");
            }

            if (model.Amount <= 0)
            {
                throw new Exception("Please specify a positive amount.");
            }

            var users = await _context.USERS.ToListAsync();
            if (!users.Any())
            {
                throw new Exception("No users found.");
            }

            var bangkokTime = GetBangkokTime();

            var coinTransactions = users.Select(user =>
            {
                if (user.A_USER_ID == null)
                    throw new Exception($"User {user.FirstName} {user.LastName} has no valid ID.");

                return new CoinTransaction
                {
                    COIN_TRANSACTION_ID = Guid.NewGuid(),
                    Amount = model.Amount,
                    Coin_Type = CoinType.ThankCoin,
                    Transaction_Date = bangkokTime,
                    Transaction_Type = "Receive from Admin",
                    Description = model.Description,
                    A_USER_ID = user.A_USER_ID,
                };
            }).ToList();

            // **แบ่ง Insert เป็น Batch ละ 1,000 รายการ**
            const int batchSize = 1000;
            for (int i = 0; i < coinTransactions.Count; i += batchSize)
            {
                var batch = coinTransactions.Skip(i).Take(batchSize);
                await _context.COIN_TRANSACTIONS.AddRangeAsync(batch);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"Admin {userId} added {model.Amount} ThankCoin to {users.Count} users.");
        }




        public async Task<CoinBalanceModel> GetUserCoinBalanceAsync(Guid userId)
        {
            var bangkokTime = GetBangkokTime();

            var coinTransactions = await _context.COIN_TRANSACTIONS
                .Where(ct => ct.A_USER_ID == userId)
                .ToListAsync();

            //const int RemainingThankCoin = 50;

            //var RemainingGive = await _context.COIN_TRANSACTIONS
            //    .Where(ct => ct.Coin_Type == CoinType.ThankCoin && ct.Transaction_Type == "Give" && ct.Transaction_Date.Date == bangkokTime.Date && ct.A_USER_ID == userId)
            //    .SumAsync(ct => Math.Abs(ct.Amount));

            //var Remaining = RemainingThankCoin - RemainingGive;
            int newamout = coinTransactions
    .Where(ct => ct.Coin_Type == CoinType.ThankCoin && (ct.Transaction_Type == "Receive" || ct.Transaction_Type == "Conversion"))
    .Sum(ct => ct.Amount);
            int giveamout = coinTransactions
    .Where(ct => ct.Coin_Type == CoinType.ThankCoin && (ct.Transaction_Type == "Give"))
    .Sum(ct => ct.Amount);
            int adminamout = coinTransactions
   .Where(ct => ct.Coin_Type == CoinType.ThankCoin && (ct.Transaction_Type == "Receive from Admin"||ct.Transaction_Type=="Receive from Mission"))
   .Sum(ct => ct.Amount);

            int total = adminamout + giveamout;

            var coinBalance = new CoinBalanceModel
            {
                KaeaCoinBalance = coinTransactions
                .Where(ct => ct.Coin_Type == CoinType.KaeaCoin)
                .Sum(ct => ct.Amount),

                //ThankCoinBalance = coinTransactions
                //.Where(ct => ct.Coin_Type == CoinType.ThankCoin)
                //.Sum(ct => ct.Amount),
                ThankCoinBalance = total,

                //ThankCoinConvert = coinTransactions
                //.Where(ct => ct.Coin_Type == CoinType.ThankCoin && ct.Transaction_Type == "Receive" && ct.Coin_Type == CoinType.ThankCoin)
                //.Sum(ct => ct.Amount),
                ThankCoinConvert = newamout

                //RemainingThankCoinGive = Remaining
            };

            return coinBalance;
        }
        //public async Task<CoinBalanceModel> GetUserCoinBalanceAsync(Guid userId)
        //{
        //    var bangkokTime = GetBangkokTime();

        //    var coinTransactions = await _context.COIN_TRANSACTIONS
        //        .Where(ct => ct.A_USER_ID == userId)
        //        .ToListAsync();

        //    // คำนวณยอดทั้งหมดของ ThankCoin ที่ผู้ใช้มี (รวมจากทุกแหล่ง)
        //    var thankCoinBalance = coinTransactions
        //        .Where(ct => ct.Coin_Type == CoinType.ThankCoin)
        //        .Sum(ct => ct.Amount);

        //    // คำนวณยอด ThankCoin ที่ได้รับจาก Admin (ไม่สามารถแลกได้)
        //    var adminThankCoin = coinTransactions
        //        .Where(ct => ct.Coin_Type == CoinType.ThankCoin && ct.Transaction_Type == "Receive from Admin")
        //        .Sum(ct => ct.Amount);

        //    // คำนวณยอด ThankCoin ที่ได้รับจาก User อื่น (สามารถแลกได้)
        //    var receivedThankCoin = coinTransactions
        //        .Where(ct => ct.Coin_Type == CoinType.ThankCoin && ct.Transaction_Type == "Receive")
        //        .Sum(ct => ct.Amount);

        //    // คำนวณยอด ThankCoin ที่ถูก Give ให้คนอื่น (ต้องลบออกจากยอดที่แลกได้)
        //    var givenThankCoin = coinTransactions
        //        .Where(ct => ct.Coin_Type == CoinType.ThankCoin && ct.Transaction_Type == "Give")
        //        .Sum(ct => ct.Amount);

        //    // ตรวจสอบว่า givenThankCoin มีค่าติดลบหรือไม่
        //    if (givenThankCoin < 0)
        //    {
        //        givenThankCoin = 0; // ถ้ามีค่าติดลบ ให้ปรับเป็น 0
        //    }

        //    // คำนวณยอด ThankCoin ที่สามารถแลกได้ (ThankCoin จาก User - ThankCoin ที่ให้คนอื่น)
        //    var exchangeableThankCoin = receivedThankCoin - givenThankCoin;

        //    // ตรวจสอบว่า exchangeableThankCoin ไม่มีค่าติดลบ
        //    if (exchangeableThankCoin < 0)
        //    {
        //        exchangeableThankCoin = 0;
        //    }

        //    var coinBalance = new CoinBalanceModel
        //    {
        //        // คำนวณยอด KaeaCoin ที่ผู้ใช้มี
        //        KaeaCoinBalance = coinTransactions
        //            .Where(ct => ct.Coin_Type == CoinType.KaeaCoin)
        //            .Sum(ct => ct.Amount),

        //        // แสดงยอด ThankCoin ทั้งหมด
        //        ThankCoinBalance = thankCoinBalance,

        //        // คำนวณยอด ThankCoin ที่สามารถแลกเป็น Kcoin ได้
        //        ThankCoinConvert = exchangeableThankCoin
        //    };

        //    return coinBalance;
        //}




        public async Task<string> ConvertThankToKaeaAsync(Guid userId, CoinConversionModel model)
        {
            var bangkokTime = GetBangkokTime();

            const int ConversionRate = 10;

            if (model.ThankCoinAmount <= 0)
            {
                throw new Exception("Pleases specify a positive amount.");
            }

            if (model.ThankCoinAmount % ConversionRate != 0)
            {
                throw new Exception("Conversion amount must be a multiple of 10.");
            }

            var requiredThankCoin = model.ThankCoinAmount;
            var kaeaCoinAmount = requiredThankCoin / ConversionRate;

            var userThankBalance = await _context.COIN_TRANSACTIONS
                .Where(ct => ct.Coin_Type == CoinType.ThankCoin && (ct.Transaction_Type == "Receive"))
                .SumAsync(c => c.Amount);

            if (userThankBalance < requiredThankCoin)
            {
                throw new Exception("Insufficient ThankCoin balance.");
            }

            _context.COIN_TRANSACTIONS.Add(new CoinTransaction
            {
                COIN_TRANSACTION_ID = Guid.NewGuid(),
                A_USER_ID = userId,
                Coin_Type = CoinType.ThankCoin,
                Amount = -requiredThankCoin,
                Transaction_Type = "Conversion",
                Transaction_Date = bangkokTime,
                Description = "Converted ThankCoin to KaeaCoin",
            });

            _context.COIN_TRANSACTIONS.Add(new CoinTransaction
            {
                COIN_TRANSACTION_ID = Guid.NewGuid(),
                A_USER_ID = userId,
                Coin_Type = CoinType.KaeaCoin,
                Amount = kaeaCoinAmount,
                Transaction_Type = "Conversion",
                Description = "Converted ThankCoin to KaeaCoin",
                Transaction_Date = bangkokTime,
            });

            await _context.SaveChangesAsync();
            return "Conversion Successful.";
        }

        public async Task<string> GiveThankCoinAsync(Guid userId, GiveThankCoinModel model)
        {
            var bangkokTime = GetBangkokTime();

            var receiverName = await _context.USERS
                .FirstOrDefaultAsync(u => u.A_USER_ID == model.receiverId);

            var giverName = await _context.USERS
                .FirstOrDefaultAsync(u => u.A_USER_ID == userId);

            if (receiverName == giverName)
                throw new Exception("Cannot give coins to yourself");

            const int maxGiveLimitPerYear = 50;
            const int maxGiveLimitPerRecipient = 10;

            if (model.amount <= 0 || model.amount > maxGiveLimitPerRecipient)
                throw new Exception($"Invalid amount. You can give a maximum of {maxGiveLimitPerRecipient} ThankCoin per recipient per year.");

            // ตรวจสอบยอดคงเหลือของเหรียญ ThankCoin ที่ผู้ให้มี
            var senderBalance = await _context.COIN_TRANSACTIONS
                .Where(c => c.A_USER_ID == userId && c.Coin_Type == CoinType.ThankCoin && c.Transaction_Type== "Receive from Admin"||c.Transaction_Type== "Receive from Mission")
                .SumAsync(c => c.Amount);

            if (senderBalance < model.amount)
                throw new Exception("Insufficient ThankCoin balance.");

            var yearStart = new DateTime(bangkokTime.Year, 1, 1);

            // คำนวณยอดรวมที่ผู้ให้ให้เหรียญในปีนี้
            var totalGivenThisYear = await _context.COIN_TRANSACTIONS
                .Where(t => t.A_USER_ID == userId && t.Coin_Type == CoinType.ThankCoin && t.Transaction_Type == "Give" && t.Transaction_Date >= yearStart)
                .SumAsync(t => t.Amount);

            if (totalGivenThisYear + model.amount > maxGiveLimitPerYear)
            {
                var remainingTotalLimit = Math.Max(maxGiveLimitPerYear - totalGivenThisYear, 0);
                throw new Exception($"Annual limit exceeded. You can give only {remainingTotalLimit} more ThankCoin this year");
            }

            // คำนวณยอดรวมที่ผู้รับได้รับจากผู้ให้ในปีนี้
            var totalReceivedByReceiverFromGiverThisYear = await _context.COIN_TRANSACTIONS
                .Where(t => t.A_USER_ID == receiverName.A_USER_ID && t.Coin_Type == CoinType.ThankCoin && t.Transaction_Type == "Receive" && t.Transaction_Date >= yearStart && t.Giver_User_ID == userId)
                .SumAsync(t => t.Amount);

            // ตรวจสอบยอดรวมเหรียญที่ผู้รับได้รับจากผู้ให้
            if (totalReceivedByReceiverFromGiverThisYear + model.amount > maxGiveLimitPerRecipient)
            {
                var remainingRecipientLimit = Math.Max(maxGiveLimitPerRecipient - totalReceivedByReceiverFromGiverThisYear, 0);
                throw new Exception($"You can gift only {remainingRecipientLimit} more ThankCoin to {receiverName.User_Name} this year.");
            }

            // สร้างธุรกรรมของผู้ให้
            var giverTransaction = new CoinTransaction
            {
                COIN_TRANSACTION_ID = Guid.NewGuid(),
                A_USER_ID = userId,
                Receiver_User_ID = receiverName.A_USER_ID,
                Coin_Type = CoinType.ThankCoin,
                Amount = -model.amount,
                Transaction_Type = "Give",
                Transaction_Date = bangkokTime,
                //Description = $"Gave {model.amount} ThankCoin to {receiverName.User_Name}."
                Description = $"ThankCoin to {receiverName.User_Name} : {model.description}"
            };

            // สร้างธุรกรรมของผู้รับ
            var receiverTransaction = new CoinTransaction
            {
                COIN_TRANSACTION_ID = Guid.NewGuid(),
                A_USER_ID = receiverName.A_USER_ID,
                Giver_User_ID = giverName.A_USER_ID,
                Coin_Type = CoinType.ThankCoin,
                Amount = model.amount,
                Transaction_Type = "Receive",
                Transaction_Date = bangkokTime,
                //Description = $"Received {model.amount} ThankCoin from {giverName.User_Name}."
                Description = $"ThankCoin from {giverName.User_Name} : {model.description}"
            };

            // เพิ่มธุรกรรมทั้งสองรายการ
            _context.COIN_TRANSACTIONS.Add(giverTransaction);
            _context.COIN_TRANSACTIONS.Add(receiverTransaction);

            // บันทึกการเปลี่ยนแปลง
            await _context.SaveChangesAsync();

            // ส่งข้อความผลลัพธ์
            return $"Given successful. {model.amount} ThankCoin given to {receiverName.User_Name}.";
        }


        public async Task<List<UserCoinTransactionViewModel>> UserCoinTransactionAsync(Guid userId)
        {
            var transaction = await _context.COIN_TRANSACTIONS
                .Where(t => t.A_USER_ID == userId)
                .OrderByDescending(t => t.Transaction_Date)
                .ToListAsync();

            if (transaction == null)
                throw new Exception("No transaction history.");

            var viewModel = transaction.Select(t => new UserCoinTransactionViewModel
            {
                COIN_TRANSACTION_ID = t.COIN_TRANSACTION_ID,
                Amount = t.Amount,
                Transaction_Type = t.Transaction_Type,
                Transaction_Date = t.Transaction_Date,
                Description = t.Description,
                Coin_Type = t.Coin_Type
            }).ToList();

            return viewModel;
        }
        
    }
}
