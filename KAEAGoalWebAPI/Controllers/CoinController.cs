using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using KAEAGoalWebAPI.Models;
using KAEAGoalWebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KAEAGoalWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CoinController : ControllerBase
    {
        private readonly ICoinService _coinService;

        public CoinController(ICoinService coinService)
        {
            _coinService = coinService;
        }

        [HttpPost("Add-KAEACoin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddKaeaCoin([FromBody] AddCoinModel model)
        {
            try
            {
                var adminUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                await _coinService.AddKaeaCoinToUserAsync(adminUserId, model);
                return Ok(new { message = "KAEACoins added successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("Add-THANKCoin")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddThankCoin([FromBody] AddCoinModel model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                await _coinService.AddThankCoinToUserAsync(userId, model);
                return Ok(new { message = "THANKCoins added successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPost("Give-THANKCoin-AllUsers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GiveThankCoinToAllUsers([FromBody] AddCoinModel model)
        {
            try
            {
                var adminUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                await _coinService.AddAllUserThankCoinAsync(adminUserId, model);
                return Ok(new { message = "THANKCoins distributed to all users successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("Coin-Balance")]
        public async Task<IActionResult> GetCoinBalance()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var coinBalance = await _coinService.GetUserCoinBalanceAsync(userId);

            return Ok(coinBalance);
        }

        [HttpPost("Convert-Coin")]
        public async Task<IActionResult> ConvertThankToKaeaAsync([FromBody] CoinConversionModel model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var result = await _coinService.ConvertThankToKaeaAsync(userId, model);
                return Ok(result);
            }
            catch(Exception ex)
            {
                return BadRequest(new { Message =  ex.Message });
            }
        }

        [HttpPost("Give-ThankCoin")]
        public async Task<IActionResult> GiveThankCoinAsync([FromBody] GiveThankCoinModel model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var result = await _coinService.GiveThankCoinAsync(userId, model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("Recent-Transaction")]
        public async Task<IActionResult> GetUserRecentTransaction()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var result = await _coinService.UserCoinTransactionAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
