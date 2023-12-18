using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using System.Security.Claims;

namespace AuthFormApp.Pages;

[Authorize(Roles = "Member")]
public class IndexModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private List<string> userNames = [];
    private List<string> usersLastLogin = [];
    private List<string> usersRegistrationTime = [];
    private const string roleLocked = "Locked";
    private const string roleMember = "Member";
    private readonly List<string> roles = [roleLocked, roleMember];
    private const string roleLockedMessage = "Blocked";
    private const string roleMemberMessage = "Active";
    private const string blockButtonName = "Block";
    private const string unblockButtonName = "Unblock";
    private const string deleteButtonName = "Delete";
    private const string tableRowName = "row";
    private const string dateTimeViewFormatString = "HH':'mm':'ss, d MMM, yyyy";
    private const string claimTypeRegistrationDateTime = "RegistrationDateTime";
    private const string claimTypeLastLogin = "LastLogin";
    private const string claimTypePersonName = "PersonName";
    private List<IdentityUser> users = [];

    public List<string> UsersEmail { get; set; } = [];
    public List<string> UsersStatus { get; set; } = [];

#pragma warning disable S2292 // Trivial properties should be auto-implemented

    public List<string> UserNames
#pragma warning restore S2292 // Trivial properties should be auto-implemented
    {
        get => userNames;
        set => userNames = value;
    }

#pragma warning disable S2292 // Trivial properties should be auto-implemented

    public List<string> UsersLastLogin
#pragma warning restore S2292 // Trivial properties should be auto-implemented
    {
        get => usersLastLogin;
        set => usersLastLogin = value;
    }

#pragma warning disable S2292 // Trivial properties should be auto-implemented

    public List<string> UsersRegistrationTime
#pragma warning restore S2292 // Trivial properties should be auto-implemented
    {
        get => usersRegistrationTime;
        set => usersRegistrationTime = value;
    }

    [BindProperty]
    public List<string> RequestResult { get; set; } = [];

    [BindProperty]
    public bool Block { get; set; } = false;

    [BindProperty]
    public bool Unblock { get; set; } = false;

    [BindProperty]
    public bool Delete { get; set; } = false;

    public IndexModel(SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGet()
    {
        string checkResult = await CheckModelStateAsync();
        if (!string.IsNullOrWhiteSpace(checkResult))
            return Redirect(checkResult);
        await CollectUsersManagementTableDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        string checkResult = await CheckModelStateAsync();
        if (!string.IsNullOrWhiteSpace(checkResult))
            return Redirect(checkResult);
        await ProcessPostRequest();
        checkResult = await CheckModelStateAsync();
        if (!string.IsNullOrWhiteSpace(checkResult))
            return Redirect(checkResult);
        await CollectUsersManagementTableDataAsync();
        return Page();
    }

    private string DefineUserStatus(IdentityUser item)
    {
        return (item.LockoutEnd is not null
                || _userManager.IsInRoleAsync(item, roleLocked).Result) ?
                roleLockedMessage : roleMemberMessage;
    }

    private static void FormatDateTimeString(ref List<string> property)
    {
        for (int i = 0; i < property.Count; i++)
        {
            property[i] = DateTime.Parse(property[i],
                CultureInfo.InvariantCulture)
                .ToString(dateTimeViewFormatString);
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

    private static void PopulateUsersPropertyList(
        IList<Claim> existingUserClaims, string claimType,
        ref List<string> property)
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
    }

    private void FormatDateTimeColumns()
    {
        FormatDateTimeString(ref usersRegistrationTime);
        FormatDateTimeString(ref usersLastLogin);
    }

    private async Task PopulateUsersClaimsAsync(IdentityUser user)
    {
        var existingUserClaims = await _userManager.GetClaimsAsync(user);
        PopulateUsersPropertyList(existingUserClaims,
            claimTypeRegistrationDateTime, ref usersRegistrationTime);
        PopulateUsersPropertyList(existingUserClaims, claimTypeLastLogin,
            ref usersLastLogin);
        PopulateUsersPropertyList(existingUserClaims, claimTypePersonName,
            ref userNames);
    }

    private async Task CollectUsersManagementTableDataAsync()
    {
        await GetUsers();
        await CollectUsersDataAsync();
        FormatDateTimeColumns();
    }

    private async Task CollectUsersDataAsync()
    {
        foreach (IdentityUser item in users)
        {
            await CollectUserData(item);
        }
    }

    private async Task ChangeUsersStatus(string role)
    {
        foreach (string item in RequestResult)
        {
            await ChangeSingleUserStatus(item, role);
        }
    }

    private async Task ChangeSingleUserStatus(string item, string role)
    {
        IdentityUser? user = await _userManager.FindByNameAsync(item);
        if (user is not null)
        {
            await ChangeUserBlockStatus(user, role);
        }
    }

    private async Task ChangeUserBlockStatus(IdentityUser user, string role)
    {
        await _userManager.RemoveFromRolesAsync(user, roles);
        await _userManager.AddToRoleAsync(user, role);
        await UpdateUserLockoutValue(user, role);
    }

    private async Task UpdateUserLockoutValue(IdentityUser user, string role)
    {
        if (role == roleMember)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
        }
        else if (role == roleLocked)
        {
            await _userManager
                .SetLockoutEndDateAsync(user, DateTime.MaxValue);
        }
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

    private static void CheckUnexpectedExceptionState(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Unexpected error occurred deleting user.");
        }
    }

    private void DefineFormSubmissionType()
    {
        Block = bool.TryParse(Request.Form[blockButtonName], out bool a) && a;
        Unblock = bool.TryParse(Request.Form[unblockButtonName], out bool b) && b;
        Delete = bool.TryParse(Request.Form[deleteButtonName], out bool c) && c;
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
        if (Block)
        {
            await ChangeUsersStatus(roleLocked);
        }
        else if (Unblock)
        {
            await ChangeUsersStatus(roleMember);
        }
        else if (Delete)
        {
            await DeleteUsers();
        }
    }

    private async Task ProcessPostRequest()
    {
        DefineFormSubmissionType();
        CollectRequestFormValues([.. Request.Form[tableRowName]]);
        await PerformRequestedAction();
    }
}