using System;
using System.Linq;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using Discord;
using Discord.Commands;
using Humanizer;
using Microsoft.Extensions.Logging;
using Modix.Data.Repositories;
using ReverseMarkdown;

namespace Modix.Bot.Modules
{
    [Name("RSS"), Group("feeds"), Summary("Get RSS feeds!")]
    public class RssModule : ModuleBase
    {
        public RssModule(IFeedSubscriptionRepository feeds, ILogger<RssModule> logger)
        {
            Subscriptions = feeds;
            Log = logger;
        }

        private IFeedSubscriptionRepository Subscriptions { get; }

        private ILogger<RssModule> Log { get; }

        [Command("add")]
        public async Task SubscribeAsync(string feedUrl)
        {
            if (feedUrl != null)
            {
                feedUrl = feedUrl.TrimStart('<').TrimEnd('>');
            }

            if (!Uri.TryCreate(feedUrl, UriKind.Absolute, out var url))
            {
                await Context.Channel.SendMessageAsync("That is not a valid feed URL.");
                return;
            }

            Feed feed = null;
            try
            {
                feed = await FeedReader.ReadAsync(url.AbsoluteUri);
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Unable to retrieve feed from the provided URL.");
                await Context.Channel.SendMessageAsync("Unable to retrieve feed from the provided URL.");
            }

            // TODO: Parse the recommended frequency from the feed.
            var frequency = TimeSpan.FromSeconds(10);
            await Subscriptions.CreateAsync(url, frequency);

            await Context.Channel.SendMessageAsync($"A subscription to {url} has been created. It will be checked for new posts every {frequency.Humanize()}.");
        }

        [Command("list")]
        public async Task ListAsync()
        {

            var subscriptions = await Subscriptions.ListAsync();
            if (subscriptions.Count == 0)
            {
                await Context.Channel.SendMessageAsync("There are no active subscriptions.");
                return;
            }

            var builder = new EmbedBuilder()
                .WithTitle("Active Subscriptions")
                .WithCurrentTimestamp();

            foreach (var sub in subscriptions.OrderByDescending(x => x.Id))
            {
                builder = builder.AddField(field => field.WithValue($"[{sub.Id}] <{sub.FeedUrl}>"));
            }

            await Context.Channel.SendMessageAsync(string.Empty, embed: builder.Build());
        }

        [Command("remove")]
        public async Task UnsubscribeAsync(int id)
        {
            await Subscriptions.DeleteAsync(id);
        }

        [Command("rss"), Summary("Retrieve an RSS feed.")]
        public async Task RunAsync([Remainder] string feedUrl)
        {
            if (!Uri.TryCreate(feedUrl, UriKind.Absolute, out var feedUri))
            {
                await Context.Channel.SendMessageAsync("That is not a valid feed URL.");
                return;
            }

            var feed = await FeedReader.ReadAsync(feedUri.AbsoluteUri);

            var post = feed.Items.FirstOrDefault();

            if (post is null)
            {
                await Context.Channel.SendMessageAsync("No posts were found.");
                return;
            }

            var converter = new Converter();
            var description = converter.Convert(post.Description);

            var builder = new EmbedBuilder()
                .WithTitle(post.Title)
                .WithDescription(description)
                .WithUrl(post.Link)
                .WithAuthor(author =>
                    author.WithName(feed.Title)
                        .WithUrl(feed.Link)
                        .WithIconUrl(feed.ImageUrl));

            if (post.PublishingDate != null)
            {
                builder = builder.WithTimestamp(new DateTimeOffset(post.PublishingDate.Value));
            }

            await Context.Channel.SendMessageAsync(string.Empty, embed: builder.Build());
        }
    }
}
