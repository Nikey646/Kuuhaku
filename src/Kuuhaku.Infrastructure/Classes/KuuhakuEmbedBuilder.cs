using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Kuuhaku.Infrastructure.Extensions;

namespace Kuuhaku.Infrastructure.Classes
{
    public class KuuhakuEmbedBuilder
    {
        private String _title;
        private String _description;
        private String _url;
        private String _image;
        private String _thumbnail;
        private List<EmbedFieldBuilder> _fields;

        public String Title
        {
            get => this._title;
            set
            {
                if (value?.Length > EmbedBuilder.MaxTitleLength)
                    throw new ArgumentException(
                        $"Title length must be less than or equal to {EmbedBuilder.MaxTitleLength}",
                        nameof(this.Title));
                this._title = value;
            }
        }

        public String Description
        {
            get => this._description;
            set
            {
                if (value?.Length > EmbedBuilder.MaxDescriptionLength)
                    throw new ArgumentException(
                        $"Description length must be less than or equal to {EmbedBuilder.MaxDescriptionLength}.",
                        nameof(this.Description));
                this._description = value;
            }
        }

        public String Url
        {
            get => this._url;
            set
            {
                if (!(value.IsEmpty() || Uri.IsWellFormedUriString(value, UriKind.Absolute)))
                    throw new ArgumentException("Url must be a well-formed URI.", nameof(this.Url));
                this._url = value;
            }
        }

        public String ThumbnailUrl
        {
            get => this._thumbnail;
            set
            {
                if (!(value.IsEmpty() || Uri.IsWellFormedUriString(value, UriKind.Absolute)))
                    throw new ArgumentException("Url must be a well-formed URI.", nameof(this.ThumbnailUrl));
                this._thumbnail = value;
            }
        }

        public String ImageUrl
        {
            get => this._image;
            set
            {
                if (!(value.IsEmpty() || Uri.IsWellFormedUriString(value, UriKind.Absolute)))
                    throw new ArgumentException("Url must be a well-formed URI.", nameof(this.ImageUrl));
                this._image = value;
            }
        }

        public List<EmbedFieldBuilder> Fields
        {
            get => this._fields;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(this.Fields),
                        "Cannot set an embed builder's fields collection to null.");
                if (value.Count > EmbedBuilder.MaxFieldCount)
                    throw new ArgumentException(
                        $"Field count must be less than or equal to {EmbedBuilder.MaxFieldCount}.",
                        nameof(this.Fields));
                this._fields = value;
            }
        }

        public DateTimeOffset? Timestamp { get; set; }
        public Color? Color { get; set; }
        public EmbedAuthorBuilder Author { get; set; }
        public EmbedFooterBuilder Footer { get; set; }

        public Int32 Length
        {
            get
            {
                var title = this.Title?.Length ?? 0;
                var author = this.Author?.Name?.Length ?? 0;
                var description = this.Description?.Length ?? 0;
                var footer = this.Footer?.Text?.Length ?? 0;
                var fields = this.Fields.Sum(f => f.Name.Length + f.Value.ToString().Length);

                return title + author + description + footer + fields;
            }
        }

        public KuuhakuEmbedBuilder()
        {
            this._fields = new List<EmbedFieldBuilder>();
        }

        public KuuhakuEmbedBuilder WithTitle(string title)
        {
            this.Title = title;
            return this;
        }

        public KuuhakuEmbedBuilder WithDescription(string description)
        {
            this.Description = description;
            return this;
        }

        public KuuhakuEmbedBuilder WithUrl(string url)
        {
            this.Url = url;
            return this;
        }

        public KuuhakuEmbedBuilder WithThumbnailUrl(string thumbnailUrl)
        {
            this.ThumbnailUrl = thumbnailUrl;
            return this;
        }

        public KuuhakuEmbedBuilder WithImageUrl(string imageUrl)
        {
            this.ImageUrl = imageUrl;
            return this;
        }

        public KuuhakuEmbedBuilder WithCurrentTimestamp()
        {
            this.Timestamp = DateTimeOffset.UtcNow;
            return this;
        }

        public KuuhakuEmbedBuilder WithTimestamp(DateTimeOffset dateTimeOffset)
        {
            this.Timestamp = dateTimeOffset;
            return this;
        }

        public KuuhakuEmbedBuilder WithColor(Color color)
        {
            this.Color = color;
            return this;
        }

        public KuuhakuEmbedBuilder WithAuthor(EmbedAuthorBuilder author)
        {
            this.Author = author;
            return this;
        }

        public KuuhakuEmbedBuilder WithAuthor(Action<EmbedAuthorBuilder> action)
        {
            var author = new EmbedAuthorBuilder();
            action(author);
            this.Author = author;
            return this;
        }

        public KuuhakuEmbedBuilder WithAuthor(string name, string iconUrl = null, string url = null)
        {
            var author = new EmbedAuthorBuilder
            {
                Name = name,
                IconUrl = iconUrl,
                Url = url
            };
            this.Author = author;
            return this;
        }

        public KuuhakuEmbedBuilder WithFooter(EmbedFooterBuilder footer)
        {
            this.Footer = footer;
            return this;
        }

        public KuuhakuEmbedBuilder WithFooter(Action<EmbedFooterBuilder> action)
        {
            var footer = new EmbedFooterBuilder();
            action(footer);
            this.Footer = footer;
            return this;
        }

        public KuuhakuEmbedBuilder WithFooter(string text, string iconUrl = null)
        {
            var footer = new EmbedFooterBuilder
            {
                Text = text,
                IconUrl = iconUrl
            };
            this.Footer = footer;
            return this;
        }

        public KuuhakuEmbedBuilder AddField(string name, object value, bool inline = false)
        {
            var field = new EmbedFieldBuilder()
                .WithIsInline(inline)
                .WithName(name)
                .WithValue(value);
            this.AddField(field);
            return this;
        }

        public KuuhakuEmbedBuilder AddField(Action<EmbedFieldBuilder> action)
        {
            var field = new EmbedFieldBuilder();
            action(field);
            this.AddField(field);
            return this;
        }

        public KuuhakuEmbedBuilder AddField(EmbedFieldBuilder field)
        {
            if (this.Fields.Count >= EmbedBuilder.MaxFieldCount)
            {
                throw new ArgumentException(
                    $"Field count must be less than or equal to {EmbedBuilder.MaxFieldCount}.",
                    nameof(field));
            }

            this.Fields.Add(field);
            return this;
        }

        public Embed Build()
        {
            if (this.Length > EmbedBuilder.MaxEmbedLength)
                throw new InvalidOperationException($"Total embed length must be less than or equal to {EmbedBuilder.MaxEmbedLength}.");

            // Sighs.
            var embed = new EmbedBuilder();
            embed.Title = this.Title;
            embed.Description = this.Description;
            if (!this.Url.IsEmpty())
                embed.Url = this.Url;
            if (!this.ThumbnailUrl.IsEmpty())
                embed.ThumbnailUrl = this.ThumbnailUrl;
            if (!this.ImageUrl.IsEmpty())
                embed.ImageUrl = this.ImageUrl;
            embed.Timestamp = this.Timestamp;
            embed.Color = this.Color;
            embed.Author = this.Author;
            embed.Footer = this.Footer;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < this.Fields.Count; i++)
                embed.AddField(this.Fields[i]);

            return embed.Build();
        }
    }
}
