using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FStorm
{
    public class Connection : IDisposable
    {
        private bool disposedValue;
        private bool isOpen = false;
        private readonly IServiceProvider serviceProvider;
        private readonly ODataService service;
        internal DbConnection DBConnection = null!;
        internal Transaction? transaction;

        public Connection(IServiceProvider serviceProvider, ODataService service)
        {
            this.serviceProvider = serviceProvider;
            this.service = service;
        }

        public Transaction BeginTransaction()
        {
            if (!isOpen) { throw new InvalidOperationException("Connection is not open yet"); }
            if (transaction == null)
            {
                transaction = serviceProvider.GetService<Transaction>()!;
                transaction.connection = this;
                transaction.transaction = DBConnection.BeginTransaction();
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
                    DBConnection.Open();
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
                    await DBConnection.OpenAsync();
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
                    DBConnection.Close();
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
                    await DBConnection.CloseAsync();
                    isOpen = false;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Create a new Command from the odata uri request passed as parameter. UriRequest is relative to the odata root uri and shoul not start with /
        /// </summary>
        /// <param name="uriRequest">the relative part of the odata request</param>
        /// <returns></returns>
        public Command Get(string uriRequest)
        {
            return Get(uriRequest, null);
        }

        /// <summary>
        /// Create a new Command from the odata uri request passed as parameter. UriRequest is relative to the odata root uri and shoul not start with /
        /// </summary>
        /// <param name="uriRequest">the relative part of the odata request</param>
        /// <returns></returns>
        public Command Get(string uriRequest, CommandOptions? options)
        {
            var cmd = serviceProvider.GetService<Command>()!;
            cmd.UriRequest = uriRequest;
            cmd.connection= this;
            cmd.transaction= transaction ?? BeginTransaction();
            cmd.CommandTimeout = options?.CommandTimeout ?? this.service.options.DefaultCommandTimeout; 
            cmd.DefaultTopRequest = options?.DefaultTopRequest ?? this.service.options.DefaultTopRequest; 
            cmd.BypassDefaultTopRequest = options?.BypassDefaultTopRequest ?? false; 
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

                    if (DBConnection != null)
                    {
                        if (isOpen)
                        {
                            DBConnection.Close();
                        }
                        DBConnection.Dispose();
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
}