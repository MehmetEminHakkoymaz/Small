namespace Small.ViewModels
{
    public class ProfileViewModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; }

        // Kullanıcının makaleleri
        public List<PostViewModel> Posts { get; set; } = new List<PostViewModel>();
    }

    public class PostViewModel
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CommentCount { get; set; }
        public string CategoryName { get; set; }
        public int LikeCount { get; set; }
    }
}
