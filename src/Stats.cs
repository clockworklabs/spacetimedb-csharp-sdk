using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SpacetimeDB
{
    /// <summary>
    /// Used for tracking the duration of network requests that were made by the local client.
    /// </summary>
    public class NetworkRequestTracker
    {
        private readonly ConcurrentQueue<(DateTime timestamp, TimeSpan duration)> _requestDurations = new ConcurrentQueue<(DateTime, TimeSpan)>();
        private readonly Dictionary<uint, DateTime> _requests = new Dictionary<uint, DateTime>();
        private uint _nextRequestId;
        
        public uint StartTrackingRequest()
        {
            // Record the start time of the request
            _requests[++_nextRequestId] = DateTime.Now;
            return _nextRequestId;
        }
        
        public void FinishTrackingRequest(uint requestId)
        {
            if (!_requests.TryGetValue(requestId, out var startTime))
            {
                throw new InvalidOperationException("No such request ID: " + requestId);
            }
            
            // Calculate the duration and add it to the queue
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;
            _requestDurations.Enqueue((endTime, duration));
            Cleanup();
        }

        private void Cleanup()
        {
            // Remove entries older than 10 minutes
            var cutoff = DateTime.UtcNow.AddMinutes(-10);
            while (_requestDurations.TryPeek(out var entry) && entry.timestamp < cutoff)
            {
                _requestDurations.TryDequeue(out _);
            }
        }

        public (TimeSpan min, TimeSpan max) GetMinMaxTimes(int lastMinutes)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-lastMinutes);
            if (!_requestDurations.Where(x => x.timestamp >= cutoff).Select(x => x.duration).Any())
            {
                return (TimeSpan.Zero, TimeSpan.Zero);
            }

            var min = _requestDurations.Where(x => x.timestamp >= cutoff).Select(x => x.duration).Min();
            var max = _requestDurations.Where(x => x.timestamp >= cutoff).Select(x => x.duration).Max();

            return (min, max);
        }
    }
    
    public class Stats
    {
        public NetworkRequestTracker ReducerRequestTracker = new NetworkRequestTracker();
        public NetworkRequestTracker SubscriptionRequestTracker = new NetworkRequestTracker();
        public NetworkRequestTracker OneOffQueryRequestTracker = new NetworkRequestTracker();
    }    
}
