using Microsoft.AspNetCore.SignalR;
using SmartQueue.Core.Interfaces;

namespace SmartQueueAPI.Hubs
{
    public class QueueHub : Hub
    {
        private readonly IEstimationService _estimationService;

        public QueueHub(IEstimationService estimationService)
        {
            _estimationService = estimationService;
        }

        // ── Client se pridruzi redu 
        //Poziva se kad customer otvara na s
        public async Task JoinQueueGroup(int queueId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"queue-{queueId}");

            // salje trenutni status
            var status = await _estimationService.GetQueueStatusAsync(queueId);
            await Clients.Caller.SendAsync("QueueStatusUpdated", status);
        }

        // ── Client leaves a queue group 
        // Poziva se kad je kastomer posluzen ili izlazi iz reda
        public async Task LeaveQueueGroup(int queueId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"queue-{queueId}");
        }

        // ── obavijesti sve klijente u redu
        // poziva se iz ticket kontrolera
        public static async Task NotifyQueueUpdated(
            IHubContext<QueueHub> hubContext,
            IEstimationService estimationService,
            int queueId)
        {
            var status = await estimationService.GetQueueStatusAsync(queueId);
            await hubContext.Clients
                .Group($"queue-{queueId}")
                .SendAsync("QueueStatusUpdated", status);
        }
    }
}


/*Why Groups: Each queue has its own SignalR group (queue-1, queue-2 etc.). 
 * When a ticket is called in Queue 1, 
 * ONLY customers waiting in Queue 1 get the update — not everyone connected to the system.
 * This is efficient and scalable.
Why send status immediately on JoinQueueGroup:
 * When a customer opens the app, they instantly see the 
 * current queue state without waiting for the next update. No extra HTTP call needed.
Why static NotifyQueueUpdated: Controllers can't directly call Hub methods
 * — they use IHubContext<QueueHub> instead. 
 * This static helper wraps that pattern cleanly so TicketController just calls one method.*/