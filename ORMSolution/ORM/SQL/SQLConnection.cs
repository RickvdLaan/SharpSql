using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading;

namespace ORM
{
    internal class SQLConnection : IDisposable
    {
        internal bool _isDisposed;

        internal static AsyncLocal<SqlConnection> SqlConnection { get; set; } = new AsyncLocal<SqlConnection>();

        public SQLConnection()
        {
            OpenConnection();
        }

        private void OpenConnection()
        {
            if (SqlConnection.Value == null)
                SqlConnection.Value = new SqlConnection(ORMUtilities.ConnectionString);

            if (!ORMUtilities.IsUnitTesting && SqlConnection.Value.State == ConnectionState.Closed)
            {
                SqlConnection.Value.Open();
            }
        }

        internal int ExecuteNonQuery(SQLBuilder sqlBuilder)
        {
            if (!ORMUtilities.IsUnitTesting)
            {
                using (var command = new SqlCommand(sqlBuilder.GeneratedQuery, SqlConnection.Value))
                {
                    if (ORMUtilities.Transaction.Value != null)
                    {
                        command.Transaction = ORMUtilities.Transaction.Value;
                    }

                    if (sqlBuilder.SqlParameters != null)
                    {
                        command.Parameters.AddRange(sqlBuilder.SqlParameters);
                    }

                    return command.ExecuteNonQuery();
                }
            }

            return -1;
        }

        internal void ExecuteEntityQuery<EntityType>(EntityType entity, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            if (!ORMUtilities.IsUnitTesting)
            {
                using (var command = new SqlCommand(sqlBuilder.GeneratedQuery, SqlConnection.Value))
                {
                    if (sqlBuilder.SqlParameters != null)
                    {
                        command.Parameters.AddRange(sqlBuilder.SqlParameters);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        ORMUtilities.DataReader(entity, reader, sqlBuilder);
                    }
                }
            }
        }

        internal void ExecuteCollectionQuery<EntityType>(ORMCollection<EntityType> ormCollection, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            if (!ORMUtilities.IsUnitTesting)
            {
                using (var command = new SqlCommand(sqlBuilder.GeneratedQuery, SqlConnection.Value))
                {
                    if (ORMUtilities.Transaction.Value != null)
                    {
                        command.Transaction = ORMUtilities.Transaction.Value;
                    }

                    if (sqlBuilder.SqlParameters != null)
                    {
                        command.Parameters.AddRange(sqlBuilder.SqlParameters);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        ORMUtilities.DataReader<ORMCollection<EntityType>, EntityType>(ormCollection, reader, sqlBuilder);
                    }
                }
            }
        }

        internal void CloseConnection()
        {
            if (SqlConnection.Value.State == ConnectionState.Open)
            {
                SqlConnection.Value.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                if (!ORMUtilities.IsInTransaction())
                {
                    CloseConnection();
                    SqlConnection.Value.Dispose();
                    SqlConnection.Value = null;
                }
            }

            _isDisposed = true;
        }
    }
}
