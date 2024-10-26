using TaskZ.Core.Enums;

namespace TaskZ.Core.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public UserRole Role { get; set; }

        //public List<Project> Projects { get; set; }
        //public List<Task> Tasks { get; set; }
    }
}
