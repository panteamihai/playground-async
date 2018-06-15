namespace AsyncWorkshop.UsagePatterns.Services
{
    public interface IMediaPathService
    {
        string Source { get; set; }

        string Destination { get; }

        void ClearDestination();
    }
}
