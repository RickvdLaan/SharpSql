using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace ORM
{
    internal class SQLBuilder : IDisposable
    {
        #region Variables & Objects

        private bool _isDisposed;

        #endregion

        #region Properties

        internal SqlConnection SqlConnection { get; set; }

        private ORMEntity Entity { get; set; }

        private ORMCollection Collection { get; set; }

        #endregion

        #region Constructor

        public SQLBuilder()
        {
            OpenConnection();
        }

        public SQLBuilder(ORMEntity entity)
            : this()
        {
            Entity = entity;
        }

        public SQLBuilder(ORMCollection collection)
            : this()
        {
            Collection = collection;
        }

        #endregion

        #region Methods

        private void OpenConnection()
        {
            SqlConnection = new SqlConnection(Utilities.ConnectionString);

            if (SqlConnection.State == ConnectionState.Closed)
            {
                SqlConnection.Open();
            }
        }

        internal void CloseConnection()
        {
            if (SqlConnection.State == ConnectionState.Open)
            {
                SqlConnection.Close();
            }
        }

        internal DataTable ExecuteDirectQuery(string query)
        {
            throw new NotImplementedException();
        }

        internal ORMEntity ExecuteEntityQuery()
        {
            throw new NotImplementedException();
        }

        internal ORMCollection ExecuteCollectionQuery()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IDisposable

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

        #endregion
    }
}