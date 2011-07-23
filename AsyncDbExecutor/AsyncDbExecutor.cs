﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Contracts;
using System.Reactive.Linq;
using Codeplex.Data.Internal;

namespace Codeplex.Data
{
    public class AsyncDbExecutor : DbExecutor
    {
        public AsyncDbExecutor(string connectionString)
            : base(new SqlConnection(connectionString))
        { }

        public AsyncDbExecutor(SqlConnection connection)
            : base(connection)
        { }

        public AsyncDbExecutor(string connectionString, IsolationLevel isolationLevel)
            : base(new SqlConnection(connectionString), isolationLevel)
        { }

        public AsyncDbExecutor(SqlConnection connection, IsolationLevel isolationLevel)
            : base(connection, isolationLevel)
        { }

        // TODO:Add Contract

        /// <summary>Async Executes and returns the data reader.</summary>
        /// <param name="query">SQL code.</param>
        /// <param name="parameter">PropertyName parameterized to PropertyName. if null then no use parameter.</param>
        /// <param name="commandType">Command Type.</param>
        /// <param name="commandBehavior">Command Behavior.</param>
        /// <returns>Query results.</returns>
        public IObservable<SqlDataReader> ExecuteReaderAsyncRaw(string query, object parameter = null, CommandType commandType = CommandType.Text, CommandBehavior commandBehavior = CommandBehavior.Default)
        {
            Contract.Requires<ArgumentException>(!String.IsNullOrEmpty(query));
            Contract.Ensures(Contract.Result<IObservable<SqlDataReader>>() != null);

            var cmd = (SqlCommand)this.PrepareExecute(query, commandType, parameter);
            return Observable.FromAsyncPattern<SqlDataReader>(
                    (ac, o) => cmd.BeginExecuteReader(ac, o, commandBehavior), cmd.EndExecuteReader)
                .Invoke()
                .Finally(() => cmd.Dispose());
        }

        IEnumerable<IDataRecord> ExecuteReaderAsyncCore(SqlDataReader reader)
        {
            using (reader)
            {
                while (!reader.IsClosed && reader.Read())
                {
                    yield return reader;
                }
            }
        }

        /// <summary>Async Executes and returns the data records.</summary>
        /// <param name="query">SQL code.</param>
        /// <param name="parameter">PropertyName parameterized to PropertyName. if null then no use parameter.</param>
        /// <param name="commandType">Command Type.</param>
        /// <param name="commandBehavior">Command Behavior.</param>
        /// <returns>Query results.</returns>
        public IObservable<IDataRecord> ExecuteReaderAsync(string query, object parameter = null, CommandType commandType = CommandType.Text, CommandBehavior commandBehavior = CommandBehavior.Default)
        {
            Contract.Requires<ArgumentException>(!String.IsNullOrEmpty(query));
            Contract.Ensures(Contract.Result<IObservable<IDataRecord>>() != null);

            return ExecuteReaderAsyncRaw(query, parameter, commandType, commandBehavior)
                .SelectMany(ExecuteReaderAsyncCore);
        }

        IEnumerable<dynamic> ExecuteReaderDynamicAsyncCore(SqlDataReader reader)
        {
            using (reader)
            {
                var record = new DynamicDataRecord(reader); // reference same reader
                while (!reader.IsClosed && reader.Read())
                {
                    yield return record;
                }
            }
        }

        /// <summary>Async Executes and returns the data records enclosing DynamicDataRecord.</summary>
        /// <param name="query">SQL code.</param>
        /// <param name="parameter">PropertyName parameterized to PropertyName. if null then no use parameter.</param>
        /// <param name="commandType">Command Type.</param>
        /// <param name="commandBehavior">Command Behavior.</param>
        /// <returns>Query results. Result type is DynamicDataRecord.</returns>
        public IObservable<dynamic> ExecuteReaderDynamicAsync(string query, object parameter = null, CommandType commandType = CommandType.Text, CommandBehavior commandBehavior = CommandBehavior.Default)
        {
            Contract.Requires<ArgumentException>(!String.IsNullOrEmpty(query));
            Contract.Ensures(Contract.Result<IObservable<dynamic>>() != null);

            return ExecuteReaderAsyncRaw(query, parameter, commandType, commandBehavior)
                .SelectMany(ExecuteReaderDynamicAsyncCore);
        }

        /// <summary>Async Executes and returns the number of rows affected.</summary>
        /// <param name="query">SQL code.</param>
        /// <param name="parameter">PropertyName parameterized to PropertyName. if null then no use parameter.</param>
        /// <param name="commandType">Command Type.</param>
        /// <returns>Rows affected.</returns>
        public IObservable<int> ExecuteNonQueryAsync(string query, object parameter = null, CommandType commandType = CommandType.Text)
        {
            Contract.Requires<ArgumentException>(!String.IsNullOrEmpty(query));
            Contract.Ensures(Contract.Result<IObservable<int>>() != null);

            var cmd = (SqlCommand)this.PrepareExecute(query, commandType, parameter);
            return Observable.FromAsyncPattern<int>(
                    (ac, o) => cmd.BeginExecuteNonQuery(ac, o), cmd.EndExecuteNonQuery)
                .Invoke()
                .Finally(() => cmd.Dispose());
        }

        // TODO:other methods

        /// <summary>Async Executes and mapping objects by ColumnName - PropertyName.</summary>
        /// <typeparam name="T">Mapping target Class.</typeparam>
        /// <param name="query">SQL code.</param>
        /// <param name="parameter">PropertyName parameterized to PropertyName. if null then no use parameter.</param>
        /// <param name="commandType">Command Type.</param>
        /// <returns>Mapped instances.</returns>
        public IObservable<T> SelectAsync<T>(string query, object parameter = null, CommandType commandType = CommandType.Text) where T : new()
        {
            Contract.Requires<ArgumentException>(!String.IsNullOrEmpty(query));
            Contract.Ensures(Contract.Result<IObservable<T>>() != null);

            var accessors = AccessorCache.Lookup(typeof(T));
            return ExecuteReaderAsync(query, parameter, commandType, CommandBehavior.SequentialAccess)
                .Select(dr => SelectCore<T>(dr, accessors));
        }


        /// <summary>Async Executes and mapping objects to ExpandoObject. Object is dynamic accessable by ColumnName.</summary>
        /// <param name="query">SQL code.</param>
        /// <param name="parameter">PropertyName parameterized to PropertyName. if null then no use parameter.</param>
        /// <param name="commandType">Command Type.</param>
        /// <returns>Mapped results(dynamic type is ExpandoObject).</returns>
        public IObservable<dynamic> SelectDynamicAsync(string query, object parameter = null, CommandType commandType = CommandType.Text)
        {
            Contract.Requires<ArgumentException>(!String.IsNullOrEmpty(query));
            Contract.Ensures(Contract.Result<IObservable<dynamic>>() != null);

            return ExecuteReaderAsync(query, parameter, commandType, CommandBehavior.SequentialAccess)
                .Select(SelectDynamicCore);
        }

        // TODO:other methods
    }
}