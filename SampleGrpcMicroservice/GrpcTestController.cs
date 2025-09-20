using GrpcChannelManagerLib.Interfaces;
using GrpcChannelManagerLib.Providers;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace SampleGrpcMicroservice.Controllers
{
    [ApiController]
    [Route("GrpcTest")]
    public class GrpcTestController : ControllerBase
    {
        private readonly IGrpcChannelManager _channelManager;
        private readonly RedisConfigProvider? _redisProvider;

        public GrpcTestController(IGrpcChannelManager channelManager, RedisConfigProvider? redisProvider = null)
        {
            _channelManager = channelManager;
            _redisProvider = redisProvider;
            
            // Subscribe to Redis updates
            if (_redisProvider != null)
            {
                _redisProvider.Subscribe(endpoints =>
                {
                    try
                    {
                        Log.Information("Updating gRPC endpoints from Redis: {Endpoints}", string.Join(", ", endpoints));
                        _channelManager.UpdateEndpoints(endpoints);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to update gRPC endpoints from Redis");
                    }
                });
            }
        }

        [HttpGet("grpc-test")]
        public IActionResult GrpcTest()
        {
            try
            {
                var channel = _channelManager.GetAllChannels().FirstOrDefault();
                return channel != null
                    ? Ok($"Channel ready: {channel.Target}")
                    : NotFound("No active gRPC channels.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "/grpc-test failed");
                return Problem("Failed to retrieve gRPC channel.");
            }
        }

        [HttpPost("update-endpoints")]
        public IActionResult UpdateEndpoints([FromBody] List<string> newEndpoints)
        {
            try
            {
                _channelManager.UpdateEndpoints(newEndpoints);
                _redisProvider?.Publish(newEndpoints); // broadcast via Redis if available
                return Ok("Endpoints updated and broadcasted via Redis.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "/update-endpoints failed");
                return Problem("Failed to update endpoints.");
            }
        }
    }
}
