// This file is part of Mystery Dungeon eXtended.

// Mystery Dungeon eXtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// Mystery Dungeon eXtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with Mystery Dungeon eXtended.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Dapper;
using MySql.Data.MySqlClient;

namespace PMDCP.DatabaseConnector.MySql
{
    public class MySql : IDatabase
    {
        public MySqlConnection connection;

        MySqlTransaction activeTransaction;

        string connectionString;
        bool isConnected;
        string database;

        public static bool TestConnection(string server, int port, string database, string user, string pass) {
            string connectionString = "Server=" + server + ";Port=" + port + ";Database=" + database + ";Uid=" + user + ";Pwd=" + pass + ";SslMode=none;";
            try {
                MySqlConnection connection = new MySqlConnection(connectionString);
                connection.Open();
                connection.Close();
                return true;
            } catch (Exception ex) {
                return false;
            }
        }

        /// <summary>
        /// Creates a new database wrapper object that wraps around
        /// the users table.
        /// </summary>
        /// <param name="svr">The name of the server</param>
        /// <param name="db">The database catalog to use</param>
        /// <param name="user">The user name</param>
        /// <param name="pass">The user password</param>
        public MySql(string server, int port, string database, string user, string pass) {
            this.connectionString = "Server=" + server + ";Port=" + port + ";Database=" + database + ";Uid=" + user + ";Pwd=" + pass + ";SslMode=none;";
            this.database = database;

            try {
                connection = new MySqlConnection(this.connectionString);
            } catch (Exception excp) {
                Exception myExcp = new Exception("Error connecting you to " +
                    "the my sql server. Internal error message: " + excp.Message, excp);
                throw myExcp;
            }

            this.isConnected = false;
        }

        /// <summary>
        /// Creates a new database wrapper object that wraps around
        /// the users table.
        /// </summary>
        /// <param name="connStr">A connection string to provide to connect
        /// to the database</param>
        public MySql(string connStr) {
            this.connectionString = connStr;

            try {
                connection = new MySqlConnection(this.connectionString);
            } catch (Exception excp) {
                Exception myExcp = new Exception("Error connecting you to " +
                    "the my sql server. Error: " + excp.Message, excp);

                throw myExcp;
            }

            this.isConnected = false;
        }

        /// <summary>
        /// Opens the connection to the SQL database.
        /// </summary>
        public void OpenConnection() {
            bool success = true;

            if (this.isConnected == false) {
                try {
                    this.connection.Open();
                } catch (Exception excp) {
                    this.isConnected = false;
                    success = false;
                    Exception myException = new Exception("Error opening connection" +
                        " to the sql server. Error: " + excp.Message, excp);

                    throw myException;
                }

                if (success) {
                    this.isConnected = true;
                }
            }
        }

        /// <summary>
        /// Closes the connection to the sql connection.
        /// </summary>
        public void CloseConnection() {
            if (this.isConnected) {
                this.connection.Close();
            }
        }

        /// <summary>
        /// Gets the current state (boolean) of the connection.
        /// True for open, false for closed.
        /// </summary>
        public bool IsConnected {
            get {
                return this.isConnected;
            }
        }

