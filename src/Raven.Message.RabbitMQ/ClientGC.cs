using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Raven.Message.RabbitMQ
{
    internal class ClientGC
    {
        Timer _timer;
        List<Client> _disposingClients = new List<Client>();
        bool _gcCalled = false;
        Dictionary<Client, ClientStats> _clientStats = new Dictionary<Client, ClientStats>();

        static Lazy<ClientGC> lazyInstance = new Lazy<ClientGC>();
        public static ClientGC Instance
        {
            get
            {
                return lazyInstance.Value;
            }
        }

        internal void Add(Client client)
        {
            if (client == null)
                return;
            if (_gcCalled)
            {
                client.PrepareToDispose();
                return;
            }
            lock (_disposingClients)
            {
                _disposingClients.Add(client);
                client.Consumer.ConsumerWorked += Consumer_ConsumerWorked;
                client.Producer.ProducerWorked += Producer_ProducerWorked;
            }
            if (_timer == null)
            {
                lock (this)
                {
                    if (_timer == null)
                    {
                        _timer = new Timer(CheckClient, null, 60000, 60000);
                    }
                }
            }
        }

        internal void Dispose()
        {
            try
            {
                _gcCalled = true;
                DoDispose(false);
            }
            catch
            {

            }
        }

        private void CheckClient(object o)
        {
            DoDispose(true);
        }

        private void DoDispose(bool checkAlive)
        {
            List<Client> copy = CopyDisposingClients();
            if (copy == null)
                return;
            foreach (Client client in copy)
            {
                if (checkAlive && IsAlive(client))
                {
                    continue;
                }
                client.Consumer.ConsumerWorked -= Consumer_ConsumerWorked;
                client.Producer.ProducerWorked -= Producer_ProducerWorked;
                client.PrepareToDispose();
                lock (_disposingClients)
                {
                    _disposingClients.Remove(client);
                }
                lock (_clientStats)
                {
                    _clientStats.Remove(client);
                }
            }
        }

        private List<Client> CopyDisposingClients()
        {
            List<Client> copy = null;
            lock (_disposingClients)
            {
                if (_disposingClients.Count > 0)
                {
                    copy = new List<Client>(_disposingClients);
                }
            }
            return copy;
        }

        private bool IsAlive(Client client)
        {
            ClientStats stats = GetClientStats(client);
            bool alive = stats.ProducerWorked > 0 || stats.ConsumerWorked > 0;
            if (alive)
            {
                stats.Reset();
            }
            return alive;
        }

        private void Producer_ProducerWorked(object sender, EventArgs e)
        {
            Producer producer = sender as Producer;
            Client client = _disposingClients.Find(c => c.Producer == producer);
            if (client == null)
                return;
            ClientStats stats = GetClientStats(client);
            stats.IncreProducerWorked();
        }

        private void Consumer_ConsumerWorked(object sender, EventArgs e)
        {
            Consumer consumer = sender as Consumer;
            Client client = _disposingClients.Find(c => c.Consumer == consumer);
            if (client == null)
                return;
            ClientStats stats = GetClientStats(client);
            stats.IncreConsumerWorked();
        }

        private ClientStats GetClientStats(Client client)
        {
            if (!_clientStats.ContainsKey(client))
            {
                lock (_clientStats)
                {
                    if (!_clientStats.ContainsKey(client))
                    {
                        ClientStats s = new ClientStats();
                        _clientStats.Add(client, s);
                    }
                }
            }
            return _clientStats[client];
        }

        class ClientStats
        {
            public int ProducerWorked;

            public int ConsumerWorked;

            public void Reset()
            {
                Interlocked.Exchange(ref ProducerWorked, 0);
                Interlocked.Exchange(ref ConsumerWorked, 0);
            }

            public void IncreProducerWorked()
            {
                Interlocked.Increment(ref ProducerWorked);
            }

            public void IncreConsumerWorked()
            {
                Interlocked.Increment(ref ConsumerWorked);
            }
        }
    }
}
