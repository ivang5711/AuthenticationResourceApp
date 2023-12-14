using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AuthFormApp.Pages.RolesManager
{
    public class SetRoleLockedModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;

        private readonly SignInManager<IdentityUser> _signInManager;

        public SetRoleLockedModel(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public SelectList Roles { get; set; }
        public SelectList Users { get; set; }

        [BindProperty, Required, Display(Name = "Role")]
        public string SelectedRole { get; set; }

        [BindProperty, Required, Display(Name = "User")]
        public string SelectedUser { get; set; }

        public async Task<IActionResult> OnGet()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(User.Identity.Name);
                await _userManager.AddToRoleAsync(user, "Locked");
                await _userManager.RemoveFromRoleAsync(user, "Member");
                await _signInManager.SignOutAsync();
                return Redirect("/Index");
            }

            await GetOptions();
            return Page();
        }

        //public async Task<IActionResult> OnPostAsync()
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var user = await _userManager.FindByNameAsync(SelectedUser);
        //        await _userManager.AddToRoleAsync(user, SelectedRole);
        //        return RedirectToPage("/RolesManager/Index");
        //    }

        //    await GetOptions();
        //    return Page();
        //}

        public async Task GetOptions()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var users = await _userManager.Users.ToListAsync();
            Roles = new SelectList(roles, nameof(IdentityRole.Name));
            Users = new SelectList(users, nameof(IdentityUser));
        }
    }
}