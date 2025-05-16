using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KAEAGoalWebAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KAEAGoalWebAPI.Services
{
    public interface IMissionService
    {
        Task<Guid> CreateMissionAsync(Guid userId, CreateMissionModel model);
        Task UpdateMissionAsync(Guid missionId, CreateMissionModel model);

        Task<List<MissionViewModel>> GetAllMissionsAsync();
        Task AcceptMissionAsync(Guid userId, AcceptMissionModel model);
        Task<List<UserMissionViewModel>> GetUserMissionsAsync(Guid userId);
        Task<string> ExecuteCodeMissionAsync(Guid userId, ExecuteCodeMissionModel model);
        Task<CollectCoinRewardResponse> CollectCoinRewardAsync(CollectCoinRewardRequest model, Guid userId);
        Task<List<MissionViewModel>> GetUnacceptedMissionsAsync(Guid userId);
        Task<List<UserMissionViewModel>> GetCompletedMissionsAsync(Guid userId);
        Task<List<UserMissionViewModel>> GetAllUserMissionsAsync();
        Task<string> ExecuteQRCodeMissionASync(Guid userId, ExecuteQRCodeModel model);
        Task<string> ApproveQRCodeMissionsAsync(Guid userId, ApproveQRCodeMissionModel model);
        Task<List<ApproveViewModel>> GetAllQRCodeApproveAsync(string missionowner);
        Task<string> ExecutePhotoMissionAsync(Guid userId, ExecutePhotoModel model);
        Task<List<ApprovePhotoViewModel>> GetAllPhotoApproveAsync(Guid userId,string missionowner);
        Task<(List<ApprovePhotoViewModel> data, int total, int totalPage)> GetPhotoApproveByMissionAsync(Guid missionId, int page, int pageSize, string missionowner, string? searchName);
        Task<(List<ApproveVideoViewModel> data, int total, int totalPage)> GetVideoApproveByMissionAsync(Guid missionId, int page, int pageSize, string missionowner, string? searchName);
        Task<(List<ApproveTextViewModel> data, int total, int totalPage)> GetTextApproveByMissionAsync(Guid missionId, int page, int pageSize, string missionowner, string? searchName);
        Task<(List<FeedViewModel> data, int total, int totalPage)> GetMissionFeedAsync(Guid userId,int page, int pageSize, string? type = null, string? missionName = null, string? displayName = null);
        Task<List<UserLikeInfo>> GetLikesForMissionAsync(Guid userMissionId);
        Task<List<MissionSelectViewModel>> GetAllPublicMissionsAsync();

        Task<List<UserSummaryViewModel>> GetUsersInMissionAsync(Guid missionId, string type);
        Task<string> SetIsViewAsync(IsViewModel model);
        Task<string> LikeMissionAsync(FeedLikeReq request);



        Task<List<MissionSelectViewModel>> GetMissionNamesByTypeAsync(string missionType , string missionowner);
        Task<string> ApprovePhotoMissionAsync(Guid userId, ApprovePhotoMissionModel model);


        Task<List<ApproveVideoViewModel>> GetAllVideoApproveAsync(Guid userId, string missionowner);
        Task<string> ApproveVideoMissionAsync(Guid userId, ApproveVideoMissionModel model);


        Task<List<MissionViewModel>> GetMissionerAsync(Guid userId);
        //Task<string> ExecuteVideoMissionAsync(Guid userId, ExecuteVideoMissionModel model);
        Task<string> MissionerAddBatchTexTMissionRewardAsync(Guid userId, Guid missionId, int Amount);
        Task<string> MissionerAddWinnerCoinRewardTextMissionAsync(Guid userId, AddCoinWinnerMission model);
        Task<string> MissionerApproveTextMissionAsync(Guid userId, ApproveTextMissionModel model);
        Task<string> ExecuteTextMissionAsync(Guid userId, ExecuteTextMissionModel model);
        Task<List<ApproveTextViewModel>> GetAllTextApproveAsync(string missionowner);
        Task<string> MissionerAddWinnerCoinRewardPhotoMissionAsync(Guid userId, AddCoinWinnerMission model);
        Task<string> MissionerAddWinnerCoinRewardQRMissionAsync(Guid userId, AddCoinWinnerMission model);
        Task<string> MissionerAddBatchPhotoMissionRewardAsync(Guid userId, Guid missionId, int Amount);
        Task<string> MissionerAddBatchQRCodeMissionRewardAsync(Guid userId, Guid missionId, int Amount);
        Task<string> ExecuteVideoMissionAsync(Guid userId, ExecuteVideoModel model);
        Task<string> MissionerAddWinnerCoinAllMissionAsync(Guid userId, AddCoinWinnerMission model, string missionowner);

    }
}
