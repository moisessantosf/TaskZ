namespace TaskZ.API.Models.Tasks
{
    public class AddCommentRequest
    {
        public string Content { get; set; }
        public Guid UserId { get; set; }
    }
}
