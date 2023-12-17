using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AuthFormApp.Pages;

[Authorize(Roles = "Member")]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public SelectList Users { get; set; }
    public List<string> UserNames { get; set; } = new();
    public List<string> UsersEmail { get; set; } = new();
    public List<string> UsersStatus { get; set; } = new();
    public List<string> UsersLastLogin { get; set; } = new();
    public List<string> UsersRegiastrationTime { get; set; } = new();

    [BindProperty]
    public List<string> RequestResult { get; set; } = new();

    [BindProperty]
    public string? Block { get; set; }

    [BindProperty]
    public string? Unblock { get; set; }

    [BindProperty]
    public string? Delete { get; set; }

    public IndexModel(ILogger<IndexModel> logger,
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _logger = logger;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGet()
    {
        if (ModelState.IsValid)
        {
            IdentityUser? user = await _userManager
                .FindByNameAsync(User.Identity!.Name!);
            if (user is null)
            {
                return Redirect("/Identity/Account/Logout");
            }

            await _signInManager.RefreshSignInAsync(user!);
            var t = user.LockoutEnd;
            if (t == DateTime.MaxValue || _userManager
                .IsInRoleAsync(user, "Locked").Result)
            {
                return Redirect("/Index");
            }
        }

        List<IdentityUser> users = await _userManager.Users.ToListAsync();
        Users = new SelectList(users, nameof(IdentityUser.UserName));

        foreach (IdentityUser item in users)
        {
            UsersEmail.Add(item.Email!);
            UsersStatus.Add(
                (item.LockoutEnd is not null
                || _userManager.IsInRoleAsync(item, "Locked").Result) ?
                "Blocked" : "Active");

            var existingUserClaims = await _userManager.GetClaimsAsync(item);

            foreach (var claim in existingUserClaims)
            {
                if (claim.Type == "RegistrationDateTime")
                {
                    DateTime parsedValue = DateTime.Parse(claim.Value, CultureInfo.InvariantCulture);

                    string res = parsedValue
                        .ToString("HH':'mm':'ss, d MMM, yyyy");

                    UsersRegiastrationTime.Add(res);
                    break;
                }
            }

            foreach (var claim in existingUserClaims)
            {
                if (claim.Type == "LastLogin")
                {
                    DateTime parsedValue = DateTime.Parse(claim.Value, CultureInfo.InvariantCulture);

                    string res = parsedValue
                        .ToString("HH':'mm':'ss, d MMM, yyyy");

                    UsersLastLogin.Add(res);
                    break;
                }
            }

            foreach (var claim in existingUserClaims)
            {
                if (claim.Type == "PersonName")
                {
                    UserNames.Add(claim.Value);
                    break;
                }
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (ModelState.IsValid)
        {
            IdentityUser? user = await _userManager
                .FindByNameAsync(User.Identity!.Name!);
            await _signInManager.RefreshSignInAsync(user!);
            var t = user!.LockoutEnd;
            if (t == DateTime.MaxValue || _userManager
                .IsInRoleAsync(user, "Locked").Result)
            {
                await _signInManager.SignOutAsync();
                return Redirect("/Index");
            }
        }

        Block = Request.Form["Block"];
        Unblock = Request.Form["Unblock"];
        Delete = Request.Form["Delete"];

        List<string?> selectedItems = Request.Form["row"].ToList();
        if (selectedItems is not null)
        {
            foreach (var item in selectedItems)
            {
                if (item is not null)
                {
                    RequestResult.Add(item);
                }
            }
        }

        if (Block != null)
        {
            Block = "I am block";
            foreach (var item in RequestResult)
            {
                var user = await _userManager.FindByNameAsync(item);
                await _userManager.RemoveFromRoleAsync(user!, "Member");
                await _userManager.AddToRoleAsync(user!, "Locked");
                await _userManager
                    .SetLockoutEndDateAsync(user!, DateTime.MaxValue);
            }
        }

        if (Unblock != null)
        {
            Unblock = "I am unblock";
            foreach (var item in RequestResult)
            {
                var user = await _userManager.FindByNameAsync(item);
                await _userManager.RemoveFromRoleAsync(user!, "Locked");
                await _userManager.AddToRoleAsync(user!, "Member");
                await _userManager.SetLockoutEndDateAsync(user!, null);
            }
        }

        if (Delete != null)
        {
            Delete = "I am delete";
            foreach (var item in RequestResult)
            {
                var user = await _userManager.FindByNameAsync(item);
                if (user is not null)
                {
                    var result = await _userManager.DeleteAsync(user);
                    var userId = await _userManager.GetUserIdAsync(user);
                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"Unexpected error occurred deleting user.");
                    }

                    _logger.LogInformation(
                        "User with ID '{UserId}' deleted themselves.", userId);
                }
            }
        }

        if (ModelState.IsValid)
        {
            IdentityUser? user = await _userManager
                .FindByNameAsync(User.Identity!.Name!);
            if (user is null)
            {
                return Redirect("/Identity/Account/Logout");
            }

            await _signInManager.RefreshSignInAsync(user);
            if (user.LockoutEnd == DateTime.MaxValue ||
                _userManager.IsInRoleAsync(user, "Locked").Result)
            {
                await _signInManager.SignOutAsync();
                return Redirect("/Index");
            }
        }

        List<IdentityUser> users = await _userManager.Users.ToListAsync();
        Users = new SelectList(users, nameof(IdentityUser.UserName));
        foreach (IdentityUser item in users)
        {
            UsersEmail.Add(item.Email!);
            UsersStatus.Add((item.LockoutEnd is not null
                || _userManager.IsInRoleAsync(item, "Locked").Result) ?
                "Blocked" : "Active");
            var existingUserClaims = await _userManager.GetClaimsAsync(item);
            foreach (var claim in existingUserClaims)
            {
                if (claim.Type == "RegistrationDateTime")
                {
                    UsersRegiastrationTime.Add(claim.Value);
                    break;
                }
            }

            foreach (var claim in existingUserClaims)
            {
                if (claim.Type == "LastLogin")
                {
                    UsersLastLogin.Add(claim.Value);
                    break;
                }
            }

            foreach (var claim in existingUserClaims)
            {
                if (claim.Type == "PersonName")
                {
                    UserNames.Add(claim.Value);
                    break;
                }
            }
        }

        return Page();
    }
}