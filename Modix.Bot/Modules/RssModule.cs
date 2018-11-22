using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Xml;
using Microsoft.SyndicationFeed.Rss;
using Microsoft.SyndicationFeed;
using System.Linq;

namespace Modix.Bot.Modules
{
    [Name("RSS"), Summary("Get RSS feeds!")]
    public class RssModule : ModuleBase
    {
        [Command("rss"), Summary("Retrieve an RSS feed.")]
        public async Task RunAsync([Remainder] string feedUrl)
        {
            using (var client = new HttpClient())
            using (var response = await client.GetAsync(feedUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var xmlReader = XmlReader.Create(stream, new XmlReaderSettings()))
                    {
                        var feedReader = new RssFeedReader(xmlReader);
                        string thumbnailUri = null;
                        string blogTitle = null;
                        string blogUri = null;
                        while (await feedReader.Read())
                        {
                            switch (feedReader.ElementType)
                            {
                                case SyndicationElementType.Category:
                                {
                                    ISyndicationCategory category = await feedReader.ReadCategory();
                                    break;
                                }
                                case SyndicationElementType.Image:
                                {
                                    var image = await feedReader.ReadImage();
                                    thumbnailUri = image.Url.AbsoluteUri;
                                    break;
                                }
                                case SyndicationElementType.Item:
                                {

                                    var item = await feedReader.ReadItem();

                                    var builder = new EmbedBuilder()
                                        .WithTitle(item.Title)
                                        .WithDescription(item.Description)
                                        .WithUrl(item.Id)
                                        .WithTimestamp(item.Published);

                                    var contributor = item.Contributors.FirstOrDefault();
                                    if (contributor != null)
                                    {
                                        builder = builder.WithAuthor(author =>
                                        {
                                            author.WithName(contributor.Name)
                                                .WithUrl(contributor.Uri);

                                            if (thumbnailUri != null)
                                            {
                                                author.WithIconUrl(thumbnailUri);
                                            }
                                        });
                                    }

                                    var embed = builder.Build();
                                    await Context.Channel.SendMessageAsync(string.Empty, embed: embed);

                                    return;
                                }
                                case SyndicationElementType.Link:
                                {
                                    ISyndicationLink link = await feedReader.ReadLink();
                                    break;
                                }
                                case SyndicationElementType.Person:
                                {
                                    ISyndicationPerson person = await feedReader.ReadPerson();
                                    break;
                                }
                                default:
                                {
                                    ISyndicationContent content = await feedReader.ReadContent();

                                    break;
                                }
                            }
                        }
                    }

                }
            }
        }
    }
}
