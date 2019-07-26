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

namespace PMDCP.DatabaseConnector.SQLite
{
    using System;
    using System.Collections.Generic;
    using System.Data.SQLite;
    using System.Diagnostics;
    using System.Text;

    public class SQLite : IDatabase
    {
        #region Fields

        SQLiteConnection connection;
        string connectionString;

        #endregion Fields

        #region Constructors

        public SQLite(string connectionString) {
            this.connectionString = connectionString;
        }

        public SQLite(string filePath, bool readOnly) {
            this.connectionString = "Data Source=" + filePath + ";Version=3;Read Only=" + readOnly + ";";
        }

        #endregion Constructors

        #region Properties

        public System.Data.ConnectionState ConnectionState {
            get {
                if (connection != null) {
                    return connection.State;
                } else {
                    return System.Data.ConnectionState.Closed;
                }
            }
        }

        public string ConnectionString {
            get {
                return connectionString;
            }
            set {
                connectionString = value;
            }
        }

        #endregion Properties

        #region Methods

        private string VerifyValueString(string value) {
            string newVal;
            newVal = value.Replace("\'", "\'\'");

            return newVal;
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
                    using (SQLiteCommand comm = new SQLiteCommand(command, connection)) {
                        comm.ExecuteScalar();
                    }
                } finally {
                    if (localConnection) {
                        CloseConnection();
                    }
                }
            }
        }

        public void CloseConnection() {
            if (ConnectionState == System.Data.ConnectionState.Open) {
                connection.Dispose();
            }
        }

        public int CountRows(string tableName) {
            return Convert.ToInt32(ExecuteQuery("SELECT COUNT(*) FROM " + tableName));
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
                    using (SQLiteCommand comm = new SQLiteCommand(command, connection)) {
                        comm.ExecuteNonQuery();
                    }
                } finally {
                    if (localConnection) {
                        CloseConnection();
                    }
                }
            }
        }

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
                using (SQLiteCommand comm = new SQLiteCommand(command, connection)) {
                    comm.ExecuteNonQuery();
                }
            } finally {
                if (localConnection) {
                    CloseConnection();
                }
            }
        }

        public void DeleteTable(string tableName) {
            bool localConnection = false;
            if (ConnectionState == System.Data.ConnectionState.Closed) {
                localConnection = true;
                OpenConnection();
            }
            try {
                string command = "DROP TABLE " + tableName;
                using (SQLiteCommand comm = new SQLiteCommand(command, connection)) {
                    comm.ExecuteNonQuery();
                }
            } finally {
                if (localConnection) {
                    CloseConnection();
                }
            }
        }

        public object ExecuteQuery(string command) {
            bool localConnection = false;
            if (ConnectionState == System.Data.ConnectionState.Closed) {
                localConnection = true;
                OpenConnection();
            }
            try {
                using (SQLiteCommand comm = new SQLiteCommand(command, connection)) {
                    return comm.ExecuteScalar();
                }
            } finally {
                if (localConnection) {
                    CloseConnection();
                }
            }
        }

        public void OpenConnection() {
            if (ConnectionState == System.Data.ConnectionState.Closed) {
                connection = new SQLiteConnection(connectionString);
                connection.Open();
            } else {

            }
        }

        public IDataColumn[] RetrieveRow(string tableName, string columns, string filterExpression) {
            bool localConnection = false;
            if (ConnectionState == System.Data.ConnectionState.Closed) {
                localConnection = true;
                OpenConnection();
            }
            try {
                using (SQLiteCommand comm = new SQLiteCommand("SELECT " + columns + " FROM " + tableName + " WHERE " + filterExpression, connection)) {
                    using (SQLiteDataReader reader = comm.ExecuteReader()) {
                        if (reader.HasRows) {
                            if (reader.Read()) {
                                DataColumn[] fields = new DataColumn[reader.FieldCount];
                                for (int i = 0; i < reader.FieldCount; i++) {
                                    fields[i] = new DataColumn(i, false, reader.GetName(i), reader.GetValue(i));
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
            return null;
        }

        public List<IDataColumn[]> RetrieveRows(string tableName, string columns, string filterExpression) {
            bool localConnection = false;
            if (ConnectionState == System.Data.ConnectionState.Closed) {
                localConnection = true;
                OpenConnection();
            }
            try {
                using (SQLiteCommand comm = new SQLiteCommand("SELECT " + columns + " FROM " + tableName + " WHERE " + filterExpression, connection)) {
                    using (SQLiteDataReader reader = comm.ExecuteReader()) {
                        if (reader.HasRows) {
                            List<IDataColumn[]> dataRows = new List<IDataColumn[]>();
                            while (reader.Read()) {
                                DataColumn[] fields = new DataColumn[reader.FieldCount];
                                for (int i = 0; i < reader.FieldCount; i++) {
                                    fields[i] = new DataColumn(i, false, reader.GetName(i), reader.GetValue(i));
                                }
                                dataRows.Add(fields);
                            }
                            return dataRows;
                        }
                    }
                }
            } finally {
                if (localConnection) {
                    CloseConnection();
                }
            }
            return null;
        }

        public bool TableExists(string tableName) {
            bool localConnection = false;
            if (ConnectionState == System.Data.ConnectionState.Closed) {
                localConnection = true;
                OpenConnection();
            }
            try {
                using (SQLiteCommand cmd = new SQLiteCommand(connection)) {
                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='" + tableName + "'";
                    using (SQLiteDataReader rdr = cmd.ExecuteReader()) {
                        if (rdr.HasRows)
                            return true;
                        else
                            return false;
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
                using (SQLiteCommand cmd = new SQLiteCommand(command, connection)) {
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

        #endregion Methods


        public void UpdateOrInsert(string tableName, IDataColumn[] columns, string filterExpression) {
            bool localConnection = false;
            if (ConnectionState == System.Data.ConnectionState.Closed) {
                localConnection = true;
                OpenConnection();
            }
            try {
                if (RetrieveRow(tableName, "*", filterExpression) != null) {
                    UpdateRow(tableName, columns, filterExpression);
                } else {
                    AddRow(tableName, columns);
                }
            } finally {
                if (localConnection) {
                    CloseConnection();
                }
            }
        }


        public IDataField CreateField(string name, string type) {
            return new Field(name, type);
        }


        public IDataColumn CreateColumn(bool primaryKey, string name, string value) {
            return new DataColumn(primaryKey, name, value);
        }


        public void BeginTransaction() {
            throw new NotImplementedException();
        }

        public void EndTransaction() {
            throw new NotImplementedException();
        }

        public void UpdateRow(string tableName, IEnumerable<IGenericDataColumn> columns, string filterExpression, object data) {
            throw new NotImplementedException();
        }

        public void UpdateRow(string tableName, IEnumerable<IGenericDataColumn> columns, object data) {
            throw new NotImplementedException();
        }

        public void UpdateOrInsert(string tableName, IDataColumn[] columns) {
            throw new NotImplementedException();
        }

        public IGenericDataColumn CreateColumn(bool primaryKey, string name) {
            throw new NotImplementedException();
        }

        public void DeleteRow(string tableName, string filterExpression, object data) {
            throw new NotImplementedException();
        }

        public bool IsTransactionActive {
            get { throw new NotImplementedException(); }
        }

        public void AddRow(string tableName, IEnumerable<IGenericDataColumn> columns, object data)
        {
            throw new NotImplementedException();
        }
    }
}
