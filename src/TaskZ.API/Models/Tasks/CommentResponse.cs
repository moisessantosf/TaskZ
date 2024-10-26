using TaskZ.Core.Entities;

namespace TaskZ.API.Models.Tasks
{
    public class CommentResponse
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid UserId { get; set; }

        public static CommentResponse FromComment(Comment comment)
        {
            return new CommentResponse
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UserId = comment.UserId
            };
        }
    }
}
