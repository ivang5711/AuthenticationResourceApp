using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace AuthFormApp.Pages;

[Authorize(Roles = "Member")]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private List<string> userNames = new();
    private List<string> usersLastLogin = new();
    private List<string> usersRegiastrationTime = new();
    private const string roleLocked = "Locked";
    private const string roleMember = "Member";
    private const string claimTypeRegistrationDateTime = "RegistrationDateTime";
    private const string claimTypeLastLogin = "LastLogin";
    private const string claimTypePersonName = "PersonName";
    private List<IdentityUser> users = new();

    public List<string> UsersEmail { get; set; } = new();
    public List<string> UsersStatus { get; set; } = new();

    public List<string> UserNames
    {
        get => userNames;
        set => userNames = value;
    }

    public List<string> UsersLastLogin
    {
        get => usersLastLogin;
        set => usersLastLogin = value;
    }

    public List<string> UsersRegiastrationTime
    {
        get => usersRegiastrationTime;
        set => usersRegiastrationTime = value;
    }

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
        string checkResult = await CheckModelStateAsync();
        if (!string.IsNullOrWhiteSpace(checkResult))
        {
            return Redirect(checkResult);
        }

        await CollectUsersManagementTableDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        string checkResult = await CheckModelStateAsync();
        if (!string.IsNullOrWhiteSpace(checkResult))
        {
            return Redirect(checkResult);
        }

        await ProcessPostRequest();
        checkResult = await CheckModelStateAsync();
        if (!string.IsNullOrWhiteSpace(checkResult))
        {
            return Redirect(checkResult);
        }

        await CollectUsersManagementTableDataAsync();
        return Page();
    }

    private string DefineUserStatus(IdentityUser item)
    {
        return (item.LockoutEnd is not null
                || _userManager.IsInRoleAsync(item, roleLocked).Result) ?
                "Blocked" : "Active";
    }

    private void FormatDateTimeString(ref List<string> property)
    {
        foreach (string item in property.ToList())
        {
            DateTime parsedValue = DateTime.Parse(
                item, CultureInfo.InvariantCulture);
            property.Add(parsedValue.ToString("HH':'mm':'ss, d MMM, yyyy"));
        }
    }

    private void CollectRequestFormValues(List<string?> selectedItems)
    {
        if (selectedItems is not null)
        {
            RequestResult.AddRange(from string? item in selectedItems
                                   where item is not null
                                   select item);
        }
    }

    private void PopulateUsersPropertyList(IList<Claim> existingUserClaims,
    string claimType, ref List<string> property)
    {
        foreach (Claim claim in existingUserClaims)
        {
            if (claim.Type == claimType)
            {
                property.Add(claim.Value);
                break;
            }
        }
    }

    private bool CheckUserLockedOut(IdentityUser user)
    {
        return user.LockoutEnd == DateTime.MaxValue ||
            _userManager.IsInRoleAsync(user, roleLocked).Result;
    }

    private async Task CollectUserData(IdentityUser user)
    {
        UsersEmail.Add(user.Email!);
        UsersStatus.Add(DefineUserStatus(user));
        await PopulateUsersClaimsAsync(user);
        FormatDateTimeColumns();
    }

    private void FormatDateTimeColumns()
    {
        FormatDateTimeString(ref usersRegiastrationTime);
        FormatDateTimeString(ref usersLastLogin);
    }

    private async Task PopulateUsersClaimsAsync(IdentityUser user)
    {
        var existingUserClaims = await _userManager.GetClaimsAsync(user);
        PopulateUsersPropertyList(existingUserClaims,
            claimTypeRegistrationDateTime, ref usersRegiastrationTime);
        PopulateUsersPropertyList(existingUserClaims, claimTypeLastLogin,
            ref usersLastLogin);
        PopulateUsersPropertyList(existingUserClaims, claimTypePersonName,
            ref userNames);
    }

    private async Task CollectUsersManagementTableDataAsync()
    {
        await GetUsers();
        await CollectUsersData();
    }

    private async Task CollectUsersData()
    {
        foreach (IdentityUser item in users)
        {
            await CollectUserData(item);
        }
    }

    private async Task BlockUsersAsync()
    {
        foreach (string item in RequestResult)
        {
            await BlockSingleUser(item);
        }
    }

    private async Task BlockSingleUser(string item)
    {
        IdentityUser? user = await _userManager.FindByNameAsync(item);
        await _userManager.RemoveFromRoleAsync(user!, roleMember);
        await _userManager.AddToRoleAsync(user!, roleLocked);
        await _userManager
            .SetLockoutEndDateAsync(user!, DateTime.MaxValue);
    }

    private async Task UnblockUsers()
    {
        foreach (string item in RequestResult)
        {
            await UnblockSingleUser(item);
        }
    }

    private async Task UnblockSingleUser(string item)
    {
        IdentityUser? user = await _userManager.FindByNameAsync(item);
        if (user is not null)
        {
            await ChangeUserDataToUnblocked(user);
        }
    }

    private async Task ChangeUserDataToUnblocked(IdentityUser user)
    {
        await _userManager.RemoveFromRoleAsync(user, roleLocked);
        await _userManager.AddToRoleAsync(user, roleMember);
        await _userManager.SetLockoutEndDateAsync(user, null);
    }

    private async Task DeleteUsers()
    {
        foreach (string item in RequestResult)
        {
            await DeleteSingleUser(item);
        }
    }

    private async Task DeleteSingleUser(string item)
    {
        IdentityUser? user = await _userManager.FindByNameAsync(item);
        if (user is not null)
        {
            IdentityResult result = await _userManager.DeleteAsync(user);
            CheckUnexpectedExceptionState(result);
        }
    }

    private void CheckUnexpectedExceptionState(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Unexpected error occurred deleting user.");
        }
    }

    private void DefineFormSubmissionType()
    {
        Block = Request.Form["Block"];
        Unblock = Request.Form["Unblock"];
        Delete = Request.Form["Delete"];
    }

    private async Task<string> CheckModelStateAsync()
    {
        if (ModelState.IsValid)
        {
            if (User is null || User.Identity is null ||
                User.Identity.Name is null)
            {
                return "/Identity/Account/Logout";
            }

            IdentityUser? user = await _userManager
                .FindByNameAsync(User.Identity.Name);
            if (user is null)
            {
                return "/Identity/Account/Logout";
            }

            await _signInManager.RefreshSignInAsync(user);
            if (CheckUserLockedOut(user))
            {
                await _signInManager.SignOutAsync();
                return "/Index";
            }
        }

        return string.Empty;
    }

    private async Task GetUsers() =>
        users = await _userManager.Users.ToListAsync();

    private async Task PerformRequestedAction()
    {
        if (Block != null)
        {
            await BlockUsersAsync();
        }
        else if (Unblock != null)
        {
            await UnblockUsers();
        }
        else if (Delete != null)
        {
            await DeleteUsers();
        }
    }

    private async Task ProcessPostRequest()
    {
        DefineFormSubmissionType();
        CollectRequestFormValues(Request.Form["row"].ToList());
        await PerformRequestedAction();
    }
}