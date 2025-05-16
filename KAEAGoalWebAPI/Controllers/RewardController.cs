using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using ClosedXML.Excel;
using KAEAGoalWebAPI.Models;
using KAEAGoalWebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc; 

namespace KAEAGoalWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class RewardController : ControllerBase
    {
        private readonly IRewardService _rewardService;

        public RewardController(IRewardService rewardService)
        {
            _rewardService = rewardService;
        }

        [HttpPost("Admin-Create-Reward")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateReward([FromForm] CreateRewardModel model)
        {
            try
            {
                await _rewardService.CreateRewardAsync(model);
                    return Ok(new { message = "Reward registered successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("rewards/{rewardId}")]
        public async Task<IActionResult> UpdateReward(Guid rewardId, [FromForm] CreateRewardModel model)
        {
            var result = await _rewardService.UpdateRewardAsync(rewardId, model);
            return Ok(new { message = result });
        }


        [HttpGet("Get-All-Reward")]
        public async Task<ActionResult<IEnumerable<RewardViewModel>>> GetAllReward()
        {
            var rewards = await _rewardService.GetAllRewardAsync();
            return Ok(rewards);
        }

        [HttpPost("Redeem-Reward")]
        public async Task<IActionResult> RedeemReward(RedeemRewardModel model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _rewardService.RedeemRewardAsync(userId, model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("Get-User-Reward")]
        public async Task<ActionResult<IEnumerable<UserRewardViewModel>>> GetUserRewards()
        {
            try
            {
                var userId =  Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                //var userId = Guid.Parse("A542677B-B038-4407-80AE-A60F1EC5CD33");
                var result = await _rewardService.GetUserRewardAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("Admin-Change-User-Reward-Status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult>ChangeUserRewardStatus(ChangeStatusUserReward model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _rewardService.ChangeStatusUserRewardAsync(userId, model);
                return Ok(result);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("Admin-Get-All-User-Reward")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserRewardViewModel>>> GetAllUserReward()
        {
            var userRewards = await _rewardService.GetAllUserRewardsAsync();
            return Ok(userRewards);
        }

        [HttpGet("Get-All-RewardCategory")]
        public async Task<ActionResult<IEnumerable<RewardCategoryViewModel>>> GetAllRewardCategory()
        {
            var rewards = await _rewardService.GetAllRewardCategoryAsync();
            return Ok(rewards);
        }

        [HttpGet("Admin-Get-All-User-Reward/export")]
        public async Task<IActionResult> ExportToExcel(DateTime startDate, DateTime endDate)
        {
            var workbook = new XLWorkbook();

            // ดึงข้อมูลทั้งหมดจาก RewardService
            var userrewards = await _rewardService.GetAllUserRewardsAsync();

            // 🔍 กรองตามช่วงวันที่ Redeem_Date
            var filteredRewards = userrewards
                .Where(ur => ur.Redeem_Date.Date >= startDate.Date && ur.Redeem_Date.Date <= endDate.Date&&ur.Reward_Status=="Prepair")
                .OrderBy(ur => ur.Redeem_Date)
                .ToList();

            var userrewardsExcel = filteredRewards.Select(ur => new UserRewardViewModelExcel
            {
                Reward_Name = ur.Reward_Name,
                Reward_Description = ur.Reward_Description,
                Reward_Status = ur.Reward_Status,
                Reward_Price = ur.Reward_Price,
                Redeem_Date = ur.Redeem_Date.ToString("dd/MM/yyyy"),
                Collect_Date = ur.Collect_Date?.ToString("dd/MM/yyyy"),
                REWARDCate_Name = ur.REWARDCate_Name,
                USER_NAME = ur.USER_NAME,
                User_Firstname = ur.User_Firstname,
                User_Lastname = ur.User_SurName,
                Department = ur.Department,
                DepartmentCode = ur.DepartmentCode,
            }).ToList();

            using (workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Sheet1");
                var columnNames = GetColumnNames<UserRewardViewModelExcel>();

                for (int col = 0; col < columnNames.Length; col++)
                {
                    worksheet.Cell(1, col + 1).Value = columnNames[col];
                }

                for (int i = 0; i < userrewardsExcel.Count; i++)
                {
                    var reward = userrewardsExcel[i];
                    var properties = typeof(UserRewardViewModelExcel).GetProperties();

                    for (int j = 0; j < properties.Length; j++)
                    {
                        var property = properties[j];
                        var value = property.GetValue(reward);
                        worksheet.Cell(i + 2, j + 1).Value = value?.ToString() ?? "N/A";
                    }
                }

                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;
                workbook.Dispose();

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ExportedData_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx");
            }
        }


        public static string[] GetColumnNames<T>()
        {
            return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Select(p => p.Name)
                            .ToArray();
        }
    }
}
