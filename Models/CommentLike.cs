namespace Small.Models
{
    public class CommentLike
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int CommentId { get; set; }
        public bool IsLike { get; set; } // true = like, false = dislike

        public Comment Comment { get; set; }
        public ApplicationUser User { get; set; }
    }
}
