using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KAEAGoalWebAPI.Models;

namespace KAEAGoalWebAPI.Services
{
    public interface ICoinService
    {
        Task AddKaeaCoinToUserAsync(Guid AdminUserId,AddCoinModel model);
        Task<CoinBalanceModel> GetUserCoinBalanceAsync(Guid userId);
        Task AddThankCoinToUserAsync(Guid adminUserId, AddCoinModel model);
        Task AddAllUserThankCoinAsync(Guid adminUserId, AddCoinModel model);

        Task<string> ConvertThankToKaeaAsync(Guid userId, CoinConversionModel model);
        Task<string> GiveThankCoinAsync(Guid userId, GiveThankCoinModel model);
        Task<List<UserCoinTransactionViewModel>> UserCoinTransactionAsync(Guid userId);
    }
}
