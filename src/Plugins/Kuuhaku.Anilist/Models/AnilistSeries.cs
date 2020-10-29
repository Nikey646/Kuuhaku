using System;

namespace Kuuhaku.Anilist.Models
{
    public class AnilistSeries
    {
        public Int32 Id { get; set; }
        public AnilistTitles Title { get; set; }
        public AnilistCoverImages CoverImage { get; set; }
        public String Format { get; set; }
        public String Type { get; set; }
        public String BannerImage { get; set; }
        public AnilistFuzzyDate StartDate { get; set; }
        public AnilistFuzzyDate EndDate { get; set; }
        public Double? AverageScore { get; set; }
        public Int32? Episodes { get; set; }
        public Int32? Chapters { get; set; }
        public Int32? Volumes { get; set; }
        public String Season { get; set; }
        public Int32? SeasonYear { get; set; }
        public Boolean IsAdult { get; set; }
        public String Status { get; set; }
        public String Description { get; set; }
        public String[] Synonyms { get; set; }
        public AnilistTag[] Tags { get; set; }
        public String[] Genres { get; set; }
        public String SiteUrl { get; set; }
    }
}
