using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using GrpcChannelManagerLib.Options;
using Grpc.Net.Client;
using GrpcChannelManagerLib.Services;

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
                Endpoints = new List<string> { "https://localhost:5001" } // Valid URI
            });

            // Mock ILogger
            var loggerMock = new Mock<ILogger<GrpcChannelManager>>();

            // Instantiate manager with mocks
            _manager = new GrpcChannelManager(optionsMock.Object, loggerMock.Object);
        }

        [Fact]
        public void GrpcServerOptions_InvalidEndpoint_ShouldThrowArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                var options = new GrpcServerOptions
                {
                    Endpoints = new List<string> { "invalid-url" } // Invalid URI
                };
            });

            Assert.Contains("Invalid gRPC endpoint URI", ex.Message);
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
        public void GetOrCreateChannel_InvalidEndpoint_ShouldThrowArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _manager.GetOrCreateChannel("invalid-url"));
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

            var channels = _manager.GetAllChannels()
                                   .Select(channels => channels.Target)
                                   .ToList();

            Assert.Equal(endpoints.Length, channels.Count());
        }

        [Fact]
        public void UpdateEndpoints_ShouldAddNewAndRemoveMissingEndpoints()
        {
            var initial = new[] { "https://localhost:5001", "https://localhost:5002" };
            var newEndpoints = new[] { "https://localhost:5002", "https://localhost:5003" };

            // Create initial channels
            foreach (var ep in initial)
                _manager.GetOrCreateChannel(ep);

            // Update endpoints
            _manager.UpdateEndpoints(newEndpoints);

            // Normalize addresses from channels
            var addresses = _manager.GetAllChannels()
                                   .Select(channels => channels.Target)
                                   .ToList();
            var expected = newEndpoints
                                    .Select(ep => new Uri(ep).Authority)  // "localhost:5001" from "https://localhost:5001"
                                    .ToList();

            foreach (var ep in expected)
                Assert.Contains(ep, addresses);

            // Ensure removed endpoint is not present
            Assert.DoesNotContain("https://localhost:5001", addresses);
        }

        [Fact]
        public void Constructor_ShouldInitializeChannelsForAllValidEndpoints()
        {
            var endpoints = new List<string>
            {
                "https://localhost:5001",
                "https://localhost:5002"
            };

            var optionsMock = new Mock<IOptionsMonitor<GrpcServerOptions>>();
            optionsMock.Setup(o => o.CurrentValue).Returns(new GrpcServerOptions
            {
                Endpoints = endpoints
            });

            var loggerMock = new Mock<ILogger<GrpcChannelManager>>();
            var manager = new GrpcChannelManager(optionsMock.Object, loggerMock.Object);

            // Get the targets from channels (host:port)
            var addresses = manager.GetAllChannels()
                                .Select(ch => ch.Target)  // Target is already "host:port"
                                .ToList();

            // Convert expected endpoints to host:port for comparison
            var expected = endpoints
                        .Select(ep => new Uri(ep).Authority)  // "localhost:5001" from "https://localhost:5001"
                        .ToList();

            foreach (var ep in expected)
            {
                Assert.Contains(ep, addresses);
            }

        }
    }
}
