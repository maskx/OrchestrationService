﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace maskx.OrchestrationService.SQL
{
    public class DbAccess : IDisposable
    {
        private readonly DbProviderFactory providerFactory;
        private DbConnection connection;
        private readonly DbCommand command;

        private enum RetryAction
        {
            None = 0,
            Reconnect = 1,
            RefreshParameters = 2
        }

        public DbAccess(string connectionString) : this(SqlClientFactory.Instance, connectionString)
        {
        }

        public DbAccess(DbProviderFactory dbProviderFactory, string connectionString)
        {
            providerFactory = dbProviderFactory;

            connection = providerFactory.CreateConnection();
            connection.ConnectionString = connectionString;
            this.command = connection.CreateCommand();
        }

        public DbAccess AddStatement(string sql, IDictionary<string, object> parameters = null)
        {
            this.command.AddStatement(sql, parameters);
            return this;
        }

        public DbAccess AddStatement(string sql, object parameters)
        {
            this.command.AddStatement(sql, parameters);
            return this;
        }

        public async Task<object> ExecuteScalarAsync()
        {
            if (string.IsNullOrEmpty(this.command.CommandText))
                return null;
            await this.connection.OpenAsync();
            object obj;
            obj = await ExcuteWithRetry<object>(() => this.command.ExecuteScalarAsync());
            this.connection.Close();
            return obj;
        }

        public async Task ExecuteReaderAsync(Action<DbDataReader> dataReader)
        {
            if (string.IsNullOrEmpty(this.command.CommandText))
                return;
            await this.connection.OpenAsync();
            using (DbDataReader reader = await ExcuteWithRetry<DbDataReader>(() => this.command.ExecuteReaderAsync()))
            {
                dataReader?.Invoke(reader);
            }
            this.connection.Close();
        }

        public async Task ExecuteReaderAsync(Action<DbDataReader, int> dataReaders, bool bulkRead = false)
        {
            if (string.IsNullOrEmpty(this.command.CommandText))
                return;
            await this.connection.OpenAsync();
            using (DbDataReader reader = await ExcuteWithRetry<DbDataReader>(() => this.command.ExecuteReaderAsync()))
            {
                if (dataReaders != null)
                {
                    int resultSet = 0;
                    do
                    {
                        if (dataReaders != null)
                            if (bulkRead)
                                dataReaders(reader, resultSet);
                            else
                                while (await reader.ReadAsync())
                                    dataReaders(reader, resultSet);

                        resultSet++;
                    } while (await reader.NextResultAsync());
                }
            }
            this.connection.Close();
        }

        private void ReConnect()
        {
            if (connection != null)
                if (connection.State == ConnectionState.Closed)
                    connection.Open();
                else
                {
                    OnReconnecting();
                    connection.Close();
                    connection.Open();
                }
        }

        public async Task<int> ExecuteNonQueryAsync()
        {
            if (string.IsNullOrEmpty(this.command.CommandText))
                return 0;
            await this.connection.OpenAsync();
            int recordsAffected;
            recordsAffected = await ExcuteWithRetry<int>(() => this.command.ExecuteNonQueryAsync());
            this.connection.Close();
            return recordsAffected;
        }

        private async Task<T> ExcuteWithRetry<T>(Func<Task<T>> func)
        {
            for (int retry = 2; ; retry--)
            {
                try
                {
                    return await func();
                }
                catch (Exception e)
                {
                    if (retry <= 0)
                        throw;

                    switch (OnContextLost(e))
                    {
                        case RetryAction.Reconnect:
                            ReConnect();
                            break;

                        case RetryAction.RefreshParameters:
                            throw;
                        default:
                            throw;
                    }
                }
            }
        }

        private RetryAction OnContextLost(Exception dbException)
        {
            var retryAction = RetryAction.None;
            if (connection is SqlConnection)
            {
                if (!(dbException is SqlException e))
                    retryAction = RetryAction.None;
                else
                    switch (e.Number)   // sys.messages
                    {
                        case 233:
                        case -2:
                        case 10054:
                            retryAction = RetryAction.Reconnect;
                            break;

                        case 201:       // Procedure or function '%.*ls' expects parameter '%.*ls', which was not supplied.
                        case 206:       // Operand type clash: %ls is incompatible with %ls.
                        case 257:       // Implicit conversion from data type %ls to %ls is not allowed. Use the CONVERT function to run this query.
                        case 8144:      // Procedure or function %.*ls has too many arguments specified.
                            retryAction = RetryAction.RefreshParameters;
                            break;
                        // To add other cases
                        default:
                            retryAction = RetryAction.None;
                            break;
                    }
            }
            return retryAction;
        }

        private void OnReconnecting()
        {
            if (!(connection is SqlConnection conn))
                return;

            SqlConnection.ClearPool(conn);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && connection != null)
            {
                if (connection.State != ConnectionState.Closed)
                {
                    connection.Dispose();          // Close()
                }

                connection = null;
            }
        }

        #endregion IDisposable Members
    }
}