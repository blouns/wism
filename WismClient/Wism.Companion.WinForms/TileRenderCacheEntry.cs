public class TileRenderCacheEntry
{
    public string TerrainType { get; set; }
    public bool HasCity { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is TileRenderCacheEntry other)
        {
            return TerrainType == other.TerrainType && HasCity == other.HasCity;
        }
        return false;
    }

    public override int GetHashCode() =>
        (TerrainType, HasCity).GetHashCode();
}
