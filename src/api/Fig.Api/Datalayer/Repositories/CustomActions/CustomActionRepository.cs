using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Api.Datalayer.Repositories.CustomActions;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.CustomActions;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;

namespace Fig.Api.Datalayer.Repositories.CustomActions
{
    public class CustomActionRepository : RepositoryBase<CustomActionBusinessEntity>, ICustomActionRepository
    {
        public CustomActionRepository(ISessionFactory sessionFactory) : base(sessionFactory)
        {
        }

        public IEnumerable<CustomActionBusinessEntity> GetByClientId(Guid clientId)
        {
            return InSession(session =>
                session.Query<CustomActionBusinessEntity>()
                    .Where(x => x.SettingClientId == clientId)
                    .ToList());
        }

        public IEnumerable<CustomActionExecutionBusinessEntity> GetAllPending(string clientName, string? instance)
        {
            return InSession(session =>
            {
                var query = session.QueryOver<CustomActionExecutionBusinessEntity>()
                    .JoinAlias(exec => exec.CustomAction, () => CustomActionBusinessEntity.CaAlias)
                    .JoinAlias(() => CustomActionBusinessEntity.CaAlias.SettingClient, () => SettingClientBusinessEntity.ScAlias)
                    .Where(exec => exec.Status == "Pending")
                    .And(() => SettingClientBusinessEntity.ScAlias.Name == clientName);

                if (instance != null)
                {
                    query.And(exec => exec.Instance == instance);
                }
                else
                {
                    // This handles the case where instance is null in the database.
                    // More complex "auto" logic (e.g., client has no instance runs) would typically be
                    // a separate query or handled in service layer based on business rules.
                    query.And(Restrictions.IsNull(Projections.Property<CustomActionExecutionBusinessEntity>(exec => exec.Instance)));
                }
                
                return query.List();
            });
        }

        public CustomActionExecutionBusinessEntity? GetExecutionById(Guid executionId)
        {
            // This might be better in CustomActionExecutionRepository if it exists and is used.
            // For now, implementing here as per ICustomActionRepository.
            return InSession(session => session.Get<CustomActionExecutionBusinessEntity>(executionId));
        }

        public void AddExecution(CustomActionExecutionBusinessEntity executionEntity)
        {
            // This might be better in CustomActionExecutionRepository.
            InSession(session => session.Save(executionEntity));
        }

        public void UpdateExecution(CustomActionExecutionBusinessEntity executionEntity)
        {
            // This might be better in CustomActionExecutionRepository.
            InSession(session => session.Update(executionEntity));
        }
    }

    // Static aliases for use in NHibernate queries to avoid magic strings
    public static class CustomActionBusinessEntityExtensions
    {
        public static CustomActionBusinessEntity CaAlias { get; private set; } = null!; // NPath / QueryOver alias
    }

    public static class SettingClientBusinessEntityExtensions
    {
        public static SettingClientBusinessEntity ScAlias { get; private set; } = null!; // NPath / QueryOver alias
    }
}
