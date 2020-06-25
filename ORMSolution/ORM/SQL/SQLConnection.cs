using Microsoft.Data.SqlClient;
using ORM.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace ORM.SQL
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
            SqlConnection = new SqlConnection(Utilities.ConnectionString);

            if (SqlConnection.State == ConnectionState.Closed)
            {
                SqlConnection.Open();
            }
        }

        internal void ExecuteCollectionQuery(ref List<ORMEntity> ormCollection, SQLBuilder sqlBuilder, ORMTableAttribute tableAttribute)
        {
            using (SqlCommand command = new SqlCommand(sqlBuilder.ToString(), SqlConnection))
            {
                if (sqlBuilder.SqlParameters != null)
                {
                    command.Parameters.AddRange(sqlBuilder.SqlParameters);
                }

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ORMEntity entity = (ORMEntity)Activator.CreateInstance(tableAttribute.EntityType);

                        for (int i = 0; i < reader.VisibleFieldCount; i++)
                        {
                            PropertyInfo prop = entity.GetType().GetProperty(reader.GetName(i), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                            if (null == prop)
                            {
                                throw new NotImplementedException(string.Format("Column [{0}] has not been implemented in [{1}].", reader.GetName(i), tableAttribute.EntityType.FullName));
                            }
                            else if (!prop.CanWrite)
                            {
                                throw new ReadOnlyException(string.Format("Property [{0}] is read-only.", reader.GetName(i), tableAttribute.EntityType.FullName));
                            }

                            prop.SetValue(entity, reader.GetValue(i));
                        }

                        ormCollection.Add(entity);
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

        internal DataTable ExecuteDirectQuery(string query, params object[] parameters)
        {
            throw new NotImplementedException();
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
