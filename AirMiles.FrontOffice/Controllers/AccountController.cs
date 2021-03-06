﻿using AirMiles.FrontOffice.Helpers;
using AirMiles.FrontOffice.Models.Account;
using AIrMiles.WebApp.Common.Data.Entities;
using AIrMiles.WebApp.Common.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AirMiles.FrontOffice.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IMailHelper _mailHelper;
        private readonly IClientRepository _clientRepository;
        private readonly IConverterHelper _converterHelper;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IMileRepository _mileRepository;
        private readonly IImageHelper _imageHelper;
        private readonly IFlightRepository _flightRepository;
        private readonly ITicketRepository _ticketRepository;

        public AccountController(IUserRepository userRepository, IMailHelper mailHelper, IClientRepository clientRepository, IConverterHelper converterHelper, ITransactionRepository transactionRepository, IMileRepository mileRepository, IImageHelper imageHelper, IFlightRepository flightRepository, ITicketRepository ticketRepository)
        {
            _userRepository = userRepository;
            _mailHelper = mailHelper;
            _clientRepository = clientRepository;
            _converterHelper = converterHelper;
            _transactionRepository = transactionRepository;
            _mileRepository = mileRepository;
            _imageHelper = imageHelper;
            _flightRepository = flightRepository;
            _ticketRepository = ticketRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region Login

        public IActionResult Login()
        {
            if (this.User.Identity.IsAuthenticated)
            {
                return this.RedirectToAction("Edit", "Account");
            }

            return this.View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var client = await _clientRepository.GetByIdAsync(Convert.ToInt32(model.ClientId));

                if (client != null)
                {
                    var user = await _userRepository.GetUserByIdAsync(client.UserId);

                    if (user != null)
                    {
                        var result = await _userRepository.LoginAsync(user.UserName, model.Password, model.RememberMe);

                        if (result.Succeeded)
                        {
                            return this.RedirectToAction("Edit", "Account");
                        }

                        this.ModelState.AddModelError(string.Empty, "Failed to login.");

                        return View(model);
                    }
                }

                this.ModelState.AddModelError(string.Empty, "This account does not exist");

                return View(model);
            }
            this.ModelState.AddModelError(string.Empty, "Failed to login.");

            return View(model);
        }

        #endregion

        #region Logout

        public async Task<IActionResult> Logout()
        {
            await _userRepository.LogoutAsync();

            return this.RedirectToAction(nameof(Login));
        }

        #endregion

        #region Register

        public IActionResult Register()
        {
            return this.View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterNewClientViewModel model)
        {
            if (ModelState.IsValid)
            {
                //Gets the User
                var user = await _userRepository.GetUserByEmailAsync(model.Username);

                if (user == null)
                {
                    // Creates a new Client
                    var client = new Client
                    {
                        RevisionMonth = DateTime.Now.Month,
                        IsAproved = true,
                        IsDeleted = false,
                        User = new User
                        {
                            BirthDate = new DateTime(1990, 2, 15),
                            FirstName = model.FirstName,
                            LastName = model.LastName,
                            UserName = model.Username,
                            Email = model.Username,
                            PhotoUrl = "~/images/Users/Default_User_Image.png"
                        }
                    };

                    // Adds the User to the DataBase
                    var result = await _userRepository.AddUserAsync(client.User, model.Password);
                    if (result != IdentityResult.Success)
                    {
                        ModelState.AddModelError(string.Empty, "The User could not be created");
                        return View(model);
                    }

                    // Add user to Role
                    await _userRepository.AddUsertoRoleAsync(client.User, "Client");
                    await _userRepository.AddUsertoRoleAsync(client.User, "Basic");
                    // Adds the Client to the DataBase
                    await _clientRepository.CreateAsync(client);

                    // Retrieves the Client ID
                    var clientID = await _clientRepository.GetByEmailAsync(model.Username);

                    // Creates a Token in order to confirm the email

                    var myToken = await _userRepository.GenerateEmailConfirmationTokenAsync(client.User);

                    // Defines the Link with its properties to be sent in the email
                    var tokenLink = this.Url.Action("ConfirmAccount", "Account", new
                    {
                        userid = client.User.Id,
                        token = myToken,
                    }, protocol: HttpContext.Request.Scheme);

                    //Sends an Email to the User with the TokenLink
                    try
                    {
                        _mailHelper.SendMail(model.Username, "Account Confirmation", $"<h1>Account Confirmation</h1>" +
                       $"To finish your account registration, " +
                       $"please click this link: <a href = \"{tokenLink}\">Confirm Account</a>"
                       + $"<br/><br/>Your Account ID is: {clientID.Id:D9}");
                        this.ViewBag.Message = "The instructions to confirm your account have been sent to the email.";
                    }
                    catch (Exception)
                    {
                        this.ModelState.AddModelError(string.Empty, "Error sending the email, please try again in a few minutes");
                    }
                    return this.View(model);
                }

                ModelState.AddModelError(string.Empty, "A User with this email is already registered");
            }

            return View(model);
        }

        public async Task<IActionResult> ConfirmAccount(string userId, string token)
        {
            // Verifies if the userId and token are not empty
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return this.NotFound();
            }

            // Gets the User through its Id
            var user = await _userRepository.GetUserByIdAsync(userId);

            if (user == null)
            {
                return this.NotFound();
            }

            var result = await _userRepository.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
            {
                return this.NotFound();
            }

            return View();
        }

        #endregion

        #region PasswordManagement

        public IActionResult ForgotPassword()
        {
            ViewData["Message"] = "Insert the registered email to recover your account.";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                // Gets the user
                var user = await _userRepository.GetUserByEmailAsync(model.Email);

                // Reports back an error if the user does not exist
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "The email doesn't correspond to a registered user.");
                    return this.View(model);

                }

                // Generates a token
                var myToken = await _userRepository.GeneratePasswordResetTokenAsync(user);

                // Builds a link which has the generated token
                var link = this.Url.Action(
                    "ResetPassword",
                    "Account",
                    new { token = myToken }, protocol: HttpContext.Request.Scheme);

                // Sends an email with a custom message to the client email with the instruction to recover his account
                _mailHelper.SendMail(model.Email, "Cinel Air Miles Password Reset", $"<h1>Password Reset</h1>" +
                $"To reset the password click in this link:</br></br>" +
                $"<a href = \"{link}\">Reset Password</a>");

                ViewData["Message"] = "The instructions to recover your password have been sent!";
                return this.View();

            }
            return this.View(model);
        }

        public async Task<IActionResult> ResetPassword(string userId, string token)
        {
            // Verifies if the userId and token are not empty
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return this.NotFound();
            }

            // Gets the User through its Id
            var user = await _userRepository.GetUserByIdAsync(userId);

            if (user == null)
            {
                return this.NotFound();
            }

            // Initializes a new Model, filling the fields 
            var model = new ResetPasswordViewModel
            {
                UserName = user.UserName,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userRepository.GetUserByEmailAsync(model.UserName);

                if (user != null)
                {
                    // Confirms the Email
                    var result = await _userRepository.ResetPasswordAsync(user, model.Token, model.Password);
                    if (result.Succeeded)
                    {
                        return this.RedirectToAction("Login");
                    }
                    else
                    {
                        this.ModelState.AddModelError(string.Empty, "Error while changing the password.");
                        return View(model);
                    }
                }
                this.ModelState.AddModelError(string.Empty, "User not found.");
                return View(model);
            }

            return View(model);
        }


        public IActionResult ChangePassword()
        {
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (this.ModelState.IsValid)
            {
                var user = await _userRepository.GetUserByEmailAsync(this.User.Identity.Name);
                if (user != null)
                {
                    var result = await _userRepository.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        //return this.RedirectToAction(nameof(Details));
                    }
                    else
                    {
                        this.ModelState.AddModelError(string.Empty, result.Errors.FirstOrDefault().Description);
                    }
                }
                else
                {
                    this.ModelState.AddModelError(string.Empty, "User not found.");
                }
            }

            return View(model);
        }


        #endregion

        #region CRUD

        [Authorize]
        public async Task<IActionResult> Edit()
        {
            // Gets the authenticated client
            var client = await _clientRepository.GetByEmailAsync(this.User.Identity.Name);

            // Extra Security
            if (client == null)
            {
                return NotFound();
            }

            // Gets the object user that belongs to the authenticated client
            var user = await _userRepository.GetUserByIdAsync(client.UserId);

            // Extra Security
            if (user == null)
            {
                return NotFound();
            }

            // Converts to an EditViewModel, with the parameters being the objects client and user
            var model = _converterHelper.ToEditViewModel(client, user, AssignEditModelBackgroundPath());

            model.StatusMiles = _mileRepository.GetClientTotalStatusMiles(client.Id).ToString();
            model.BonusMiles = _mileRepository.GetClientTotalBonusMiles(client.Id).ToString();

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Edit(IFormFile photo, int clientID)
        {
            var client = await _clientRepository.GetByEmailAsync(this.User.Identity.Name);

            var user = await _userRepository.GetUserByIdAsync(client.UserId);

            var beforeModel = _converterHelper.ToEditViewModel(client, user, AssignEditModelBackgroundPath());

            beforeModel.StatusMiles = _mileRepository.GetClientTotalStatusMiles(client.Id).ToString();
            beforeModel.BonusMiles = _mileRepository.GetClientTotalBonusMiles(client.Id).ToString();

            if (photo != null && photo.Length > 0)
            {
                string path = await _imageHelper.UploadImageAsync(photo, "Users", clientID);
                user.PhotoUrl = path;
            }
            else
            {
                return View(beforeModel);
            }

            // Photo was updated correctly
            var updatedModel = _converterHelper.ToEditViewModel(client, user, AssignEditModelBackgroundPath());

            try
            {
                await _userRepository.UpdateUserAsync(user);
            }
            catch (Exception)
            {
                this.ModelState.AddModelError(string.Empty, "An error occured while updating your information.");

                return View(beforeModel);
            }

            updatedModel.StatusMiles = _mileRepository.GetClientTotalStatusMiles(client.Id).ToString();
            updatedModel.BonusMiles = _mileRepository.GetClientTotalBonusMiles(client.Id).ToString();

            return View(updatedModel);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditInfo(string firstName, string lastName, string birthDate)
        {
            try
            {
                var user = await _userRepository.GetUserByEmailAsync(this.User.Identity.Name);
                if (user == null)
                {
                    return NotFound();
                }

                user.FirstName = firstName;
                user.LastName = lastName;
                user.BirthDate = DateTime.ParseExact(birthDate, "yyyy-MM-dd", null);

                var result = await _userRepository.UpdateUserAsync(user);
                if (!result.Succeeded)
                {
                    return StatusCode(500, "Database Error");
                }

                return StatusCode(200, "Info Successfully Updated");
            }
            catch (Exception)
            {
                return StatusCode(520, "Unknown Error");
            }


        }

        #endregion

        #region MilesCard

        public async Task<IActionResult> MilesCard(MilesCardViewModel model)
        {
            // Gets the authenticated client
            var client = await _clientRepository.GetByEmailAsync(this.User.Identity.Name);

            // Extra Security
            if (client == null)
            {
                return NotFound();
            }

            // Gets the object user that belongs to the authenticated client
            var user = await _userRepository.GetUserByIdAsync(client.UserId);

            // Extra Security
            if (user == null)
            {
                return NotFound();
            }

            model = _converterHelper.ToMilesCardViewModel(client, user);

            if (User.IsInRole("Basic"))
            {
                model.Status = "Basic";
                model.BackColor = "#00c292";
                model.StatusPhoto = "/lib/ClientTemplate/img/card/plane_basic.png";
            }
            else if (User.IsInRole("Silver"))
            {
                model.Status = "Silver";
                model.BackColor = "silver";
                model.StatusPhoto = "/lib/ClientTemplate/img/card/plane_silver.png";
            }
            else if (User.IsInRole("Gold"))
            {
                model.Status = "Gold";
                model.BackColor = "#FFDF01";
                model.StatusPhoto = "/lib/ClientTemplate/img/card/plane_gold.png";
            }

            model.StatusMiles = _mileRepository.GetClientTotalStatusMiles(client.Id).ToString();
            model.BonusMiles = _mileRepository.GetClientTotalBonusMiles(client.Id).ToString();

            return View(model);
        }

        #endregion

        #region AdditionalMethods

        public string AssignEditModelBackgroundPath()
        {
            if (this.User.IsInRole("Gold"))
            {
                return "/lib/ClientTemplate/img/status/Gold.jpg";
            }
            else if (this.User.IsInRole("Silver"))
            {
                return "/lib/ClientTemplate/img/status/Silver.jpg";
            }
            else
            {
                return "/lib/ClientTemplate/img/status/Basic.jpg";
            }
        }

        #endregion
    }
}