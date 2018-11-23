using System;
using System.Linq;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using Discord;
using Discord.Commands;

namespace Modix.Bot.Modules
{
    [Name("RSS"), Summary("Get RSS feeds!")]
    public class RssModule : ModuleBase
    {
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

            var builder = new EmbedBuilder()
                .WithTitle(post.Title)
                .WithDescription(post.Description)
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
