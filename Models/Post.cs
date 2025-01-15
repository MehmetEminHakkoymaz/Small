using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Small.Models
{
    public class Post
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; }

        [Required]
        [StringLength(5000, ErrorMessage = "Content cannot exceed 5000 characters.")]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Kullanıcıyla ilişki
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        // Yorumlarla ilişki
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        // Kategoriyle ilişki
        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();

    }
}
