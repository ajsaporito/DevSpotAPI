using DevSpotAPI.Models.Shared;
using DevSpotAPI.Data;

namespace DevSpotAPI.Repositories
{
	public class Repository : IRepository
	{
		private readonly Context _context;

		public Repository(Context context)
		{
			_context = context;
		}

		public void Create<TEntity>(TEntity entity) where TEntity : class
		{
			_context.Set<TEntity>().Add(entity);
		}

		public TEntity? Read<TEntity>(int id) where TEntity : class
		{
			return _context.Set<TEntity>().Find(id);
		}

		public List<TEntity> ReadAll<TEntity>() where TEntity : class
		{
			return _context.Set<TEntity>().ToList();
		}

		public void Update<TEntity>(TEntity entity) where TEntity : class
		{
			_context.Set<TEntity>().Update(entity);
		}

		public void Delete<TEntity>(TEntity entity) where TEntity : class
		{
			_context.Set<TEntity>().Remove(entity);
		}

		public void SaveChanges()
		{
			_context.SaveChanges();
		}
	}
}
