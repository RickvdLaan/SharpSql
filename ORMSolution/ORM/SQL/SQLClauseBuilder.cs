using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ORM
{
    public class SQLClauseBuilder<TEntity> : SQLClauseBuilderBase where TEntity: ORMEntity
    {
        public SQLClause Where<TField>(Expression<Func<TEntity, TField>> field, TField value)
        {
            MemberExpression member = field.Body as MemberExpression;
            PropertyInfo propertyInfo = member.Member as PropertyInfo;
            
            return new SQLClause($"{propertyInfo.Name} = @param1", SQLClauseType.Where, new [] { value as object });
        }
    }
}