        public string VerifyValueString(string value) {
            if (value == null) {
                return "";
            }

            string newVal;
            newVal = value.Replace("\'", "\'\'");
            newVal = newVal.Replace(@"\", "");

            return newVal;
        }

        /// <summary>
        /// Adds a user into the database
        /// </summary>
        /// <param name="username">The user login</param>
        /// <param name="password">The user password</param>
        public void AddUser(string username, string password) {
            string Query = "INSERT INTO users(usr_name, usr_pass) values" +
                "('" + username + "','" + password + "')";

            MySqlCommand addUser = new MySqlCommand(Query, this.connection);
            AddCommandToTransaction(addUser);
            try {
                addUser.CommandTimeout = 0;
                addUser.ExecuteNonQuery();
            } catch (Exception excp) {
                Exception myExcp = new Exception("Could not add user. Error: " +
                    excp.Message, excp);
                throw (myExcp);
            }
        }

        /// <summary>
        /// Verifies whether a user with the supplied user
        /// credentials exists in the database or not. User
        /// credentials are case-sensitive.
        /// </summary>
        /// <param name="username">The user login</param>
        /// <param name="password">The user password</param>
        /// <returns>A boolean value. True if the user exists
        /// in the database, false if the user does not exist
        /// in the database.</returns>
        public bool VerifyUser(string username, string password) {
            int returnValue = 0;

            string Query = "SELECT COUNT(*) FROM users where (usr_Name=" +
                "'" + username + "' and usr_Pass='" + password + "') LIMIT 1";

            MySqlCommand verifyUser = new MySqlCommand(Query, this.connection);

            try {
                verifyUser.CommandTimeout = 0;
                verifyUser.ExecuteNonQuery();

                MySqlDataReader myReader = verifyUser.ExecuteReader();

                while (myReader.Read() != false) {
                    returnValue = myReader.GetInt32(0);
                }

                myReader.Close();
            } catch (Exception excp) {
                Exception myExcp = new Exception("Could not verify user. Error: " +
                    excp.Message, excp);
                throw (myExcp);
            }

            if (returnValue == 0) {
                return false;
            } else {
                return true;
            }
        }

        /// <summary>
        /// Checks whether a supplied user name exists or not
        /// </summary>
        /// <param name="username">The user name</param>
        /// <returns>True if the username is already in the table,
        /// false if the username is not in the table</returns>
        public bool UserExists(string username) {
            int returnValue = 0;

            string Query = "SELECT COUNT(*) FROM users where (usr_Name=" +
                "'" + username + "') LIMIT 1";

            MySqlCommand verifyUser = new MySqlCommand(Query, this.connection);

            try {
                verifyUser.CommandTimeout = 0;
                verifyUser.ExecuteNonQuery();

                MySqlDataReader myReader = verifyUser.ExecuteReader();

                while (myReader.Read() != false) {
                    returnValue = myReader.GetInt32(0);
                }

                myReader.Close();
            } catch (Exception excp) {
                Exception myExcp = new Exception("Could not verify user. Error: " +
                    excp.Message, excp);
                throw (myExcp);
            }

            if (returnValue == 0) {
                return false;
            } else {
                return true;
            }
        }

        public void CreateTable(string tableName, IDataField[] fields) {
            if (fields.Length > 0) {
                bool localConnection = false;
                if (ConnectionState == System.Data.ConnectionState.Closed) {
                    localConnection = true;
                    OpenConnection();
                }
                try {
                    string command = "CREATE TABLE " + tableName + " (" + fields[0].Name + " " + fields[0].Type;
                    for (int i = 1; i < fields.Length; i++) {
                        command += ", " + fields[i].Name + " " + fields[i].Type;
                    }
                    command += ")";
                    using (MySqlCommand comm = new MySqlCommand(command, connection)) {
                        if (!localConnection) {
                            AddCommandToTransaction(comm);
                        }
                        comm.CommandTimeout = 0;
                        comm.ExecuteNonQuery();
                    }
                } finally {
                    if (localConnection) {
                        CloseConnection();
                    }
                }
            }
        }

        public void DeleteTable(string tableName) {
            throw new NotImplementedException();
        }

        public DataColumnCollection RetrieveRow(string query) {
            if (!string.IsNullOrEmpty(query)) {
                bool localConnection = false;
                if (ConnectionState == System.Data.ConnectionState.Closed) {
                    localConnection = true;
                    OpenConnection();
                }
                try {
                    using (MySqlCommand comm = new MySqlCommand(query, connection)) {
                        comm.CommandTimeout = 0;
                        using (MySqlDataReader reader = comm.ExecuteReader()) {
                            if (reader.HasRows) {
                                if (reader.Read()) {
                                    DataColumnCollection fields = new DataColumnCollection(reader.FieldCount);
                                    for (int i = 0; i < reader.FieldCount; i++) {
                                        fields.SetColumn(i, new DataColumn(i, false, reader.GetName(i), reader.GetValue(i)));
                                    }
                                    return fields;
                                }
                            }
                        }
                    }
                } finally {
                    if (localConnection) {
                        CloseConnection();
                    }
                }
            }
            return null;
        }

        public IDataColumn[] RetrieveRow(string tableName, string columns, string filterExpression) {
            DataColumnCollection data = RetrieveRow("SELECT " + columns + " FROM " + tableName + " WHERE " + filterExpression);
            if (data != null) {
                return data.Columns;
            } else {
                return null;
            }
        }

        public IEnumerable<DataColumnCollection> RetrieveRowsEnumerable(string query) {
            if (!string.IsNullOrEmpty(query)) {
                bool localConnection = false;
                if (ConnectionState == System.Data.ConnectionState.Closed) {
                    localConnection = true;
                    OpenConnection();
                }
                try {
                    using (MySqlCommand comm = new MySqlCommand(query, connection)) {
                        comm.CommandTimeout = 0;
                        using (MySqlDataReader reader = comm.ExecuteReader()) {
                            if (reader.HasRows) {
                                List<DataColumnCollection> databaseRows = new List<DataColumnCollection>();
                                while (reader.Read()) {
                                    DataColumnCollection fields = new DataColumnCollection(reader.FieldCount);
                                    for (int i = 0; i < reader.FieldCount; i++) {
                                        fields.SetColumn(i, new DataColumn(i, false, reader.GetName(i), reader.GetValue(i)));
                                    }
                                    yield return fields;
                                }
                            }
                        }
                    }
                } finally {
                    if (localConnection) {
                        CloseConnection();
                    }
                }
            }
        }

        public IEnumerable<Object[]> RetrieveRowsEnumerableQuick(string query) {
            if (!string.IsNullOrEmpty(query)) {
                bool localConnection = false;
                if (ConnectionState == System.Data.ConnectionState.Closed) {
                    localConnection = true;
                    OpenConnection();
                }
                try {
                    using (MySqlCommand comm = new MySqlCommand(query, connection)) {
                        comm.CommandTimeout = 0;
                        using (MySqlDataReader reader = comm.ExecuteReader()) {
                            if (reader.HasRows) {
                                List<DataColumnCollection> databaseRows = new List<DataColumnCollection>();
                                while (reader.Read()) {
                                    Object[] values = new object[reader.FieldCount];
                                    reader.GetValues(values);
                                    yield return values;
                                }
                            }
                        }
                    }
                } finally {
                    if (localConnection) {
                        CloseConnection();
                    }
                }
            }
        }

        public List<DataColumnCollection> RetrieveRows(string query) {
            if (!string.IsNullOrEmpty(query)) {
                bool localConnection = false;
                if (ConnectionState == System.Data.ConnectionState.Closed) {
                    localConnection = true;
                    OpenConnection();
                }
                try {
                    using (MySqlCommand comm = new MySqlCommand(query, connection)) {
                        comm.CommandTimeout = 0;
                        using (MySqlDataReader reader = comm.ExecuteReader()) {
                            if (reader.HasRows) {
                                List<DataColumnCollection> databaseRows = new List<DataColumnCollection>();
                                while (reader.Read()) {
                                    DataColumnCollection fields = new DataColumnCollection(reader.FieldCount);
                                    for (int i = 0; i < reader.FieldCount; i++) {
                                        fields.SetColumn(i, new DataColumn(i, false, reader.GetName(i), reader.GetValue(i)));
                                    }
                                    databaseRows.Add(fields);
                                }
                                return databaseRows;
                            }
                        }
                    }
                } finally {
                    if (localConnection) {
                        CloseConnection();
                    }
                }
            }
            return null;
        }

        public void AddRow(string tableName, IDataColumn[] columns) {
            if (columns.Length > 0) {
                bool localConnection = false;
                if (ConnectionState == System.Data.ConnectionState.Closed) {
                    localConnection = true;
                    OpenConnection();
                }
                try {
                    string command = "INSERT INTO " + tableName;
                    string columnsToAdd = columns[0].Name;
                    string valuesToAdd = "\'" + VerifyValueString(columns[0].Value.ToString()) + "\'";
                    for (int i = 1; i < columns.Length; i++) {
                        columnsToAdd += ", " + columns[i].Name;
                        valuesToAdd += ", \'" + VerifyValueString(columns[i].Value.ToString()) + "\'";
                    }
                    command += " (" + columnsToAdd + ") VALUES (" + valuesToAdd + ")";
                    using (MySqlCommand comm = new MySqlCommand(command, connection)) {
                        if (!localConnection) {
                            AddCommandToTransaction(comm);
                        }
                        comm.CommandTimeout = 0;
                        comm.ExecuteScalar();
                    }
                } finally {
                    if (localConnection) {
                        CloseConnection();
                    }
                }
            }
        }

        [Obsolete("Obsolete in favor of Dapper-based implementation.", true)]
        public void DeleteRow(string tableName, string filterExpression) {
            bool localConnection = false;
            if (ConnectionState == System.Data.ConnectionState.Closed) {
                localConnection = true;
                OpenConnection();
            }
            try {
                string command = "DELETE FROM " + tableName;
                if (!string.IsNullOrEmpty(filterExpression)) {
                    command += " WHERE " + filterExpression;
                }
                using (MySqlCommand comm = new MySqlCommand(command, connection)) {
                    if (!localConnection) {
                        AddCommandToTransaction(comm);
                    }
                    comm.CommandTimeout = 0;
                    comm.ExecuteNonQuery();
                }
            } finally {
                if (localConnection) {
                    CloseConnection();
                }
            }
        }

        public void DeleteRow(string tableName, string filterExpression, object data) {
            bool localConnection = false;
            if (ConnectionState == System.Data.ConnectionState.Closed) {
                localConnection = true;
                OpenConnection();
            }
            try {
                var queryBuilder = new StringBuilder();
                queryBuilder.Append("DELETE FROM ");
                queryBuilder.Append(tableName);
                if (!string.IsNullOrEmpty(filterExpression)) {
                    queryBuilder.Append(" WHERE ");
                    queryBuilder.Append(filterExpression);
                }

                connection.Execute(queryBuilder.ToString(), data, SelectTransaction(localConnection));
            } finally {
                if (localConnection) {
                    CloseConnection();
                }
            }
        }

        public object ExecuteQuery(string query) {
            if (!string.IsNullOrEmpty(query)) {
                bool localConnection = false;
                if (ConnectionState == System.Data.ConnectionState.Closed) {
                    localConnection = true;
                    OpenConnection();
                }
                try {
                    using (MySqlCommand comm = new MySqlCommand(query, connection)) {
                        if (!localConnection) {
                            AddCommandToTransaction(comm);
                        }
                        comm.CommandTimeout = 0;
                        return comm.ExecuteScalar();
                    }
                } finally {
                    if (localConnection) {
                        CloseConnection();
                    }
                }
            }
            return null;
        }

        public void ExecuteNonQuery(string query) {
            if (!string.IsNullOrEmpty(query)) {
                bool localConnection = false;
                if (ConnectionState == System.Data.ConnectionState.Closed) {
                    localConnection = true;
                    OpenConnection();
                }
                try {
                    using (MySqlCommand comm = new MySqlCommand(query, connection)) {
                        if (!localConnection) {
                            AddCommandToTransaction(comm);
                        }
                        comm.CommandTimeout = 0;
                        comm.ExecuteNonQuery();

                    }
                } finally {
                    if (localConnection) {
                        CloseConnection();
                    }
                }
            }
        }

        public System.Data.ConnectionState ConnectionState {
            get {
                if (connection != null) {
                    return connection.State;
                } else {
                    return System.Data.ConnectionState.Closed;
                }
            }
        }

        public int CountRows(string tableName) {
            throw new NotImplementedException();
        }

        public bool TableExists(string tableName) {
            bool localConnection = false;
            if (ConnectionState == System.Data.ConnectionState.Closed) {
                localConnection = true;
                OpenConnection();
            }
            try {
                string command = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '" + database + "' AND table_name = '" + tableName + "' LIMIT 1";
                using (MySqlCommand cmd = new MySqlCommand(command, connection)) {
                    cmd.CommandTimeout = 0;
                    //cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='" + tableName + "'";
                    cmd.ExecuteNonQuery();

                    int returnValue = 0;

                    using (MySqlDataReader rdr = cmd.ExecuteReader()) {
                        while (rdr.Read() != false) {
                            returnValue = rdr.GetInt32(0);
                        }
                    }

                    if (returnValue == 0) {
                        return false;
                    } else {
                        return true;
                    }
                }
            } finally {
                if (localConnection) {
                    CloseConnection();
                }
            }
        }

        public void UpdateRow(string tableName, IDataColumn[] columns, string filterExpression) {
            bool localConnection = false;
            if (ConnectionState == System.Data.ConnectionState.Closed) {
                localConnection = true;
                OpenConnection();
            }
            try {
                string command = "UPDATE " + tableName;
                string setCommand = " SET " + columns[0].Name + " = \'" + VerifyValueString(columns[0].Value.ToString()) + "\'";
                for (int i = 1; i < columns.Length; i++) {
                    setCommand += ", " + columns[i].Name + " = \'" + VerifyValueString(columns[i].Value.ToString()) + "\'";
                }
                command += setCommand;
                if (!string.IsNullOrEmpty(filterExpression)) {
                    command += " WHERE " + filterExpression;
                }
                using (MySqlCommand cmd = new MySqlCommand(command, connection)) {
                    if (!localConnection) {
                        AddCommandToTransaction(cmd);
                    }
                    cmd.CommandTimeout = 0;
                    cmd.ExecuteNonQuery();
                }
            } finally {
                if (localConnection) {
                    CloseConnection();
                }
            }
        }

        public void UpdateRow(string tableName, IDataColumn[] columns) {
            UpdateRow(tableName, columns, null);
        }

        public void UpdateOrInsert(string tableName, IDataColumn[] columns, string filterExpression) {
            UpdateOrInsert(tableName, columns);
        }

        public void UpdateOrInsert(string tableName, IDataColumn[] columns) {
            bool localConnection = false;
            if (ConnectionState == System.Data.ConnectionState.Closed) {
                localConnection = true;
                OpenConnection();
            }
            try {
                StringBuilder commandBuilder = new StringBuilder();
                commandBuilder.Append("INSERT INTO ");
                commandBuilder.Append(tableName);

                commandBuilder.Append(" SET ");
                commandBuilder.Append(columns[0].Name);
                commandBuilder.Append(" = \'");
                commandBuilder.Append(VerifyValueString(columns[0].Value.ToString()));
                commandBuilder.Append("\'");

                for (int i = 1; i < columns.Length; i++) {
                    commandBuilder.Append(", ");
                    commandBuilder.Append(columns[i].Name);
                    commandBuilder.Append(" = \'");
                    commandBuilder.Append(VerifyValueString(columns[i].Value.ToString()));
                    commandBuilder.Append("\'");
                }

                bool addComma = false;
                commandBuilder.Append(" ON DUPLICATE KEY UPDATE ");
                for (int i = 1; i < columns.Length; i++) {
                    if (!columns[i].PrimaryKey) {
                        if (addComma) {
                            commandBuilder.Append(", ");
                        }
                        commandBuilder.Append(columns[i].Name);
                        commandBuilder.Append(" = \'");
                        commandBuilder.Append(VerifyValueString(columns[i].Value.ToString()));
                        commandBuilder.Append("\'");
                        if (!addComma) {
                            addComma = true;
                        }
                    }
                }
                //string command = "REPLACE INTO " + tableName;
                //string setCommand = " SET " + columns[0].Name + " = \'" + VerifyValueString(columns[0].Value.ToString()) + "\'";
                //for (int i = 1; i < columns.Length; i++) {
                //    setCommand += ", " + columns[i].Name + " = \'" + VerifyValueString(columns[i].Value.ToString()) + "\'";
                //}
                //command += setCommand;
                using (MySqlCommand cmd = new MySqlCommand(commandBuilder.ToString(), connection)) {
                    if (!localConnection) {
                        AddCommandToTransaction(cmd);
                    }
                    cmd.CommandTimeout = 0;
                    cmd.ExecuteNonQuery();
                }
                //if (RetrieveRow(tableName, columns[0].Name /* Old value was "*" */, filterExpression) != null) {
                //    UpdateRow(tableName, columns, filterExpression);
                //} else {
                //    AddRow(tableName, columns);
                //}
            } finally {
                if (localConnection) {
                    CloseConnection();
                }
            }
        }

        public IDataField CreateField(string name, string type) {
            throw new NotImplementedException();
        }

        public IDataColumn CreateColumn(bool primaryKey, string name, string value) {
            if (value == null) {
                value = "";
            }
            return new DataColumn(primaryKey, name, value);
        }

        public string ConnectionString {
            get {
                return this.connectionString;
            }
            set {
                this.connectionString = value;
            }
        }


        public void BeginTransaction() {
            if (connection.State == System.Data.ConnectionState.Open) {
                if (activeTransaction == null) {
                    activeTransaction = connection.BeginTransaction();
                } else {
                    throw new Exception("Another transaction is currently active!");
                }
            }
        }

        public void EndTransaction() {
            if (activeTransaction != null) {
                activeTransaction.Commit();

                activeTransaction.Dispose();
                activeTransaction = null;
            } else {
                throw new Exception("There are no active transactions!");
            }
        }

        private void AddCommandToTransaction(MySqlCommand command) {
            if (activeTransaction != null) {
                command.Transaction = activeTransaction;
            }
        }

        public void UpdateRow(string tableName, IEnumerable<IGenericDataColumn> columns, string filterExpression, object data) {
            bool localConnection = false;
            if (ConnectionState == System.Data.ConnectionState.Closed) {
                localConnection = true;
                OpenConnection();
            }
            try {
                var queryBuilder = new StringBuilder();
                queryBuilder.Append("UPDATE ");
                queryBuilder.Append(tableName);
                queryBuilder.Append(" SET ");

                var enumerator = columns.GetEnumerator();
                // If the enumerable is empty, exit right away - no columns to update
                if (!enumerator.MoveNext()) {
                    return;
                }

                queryBuilder.Append(enumerator.Current.Name);
                queryBuilder.Append(" = @");
                queryBuilder.Append(enumerator.Current.Name);

                while (enumerator.MoveNext()) {
                    queryBuilder.Append(", ");
                    queryBuilder.Append(enumerator.Current.Name);
                    queryBuilder.Append(" = @");
                    queryBuilder.Append(enumerator.Current.Name);
                }

                if (!string.IsNullOrEmpty(filterExpression)) {
                    queryBuilder.Append(" WHERE ");
                    queryBuilder.Append(filterExpression);
                }

                connection.Execute(queryBuilder.ToString(), data, SelectTransaction(localConnection));
            } finally {
                if (localConnection) {
                    CloseConnection();
                }
            }
        }

        public void UpdateRow(string tableName, IEnumerable<IGenericDataColumn> columns, object data) {
            UpdateRow(tableName, columns, null, data);
        }

        public IGenericDataColumn CreateColumn(bool primaryKey, string name) {
            return new GenericDataColumn(name, primaryKey);
        }

        public bool IsTransactionActive {
            get { return activeTransaction != null; }
        }

        private IDbTransaction SelectTransaction(bool isLocalConnection) {
            if (!isLocalConnection && activeTransaction != null) {
                return activeTransaction;
            } else {
                return null;
            }
        }

        public void AddRow(string tableName, IEnumerable<IGenericDataColumn> columns, object data)
        {
            bool localConnection = false;
            if (ConnectionState == System.Data.ConnectionState.Closed)
            {
                localConnection = true;
                OpenConnection();
            }
            try
            {
                var columnsList = new List<IGenericDataColumn>(columns);

                var queryBuilder = new StringBuilder();
                queryBuilder.Append("INSERT INTO ");
                queryBuilder.Append(tableName);
                queryBuilder.Append(" ");

                var enumerator = columnsList.GetEnumerator();
                // If the enumerable is empty, exit right away - no columns to add
                if (!enumerator.MoveNext())
                {
                    return;
                }

                queryBuilder.Append("(");
                queryBuilder.Append(enumerator.Current.Name);

                while (enumerator.MoveNext())
                {
                    queryBuilder.Append(", ");
                    queryBuilder.Append(enumerator.Current.Name);
                }

                queryBuilder.Append(") VALUES (");

                enumerator = columnsList.GetEnumerator();
                // If the enumerable is empty, exit right away - no columns to add
                if (!enumerator.MoveNext())
                {
                    return;
                }
                
                queryBuilder.Append("@");
                queryBuilder.Append(enumerator.Current.Name);

                while (enumerator.MoveNext())
                {
                    queryBuilder.Append(", ");
                    queryBuilder.Append("@");
                    queryBuilder.Append(enumerator.Current.Name);
                }

                queryBuilder.Append(")");

                connection.Execute(queryBuilder.ToString(), data, SelectTransaction(localConnection));
            }
            finally
            {
                if (localConnection)
                {
                    CloseConnection();
                }
            }
        }
    }
}
