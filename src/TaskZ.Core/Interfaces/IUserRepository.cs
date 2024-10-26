using TaskZ.Core.Entities;

namespace TaskZ.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserByIdAsync(Guid userId);
    }
}
