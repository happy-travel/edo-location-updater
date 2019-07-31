using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace HappyTravel.Edo.LocationUpdater.Services
{
    public class Host : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => throw new NotImplementedException();


        public Task StopAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
