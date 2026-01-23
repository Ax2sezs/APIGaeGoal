using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KAEAGoalWebAPI.Models;
using Microsoft.AspNetCore.Http;

namespace KAEAGoalWebAPI.Services
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(Guid userId, UserRegistrationModel model);
        Task<LoginResponseModel> LoginAsync(UserLoginModel model);
        Task<string> LogoutAsync(Guid userId);
        Task<LoginResponseModel> RefreshTokenAsync();
        Task<UserDetailsModel?> GetCurrentUserDetailsAsync(Guid userId);
        Task<bool> IsUserAdminAsync(Guid userId);
        Task<UserDetailsModel> GetUserDetailsAsync(Guid userId);
        Task<List<UserDetailsModel>> GetAllUserAsync(Guid userId);
        Task<string> UpdateUserAsync(Guid userId, UserUpdateModel model);
        Task<string> UpdateDisplayNameAsync(Guid userId, UserUpdateDisplayNameModel model);
        Task<string> ResetPasswordAsync(Guid adminId, Guid userId);
        Task<string> ChangePasswordAsync(Guid userId, ChangePasswordModel model);
        Task<string> UpdatePhotoProfileAsync(Guid userId, IFormFile photo);
        Task<PaginatedList<UserDetailsModel>> GetFilteredUsersAsync(UserFilterModel filter, int pageNumber, int pageSize);
        (List<User> data, int total, int totalPage) GetUsersByDepartment(string department, string site, string nameFilter, int page, int pageSize);
        List<Department> GetDepartmentsBySite(string site);
        Task<string> UploadHomeBannerAsync(IFormFile photo);
        IEnumerable<HomeBanner> GetAllBanners();
        Task<bool> DeleteHomeBannerAsync(int bannerId);
        Task<bool> UpdateUserStateCodeAsync(Guid userId, int stateCode);
        Task<int> CloseUsersAsync(List<Guid> userIds);
        Task<List<UserInfoDto>> GetUsersInfoByLogonNamesAsync(List<string> logonNames);







    }
}
