using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Small.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
        public string Name { get; set; }

        public ICollection<Post> Posts { get; set; } = new List<Post>(); // Bir kategori birden fazla post içerebilir
    }
}
