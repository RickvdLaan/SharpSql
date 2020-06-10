using Microsoft.Data.SqlClient;
using ORM.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace ORM
{
    internal class SQLBuilder : IDisposable
    {
        #region Variables & Objects

        internal bool _isDisposed;

        internal List<SQLClause> SQLClauses { get; set; }

        #endregion

        #region Properties

        internal SqlConnection SqlConnection { get; set; }

        #endregion

        #region Constructor

        public SQLBuilder()
        {
            SQLClauses = new List<SQLClause>();
            OpenConnection();
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

        internal DataTable ExecuteDirectQuery(string query, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        internal ORMEntity ExecuteEntityQuery()
        {
            throw new NotImplementedException();
        }

        internal void ExecuteCollectionQuery(ref List<ORMEntity> ormCollection, ref string query, ORMTableAttribute tableAttribute, long maxNumberOfItemsToReturn)
        {
            StringBuilder stringBuilder = new StringBuilder();

            SQLClauses.Add(new SQLClause(Select(maxNumberOfItemsToReturn)));
            SQLClauses.Add(new SQLClause(From(tableAttribute.TableName)));
            SQLClauses.Add(new SQLClause(Semicolon()));

            foreach (SQLClause sqlClause in SQLClauses)
            {
                stringBuilder.Append(sqlClause.Sql);
            }

            query = stringBuilder.ToString();

            using (SqlCommand command = new SqlCommand(query, SqlConnection))
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ORMEntity entity = (ORMEntity)Activator.CreateInstance(tableAttribute.EntityType);

                        for (int i = 0; i < reader.VisibleFieldCount; i++)
                        { 
                            PropertyInfo prop = entity.GetType().GetProperty(reader.GetName(i), BindingFlags.Public | BindingFlags.Instance);

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

        public string From(string tableName)
        {
            return string.Format("from {0}", tableName);
        }

        public string Select(long top = -1)
        {
            return string.Format("select {0}* ", top >= 0 ? $"top { top } " : string.Empty);
        }

        public string Semicolon()
        {
            return ";";
        }

        private void LogException(Exception exception)
        {
            Console.WriteLine("Exception Type: {0}", exception.GetType());
            Console.WriteLine("Message: {0}", exception.Message);
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