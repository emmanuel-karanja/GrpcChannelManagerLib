using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using GrpcChannelManagerLib.Services;
using GrpcChannelManagerLib.Options;
using Grpc.Net.Client;
using System.Linq;

namespace GrpcChannelManagerLib.Tests
{
    public class GrpcChannelManagerTests
    {
        private readonly GrpcChannelManager _manager;

        public GrpcChannelManagerTests()
        {
            // Mock IOptionsMonitor<GrpcServerOptions>
            var optionsMock = new Mock<IOptionsMonitor<GrpcServerOptions>>();
            optionsMock.Setup(o => o.CurrentValue).Returns(new GrpcServerOptions
            {
                Endpoints = new List<string> { "https://localhost:5001" }
            });

            // Mock ILogger
            var loggerMock = new Mock<ILogger<GrpcChannelManager>>();

            // Instantiate manager with mocks
            _manager = new GrpcChannelManager(optionsMock.Object, loggerMock.Object);
        }

        [Fact]
        public void GetOrCreateChannel_ShouldReturnSameChannelForSameEndpoint()
        {
            string endpoint = "https://localhost:5001";

            var channel1 = _manager.GetOrCreateChannel(endpoint);
            var channel2 = _manager.GetOrCreateChannel(endpoint);

            Assert.Same(channel1, channel2);
        }

        [Fact]
        public void GetOrCreateChannel_ShouldReturnDifferentChannelsForDifferentEndpoints()
        {
            var channel1 = _manager.GetOrCreateChannel("https://localhost:5001");
            var channel2 = _manager.GetOrCreateChannel("https://localhost:5002");

            Assert.NotSame(channel1, channel2);
        }

       [Fact]
        public void GetOrCreateChannel_InvalidEndpoint_ShouldThrow()
        {
            Assert.Throws<UriFormatException>(() => _manager.GetOrCreateChannel(""));
        }

        [Fact]
        public void RemoveChannel_ShouldDisposeChannelAndRemoveIt()
        {
            string endpoint = "https://localhost:5001";
            var channel = _manager.GetOrCreateChannel(endpoint);

            _manager.RemoveChannel(endpoint);

            Assert.DoesNotContain(channel, _manager.GetAllChannels());
        }

        [Fact]
        public void GetAllChannels_ShouldReturnAllCreatedChannels()
        {
            var endpoints = new[] { "https://localhost:5001", "https://localhost:5002" };
            foreach (var ep in endpoints) _manager.GetOrCreateChannel(ep);

            var channels = _manager.GetAllChannels();

            Assert.Equal(endpoints.Length, new List<GrpcChannel>(channels).Count);
        }

        [Fact]
        public void UpdateEndpoints_ShouldAddNewAndRemoveMissingEndpoints()
        {
            var initial = new[] { "https://localhost:5001", "https://localhost:5002" };
            var newEndpoints = new[] { "https://localhost:5002", "https://localhost:5003" };

            // Create initial channels
            foreach (var ep in initial) _manager.GetOrCreateChannel(ep);

            // Update endpoints
            _manager.UpdateEndpoints(newEndpoints);

            // Get all channels and convert their Target (Uri) to string
            var addresses = _manager.GetAllChannels()
                                    .Select(ch => ch.Target.ToString())
                                    .ToList();

            Assert.Contains("localhost:5002", addresses);
            Assert.Contains("localhost:5003", addresses);
            Assert.DoesNotContain("localhost:5001", addresses);
        }

    }
}
