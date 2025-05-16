using KAEAGoalWebAPI.Data;
using KAEAGoalWebAPI.Helpers;
using KAEAGoalWebAPI.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.IIS.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KAEAGoalWebAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        //private readonly SecondApplicationDbContext _scontext;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AuthService(ApplicationDbContext context, ILogger<MissionService> logger, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            //_scontext = scontext ?? throw new ArgumentNullException(nameof(scontext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

            _context = context;
            //_scontext = scontext;
            _logger = logger;
            _configuration = configuration;

            _logger.LogInformation("Configuration injected into MissionService.");
        }
        private DateTime GetBangkokTime()
        {
            var bangkokTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bangkokTimeZone);
        }

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<string> RegisterAsync(Guid userId, UserRegistrationModel model)
        {
            var existingUser = await _context.USERS.FirstOrDefaultAsync(u => u.LOGON_NAME == model.LOGON_NAME);
            if (existingUser != null) return null;

            var newUserID = Guid.NewGuid();
            var newUserPassword = _configuration["AppSettings:Defaultpassword"];
            var bangkokTime = GetBangkokTime();

            if (string.IsNullOrEmpty(newUserPassword))
            {
                throw new InvalidOperationException("Default password is not set in appsettings.json.");
            }

            var user = new User
            {
                A_USER_ID = newUserID,
                LOGON_NAME = model.LOGON_NAME,
                USER_PASSWORD = HashPassword(newUserPassword),
                FirstName = model.FirstName,
                LastName = model.LastName,
                DisplayName = $"{model.FirstName} {model.LastName}",
                BranchCode = model.BranchCode,
                Department = model.Department,
                DepartmentCode = model.DepartmentCode,
                Branch = model.Branch,
                CreatedBy = userId,
                CreatedOn = bangkokTime,
                UpdatedBy = userId,
                UpdatedOn = bangkokTime,
                StateCode = model.StateCode,
                DeletionStateCode = model.DeletionStateCode,
                VersionNumber = Guid.NewGuid(),
                IsBkk = model.IsBkk,
                IsAdmin = model.IsAdmin,
                User_Name = model.User_Name,
                Isshop = model.Isshop,
                Issup = model.Issup,
                ST_Dept_Id = null,
                IsQSC = model.IsQSC,
                USER_EMAIL = model.USER_EMAIL,
                User_Position = model.User_Position,
            };

            _context.USERS.Add(user);
            await _context.SaveChangesAsync();

            // แจก ThankCoin ตอนสมัคร
            var coinTransaction = new CoinTransaction
            {
                COIN_TRANSACTION_ID = Guid.NewGuid(),
                Amount = 50, // จำนวนเหรียญเริ่มต้น
                Coin_Type = CoinType.ThankCoin,
                Transaction_Date = bangkokTime,
                Transaction_Type = "Receive from Admin",
                Description = "Welcome",
                A_USER_ID = user.A_USER_ID,
            };

            _context.COIN_TRANSACTIONS.Add(coinTransaction);
            await _context.SaveChangesAsync();

            //return GenerateJwtToken(user);
            return "Registered";
        }

        public async Task<string> UpdateUserAsync(Guid userId, UserUpdateModel model)
        {
            var bangkokTime = GetBangkokTime();
            var IsAdmin = await _context.USERS
                .FirstOrDefaultAsync(u => u.A_USER_ID == userId);
            if (IsAdmin == null || IsAdmin.IsAdmin != 9)
                throw new Exception("UnAuthorized");

            var updateUser = await _context.USERS
                .FirstOrDefaultAsync(u => u.A_USER_ID == model.A_USER_ID);

            if (updateUser == null)
                throw new Exception("User not found.");

            updateUser.FirstName = model.FirstName;
            updateUser.LastName = model.LastName;
            updateUser.BranchCode = model.BranchCode;
            updateUser.Branch = model.Branch;
            updateUser.StateCode = model.StateCode;
            updateUser.DeletionStateCode = model.DeletionStateCode;
            updateUser.IsBkk = model.IsBkk;
            updateUser.IsAdmin = model.IsAdmin;
            updateUser.User_Name = model.User_Name;
            updateUser.Isshop = model.Isshop;
            updateUser.Issup = model.Issup;
            updateUser.IsQSC = model.IsQSC;
            updateUser.USER_EMAIL = model.USER_EMAIL;
            updateUser.User_Position = model.User_Position;
            updateUser.Site = model.Site;

            updateUser.UpdatedBy = userId;
            updateUser.UpdatedOn = bangkokTime;

            await _context.SaveChangesAsync();

            return "User updated successfully.";
        }

        public async Task<string> UpdateDisplayNameAsync(Guid userId, UserUpdateDisplayNameModel model)
        {
            var displayName = await _context.USERS
                .FirstOrDefaultAsync(u => u.A_USER_ID == userId);

            if (displayName == null)
                throw new Exception("User not found.");

            displayName.DisplayName = model.DisplayName;

            await _context.SaveChangesAsync();

            return "Display Name Updated.";
        }

        public async Task<LoginResponseModel> LoginAsync(UserLoginModel model)
        {
            var bangkokTime = GetBangkokTime();

            var user = await _context.USERS
                .Where(u => EF.Functions.Collate(u.LOGON_NAME, "SQL_Latin1_General_CP1_CS_AS") == model.LOGON_NAME)
                .FirstOrDefaultAsync();
            if (user == null || !VerifyPassword(model.USER_PASSWORD, user.USER_PASSWORD))
                return null;

            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            var existingRefreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.A_USER_ID == user.A_USER_ID);

            DateTime refreshTokenExpiry = bangkokTime.AddDays(7);

            if (existingRefreshToken != null)
            {
                if (existingRefreshToken.ExpiryDate < refreshTokenExpiry)
                {
                    _context.RefreshTokens.Remove(existingRefreshToken);
                    await _context.SaveChangesAsync();

                    existingRefreshToken = new RefreshToken
                    {
                        Id = Guid.NewGuid(),
                        Token = refreshToken,
                        A_USER_ID = user.A_USER_ID,
                        ExpiryDate = refreshTokenExpiry,
                    };
                    await _context.RefreshTokens.AddAsync(existingRefreshToken);
                }
                else
                {
                    existingRefreshToken.Token = refreshToken;
                    existingRefreshToken.ExpiryDate = refreshTokenExpiry;
                    _context.RefreshTokens.Update(existingRefreshToken);
                }
            }
            else
            {
                existingRefreshToken = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    Token = refreshToken,
                    A_USER_ID = user.A_USER_ID,
                    ExpiryDate = refreshTokenExpiry,
                };
                await _context.RefreshTokens.AddAsync(existingRefreshToken);
            }

            await _context.SaveChangesAsync();

            if (_httpContextAccessor?.HttpContext != null)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = refreshTokenExpiry,
                    IsEssential = true
                };

                _httpContextAccessor.HttpContext.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
            }

            return new LoginResponseModel
            {
                AccessToken = accessToken,
            };
        }

        public async Task<LoginResponseModel> RefreshTokenAsync()
        {
            var bangkokTime = GetBangkokTime();
            var refreshToken = _httpContextAccessor.HttpContext.Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
                throw new UnauthorizedAccessException("Refresh token is missing.");

            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null || storedToken.ExpiryDate < bangkokTime)
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");

            var user = await _context.USERS.FindAsync(storedToken.A_USER_ID);
            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            var newAccessToken = GenerateJwtToken(user);

            return new LoginResponseModel
            {
                AccessToken = newAccessToken,
            };
        }

        public async Task<string> LogoutAsync(Guid userId)
        {
            var bangkokTime = GetBangkokTime();

            var tokensToRemove = await _context.RefreshTokens
                .Where(rt => rt.A_USER_ID == userId)
                .ToListAsync();

            if (!tokensToRemove.Any())
            {
                return "User already logged out or no refresh token found.";
            }

            _context.RefreshTokens.RemoveRange(tokensToRemove);
            await _context.SaveChangesAsync();

            if (_httpContextAccessor?.HttpContext != null)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = bangkokTime.AddDays(-1)
                };

                _httpContextAccessor.HttpContext.Response.Cookies.Append("refreshToken", "", cookieOptions);
            }

            return "Logged out successfully";
        }

        private string GenerateJwtToken(User user)
        {
            var bangkokTime = GetBangkokTime();

            //Role value
            string role = user.IsAdmin switch
            {
                9 => "Admin",
                5 => "User",
                4 => "Missioner",
                1 => "SuperAdmin",
                _ => "Unknown"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.A_USER_ID.ToString()),
                new Claim(ClaimTypes.Name, user.LOGON_NAME ?? string.Empty),
                new Claim(ClaimTypes.Role, role),
                new Claim("MissionOwner",   user.IsBkk.ToString())
            };

            var privateKeyPath = _configuration["Jwt:PrivateKeyPath"];
            if (string.IsNullOrEmpty(privateKeyPath) || !File.Exists(privateKeyPath))
                throw new Exception("Private key file not found!");

            var privateKey = File.ReadAllText(privateKeyPath);
            var rsa = RSA.Create();
            rsa.ImportFromPem(privateKey.ToCharArray());

            var signingCredentials = new SigningCredentials(
                new RsaSecurityKey(rsa),
                SecurityAlgorithms.RsaSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: bangkokTime.AddHours(12),
                signingCredentials: signingCredentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randombytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randombytes);
            }
            return Convert.ToBase64String(randombytes);
        }

        private string HashPassword(string USER_PASSWORD)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(USER_PASSWORD)));
            }
        }

        private bool VerifyPassword(string USER_PASSWORD, string storedPasswordHash)
        {
            return storedPasswordHash == HashPassword(USER_PASSWORD);
        }

        public async Task<UserDetailsModel?> GetCurrentUserDetailsAsync(Guid userId)
        {
            var user = await _context.USERS.FirstOrDefaultAsync(u => u.A_USER_ID == userId);

            if (user == null)
                return null;

            if (string.IsNullOrEmpty(user.DisplayName))
            {
                user.DisplayName = user.LOGON_NAME;
            }

            return new UserDetailsModel
            {
                A_USER_ID = user.A_USER_ID,
                LOGON_NAME = user.LOGON_NAME,
                BranchCode = user.BranchCode,
                DisplayName = user.DisplayName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ImageUrls = user.ImageUrls,
                Branch = user.Branch,
                Department = user.Department,
                CreatedOn = user.CreatedOn,
                UpdatedOn = user.UpdatedOn,
                StateCode = user.StateCode,
                DeletionStateCode = user.DeletionStateCode,
                IsBkk = user.IsBkk,
                IsAdmin = user.IsAdmin,
                User_Name = user.User_Name,
                Isshop = user.Isshop,
                Issup = user.Issup,
                USER_EMAIL = user.USER_EMAIL,
                User_Position = user.User_Position,
                isForcePassChange = user.isForcePassChange,
                isRegister = user.isRegister,
                AU_Employee_ID = user.AU_Employee_ID,
                DepartmentCode = user.DepartmentCode
            };
        }

        public async Task<bool> IsUserAdminAsync(Guid userId)
        {
            var user = await _context.USERS.FirstOrDefaultAsync(u => u.A_USER_ID == userId);
            return user != null && user.IsAdmin == 9; // Assuming 9 means Admin
        }

        public async Task<UserDetailsModel> GetUserDetailsAsync(Guid userId)
        {
            var user = await _context.USERS.FirstOrDefaultAsync(u => u.A_USER_ID == userId);
            if (user == null)
            {
                return null;
            }

            var userModel = new UserDetailsModel
            {
                A_USER_ID = user.A_USER_ID,
                LOGON_NAME = user.LOGON_NAME,
                BranchCode = user.BranchCode,
                DisplayName = user.DisplayName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ImageUrls = user.ImageUrls,
                Branch = user.Branch,
                CreatedOn = user.CreatedOn,
                UpdatedOn = user.UpdatedOn,
                StateCode = user.StateCode,
                DeletionStateCode = user.DeletionStateCode,
                IsBkk = user.IsBkk,
                IsAdmin = user.IsAdmin,
                User_Name = user.User_Name,
                Isshop = user.Isshop,
                Issup = user.Issup,
                USER_EMAIL = user.USER_EMAIL,
                User_Position = user.User_Position,
                isForcePassChange = user.isForcePassChange,
                isRegister = user.isRegister,
                AU_Employee_ID = user.AU_Employee_ID,
            };

            return userModel;
        }

        public async Task<List<UserDetailsModel>> GetAllUserAsync(Guid userId)
        {
            var user = await _context.USERS
                .ToListAsync();

            var model = user.Select(user => new UserDetailsModel
            {
                A_USER_ID = user.A_USER_ID,
                LOGON_NAME = user.LOGON_NAME,
                BranchCode = user.BranchCode,
                DisplayName = user.DisplayName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ImageUrls = user.ImageUrls,
                Branch = user.Branch,
                CreatedOn = user.CreatedOn,
                UpdatedOn = user.UpdatedOn,
                StateCode = user.StateCode,
                DeletionStateCode = user.DeletionStateCode,
                IsBkk = user.IsBkk,
                IsAdmin = user.IsAdmin,
                User_Name = user.User_Name,
                Isshop = user.Isshop,
                Issup = user.Issup,
                USER_EMAIL = user.USER_EMAIL,
                User_Position = user.User_Position,
                isForcePassChange = user.isForcePassChange,
                isRegister = user.isRegister,
                AU_Employee_ID = user.AU_Employee_ID,
                Department = user.Department
            }).ToList();

            return model;
        }

        public async Task<string> ResetPasswordAsync(Guid adminId, Guid userId)
        {
            var adminUser = await _context.USERS
                .FirstOrDefaultAsync(u => u.A_USER_ID == adminId);
            if (adminUser == null || adminUser.IsAdmin != 9)
                return "Unauthorized";

            var user = await _context.USERS
                .FirstOrDefaultAsync(u => u.A_USER_ID == userId);
            if (user == null)
                return "User not found.";

            var defaultpassword = _configuration["AppSettings:Defaultpassword"];
            if (string.IsNullOrEmpty(defaultpassword))
                return "Default password not set in configuration";

            user.USER_PASSWORD = HashPassword(defaultpassword);
            user.isForcePassChange = true;

            await _context.SaveChangesAsync();

            return "User password has been reset successfully.";
        }

        public async Task<string> ChangePasswordAsync(Guid userId, ChangePasswordModel model)
        {
            try
            {
                var bangkokTime = GetBangkokTime();

                if (model.PASSWORD != model.CONFIRM_PASSWORD)
                    return "New password and confirm password do not match.";

                if (!PasswordValidator.IsValidPassword(model.PASSWORD))
                    return "New password must be at least 8 characters long, include an uppercase letter, a lowercase letter, a number, and a special character.";

                var user = await _context.USERS.FirstOrDefaultAsync(u => u.A_USER_ID == userId);
                if (user == null)
                    return "User not found.";

                if (user.USER_PASSWORD != HashPassword(model.CURRENT_PASSWORD))
                    return "Current password is incorrect.";

                user.USER_PASSWORD = HashPassword(model.PASSWORD);
                user.UpdatedOn = bangkokTime;

                await _context.SaveChangesAsync();
                return "Password has been changed successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while changing password for User");
                return "An error occurred while changing the password.";
            }
        }


        public async Task<string> UpdatePhotoProfileAsync(Guid userId, IFormFile photo)
        {
            var bangkokTime = GetBangkokTime();

            var user = await _context.USERS
                .FirstOrDefaultAsync(u => u.A_USER_ID == userId);

            if (user == null)
                throw new Exception("User not found.");

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
            var uploadDirectory = _configuration["AppSettings:ImageProfileUploadPath"];

            if (photo.Length > maxFileSize)
            {
                throw new Exception($"File {photo.FileName} is too large. Maximum size allowed is 50MB.");
            }

            if (!allowedFileTypes.Contains(photo.ContentType))
            {
                throw new Exception($"Invalid file type for {photo.FileName}. Only JPEG, PNG GIF images are allowed.");
            }

            if (photo.Length > 0)
            {
                var uniqueFileName = $"{user.A_USER_ID}_{photo.FileName}";
                var uploadPath = Path.Combine(uploadDirectory, uniqueFileName);

                Directory.CreateDirectory(Path.GetDirectoryName(uploadPath) ?? string.Empty);

                await using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                var imagePath = _configuration["AppSettings:ImageProfilePath"];
                var fileUrl = Path.Combine(imagePath, uniqueFileName).Replace("\\", "/");

                user.ImageUrls = fileUrl;

                await _context.SaveChangesAsync();
            }

            return "Profile has been updated.";
        }
        //public async Task<List<UserDetailsModel>> GetFilteredUsersAsync(UserFilterModel filter)
        //{
        //    var query = _context.USERS.AsQueryable();

        //    if (!string.IsNullOrEmpty(filter.LOGON_NAME))
        //        query = query.Where(u => u.LOGON_NAME.Contains(filter.LOGON_NAME));

        //    if (!string.IsNullOrEmpty(filter.USER_NAME))
        //        query = query.Where(u => u.User_Name.Contains(filter.USER_NAME));

        //    if (!string.IsNullOrEmpty(filter.USER_POSITION))
        //        query = query.Where(u => u.User_Position.Contains(filter.USER_POSITION));

        //    if (!string.IsNullOrEmpty(filter.BranchCode))
        //        query = query.Where(u => u.BranchCode.Contains(filter.BranchCode));

        //    if (!string.IsNullOrEmpty(filter.Branch))
        //        query = query.Where(u => u.Branch.Contains(filter.Branch));

        //    if (!string.IsNullOrEmpty(filter.Position))
        //        query = query.Where(u => u.User_Position.Contains(filter.Position));

        //    var users = await query.ToListAsync();

        //    var model = users.Select(user => new UserDetailsModel
        //    {
        //        A_USER_ID = user.A_USER_ID,
        //        LOGON_NAME = user.LOGON_NAME,
        //        BranchCode = user.BranchCode,
        //        DisplayName = user.DisplayName,
        //        FirstName = user.FirstName,
        //        LastName = user.LastName,
        //        ImageUrls = user.ImageUrls,
        //        Branch = user.Branch,
        //        CreatedOn = user.CreatedOn,
        //        UpdatedOn = user.UpdatedOn,
        //        StateCode = user.StateCode,
        //        DeletionStateCode = user.DeletionStateCode,
        //        IsBkk = user.IsBkk,
        //        IsAdmin = user.IsAdmin,
        //        User_Name = user.User_Name,
        //        Isshop = user.Isshop,
        //        Issup = user.Issup,
        //        USER_EMAIL = user.USER_EMAIL,
        //        User_Position = user.User_Position,
        //        isForcePassChange = user.isForcePassChange,
        //        isRegister = user.isRegister,
        //        AU_Employee_ID = user.AU_Employee_ID,
        //    }).ToList();

        //    return model;
        //}
        public async Task<PaginatedList<UserDetailsModel>> GetFilteredUsersAsync(UserFilterModel filter, int pageNumber, int pageSize)
        {
            var query = _context.USERS.AsQueryable();

            //if (!string.IsNullOrEmpty(filter.LOGON_NAME))
            //    query = query.Where(u => u.LOGON_NAME.Contains(filter.LOGON_NAME));

            if (!string.IsNullOrEmpty(filter.USER_NAME))
                query = query.Where(u => u.User_Name.Contains(filter.USER_NAME));

            if (!string.IsNullOrEmpty(filter.displayName))
                query = query.Where(u => u.DisplayName.Contains(filter.displayName));

            if (!string.IsNullOrEmpty(filter.Department))
                query = query.Where(u => u.DepartmentCode.Contains(filter.Department));

            if (!string.IsNullOrEmpty(filter.BranchCode))
                query = query.Where(u => u.BranchCode.Contains(filter.BranchCode));

            //if (!string.IsNullOrEmpty(filter.Branch))
            //    query = query.Where(u => u.Branch.Contains(filter.Branch));

            //if (!string.IsNullOrEmpty(filter.Position))
            //    query = query.Where(u => u.User_Position.Contains(filter.Position));

            // คำนวณจำนวนข้อมูลทั้งหมด
            var totalRecords = await query.CountAsync();

            // ดึงข้อมูลที่ผ่านการ filter โดยใช้ Skip และ Take
            var users = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // แปลงข้อมูลที่ได้มาเป็น UserDetailsModel
            var model = users.Select(user => new UserDetailsModel
            {
                A_USER_ID = user.A_USER_ID,
                LOGON_NAME = user.LOGON_NAME,
                BranchCode = user.BranchCode,
                DisplayName = user.DisplayName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ImageUrls = user.ImageUrls,
                //Branch = user.Branch,
                DepartmentCode = user.DepartmentCode,
                Department = user.Department,
                //CreatedOn = user.CreatedOn,
                //UpdatedOn = user.UpdatedOn,
                //StateCode = user.StateCode,
                //DeletionStateCode = user.DeletionStateCode,
                //IsBkk = user.IsBkk,
                //IsAdmin = user.IsAdmin,
                User_Name = user.User_Name,
                //Isshop = user.Isshop,
                //Issup = user.Issup,
                //USER_EMAIL = user.USER_EMAIL,
                User_Position = user.User_Position,
                //isForcePassChange = user.isForcePassChange,
                //isRegister = user.isRegister,
                //AU_Employee_ID = user.AU_Employee_ID,
            }).ToList();

            return PaginatedList<UserDetailsModel>.Create(model, totalRecords, pageNumber, pageSize);
        }




        public (List<User> data, int total, int totalPage) GetUsersByDepartment(string department, string site, string nameFilter, int page, int pageSize)
        {
            var query = _context.USERS
                .Where(u => u.DepartmentCode == department && u.BranchCode == site);

            // ✅ กรองจากชื่อถ้ามีการกรอกเข้ามา (จาก User_Name หรือ DisplayName)
            if (!string.IsNullOrEmpty(nameFilter))
            {
                query = query.Where(u =>
                    u.User_Name.Contains(nameFilter) ||
                    u.DisplayName.Contains(nameFilter));
            }

            var projectedQuery = query.Select(u => new User
            {
                A_USER_ID = u.A_USER_ID,
                DisplayName = u.DisplayName,
                User_Name = u.User_Name,
                Department = u.Department,
                User_Position = u.User_Position,
                ImageUrls = u.ImageUrls
            });

            int total = projectedQuery.Count();
            int totalPage = (int)Math.Ceiling((double)total / pageSize);

            var data = projectedQuery
                .OrderBy(u => u.DisplayName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (data, total, totalPage);
        }



        public List<Department> GetDepartmentsBySite(string site)
        {
            return _context.DEPARTMENT
                .Where(d => d.Site == site)
                .OrderBy(d => d.DepartmentName)
                .ToList();
        }
        public async Task<string> UploadHomeBannerAsync(IFormFile photo)
        {
            var bangkokTime = GetBangkokTime();

            // ตรวจสอบประเภทไฟล์ที่อนุญาต
            var allowedFileTypes = new[] {
        "image/jpeg", "image/png", "image/gif",
        "image/webp", "image/bmp", "image/tiff",
        "image/svg+xml", "image/heif", "image/heic"
    };
            long maxFileSize = 10 * 1024 * 1024; // 10MB
            var uploadDirectory = _configuration["AppSettings:ImageProfileUploadPath"];

            if (photo.Length > maxFileSize)
            {
                throw new Exception($"File {photo.FileName} is too large. Maximum size allowed is 10MB.");
            }

            if (!allowedFileTypes.Contains(photo.ContentType))
            {
                throw new Exception($"Invalid file type for {photo.FileName}. Only image files are allowed.");
            }

            if (photo.Length > 0)
            {
                // ตั้งชื่อไฟล์ไม่ให้ซ้ำกัน
                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(photo.FileName)}";
                var uploadPath = Path.Combine(uploadDirectory, uniqueFileName);

                Directory.CreateDirectory(Path.GetDirectoryName(uploadPath) ?? string.Empty);

                await using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }

                var imagePath = _configuration["AppSettings:ImageProfilePath"] ; // สำหรับ public access เช่น /uploads/homebanners/
                var fileUrl = Path.Combine(imagePath, uniqueFileName).Replace("\\", "/");

                var banner = new HomeBanner
                {
                    FileName = uniqueFileName,
                    FilePath = fileUrl,
                    UploadDate = bangkokTime
                };

                _context.HomeBanners.Add(banner);
                await _context.SaveChangesAsync();
            }

            return "Home banner uploaded successfully.";
        }


        public IEnumerable<HomeBanner> GetAllBanners()
        {
            return _context.HomeBanners.OrderByDescending(b => b.UploadDate).ToList();
        }

        public async Task<bool> DeleteHomeBannerAsync(int bannerId)
        {
            var banner = await _context.HomeBanners.FindAsync(bannerId);

            if (banner == null)
            {
                return false; // ไม่พบ banner
            }

            // ลบไฟล์จากระบบ
            var filePath = banner.FilePath; // ใช้ FilePath จากฐานข้อมูล
            if (File.Exists(filePath))
            {
                File.Delete(filePath); // ลบไฟล์จริงจาก disk
            }

            // ลบข้อมูลจากฐานข้อมูล
            _context.HomeBanners.Remove(banner);
            await _context.SaveChangesAsync();

            return true;
        }

    }




    //public async Task<string> NewRegisterAsync(int employeeId)
    //{
    //    var existingEmployee = await _scontext.AuUsers
    //        .FirstOrDefaultAsync(u => u.AU_Employee_ID == employeeId);

    //    if (existingEmployee != null)
    //        return "Registration failed: Employee does not exist.";


    //    return "Registered successful.";


    //}
}

