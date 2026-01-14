namespace DevSpotAPI.Models.Shared
{
	public interface IRepository
	{
		void Create<TEntity>(TEntity entity) where TEntity : class;
		TEntity? Read<TEntity>(int id) where TEntity : class;
		List<TEntity> ReadAll<TEntity>() where TEntity : class;
		void Update<TEntity>(TEntity entity) where TEntity : class;
		void Delete<TEntity>(TEntity entity) where TEntity : class;
		void SaveChanges();
	}
}
