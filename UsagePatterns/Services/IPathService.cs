namespace AsyncWorkshop.UsagePatterns.Services
{
    public interface IPathService
    {
        string Source { get; set; }

        string Destination { get; }

        string Utility { get; }

        void ClearDestination();

        void ClearStandByList();
    }
}
