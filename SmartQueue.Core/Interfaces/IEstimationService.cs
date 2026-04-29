using SmartQueue.Core.DTOs.QueueDTOs;

namespace SmartQueue.Core.Interfaces
{
    public interface IEstimationService
    {
        // Called when ticket is created → calculates initial estimate
        //pOZIVA SE KADA JE TICKET STVOREN / IZRACUNAVA INICIJALNU PROCJENU VREMENA
        Task<int> CalculateEstimatedWaitAsync(int queueId, int position);

        // POZIVA SE KADA DODDJE DO PROMJENE STATUSA TICKETA / rekalkulira update-anu poziciju
        Task<int> RecalculateForPositionAsync(int queueId, int newPosition);

        // Pozvan kada je ticket gotov / updatea statisticke snapshote za tier 2
        Task UpdateStatSnapshotsAsync(int queueId, double actualServiceMinutes);

        // Vraca full live queue status za SignalR i dashboard
        
        Task<QueueStatusDto> GetQueueStatusAsync(int queueId);
    }
}

/*Interfejs---- Defines the contract without caring about implementation. 
 *  we can swap Tier 1 -> Tier 2 ---> ML without touching a single controller.
 * Controllers only ever see IEstimationService ,a nikad konkretnu klasu*/