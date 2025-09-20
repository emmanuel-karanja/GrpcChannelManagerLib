namespace GrpcChannelManagerLib.Options;

using System;
using System.Collections.Generic;

public class GrpcServerOptions
{
    private List<string> _endpoints = new();

    public List<string> Endpoints
    {
        get => _endpoints;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(Endpoints));

            var validEndpoints = new List<string>();
            foreach (var ep in value)
            {
                if (string.IsNullOrWhiteSpace(ep))
                    continue;

                var trimmed = ep.Trim();

                if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
                    throw new ArgumentException($"Invalid gRPC endpoint URI: '{ep}'", nameof(Endpoints));

                validEndpoints.Add(trimmed);
            }

            _endpoints = validEndpoints;
        }
    }
}

