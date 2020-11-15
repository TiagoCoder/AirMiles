﻿using AirMiles.FrontOffice.Models.Account;
using AIrMiles.WebApp.Common.Data.Entities;
using System.Collections.Generic;

namespace AirMiles.FrontOffice.Helpers
{
    public interface IConverterHelper
    {
        EditViewModel ToEditViewModel(Client client, User user);

        IEnumerable<Balance_MovementsViewModel> ToBalanceMovementsViewModel(List<Transaction> transactions);

        Mile ToMile(BuyMilesViewModel model, int clientId);

        Transaction ToTransaction(BuyMilesViewModel model, Mile mile);
    }
}
