using LibHac.Tools.Fs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RomManagerShared.Base.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
public class GenericRepository<TEntity> where TEntity : class
{
    private readonly AppDbContext _context;
    private readonly DbSet<TEntity> _set;

    public GenericRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _set = context.Set<TEntity>();
    }

    public ValueTask<TEntity?> GetByIdAsync(string id)
    {
        return _set.FindAsync(id);
    }

    public async ValueTask AddAsync(TEntity entity)
    {
        await _set.AddAsync(entity);
    }

    public async ValueTask AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await _set.AddRangeAsync(entities);
    }

    public void Update(TEntity entity)
    {
        _set.Update(entity);
    }

    public void UpdateRange(IEnumerable<TEntity> entities)
    {
        _set.UpdateRange(entities);
    }

    public void Delete(TEntity entity)
    {
        _set.Remove(entity);
    }

    public void DeleteRange(IEnumerable<TEntity> entities)
    {
        _set.RemoveRange(entities);
    }

    public Task<List<TEntity>> GetAllAsync()
    {
        return _set.ToListAsync();
    }

    public Task<TEntity?> GetByPropertyAsync(string propertyName, string propertyValue)
    {
        return _set.FirstOrDefaultAsync(e => EF.Property<string>(e, propertyName) == propertyValue);
    }

    public Task<bool> ExistsByPropertyAsync(string propertyName, string propertyValue)
    {
        return _set.AnyAsync(e => EF.Property<string>(e, propertyName) == propertyValue);
    }

    public async Task AddOrUpdateByTitleIDAsync(TEntity entity, string titleID, bool saveChanges = true)
    {
        var exists = await ExistsByPropertyAsync("TitleID", titleID);
        if (!exists)
        {
            await AddAsync(entity);
        }
        if (saveChanges)
        {
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddOrUpdateByPropertyRangeAsync(IEnumerable<TEntity> entities, Type type, string idPropertyName)
    {
        var tidProperty = type.GetProperty(idPropertyName);
        var tasks = entities
            .Select((entity, index) =>
            {
                var tidValue = (string)tidProperty.GetValue(entity);
                return new { Entity = entity, Index = index, TidValue = tidValue };
            })
            .Where(entry => entry.TidValue is not null)
            .Select(entry => AddOrUpdateByTitleIDAsync(entry.Entity, entry.TidValue, false));

        await Task.WhenAll(tasks);
        if (entities.Any())
        {
            await _context.SaveChangesAsync();
        }
    }
}

