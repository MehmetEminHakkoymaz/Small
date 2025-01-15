using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Small.Models;
using Microsoft.EntityFrameworkCore; // Include için gerekli
using Small.ViewModels; // Eğer ViewModel oluşturacaksak

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager) 
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.Users
            .Include(u => u.Posts) // Post ilişkisini yükle
                .ThenInclude(p => p.Comments) // Comment ilişkisini yükle
            .Include(u => u.Posts)
                .ThenInclude(p => p.PostLikes) // PostLikes ilişkisini yükle
            .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

        if (user == null)
        {
            return NotFound();
        }

        var model = new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            ProfilePicture = user.ProfilePicture,
            CreatedAt = user.CreatedAt,
            Posts = user.Posts.Select(p => new PostViewModel
            {
                Title = p.Title,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                CommentCount = p.Comments.Count, // Yorum sayısını al
                LikeCount = p.PostLikes.Count, // Beğeni sayısını al
                CategoryName = p.Category != null ? p.Category.Name : "Kategori Yok"
            }).ToList()
        };

        return View(model);
    }


    [HttpPost]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.FullName = model.FullName;
            user.ProfilePicture = model.ProfilePicture;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        return View("Index", model);
    }
}
