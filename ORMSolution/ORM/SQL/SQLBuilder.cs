﻿using Microsoft.Data.SqlClient;
using ORM.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
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

        internal void AddSQLClause(SQLClause clause)
        {
            AddSQLClauses(clause);
        }

        internal void AddSQLClauses(params SQLClause[] clauses)
        {
            SQLClauses.AddRange(clauses);
        }

        internal void ExecuteCollectionQuery(ref List<ORMEntity> ormCollection, out string query, SqlParameter[] sqlParameters, ORMTableAttribute tableAttribute, long maxNumberOfItemsToReturn)
        {
            SQLClauseBuilderBase clauseBuilder = new SQLClauseBuilderBase();

            AddSQLClauses(
                clauseBuilder.Select(maxNumberOfItemsToReturn),
                clauseBuilder.From(tableAttribute.TableName),
                clauseBuilder.Semicolon());

            BuildSelectQuery(out query, out sqlParameters);

            using (SqlCommand command = new SqlCommand(query, SqlConnection))
            {
                command.Parameters.AddRange(sqlParameters);

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

        internal void BuildSelectQuery(out string query, out SqlParameter[] sqlParameters)
        {
            StringBuilder stringBuilder = new StringBuilder();

            SQLClause select = SQLClauses.Where(x => x.Type == SQLClauseType.Select).First();
            SQLClause from = SQLClauses.Where(x => x.Type == SQLClauseType.From).First();
            List<SQLClause> where = SQLClauses.Where(x => x.Type == SQLClauseType.Where).ToList();
            SQLClause semicolon = SQLClauses.Where(x => x.Type == SQLClauseType.Semicolon).First();

            stringBuilder.Append(select.Sql);
            stringBuilder.Append(from.Sql);

            var tempList = new List<SqlParameter>();

            if (where.Any())
            {
                SQLClause clause = where.First();

                stringBuilder.Append($"WHERE ({clause.Sql})");
                tempList.AddRange(clause.Parameters.ToList());
            }

            sqlParameters = tempList.ToArray();
            stringBuilder.Append(semicolon.Sql);

            query = stringBuilder.ToString();
        }


        public SqlParameter[] SqlParameters
        {
            get; private set;
        }

        private List<object> _sqlParameters = new List<object>(10);

        private void GenerateSqlParameters()
        {
            SqlParameters = new SqlParameter[_sqlParameters.Count];

            for (int i = 0; i < _sqlParameters.Count; i++)
            {
                string paramName = $"@param{i + 1}";
                SqlParameters[i] = (new SqlParameter(paramName, _sqlParameters[i]));
            }
        }

        internal string BuildQuery(Expression body)
        {
            string query = ParseExpression(body);
            GenerateSqlParameters();

            return query;
        }

        private string ParseExpression(Expression body)
        {
            switch (body.NodeType)
            {
                case ExpressionType.Equal:
                    {
                        var type = body as BinaryExpression;
                        var left = type.Left as MemberExpression;
                        var right = type.Right as ConstantExpression;

                        return $"({BuildQuery(left)} = {BuildQuery(right)})";
                    }
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    {
                        var type = body as BinaryExpression;
                        var left = type.Left;
                        var right = type.Right;

                        return $"({BuildQuery(left)} OR {BuildQuery(right)})";
                    }
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    {
                        var type = body as BinaryExpression;
                        var left = type.Left;
                        var right = type.Right;

                        return $"({BuildQuery(left)} AND {BuildQuery(right)})";
                    }
                case ExpressionType.MemberAccess:
                    {
                        var type = body as MemberExpression;

                        return type.Member.Name;
                    }
                case ExpressionType.Constant:
                    {
                        var type = body as ConstantExpression;

                        _sqlParameters.Add(type.Value);

                        return $"@param{_sqlParameters.Count}";
                    }
                default:
                    throw new NotImplementedException();
            }
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