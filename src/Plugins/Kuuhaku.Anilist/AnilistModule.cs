using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Kuuhaku.Anilist.Models;
using Kuuhaku.Commands.Models;
using Kuuhaku.Commands.Services;
using Kuuhaku.Infrastructure.Classes;
using Kuuhaku.Infrastructure.Extensions;
using Kuuhaku.Infrastructure.Models;
using Newtonsoft.Json;

namespace Kuuhaku.Anilist
{
    public class AnilistModule : KuuhakuModule
    {
        private const String ApiUrl = "https://graphql.anilist.co/";
        private const String AnilistIcon = "https://anilist.co/img/icons/android-chrome-192x192.png";

        private HttpClient _client;
        private readonly InteractiveSearchService _interactiveSearchService;

        public AnilistModule(HttpClient client, InteractiveSearchService interactiveSearchService)
        {
            this._client = client;
            this._interactiveSearchService = interactiveSearchService;
        }

        [Command("anime")]
        public Task AnimeAsync(String searchQuery)
            => this.SearchAsync(searchQuery, "ANIME");

        [Command("manga")]
        public Task MangaAsync(String searchQuery)
            => this.SearchAsync(searchQuery, "MANGA");

        private async Task SearchAsync(String query, String type)
        {
            var requestParams = new
            {
                query = SearchQuery,
                variables = new {query, type},
            };

            var response = await this._client.PostAsync(ApiUrl, new StringContent(JsonConvert.SerializeObject(requestParams), Encoding.UTF8, "application/json"));
            var json = await response.Content.ReadAsStringAsync();

            var searchResultsHolder = JsonConvert.DeserializeAnonymousType(json, new {data = new { Page = new { media = new AnilistSeries[0] }}});
            var searchResults = searchResultsHolder.data.Page.media;

            if (searchResults.Length == 1)
            {
                await this.ReplyAsync(this.MakeEmbed(searchResults[0]));
                return;
            }

            var matchingName = searchResults.FirstOrDefault(s => this.NameMatches(s, query));
            if (matchingName != default)
            {
                await this.ReplyAsync(this.MakeEmbed(matchingName));
                return;
            }

            await this.ListSearchAsync(searchResults, query);
        }

        private Task ListSearchAsync(AnilistSeries[] series, String query)
        {
            var pages = series
                .Take(4)
                .Select(this.MakeEmbed);

            return this._interactiveSearchService
                .HandleSearchAsync(this.Context, (series.Take(4), pages.ToArray()),
                    s => (s.GetTitle(), s.GetDescription(256)),
                    e => e.WithAuthor($"Anilist - Search results for {query}")
                        .WithColor(EmbedColorType.Info).WithFooter(this.Context));
        }

        private KuuhakuEmbedBuilder MakeEmbed(AnilistSeries series)
        {
            var embed = this.Embed
                .WithColor()
                .WithAuthor($"Anilist - {series.GetTitle()}", AnilistIcon, series.SiteUrl)
                .WithDescription(series.GetDescription());

            var isNsfw = (this.Channel as ITextChannel)?.IsNsfw ?? false;

            if (!series.IsAdult || isNsfw)
            {
                embed = series.BannerImage.IsEmpty()
                    ? embed.WithImageUrl(series.GetCover())
                    : embed.WithImageUrl(series.BannerImage).WithThumbnailUrl(series.GetCover());
            }

            var titles = new List<String>(series.Synonyms);
            if (!series.Title.English.IsEmpty())
                titles.Add(series.Title.Romaji);
            if (!series.Title.English.IsEmpty() && !series.Title.Romaji.IsEmpty())
                titles.Add(series.Title.Native);

            titles.Reverse();

            embed = embed.WithFieldIf("Also known as...", titles.Humanize(), false, titles.Count > 0)
                .WithFieldIf("Started on", series.StartDate.GetDate, true, series.StartDate.Year.HasValue)
                .WithFieldIf("Ended on", series.EndDate.GetDate, true, series.EndDate.Year.HasValue)
                .WithFieldIf("Average Score", () => series.AverageScore ?? 0, true, series.AverageScore.HasValue);

            embed = embed.WithFieldIf("Episodes", () => series.Episodes.Value, true, series.Episodes.HasValue)
                .WithFieldIf("Chapters", () => series.Chapters.Value, true, series.Chapters.HasValue)
                .WithFieldIf("Volumes", () => series.Volumes.Value, true, series.Volumes.HasValue)
                .WithField("Status", series.Status.ToLowerInvariant().Humanize(LetterCasing.Title))
                .WithFieldIf("Season", () => $"{series.Season.ToLowerInvariant().Humanize(LetterCasing.Title)} {series.SeasonYear}", true, series.SeasonYear.HasValue)
                .WithFieldIf("Genres", series.Genres.GetAsString(), false, series.Genres.Length > 0)
                .WithFieldIf("Tags", series.Tags.Select(t => t.IsSpoiler ? t.Name.MdSpoiler() : t.Name).GetAsString(), false, series.Tags.Length > 0);

            return embed;
        }

        private Boolean NameMatches(AnilistSeries series, String query)
        {
            return String.Equals(series.Title.English, query, StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(series.Title.Romaji, query, StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(series.Title.Native, query, StringComparison.OrdinalIgnoreCase) ||
                   series.Synonyms.Any(s => String.Equals(s, query, StringComparison.OrdinalIgnoreCase));
        }

        private const String SearchQuery = @"query ($query: String, $type: MediaType) {
  Page {
    media(search: $query, type: $type) {
      id
      title {
        romaji
        english
        native
      }
      coverImage {
        medium
        extraLarge
      }
      bannerImage
      startDate {
        year
        month
        day
      }
      endDate {
        year
        month
        day
      }
      format
      type
      averageScore
      episodes
      chapters
      volumes
      season
      seasonYear
      isAdult
      status
      description(asHtml: false)
      synonyms
      tags {
        name
        rank
        isSpoiler: isMediaSpoiler
      }
      genres
      siteUrl
    }
    pageInfo {
      total
      perPage
      currentPage
      lastPage
      hasNextPage
    }
  }
}
";
    }
}
