﻿using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FStorm
{
    public class Connection : IDisposable
    {
        private bool disposedValue;
        private bool isOpen = false;
        private readonly IServiceProvider serviceProvider;

        internal DbConnection connection = null!;
        internal Transaction? transaction;
        

        public Connection(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public Transaction BeginTransaction()
        {
            if (!isOpen) { throw new InvalidOperationException("Connection is not open yet"); }
            if (transaction == null)
            {
                transaction = serviceProvider.GetService<Transaction>()!;
                transaction.connection = this;
                transaction.transaction = connection.BeginTransaction();
                return transaction;
            }
            else
            {
                return transaction;
            }
        }

        public void Open()
        {
            try
            {
                if (!isOpen)
                {
                    connection.Open();
                    isOpen = true;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task OpenAsync()
        {
            try
            {
                if (!isOpen)
                {
                    await connection.OpenAsync();
                    isOpen = true;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Close()
        {
            try
            {
                if (isOpen) 
                { 
                    connection.Close();
                    isOpen = false;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task CloseAsync()
        {
            try
            {
                if (isOpen)
                {
                    await connection.CloseAsync();
                    isOpen = false;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public GetCommand Get(GetRequest configuration)
        {
            var cmd = serviceProvider.GetService<GetCommand>()!;
            cmd.Configuration = configuration;
            cmd.connection= this;
            cmd.transaction= transaction ?? BeginTransaction();
            return cmd;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (transaction != null)
                    {
                        transaction.Dispose();
                    }

                    if (connection != null)
                    {
                        if (isOpen)
                        {
                            connection.Close();
                        }
                        connection.Dispose();
                    }
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class Transaction : IDisposable
    {
        private bool disposedValue;
        internal Connection connection = null!;
        internal bool isCommittedOrRollbacked = false;
        internal DbTransaction transaction = null!;

        public Transaction()
        { }

        public void Commit()
        {
            try
            {
                if (transaction != null && !isCommittedOrRollbacked)
                {
                    transaction.Commit();
                    isCommittedOrRollbacked = true;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Rollback()
        {
            try
            {
                if (transaction != null && !isCommittedOrRollbacked)
                {
                    transaction.Rollback();
                    isCommittedOrRollbacked = true;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (transaction != null)
                    {
                        if (!isCommittedOrRollbacked)
                        {
                            transaction.Rollback();
                        }
                        transaction.Dispose();
                        connection.transaction = null;
                    }
                }
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

}