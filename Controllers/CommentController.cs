//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Small.Data;
//using Small.Models;
//using System.Security.Claims;
//using System.Threading.Tasks;

//namespace Small.Controllers
//{
//    [Authorize]
//    public class CommentController : Controller
//    {
//        private readonly ApplicationDbContext _context;

//        public CommentController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(int postId, string content)
//        {
//            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            if (userId == null || string.IsNullOrEmpty(content))
//            {
//                TempData["ErrorMessage"] = "Invalid comment.";
//                return RedirectToAction("Detail", "Post", new { id = postId });
//            }

//            var comment = new Comment
//            {
//                Content = content,
//                UserId = userId,
//                PostId = postId
//            };

//            _context.Comments.Add(comment);
//            await _context.SaveChangesAsync();

//            TempData["SuccessMessage"] = "Comment added successfully.";
//            return RedirectToAction("Detail", "Post", new { id = postId });
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Delete(int id)
//        {
//            var comment = await _context.Comments.FindAsync(id);
//            if (comment == null || (comment.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin")))
//            {
//                return Forbid();
//            }

//            _context.Comments.Remove(comment);
//            await _context.SaveChangesAsync();

//            TempData["SuccessMessage"] = "Comment deleted successfully.";
//            return RedirectToAction("Detail", "Post", new { id = comment.PostId });
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> LikeComment(int id)
//        {
//            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            if (_context.CommentLikes.Any(cl => cl.CommentId == id && cl.UserId == userId && cl.IsLike))
//            {
//                TempData["ErrorMessage"] = "You have already liked this comment.";
//                return RedirectToAction("Detail", "Post", new { id = _context.Comments.Find(id).PostId });
//            }

//            _context.CommentLikes.Add(new CommentLike
//            {
//                CommentId = id,
//                UserId = userId,
//                IsLike = true
//            });
//            await _context.SaveChangesAsync();

//            TempData["SuccessMessage"] = "Comment liked successfully.";
//            return RedirectToAction("Detail", "Post", new { id = _context.Comments.Find(id).PostId });
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DislikeComment(int id)
//        {
//            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            if (_context.CommentLikes.Any(cl => cl.CommentId == id && cl.UserId == userId && !cl.IsLike))
//            {
//                TempData["ErrorMessage"] = "You have already disliked this comment.";
//                return RedirectToAction("Detail", "Post", new { id = _context.Comments.Find(id).PostId });
//            }

//            _context.CommentLikes.Add(new CommentLike
//            {
//                CommentId = id,
//                UserId = userId,
//                IsLike = false
//            });
//            await _context.SaveChangesAsync();

//            TempData["SuccessMessage"] = "Comment disliked successfully.";
//            return RedirectToAction("Detail", "Post", new { id = _context.Comments.Find(id).PostId });
//        }


//    }
//}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Small.Data;
using Small.Models;
using System.Security.Claims;

namespace Small.Controllers
{
    [Authorize]
    public class CommentController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public CommentController(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // Add Comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int postId, string content)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || string.IsNullOrEmpty(content))
            {
                TempData["ErrorMessage"] = "Invalid comment.";
                return RedirectToAction("Detail", "Post", new { id = postId });
            }

            var comment = new Comment
            {
                Content = content,
                UserId = userId,
                PostId = postId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment added successfully.";
            return RedirectToAction("Detail", "Post", new { id = postId });
        }

        // Edit Comment
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null || comment.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Forbid();
            }
            return View(comment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string content)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null || comment.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Forbid();
            }

            comment.Content = content;
            comment.UpdatedAt = DateTime.UtcNow;

            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment updated successfully.";
            return RedirectToAction("Detail", "Post", new { id = comment.PostId });
        }

        // Delete Comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var comment = await _context.Comments.Include(c => c.CommentLikes).FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null || (comment.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin")))
            {
                return Forbid();
            }

            _context.CommentLikes.RemoveRange(comment.CommentLikes);
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment deleted successfully.";
            return RedirectToAction("Detail", "Post", new { id = comment.PostId });
        }

        // Like Comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (_context.CommentLikes.Any(cl => cl.CommentId == id && cl.UserId == userId && cl.IsLike))
            {
                TempData["ErrorMessage"] = "You have already liked this comment.";
                return RedirectToAction("Detail", "Post", new { id = _context.Comments.Find(id).PostId });
            }

            _context.CommentLikes.Add(new CommentLike
            {
                CommentId = id,
                UserId = userId,
                IsLike = true
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment liked successfully.";
            return RedirectToAction("Detail", "Post", new { id = _context.Comments.Find(id).PostId });
        }

        // Dislike Comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dislike(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (_context.CommentLikes.Any(cl => cl.CommentId == id && cl.UserId == userId && !cl.IsLike))
            {
                TempData["ErrorMessage"] = "You have already disliked this comment.";
                return RedirectToAction("Detail", "Post", new { id = _context.Comments.Find(id).PostId });
            }

            _context.CommentLikes.Add(new CommentLike
            {
                CommentId = id,
                UserId = userId,
                IsLike = false
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment disliked successfully.";
            return RedirectToAction("Detail", "Post", new { id = _context.Comments.Find(id).PostId });
        }
    }
}

