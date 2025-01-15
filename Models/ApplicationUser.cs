using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace Small.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Kullanıcının tam adı
        public string? FullName { get; set; }

        // Profil fotoğrafı URL veya yolu
        public string? ProfilePicture { get; set; }

        // Kullanıcıya ait yazılar
        public ICollection<Post> Posts { get; set; } = new List<Post>();

        // Kullanıcının oluşturulma ve güncellenme zamanı
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Kullanıcının admin olup olmadığını kontrol eden özellik
        public bool IsAdmin { get; set; } = false;
    }
}
