using DevBrain.Domain.Entities;
using DevBrain.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevBrain.Infrastructure.Persistence;

public class EFUserRepository : IUserRepository
{
    private readonly DevBrainDbContext _context;

    public EFUserRepository(DevBrainDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Case-insensitive email search
        var emailLower = email.ToLower();
        return await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower, cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
}
