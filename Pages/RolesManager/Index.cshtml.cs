using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthFormApp.Pages.RolesManager
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManger;

        public IndexModel(RoleManager<IdentityRole> roleManager)
        {
            _roleManger = roleManager;
        }

        public List<IdentityRole> Roles { get; set; }

        public void OnGet()
        {
            Roles = _roleManger.Roles.ToList();
        }
    }
}