using System.Data.Common;

namespace FStorm
{
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
                            transaction.Commit();
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