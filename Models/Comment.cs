using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Small.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } // UpdatedAt alanı eklendi

        public int Likes { get; set; } = 0;
        public int Dislikes { get; set; } = 0;

        // İlişkiler
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int PostId { get; set; }
        public Post Post { get; set; }

        public ICollection<CommentLike> CommentLikes { get; set; } // Navigasyon özelliği eklendi
    }
}
