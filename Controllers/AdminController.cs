using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Small.Data;
using Small.Models;
using Small.ViewModels;

namespace Small.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // Admin Paneli Ana Sayfası
        public IActionResult Index()
        {
            ViewBag.UserCount = _context.Users.Count();
            ViewBag.PostCount = _context.Posts.Count();
            ViewBag.CategoryCount = _context.Categories.Count();
            ViewBag.Categories = _context.Categories.ToList(); // Kategorileri gönder
            return View();
        }

        // Kullanıcı Yönetim Sayfası
        public async Task<IActionResult> UserManagement()
        {
            var users = _userManager.Users.ToList();
            var roles = new Dictionary<string, IList<string>>();

            foreach (var user in users)
            {
                roles[user.Id] = await _userManager.GetRolesAsync(user);
            }

            ViewBag.Roles = roles;
            return View(users);
        }

        // Kullanıcı Silme
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
                result.Succeeded ? "User deleted successfully." : "Error deleting user.";

            return RedirectToAction(nameof(UserManagement));
        }

        // Kullanıcı Düzenleme (GET)
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = _roleManager.Roles.Select(r => r.Name).ToList();

            var model = new EditUserViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Roles = allRoles.Select(role => new UserRole
                {
                    RoleName = role,
                    IsSelected = userRoles.Contains(role)
                }).ToList()
            };

            return View(model);
        }

        // Kullanıcı Düzenleme (POST)
        [HttpPost]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName).ToList();

            // Yeni roller ekle
            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(currentRoles));
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Failed to add roles.");
                return View(model);
            }

            // Eski rollerden kaldır
            result = await _userManager.RemoveFromRolesAsync(user, currentRoles.Except(selectedRoles));
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Failed to remove roles.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Roles updated successfully.";
            return RedirectToAction(nameof(UserManagement));
        }

        // Yeni Rol Oluşturma (GET)
        public IActionResult CreateRole()
        {
            return View(new CreateRoleViewModel());
        }

        // Yeni Rol Oluşturma (POST)
        [HttpPost]
        public async Task<IActionResult> CreateRole(CreateRoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(model.RoleName));
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Role created successfully.";
                    return RedirectToAction(nameof(UserManagement));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }


        // Yeni Kategori Ekle (POST)
        [HttpPost]
        public IActionResult AddCategory(string categoryName)
        {
            if (!string.IsNullOrEmpty(categoryName))
            {
                var newCategory = new Category { Name = categoryName };
                _context.Categories.Add(newCategory);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Category added successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Category name cannot be empty.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Kategori Sil (POST)
        [HttpPost]
        public IActionResult DeleteCategory(int categoryId)
        {
            var category = _context.Categories.Find(categoryId);
            if (category != null)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Category deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Category not found.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
