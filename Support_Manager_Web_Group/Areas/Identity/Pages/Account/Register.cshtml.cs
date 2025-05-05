// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // If using email sender
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Support_Manager_Web_Group.Models; // *** Use your ApplicationUser model ***

namespace Support_Manager_Web_Group.Areas.Identity.Pages.Account // Ensure namespace matches
{
    [AllowAnonymous] // Allow access to registration page without login
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager; // *** Use ApplicationUser ***
        private readonly IUserStore<ApplicationUser> _userStore;   // *** Use ApplicationUser ***
        private readonly IUserEmailStore<ApplicationUser> _emailStore; // *** Use ApplicationUser ***
        private readonly ILogger<RegisterModel> _logger;
        // private readonly IEmailSender _emailSender; // Uncomment if using email sending

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger
            /*, IEmailSender emailSender */) // Uncomment if using email sending
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            // _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        // --- InputModel Updated ---
        public class InputModel
        {
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
            [Display(Name = "Full Name")] // Display name for the label
            public string FullName { get; set; } // *** ADDED FullName ***

            [StringLength(50)]
            [Display(Name = "Employee ID (Optional)")]
            public string EmployeeID { get; set; } // *** ADDED EmployeeID (Optional) ***

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 4)] // Adjusted minimum length
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }
        // --------------------------


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/"); // Redirect to home page after registration
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // --- Create ApplicationUser with added properties ---
                var user = CreateUser();

                // *** Set custom properties from InputModel ***
                user.FullName = Input.FullName;
                user.EmployeeID = Input.EmployeeID; // Assign EmployeeID if provided
                // *********************************************

                // Set required Identity properties (UserName typically same as Email here)
                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // Create the user using UserManager (hashes password etc.)
                var result = await _userManager.CreateAsync(user, Input.Password);
                // ---------------------------------------------------

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // --- Assign Default Role (e.g., "Employee") ---
                    // Ensure roles were seeded in Program.cs
                    if (await _userManager.IsInRoleAsync(user, "Employee") == false &&
                        await _userManager.IsInRoleAsync(user, "IT Support") == false &&
                        await _userManager.IsInRoleAsync(user, "IT Manager") == false) // Avoid adding if already has a role
                    {
                        var roleResult = await _userManager.AddToRoleAsync(user, "Employee"); // Default role
                        if (!roleResult.Succeeded)
                        {
                            _logger.LogError($"Error assigning default role 'Employee' to user {user.Email}.");
                            // Log errors but continue registration process
                        }
                        else
                        {
                            _logger.LogInformation($"User {user.Email} assigned to default role 'Employee'.");
                        }
                    }
                    // ---------------------------------------------

                    var userId = await _userManager.GetUserIdAsync(user);

                    // Email confirmation logic (commented out if RequireConfirmedAccount is false)
                    // var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    // code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    // var callbackUrl = Url.Page( "/Account/ConfirmEmail", pageHandler: null, values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl }, protocol: Request.Scheme);
                    // await _emailSender.SendEmailAsync(Input.Email, "Confirm your email", $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        // Automatically sign in the user after registration
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                // If creation failed, add errors to ModelState
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                //Use ActivatorUtilities if ApplicationUser has constructor dependencies,
                //otherwise Activator.CreateInstance is fine.
                return Activator.CreateInstance<ApplicationUser>();

                // Safer approach using built-in factory if ApplicationUser has parameterless constructor
                var user = new ApplicationUser();
                return user;

            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            // Cast the UserStore to one that supports Email
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
