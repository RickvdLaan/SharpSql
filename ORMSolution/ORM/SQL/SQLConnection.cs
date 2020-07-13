using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace ORM
{
    internal class SQLConnection : IDisposable
    {
        internal bool _isDisposed;

        internal SqlConnection SqlConnection { get; set; }

        public SQLConnection()
        {
            OpenConnection();
        }

        private void OpenConnection()
        {
            SqlConnection = new SqlConnection(ORMUtilities.ConnectionString);

            if (!ORMUtilities.IsUnitTesting() && SqlConnection.State == ConnectionState.Closed)
            {
                SqlConnection.Open();
            }
        }

        internal void ExecuteEntityQuery<EntityType>(EntityType entity, SQLBuilder sqlBuilder)
            where EntityType : ORMEntity
        {
            if (!ORMUtilities.IsUnitTesting())
            {
                using (var command = new SqlCommand(sqlBuilder.GeneratedQuery, SqlConnection))
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
            if (!ORMUtilities.IsUnitTesting())
            {
                using (var command = new SqlCommand(sqlBuilder.GeneratedQuery, SqlConnection))
                {
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
            if (SqlConnection.State == ConnectionState.Open)
            {
                SqlConnection.Close();
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
                // Free managed resources.
                CloseConnection();
                SqlConnection.Dispose();
            }

            _isDisposed = true;
        }
    }
}
