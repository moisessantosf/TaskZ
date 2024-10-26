using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskZ.Core.Entities;
using TaskZ.Core.Interfaces;
using TaskZ.Infrastructure.Data;

namespace TaskZ.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserByIdAsync(Guid userId)
        {
            return await _context.Users.FindAsync(userId);
        }
    }
}
