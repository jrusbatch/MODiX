using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Modix.Data.Repositories
{
    public class FeedSubscription : IEquatable<FeedSubscription>
    {
        public int Id { get; set; }

        public Uri FeedUrl { get; set; }

        public DateTimeOffset? LastChecked { get; set; }
        public TimeSpan Frequency { get; set; }

        public bool Equals(FeedSubscription other)
        {
            return !(other is null) && FeedUrl.Equals(other.FeedUrl);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FeedSubscription);
        }

        public override int GetHashCode()
        {
            return FeedUrl.GetHashCode();
        }
    }

    public interface IFeedSubscriptionRepository
    {
        Task<FeedSubscription> GetAsync(int id);

        Task<IReadOnlyList<FeedSubscription>> ListAsync();

        Task CreateAsync(Uri feed, TimeSpan frequency);

        Task DeleteAsync(int feedId);
    }

    public class FeedSubscriptionRepository : IFeedSubscriptionRepository
    {
        private int nextId = 0;
        private readonly Dictionary<int, FeedSubscription> feeds =
            new Dictionary<int, FeedSubscription>();

        public Task<FeedSubscription> GetAsync(int id)
        {
            feeds.TryGetValue(id, out var feed);

            return Task.FromResult(feed);
        }

        public Task<IReadOnlyList<FeedSubscription>> ListAsync()
        {
            IReadOnlyList<FeedSubscription> result = Array.AsReadOnly(feeds.Values.OrderBy(x => x.Id).ToArray());

            return Task.FromResult(result);
        }

        public Task CreateAsync(Uri feedUrl, TimeSpan frequency)
        {
            var id = Interlocked.Increment(ref nextId);
            var feed = new FeedSubscription { Id = id, FeedUrl = feedUrl, Frequency = frequency };

            feeds.Add(id, feed);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(int feedId)
        {
            feeds.Remove(feedId);

            return Task.CompletedTask;
        }
    }
}
