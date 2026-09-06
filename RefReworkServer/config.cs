namespace RefReworkServer;

public record RefGPConfig
{
    public bool blockDogtagSalesToOtherTraders { get; set; } = true;

    public int legaMedalBarterGpCost { get; set; } = 100;

    public required Dev dev { get; set; }

    public record Dev
    {
        public bool showFullError { get; set; }
    }
}
