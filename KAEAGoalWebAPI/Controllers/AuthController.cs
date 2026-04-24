using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.InkML;
using KAEAGoalWebAPI.Models;
using KAEAGoalWebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KAEAGoalWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("Admin-Register")]
        [Authorize]
        public async Task<IActionResult> Register([FromBody] UserRegistrationModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _authService.RegisterAsync(userId, model);

            if (string.IsNullOrEmpty(result))
                return BadRequest("Registration failed. User may already exists.");

            return Ok(new { result });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var token = await _authService.LoginAsync(model);

                if (token == null)
                    return Unauthorized("Invalid Username or Password.");

                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                // Log the exception here if needed
                // e.g., _logger.LogError(ex, "An error occurred during login.");

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An unexpected error occurred. Please try again later.",
                    Error = ex.Message // Remove this in production to avoid exposing sensitive information
                });
            }
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _authService.LogoutAsync(userId);

                Response.Cookies.Delete("refreshToken");

                return Ok(new { Message = result });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = ex.Message });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var result = await _authService.RefreshTokenAsync();
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }


        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetSelfDetails()
        {
            // Extract the user ID from the JWT token
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Fetch user details
            var userDetails = await _authService.GetCurrentUserDetailsAsync(currentUserId);

            if (userDetails == null)
                return NotFound(new { message = "User not found." });

            return Ok(userDetails);
        }

        [Authorize]
        [HttpGet("is-admin")]
        public IActionResult CheckIfUserIsAdmin()
        {
            var isAdmin = User.IsInRole("Admin");
            return Ok(new { isAdmin });
        }

        [Authorize]
        [HttpGet("admin-only")]
        public async Task<IActionResult> AdminOnlyAction()
        {
            var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            if (!await _authService.IsUserAdminAsync(currentUserId))
                return Forbid("You are not authorized to access this resource.");

            return Ok("Welcome, Admin!");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserDetails(Guid id)
        {
            var userModel = await _authService.GetUserDetailsAsync(id);
            if (userModel == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(userModel);
        }

        [HttpGet("Get-All-User")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserDetailsModel>>> GetAllUserAsync()
        {
            try
            {
                var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var users = await _authService.GetAllUserAsync(currentUserId);
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred: {ex.Message}");
            }
        }


        [HttpPut("Admin-Update-User-Detail")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserDetail(UserUpdateModel model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _authService.UpdateUserAsync(userId, model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPost("get-users-info")]
        public async Task<IActionResult> GetUsersInfo(
           [FromBody] GetUsersInfoRequest request)
        {
            if (request?.LogonNames == null || !request.LogonNames.Any())
                return BadRequest("LogonNames list is required");

            var users = await _authService
                .GetUsersInfoByLogonNamesAsync(request.LogonNames);

            return Ok(users);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("Admin-Update-User-StateCode")]
        public async Task<IActionResult> UpdateUserStateCode(
    [FromBody] UpdateUserStateCodeRequest request)
        {
            if (request == null)
                return BadRequest("Invalid payload");

            var result = await _authService
                .UpdateUserStateCodeAsync(request.A_USER_ID, request.StateCode);

            if (!result)
                return NotFound("User not found");

            return Ok(new
            {
                success = true,
                message = "User state updated"
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("close-users")]
        public async Task<IActionResult> CloseUsers(
            [FromBody] CloseUsersRequest request)
        {
            if (request?.A_USER_ID == null || !request.A_USER_ID.Any())
                return BadRequest("User ID list is required");

            var closedCount = await _authService
                .CloseUsersAsync(request.A_USER_ID);

            if (closedCount == 0)
                return NotFound("No users found");

            return Ok(new
            {
                success = true,
                closedCount
            });
        }

        [HttpPut("Update-Display-Name")]
        [Authorize]
        public async Task<IActionResult> UpdateDisplayName(UserUpdateDisplayNameModel model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _authService.UpdateDisplayNameAsync(userId, model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("Admin-Reset-Password")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetPassword(Guid userId)
        {
            try
            {
                var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _authService.ResetPasswordAsync(adminId, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex });
            }
        }

        [HttpPut("Change-Password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); // รับ userId จาก JWT token
                var result = await _authService.ChangePasswordAsync(userId, model); // เรียกฟังก์ชัน ChangePasswordAsync

                // ตรวจสอบข้อความผลลัพธ์จาก ChangePasswordAsync
                if (result.Contains("error") || result.Contains("not match") || result.Contains("incorrect") || result.Contains("must be"))
                {
                    return BadRequest(new { Message = result }); // ส่งข้อความผิดพลาดในกรณีที่เกิดข้อผิดพลาด
                }

                return Ok(new { Message = result }); // ส่งข้อความยืนยันการเปลี่ยนรหัสผ่านสำเร็จ
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message }); // ส่งข้อผิดพลาดจาก Exception
            }
        }

        [HttpPut("Update-Photo-Profile")]
        [Authorize]
        public async Task<IActionResult> UpdatePhotoProfile(IFormFile photo)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _authService.UpdatePhotoProfileAsync(userId, photo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex });
            }
        }

        [HttpPost("get-filter-user")]
        public async Task<IActionResult> GetFilteredUsers([FromBody] UserFilterModel filter)
        {
            if (filter.PageNumber <= 0)
                filter.PageNumber = 1;

            if (filter.PageSize <= 0)
                filter.PageSize = 10;

            var result = await _authService.GetFilteredUsersAsync(filter, filter.PageNumber, filter.PageSize);
            return Ok(result);
        }


        [HttpGet("FilterByDepartment")]
        public IActionResult GetUsersByDepartment(string department, string site, string? nameFilter, int page = 1, int pageSize = 10)
        {
            var (data, total, totalPage) = _authService.GetUsersByDepartment(department, site, nameFilter, page, pageSize);

            return Ok(new
            {
                data,
                total,
                totalPage,
                currentPage = page,
                pageSize
            });
        }


        [HttpGet("GetDepartmentsBySite")]
        public IActionResult GetDepartmentsBySite(string site)
        {
            if (string.IsNullOrEmpty(site))
            {
                return BadRequest(new { message = "Site is required" });
            }

            var departments = _authService.GetDepartmentsBySite(site);

            return Ok(departments);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadBanner(IFormFile photo)
        {
            var result = await _authService.UploadHomeBannerAsync(photo);
            return Ok(new { message = result });
        }



        [HttpGet("Banner")]
        public IActionResult GetAll()
        {
            var banners = _authService.GetAllBanners();
            return Ok(banners);
        }
        [HttpDelete("deleteBanners")]
        public async Task<IActionResult> DeleteBanner(int id)
        {
            var result = await _authService.DeleteHomeBannerAsync(id);

            if (result)
            {
                return Ok(new { message = "Banner deleted successfully." });
            }
            else
            {
                return NotFound(new { message = "Banner not found." });
            }
        }




    }
}
