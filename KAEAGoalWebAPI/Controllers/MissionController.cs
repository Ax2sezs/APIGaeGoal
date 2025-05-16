using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using KAEAGoalWebAPI.Models;
using KAEAGoalWebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace KAEAGoalWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]

    public class MissionController : ControllerBase
    {
        private readonly IMissionService _missionService;
        private readonly IConfiguration _configuration;

        public MissionController(IMissionService missionService, IConfiguration configuration)
        {
            _missionService = missionService;
            _configuration = configuration;
        }

        [HttpPost("Admin-Create-Mission")]
        [Authorize(Roles = "Admin, Missioner")]
        public async Task<IActionResult> CreateMission([FromForm] CreateMissionModel model)
        {
            try
            {
                if (model.Images == null || !model.Images.Any())
                {
                    model.ImageUrls = new List<string> { _configuration["AppSettings:DefaultMissionImage"] };
                }
                else
                {
                    var imageUrls = new List<string>();
                    var allowedFileTypes = new[] {
                        "image/jpeg",
                        "image/png",
                        "image/gif",
                        "image/webp",
                        "image/bmp",
                        "image/tiff",
                        "image/svg+xml",
                        "image/heif",  // .heif (iPhone, Android)
                        "image/heic"   // .heic (iPhone)
                    };
                    long maxFileSize = 50 * 1024 * 1024;
                    var uploadDirectory = _configuration["AppSettings:ImageUploadPath"];

                    foreach (var file in model.Images)
                    {
                        if (file.Length > maxFileSize)
                        {
                            return BadRequest(new { Message = $"File {file.FileName} is too large. Maximum size allowed is 50MB." });
                        }

                        if (!allowedFileTypes.Contains(file.ContentType))
                        {
                            return BadRequest(new { Message = $"Invalid file type for {file.FileName}. Only JPEG, PNG GIF images are allowed." });
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

                            var imagePath = _configuration["AppSettings:ImagePath"];
                            var fileUrl = Path.Combine(imagePath, uniqueFileName);
                            imageUrls.Add(fileUrl);
                        }
                    }

                    model.Images = null;
                    model.ImageUrls = imageUrls;
                }
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var missionId = await _missionService.CreateMissionAsync(userId, model);

                return Ok(new { MissionId = missionId });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = ex.Message });
            }

            



            //    var imageUrls = new List<string>();

            //    var allowedFileTypes = new[] { "image/jpeg", "image/png", "image/gif" };

            //    long maxFileSize = 50 * 1024 * 1024;

            //    var uploadDirectory = _configuration["AppSettings:ImageUploadPath"];
            //    //if (string.IsNullOrWhiteSpace(uploadDirectory))
            //    //{
            //    //    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Image upload path is not configured." });
            //    //}

            //    foreach (var file in model.Images)
            //    {
            //        if (file.Length > maxFileSize)
            //        {
            //            return BadRequest(new { Message = $"File {file.FileName} is too large. Maximum size allowed is 5MB." });
            //        }

            //        if (!allowedFileTypes.Contains(file.ContentType))
            //        {
            //            return BadRequest(new { Message = $"Invalid file type for {file.FileName}. Only JPEG, PNG, GIF images are allowed." });
            //        }

            //        if (file.Length > 0)
            //        {
            //            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            //            var uploadPath = Path.Combine(uploadDirectory, uniqueFileName);

            //            Directory.CreateDirectory(Path.GetDirectoryName(uploadPath) ?? string.Empty);

            //            await using (var stream = new FileStream(uploadPath, FileMode.Create))
            //            {
            //                await file.CopyToAsync(stream);
            //            }
            //            var imagepath = _configuration["AppSettings:ImagePath"];
            //            //var fileUrl = $"{Request.Scheme}://{Request.Host}/mission-images/{uniqueFileName}";
            //            var fileUrl = Path.Combine(imagepath, uniqueFileName);
            //            imageUrls.Add(fileUrl);
            //        }
            //    }

            //    model.Images = null;
            //    model.ImageUrls = imageUrls;

            //    var missionId = await _missionService.CreateMissionAsync(model);

            //    return Ok(new { MissionId = missionId });
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = ex.Message });
            //}
        }

        [HttpPut("Admin-Update-Mission/{missionId}")]
        [Authorize(Roles = "Admin, Missioner")]
        public async Task<IActionResult> UpdateMission(Guid missionId, [FromForm] CreateMissionModel model)
        {
            try
            {
                await _missionService.UpdateMissionAsync(missionId, model);

                return Ok(new { Message = "Mission updated successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = ex.Message });
            }
        }


        [HttpGet("Get-All-Mission")]
        public async Task<ActionResult<IEnumerable<MissionViewModel>>> GetMissions()
        {
            var missions = await _missionService.GetAllMissionsAsync();
            return Ok(missions);
        }

        [HttpGet("Get-Approve-QRCode-Mission")]
        [Authorize(Roles = "Admin, Missioner")]
        public async Task<ActionResult<IEnumerable<ApproveViewModel>>> GetApproveQRCode()
        {
            var approves = await _missionService.GetAllQRCodeApproveAsync("");
            return Ok(approves);
        }

        [HttpGet("Admin-Get-User-Mission")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUserMissions()
        {
            var usermissions = await _missionService.GetAllUserMissionsAsync();
            return Ok(usermissions);
        }

        [HttpGet("Get-Unaccepted-Mission")]
        public async Task<IActionResult> GetUnacceptedMissions()
        {
            try
            {
                // Extract userId from JWT token
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                //var userId = Guid.Parse("A542677B-B038-4407-80AE-A60F1EC5CD33");

                // Get unaccepted missions
                var missions = await _missionService.GetUnacceptedMissionsAsync(userId);

                if (!missions.Any())
                {
                    return NotFound(new { Message = $"No unaccepted missions available." });
                }

                return Ok(missions);
            }
            catch (Exception ex)
            {
                // Log exception
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("Get-Completed-Mission")]
        public async Task<IActionResult> GetCompletedMisions()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var missions = await _missionService.GetCompletedMissionsAsync(userId);

                if (!missions.Any())
                {
                    return NotFound(new { Message = $"No Completed missions available." });
                }

                return Ok(missions);
            }
            catch(Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("Accept-Mission")]
        public async Task<IActionResult> AcceptMission([FromBody] AcceptMissionModel model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                await _missionService.AcceptMissionAsync(userId, model);
                return Ok(new { message = "Mission accepted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("User-Mission")]
        public async Task<IActionResult> GetUserMissions()
        {
            try
            {
                // Extract the user ID from the JWT token (using NameIdentifier)
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new { Message = "User is not authenticated." });
                }

                var userId = Guid.Parse(userIdClaim); // Extracted userId from JWT token

                // Call the service method with the extracted userId
                var userMissions = await _missionService.GetUserMissionsAsync(userId);

                if (userMissions == null || !userMissions.Any())
                {
                    return NotFound(new { Message = $"No missions found for this user." });
                }

                return Ok(userMissions);
            }
            catch (Exception ex)
            {
                // Log exception details
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = ex.Message });
            }
        }

        [HttpGet("feed")]
        public async Task<IActionResult> GetMissionFeed(
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 10,
     [FromQuery] string? type = null,
     [FromQuery] string? missionName = null,
     [FromQuery] string? displayName = null)
        {
            try
            {
                // ✅ ดึง userId แบบเดียวกับ Get-Completed-Mission
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var (data, total, totalPage) = await _missionService.GetMissionFeedAsync(
                    userId,
                    page,
                    pageSize,
                    type,
                    missionName,
                    displayName
                );

                return Ok(new
                {
                    data,
                    total,
                    totalPage,
                    currentPage = page
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPost("like")]
        public async Task<IActionResult> LikeMission([FromBody] FeedLikeReq request)
        {
            var result = await _missionService.LikeMissionAsync(request);

            if (result.Contains("already"))
                return BadRequest(result);

            return Ok(new { message = result });
        }
        [HttpGet("getLikesForMission")]
        public async Task<IActionResult> GetLikesForMissionAsync(Guid userMissionId)
        {
            var result = await _missionService.GetLikesForMissionAsync(userMissionId);
            return Ok(result);
        }



        [HttpPost("Execute-Code-Mission")]
        public async Task<IActionResult> ExecuteMission([FromBody] ExecuteCodeMissionModel model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _missionService.ExecuteCodeMissionAsync(userId, model);
                return Ok(new { Message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpPost("Execute-Text-Mission")]
        public async Task<IActionResult> ExecuteMission([FromBody] ExecuteTextMissionModel model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _missionService.ExecuteTextMissionAsync(userId, model);
                return Ok(new { Message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpPost("Execute-Photo-Mission")]
        public async Task<IActionResult> ExecuteMission([FromForm] ExecutePhotoModel model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                //var userId = Guid.Parse("A542677B-B038-4407-80AE-A60F1EC5CD33");
                var result = await _missionService.ExecutePhotoMissionAsync(userId, model);
                return Ok(new { Message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpPost("Execute-Video-Mission")]
        [RequestSizeLimit(104857600)] // 200MB
        //[DisableRequestSizeLimit] 
        public async Task<IActionResult> ExecuteVideoMission([FromForm] ExecuteVideoModel model)
        {

            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                //var userId = Guid.Parse("A542677B-B038-4407-80AE-A60F1EC5CD33");
                var result = await _missionService.ExecuteVideoMissionAsync(userId, model);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost("Execute-QR-Mission")]
        public async Task<IActionResult> ExecuteMission([FromBody] ExecuteQRCodeModel model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _missionService.ExecuteQRCodeMissionASync(userId, model);
                return Ok(new { Message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message  = ex.Message});
            }
        }

        

        [HttpGet("Get-All-Approve-Photo-Mission")]
        [Authorize(Roles = "Admin, Missioner")]
        public async Task<ActionResult<IEnumerable<ApprovePhotoViewModel>>> GetAllApprovePhotoMission()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var missionowner = User.FindFirstValue("MissionOwner");

            //var userId = Guid.Parse("A542677B-B038-4407-80AE-A60F1EC5CD33");
            //var missionowner = "2";

            var approve = await _missionService.GetAllPhotoApproveAsync(userId, missionowner);
            return Ok(approve);
        }

        [HttpGet("GetPublicMissionName")]
        public async Task<IActionResult> GetAllPublicMissions()
        {
            var missions = await _missionService.GetAllPublicMissionsAsync();
            return Ok(missions);
        }


        [HttpGet("missions-by-type")]
        [Authorize(Roles = "Admin, Missioner")]
        public async Task<ActionResult<List<MissionSelectViewModel>>> GetMissionsByType([FromQuery] string missionType)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var missionowner = User.FindFirstValue("MissionOwner");
            var missions = await _missionService.GetMissionNamesByTypeAsync(missionType, missionowner);
            return Ok(missions);
        }
        [HttpGet("photo-approves")]
        [Authorize(Roles = "Admin, Missioner")]
        public async Task<IActionResult> GetPhotoApproves(Guid missionId, int page = 1, int pageSize = 20, [FromQuery] string? searchName = null)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var missionowner = User.FindFirstValue("MissionOwner");

            var (data, total, totalPage) = await _missionService.GetPhotoApproveByMissionAsync(missionId, page, pageSize,missionowner,searchName);

            return Ok(new
            {
                total,
                page,
                totalPage,
                pageSize,
                data
            });
        }
        [HttpGet("video-approves")]
        public async Task<IActionResult> GetVideoApproves(Guid missionId, int page = 1, int pageSize = 20, [FromQuery] string? searchName = null)

        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var missionowner = User.FindFirstValue("MissionOwner");
            var (data, total, totalPage) = await _missionService.GetVideoApproveByMissionAsync(missionId, page, pageSize, missionowner,searchName);
            return Ok(new
            {
                total,
                page,
                totalPage,
                pageSize,
                data,
            });
        }
        [HttpGet("text-approves")]
        public async Task<IActionResult> GetTextApproves(
     Guid missionId,
     int page = 1,
     int pageSize = 20,
     [FromQuery] string? searchName = null)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var missionowner = User.FindFirstValue("MissionOwner");

            var (data, total, totalPage) = await _missionService
                .GetTextApproveByMissionAsync(missionId, page, pageSize, missionowner, searchName);

            return Ok(new
            {
                total,
                page,
                totalPage,
                pageSize,
                data
            });
        }

        [HttpPost("Admin-Set-IsView")]
        public async Task<IActionResult> SetIsView([FromBody] IsViewModel model)
        {
            try
            {
                var result = await _missionService.SetIsViewAsync(model);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        [HttpPost("Admin-Approve-Photo-Mission")]
        [Authorize(Roles = "Admin, Missioner")]
        public async Task<IActionResult> ApprovePhotoMission([FromBody] ApprovePhotoMissionModel model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                await _missionService.ApprovePhotoMissionAsync(userId, model);
                return Ok(new { Message = "Photo Mission approval processed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }



        [HttpGet("Get-All-Approve-Video-Mission")]
        [Authorize(Roles = "Admin, Missioner")]
        public async Task<ActionResult<IEnumerable<ApprovePhotoViewModel>>> GetAllApproveVideoMission()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var missionowner = User.FindFirstValue("MissionOwner");

            //var userId = Guid.Parse("A542677B-B038-4407-80AE-A60F1EC5CD33");
            //var missionowner = "2";

            var approve = await _missionService.GetAllVideoApproveAsync(userId, missionowner);
            return Ok(approve);
        }

        [HttpPost("Admin-Approve-Video-Mission")]
        [Authorize(Roles = "Admin, Missioner")]
        public async Task<IActionResult> ApproveVideoMission([FromBody] ApproveVideoMissionModel model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                await _missionService.ApproveVideoMissionAsync(userId, model);
                return Ok(new { Message = "Video Mission approval processed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }


        [HttpGet("Get-All-Approve-Text-Mission")]
        [Authorize(Roles = "Admin, Missioner")]
        public async Task<ActionResult<IEnumerable<ApproveTextViewModel>>> GetAllApproveTextMission()
        {
            //var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var missionowner = User.FindFirstValue("MissionOwner");

            var approve = await _missionService.GetAllTextApproveAsync(missionowner);
            return Ok(approve);
        }


        [HttpPost("Admin-Approve-Text-Mission")]
        [Authorize(Roles = "Admin, Missioner")]
        public async Task<IActionResult> MissionerApproveTextMission([FromBody] ApproveTextMissionModel model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                await _missionService.MissionerApproveTextMissionAsync(userId, model);
                return Ok(new { Message = "Text Mission approval processed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpPost("Admin-Approve-QRCode-Mission")]
        [Authorize(Roles = "Admin, Missioner")]
        public async Task<IActionResult> ApproveQRCodeMission([FromBody] ApproveQRCodeMissionModel model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                await _missionService.ApproveQRCodeMissionsAsync(userId, model);
                return Ok(new { Message = "QR Code Mission approval processed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpGet("select-user-in-mission")]
        public async Task<IActionResult> GetUsersInMission([FromQuery] Guid missionId, [FromQuery] string type)
        {
            if (missionId == Guid.Empty || string.IsNullOrWhiteSpace(type))
                return BadRequest("Invalid missionId or type");

            var users = await _missionService.GetUsersInMissionAsync(missionId, type);

            return Ok(users);
        }

        [HttpPost("Missioner-Add-Winners-Coin-Reward-All")]
        [Authorize(Roles = "Admin, Missioner")]
        public async Task<IActionResult> AddWinnerCoinAllMission([FromBody] AddCoinWinnerMission model)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var missionowner = User.FindFirstValue("MissionOwner");


                //var userId = Guid.Parse("A542677B-B038-4407-80AE-A60F1EC5CD33");
                //var missionowner = "1";

                var result = await _missionService.MissionerAddWinnerCoinAllMissionAsync(userId, model, missionowner);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }



        [HttpGet("Get-Missioner-Mission")]
        [Authorize(Roles = "Admin, Missioner")]
        public async Task<ActionResult<IEnumerable<MissionViewModel>>> GetAllMissionerMission()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _missionService.GetMissionerAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(ex);
            }
        }
       

        //[HttpPost("Collect-Mission-reward")]
        //public async Task<IActionResult> CollectReward([FromBody] CollectCoinRewardRequest model)
        //{
        //    try
        //    {
        //        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        //        var response = await _missionService.CollectCoinRewardAsync(model, userId);

        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new CollectCoinRewardResponse
        //        {
        //            IsSuccess = false,
        //            Message = ex.Message
        //        });
        //    }
        //}



        //[HttpGet("test-config")]
        //public IActionResult TestConfig([FromServices] IConfiguration configuration)
        //{
        //    var value = configuration["AppSettings:ImagePhotoMissionUploadPath"];
        //    return Ok(value);
        //}

        //[HttpPost("Execute-Video-Mission")]
        ////[Authorize]
        //public async Task<IActionResult> ExecuteVideoMission([FromForm]ExecuteVideoMissionModel model)
        //{
        //    try
        //    {
        //        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        //        var result = await _missionService.ExecuteVideoMissionAsync(userId, model);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return NotFound(ex);
        //    }
        //}

        //[HttpPost("Missioner-Add-Winners-Coin-Reward-QRCode")]
        //[Authorize (Roles = "Admin, Missioner")]
        //public async Task<IActionResult> AddWinnerCoinQRCode([FromBody] AddCoinWinnerMission model)
        //{
        //    try
        //    {
        //        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        //        var result = await _missionService.MissionerAddWinnerCoinRewardQRMissionAsync(userId, model);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex);
        //    }
        //}

        //[HttpPost("Missioner-Add-Winners-Coin-Reward-Photo")]
        //[Authorize(Roles = "Admin, Missioner")]
        //public async Task<IActionResult> AddWinnerCoinPhoto([FromBody] AddCoinWinnerMission model)
        //{
        //    try
        //    {
        //        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        //        var missionowner = User.FindFirstValue("MissionOwner");
                 
        //       var result = await _missionService.MissionerAddWinnerCoinRewardPhotoMissionAsync(userId, model);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex);
        //    }
        //}
        
        

        //[HttpPost("Missioner-Add-Batch-Coin-Reward-QRCode")]
        //[Authorize(Roles = "Admin, Missioner")]
        //public async Task<IActionResult> AddBatchCoinRewardQRCode([FromBody] Guid missionId, int Amount)
        //{
        //    try
        //    {
        //        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        //        var result = await _missionService.MissionerAddBatchQRCodeMissionRewardAsync(userId, missionId, Amount);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex);
        //    }
        //}

        //[HttpPost("Missioner-Add-Batch-Coin-Reward-Photo")]
        //[Authorize(Roles = "Admin, Missioner")]
        //public async Task<IActionResult> AddBatchCoinRewardPhoto([FromBody] Guid missionId, int Amount)
        //{
        //    try
        //    {
        //        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        //        var result = await _missionService.MissionerAddBatchPhotoMissionRewardAsync(userId, missionId, Amount);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex);
        //    }
        //}
        //[HttpPost("Missioner-Add-Winners-Coin-Reward-Text")]
        //[Authorize(Roles = "Admin, Missioner")]
        //public async Task<IActionResult> AddWinnerCoinText([FromBody] AddCoinWinnerMission model)
        //{
        //    try
        //    {
        //        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        //        var result = await _missionService.MissionerAddWinnerCoinRewardTextMissionAsync(userId, model);
        //        return Ok(result);
        //    }
        //    catch(Exception ex)
        //    {
        //        return BadRequest(ex);
        //    }
        //}

        //[HttpPost("Missioner-Add-Batch-Coin-Reward-Text")]
        //[Authorize(Roles = "Admin, Missioner")]
        //public async Task<IActionResult> AddBatchCoinRewardText([FromBody] Guid missionId, int Amount)
        //{
        //    try
        //    {
        //        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        //        var result = await _missionService.MissionerAddBatchTexTMissionRewardAsync(userId, missionId, Amount);
        //        return Ok(result);
        //    }
        //    catch(Exception ex)
        //    {
        //        return BadRequest(ex); 
        //    }
        //}



        

         

    }
}
