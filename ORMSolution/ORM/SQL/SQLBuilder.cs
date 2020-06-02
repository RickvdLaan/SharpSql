using Microsoft.Data.SqlClient;

namespace ORM
{
    internal class SQLBuilder
    {
        public SQLBuilder(ORMEntity entity)
        { 
        
        }

        public SQLBuilder(ORMCollection collection)
        {

        }

        public void OpenConnection()
        {
            SqlConnection sqlConnection = new SqlConnection(Utilities.ConnectionString);
            sqlConnection.Open();
        }

        public void CloseConnection()
        {
            SqlConnection sqlConnection = new SqlConnection(Utilities.ConnectionString);
            sqlConnection.Close();
        }
    }
}