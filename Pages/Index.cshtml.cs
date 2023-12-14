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
    public int MyProperty { get; set; } = 777;

    public SelectList Users { get; set; }
    public List<string> UserNames { get; set; } = new List<string>();
    public List<string> UsersEmail { get; set; } = new List<string>();
    public List<string> UsersStatus { get; set; } = new List<string>();
    public List<string> UsersLastLogin { get; set; } = new List<string>();  
    public List<string> UsersRegiastrationTime { get; set; } = new List<string>();  

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
        }

        List<IdentityUser> users = await _userManager.Users.ToListAsync();
        Users = new SelectList(users, nameof(IdentityUser.UserName));

        foreach (IdentityUser item in users)
        {
            UserNames.Add(item.UserName);   
            UsersEmail.Add(item.Email);
            UsersStatus.Add(item.LockoutEnd is not null ? "Blocked" : "Active");
        }

        return Page();
    }
}