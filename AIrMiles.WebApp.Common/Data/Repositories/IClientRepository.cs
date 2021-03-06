﻿using AIrMiles.WebApp.Common.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIrMiles.WebApp.Common.Data.Repositories
{
    public interface IClientRepository : IGenericRepository<Client>
    {
        Task<Client> GetByEmailAsync(string email);
        IQueryable<Client> GetAllWithUsers();
    }
}
