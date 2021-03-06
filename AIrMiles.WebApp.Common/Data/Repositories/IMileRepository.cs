﻿using AIrMiles.WebApp.Common.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIrMiles.WebApp.Common.Data.Repositories
{
    public interface IMileRepository : IGenericRepository<Mile>
    {
        int GetClientTotalStatusMiles(int clientID);

        int GetClientTotalBonusMiles(int clientID);

        List<Mile> GetAllBonusMiles(int clientID);

        Task<bool> SpendMilesAsync(List<Mile> clientMiles, int milesToSpend);
        Task DeleteExpiredMilesAsync();

        Task ResetClientStatusMiles(int clientId);
    }
}
