namespace Wism.Client.Data.Entities.Location
{
    public class SearchRuinsCommandEntity : SearchLocationCommandEntity
    {
        public BoonEntity Boon { get; set; }

        public int[] AllyIdsResult { get; set; }

        public string ArtifactShortNameResult { get; set; }

        public int? StrengthResult { get; set; }

        public int? GoldResult { get; set; }
    }
}