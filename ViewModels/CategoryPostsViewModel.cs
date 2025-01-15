namespace Small.ViewModels
{
    public class CategoryPostsViewModel
    {
        public string CategoryName { get; set; }
        public List<PostViewModel> Posts { get; set; } = new List<PostViewModel>();
    }
}
