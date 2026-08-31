namespace RefReworkServer;

public record RefGPConfig
{
    public bool enable { get; set; } = true;

    public bool refBuysInGPCoins { get; set; } = true;

    public bool refOnlyBuysDogtags { get; set; } = true;

    public bool refAlsoBuysLegaMedals { get; set; } = true;

    public bool blockDogtagSalesToOtherTraders { get; set; } = true;

    public bool blockLegaMedalSalesToOtherTraders { get; set; } = true;

    public int gpCoinRoubleValue { get; set; } = 2500;

    public int legaMedalGpCoinRoubleValue { get; set; } = 9000;

    public required Dev dev { get; set; }

    public record Dev
    {
        public bool showFullError { get; set; }
    }
}
