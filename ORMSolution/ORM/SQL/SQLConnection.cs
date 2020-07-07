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

        internal void ExecuteCollectionQuery<T>(ORMCollection<T> ormCollection, SQLBuilder sqlBuilder)
            where T : ORMEntity
        {
            if (ORMUtilities.IsUnitTesting())
            {
                return;
            }

            using (var command = new SqlCommand(sqlBuilder.ToString(), SqlConnection))
            {
                if (sqlBuilder.SqlParameters != null)
                {
                    command.Parameters.AddRange(sqlBuilder.SqlParameters);
                }

                using (var reader = command.ExecuteReader())
                {
                    ORMUtilities.DataReader<ORMCollection<T>, T>(ormCollection, reader);
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
