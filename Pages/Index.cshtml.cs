using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthFormApp.Pages;

[Authorize(Roles = "Member")]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    public int MyProperty { get; set; } = 777;

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

        return Page();
    }
}