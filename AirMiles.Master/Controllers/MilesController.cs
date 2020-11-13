﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AirMiles.Master.Helpers;
using AirMiles.Master.Models.Miles;
using AIrMiles.WebApp.Common.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AirMiles.Master.Controllers
{
    public class MilesController : Controller
    {
        private readonly IMilesRequestRepository _milesRequestRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IPartnerRepository _partnerRepository;
        private readonly IUserRepository _userRepository;
        private readonly IConverterHelper _converterHelper;

        public MilesController(
            IMilesRequestRepository milesRequestRepository,
            IClientRepository clientRepository,
            IPartnerRepository partnerRepository,
            IUserRepository userRepository,
            IConverterHelper converterHelper)
        {
            _milesRequestRepository = milesRequestRepository;
            _clientRepository = clientRepository;
            _partnerRepository = partnerRepository;
            _userRepository = userRepository;
            _converterHelper = converterHelper;
        }

        public IConverterHelper ConverterHelper { get; }


        #region BackOffice
        public async Task<IActionResult> RequestsIndex()
        {
            var list = _milesRequestRepository.GetAll().Where(r => !r.IsDeleted && !r.IsAproved).ToList();

            var model = new List<RequestsIndexViewModel>();
            foreach (var request in list)
            {
                request.Client = await _clientRepository.GetByIdAsync(request.ClientId);
                request.Client.User = await _userRepository.GetUserByIdAsync(request.Client.UserId);
                request.Partner = await _partnerRepository.GetByIdAsync(request.PartnerId);
                model.Add(_converterHelper.ToRequestsIndexViewModel(request));
            }            

            return View(model);
        }

        public async Task<IActionResult> Aprove(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request = await _milesRequestRepository.GetByIdAsync(id.Value);
            if (request == null)
            {
                return NotFound();
            }

            request.IsAproved = true;
            await _milesRequestRepository.UpdateAsync(request);
            return RedirectToAction(nameof(RequestsIndex));
        }

        public async Task<IActionResult> Reject(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request = await _milesRequestRepository.GetByIdAsync(id.Value);
            if (request == null)
            {
                return NotFound();
            }

            request.IsDeleted = true;
            await _milesRequestRepository.UpdateAsync(request);
            return RedirectToAction(nameof(RequestsIndex));
        }

        #endregion

    }
}