using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Small.Data;
using Small.Models;
using System.Security.Claims;

namespace Small.Controllers
{

    [Authorize]
    [ServiceFilter(typeof(CategoriesFilter))]
    public class PostController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public PostController(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // Create Post
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post post)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                TempData["ErrorMessage"] = "You must be logged in to create a post.";
                return RedirectToAction("Index", "Home");
            }

            post.UserId = userId;
            post.CreatedAt = DateTime.UtcNow;

            ModelState.Remove("User");
            ModelState.Remove("UserId");
            ModelState.Remove("Category");

            if (ModelState.IsValid)
            {
                _context.Posts.Add(post);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Post created successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Failed to create post due to invalid data.";
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View(post);
        }
        // List Posts
        [HttpGet]
        public async Task<IActionResult> Index(string searchKeyword, int? categoryId, string filterByDate)
        {
            var posts = _context.Posts.Include(p => p.User).Include(p => p.Category).AsQueryable();

            // Arama işlemi (başlık ve içerik)
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                posts = posts.Where(p => p.Title.Contains(searchKeyword) || p.Content.Contains(searchKeyword));
            }

            // Kategoriye göre filtreleme
            if (categoryId.HasValue)
            {
                posts = posts.Where(p => p.CategoryId == categoryId.Value);
            }

            // Tarihe göre filtreleme
            if (!string.IsNullOrEmpty(filterByDate))
            {
                var today = DateTime.UtcNow;
                switch (filterByDate)
                {
                    case "Last7Days":
                        posts = posts.Where(p => p.CreatedAt >= today.AddDays(-7));
                        break;
                    case "Last30Days":
                        posts = posts.Where(p => p.CreatedAt >= today.AddDays(-30));
                        break;
                    case "LastYear":
                        posts = posts.Where(p => p.CreatedAt >= today.AddYears(-1));
                        break;
                }
            }

            var postList = await posts.ToListAsync();

            ViewData["SearchKeyword"] = searchKeyword;
            ViewData["CategoryId"] = categoryId;
            ViewData["FilterByDate"] = filterByDate;
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");

            return View(postList);
        }

        // View Post Details
        public async Task<IActionResult> Detail(int id)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .Include(p => p.Comments)
                .ThenInclude(c => c.CommentLikes)
                .Include(p => p.PostLikes) // Post'un kalpleri
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            return View(post);
        }

        // Like/Unlike Post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LikePost(int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                TempData["ErrorMessage"] = "You must be logged in to like a post.";
                return RedirectToAction(nameof(Detail), new { id = postId });
            }

            var existingLike = await _context.PostLikes
                .FirstOrDefaultAsync(pl => pl.PostId == postId && pl.UserId == userId);

            if (existingLike != null)
            {
                _context.PostLikes.Remove(existingLike);
                TempData["SuccessMessage"] = "You unliked the post.";
            }
            else
            {
                _context.PostLikes.Add(new PostLike
                {
                    PostId = postId,
                    UserId = userId,
                    LikedAt = DateTime.UtcNow
                });
                TempData["SuccessMessage"] = "You liked the post.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Detail), new { id = postId });
        }

        // Edit Post
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null || post.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                TempData["ErrorMessage"] = "You do not have permission to edit this post.";
                return Forbid();
            }
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Post post)
        {
            var existingPost = await _context.Posts.FindAsync(id);
            if (existingPost == null)
            {
                TempData["ErrorMessage"] = "Post not found.";
                return RedirectToAction(nameof(Index));
            }

            existingPost.Title = post.Title;
            existingPost.Content = post.Content;
            existingPost.UpdatedAt = DateTime.UtcNow;

            _context.Update(existingPost);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Post updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Delete Post
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id);
            if (post == null || (post.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier) && !User.IsInRole("Admin")))
            {
                TempData["ErrorMessage"] = "You do not have permission to delete this post.";
                return Forbid();
            }
            return View(post);
        }

        // Delete Post
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.Posts
                .Include(p => p.PostLikes)
                .Include(p => p.Comments)
                .ThenInclude(c => c.CommentLikes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post != null)
            {
                _context.PostLikes.RemoveRange(post.PostLikes);

                foreach (var comment in post.Comments)
                {
                    _context.CommentLikes.RemoveRange(comment.CommentLikes);
                }

                _context.Comments.RemoveRange(post.Comments);
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Post and all associated data deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Post not found.";
            }

            return RedirectToAction(nameof(Index));
        }

        //admindelete
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDelete(int id)
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                TempData["ErrorMessage"] = "Post not found.";
                return RedirectToAction("Index");
            }

            return View(post);
        }

        //adminDeleteConfrim
        [HttpPost, ActionName("AdminDelete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDeleteConfirmed(int id)
        {
            var post = await _context.Posts
                .Include(p => p.PostLikes)
                .Include(p => p.Comments)
                .ThenInclude(c => c.CommentLikes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                TempData["ErrorMessage"] = "Post not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // İlişkili verileri sil
                _context.PostLikes.RemoveRange(post.PostLikes);
                foreach (var comment in post.Comments)
                {
                    _context.CommentLikes.RemoveRange(comment.CommentLikes);
                }
                _context.Comments.RemoveRange(post.Comments);
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Post and all associated data deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }


        // Add Comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateComment(int postId, string content)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || string.IsNullOrEmpty(content))
            {
                TempData["ErrorMessage"] = "Invalid comment.";
                return RedirectToAction("Detail", new { id = postId });
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
            return RedirectToAction("Detail", new { id = postId });
        }

        // Edit Comment
        [HttpGet]
        public async Task<IActionResult> EditComment(int id)
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
        public async Task<IActionResult> EditComment(int id, string content)
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
            return RedirectToAction("Detail", new { id = comment.PostId });
        }

        // Delete Comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
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
            return RedirectToAction("Detail", new { id = comment.PostId });
        }

        // Admin Delete Comment
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminDeleteComment(int commentId, int postId)
        {
            var comment = await _context.Comments.Include(c => c.CommentLikes).FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment == null)
            {
                TempData["ErrorMessage"] = "Comment not found.";
                return RedirectToAction("Detail", new { id = postId });
            }

            _context.CommentLikes.RemoveRange(comment.CommentLikes);
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment deleted successfully.";
            return RedirectToAction("Detail", new { id = postId });
        }

        // Like Comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LikeComment(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingDislike = await _context.CommentLikes
                .FirstOrDefaultAsync(cl => cl.CommentId == id && cl.UserId == userId && !cl.IsLike);

            if (existingDislike != null)
            {
                _context.CommentLikes.Remove(existingDislike);
            }

            var existingLike = await _context.CommentLikes
                .FirstOrDefaultAsync(cl => cl.CommentId == id && cl.UserId == userId && cl.IsLike);

            if (existingLike == null)
            {
                _context.CommentLikes.Add(new CommentLike
                {
                    CommentId = id,
                    UserId = userId,
                    IsLike = true
                });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "You liked this comment.";
            }
            else
            {
                TempData["ErrorMessage"] = "You have already liked this comment.";
            }

            return RedirectToAction("Detail", new { id = _context.Comments.Find(id).PostId });
        }

        // Dislike Comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DislikeComment(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingLike = await _context.CommentLikes
                .FirstOrDefaultAsync(cl => cl.CommentId == id && cl.UserId == userId && cl.IsLike);

            if (existingLike != null)
            {
                _context.CommentLikes.Remove(existingLike);
            }

            var existingDislike = await _context.CommentLikes
                .FirstOrDefaultAsync(cl => cl.CommentId == id && cl.UserId == userId && !cl.IsLike);

            if (existingDislike == null)
            {
                _context.CommentLikes.Add(new CommentLike
                {
                    CommentId = id,
                    UserId = userId,
                    IsLike = false
                });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "You disliked this comment.";
            }
            else
            {
                TempData["ErrorMessage"] = "You have already disliked this comment.";
            }

            return RedirectToAction("Detail", new { id = _context.Comments.Find(id).PostId });
        }

        [HttpGet]
        public IActionResult Search(string keyword, int? categoryId)
        {
            var query = _context.Posts
                .Include(p => p.User)
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p => p.Title.Contains(keyword) || p.Content.Contains(keyword));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var posts = query.ToList();
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");
            return View(posts);
        }

        public async Task<IActionResult> PostsByCategory(int categoryId)
        {
            var posts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();

            var categoryName = await _context.Categories
                .Where(c => c.Id == categoryId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();

            ViewBag.CategoryName = categoryName ?? "Kategori Bulunamadı";

            return View(posts);
        }


    }
}
