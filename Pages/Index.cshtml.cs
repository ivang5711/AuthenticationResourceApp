using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AuthFormApp.Pages;

[Authorize(Roles = "Member")]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;

    public SelectList Users { get; set; }
    public List<string> UserNames { get; set; } = new List<string>();
    public List<string> UsersEmail { get; set; } = new List<string>();
    public List<string> UsersStatus { get; set; } = new List<string>();
    public List<string> UsersLastLogin { get; set; } = new List<string>();
    public List<string> UsersRegiastrationTime { get; set; } = new List<string>();

    [BindProperty]
    public List<string> MyProperty { get; set; } = new();

    public string MyProperty1 { get; set; }

    public IndexModel(ILogger<IndexModel> logger, SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _logger = logger;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGet()
    {
        if (ModelState.IsValid)
        {
            IdentityUser? user = await _userManager.FindByNameAsync(User.Identity!.Name!);
            await _signInManager.RefreshSignInAsync(user!);
            var t = user.LockoutEnd;

            if (t == DateTime.MaxValue || _userManager.IsInRoleAsync(user, "Locked").Result)
            {
                return Redirect("/Index");
            }
        }

        List<IdentityUser> users = await _userManager.Users.ToListAsync();
        Users = new SelectList(users, nameof(IdentityUser.UserName));

        //var claims = ClaimsPrincipal.Current.Identities.First().Claims.ToList();

        //var nameIdentifier = User.FindFirst(ClaimTypes.Country);

        foreach (IdentityUser item in users)
        {
            //UserNames.Add(item.UserName);
            UsersEmail.Add(item.Email);
            UsersStatus.Add((item.LockoutEnd is not null || _userManager.IsInRoleAsync(item, "Locked").Result) ? "Blocked" : "Active");

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

    public async Task<IActionResult> OnPost()
    {
        if (ModelState.IsValid)
        {
            IdentityUser? user = await _userManager.FindByNameAsync(User.Identity!.Name!);
            await _signInManager.RefreshSignInAsync(user!);
            var t = user.LockoutEnd;

            if (t == DateTime.MaxValue || _userManager.IsInRoleAsync(user, "Locked").Result)
            {
                return Redirect("/Index");
            }
        }


        var selectedItems = Request.Form["row"].ToList();

        foreach(var item in selectedItems)
        {
            if (item is not null)
            {
                MyProperty.Add(item);
            }
        }


        List<IdentityUser> users = await _userManager.Users.ToListAsync();
        Users = new SelectList(users, nameof(IdentityUser.UserName));

        //var claims = ClaimsPrincipal.Current.Identities.First().Claims.ToList();

        //var nameIdentifier = User.FindFirst(ClaimTypes.Country);

        foreach (IdentityUser item in users)
        {
            //UserNames.Add(item.UserName);
            UsersEmail.Add(item.Email);
            UsersStatus.Add((item.LockoutEnd is not null || _userManager.IsInRoleAsync(item, "Locked").Result) ? "Blocked" : "Active");

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