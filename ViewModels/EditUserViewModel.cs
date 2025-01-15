using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Small.ViewModels
{
    public class EditUserViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        public List<UserRole> Roles { get; set; } = new List<UserRole>();
    }

    public class UserRole
    {
        public string RoleName { get; set; }
        public bool IsSelected { get; set; }
    }
}
