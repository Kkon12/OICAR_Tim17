using SmartQueue.Core.DTOs.QueueDTOs;

namespace SmartQueue.Core.Interfaces
{
    public interface IEstimationService
    {
        // Called when ticket is created → calculates initial estimate
        Task<int> CalculateEstimatedWaitAsync(int queueId, int position);

        // Called when ticket status changes → recalculate for updated position
        Task<int> RecalculateForPositionAsync(int queueId, int newPosition);

        // Called when ticket is completed → updates stat snapshots for Tier 2
        Task UpdateStatSnapshotsAsync(int queueId, double actualServiceMinutes);

        // Returns full live queue status for SignalR and dashboard
        Task<QueueStatusDto> GetQueueStatusAsync(int queueId);
    }
}

/*Why an interface: Defines the contract without caring about implementation. 
 * This means we can swap Tier 1 → Tier 2 → ML without touching a single controller.
 * Controllers only ever see IEstimationService — never the concrete class.*/