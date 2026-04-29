import requests
import time
import random
import urllib3

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# ----------------------------------------------------------------------
# CONFIGURATION
# ----------------------------------------------------------------------
API_BASE       = "http://localhost:5179"
ADMIN_EMAIL    = "admin@smartqueue.com"
ADMIN_PASSWORD = "Admin123!"

# Queue to simulate. Seeder creates:
#   Queue 1 - Opca medicina    (default 7 min, 2 open counters)
#   Queue 2 - Blagajna         (default 3 min, 1 open counter)
#   Queue 3 - Info salter      (default 5 min, 1 open counter)
QUEUE_ID = 1

# How many tickets to take during the simulation
NUM_TICKETS = 25

# Simulated service time range in seconds (not real minutes -- speeds up the demo)
MIN_SERVICE_SEC = 180  #default je bilo 3 ,180 3 min
MAX_SERVICE_SEC = 420 #=7min  

# Pause between each new ticket being taken
TICKET_DELAY_SEC = 1
# ----------------------------------------------------------------------


def login():
    print("Logging in...")
    r = requests.post(
        f"{API_BASE}/api/Auth/login",
        json={"email": ADMIN_EMAIL, "password": ADMIN_PASSWORD},
        verify=False,
        timeout=10
    )
    if r.status_code != 200:
        print(f"  Login failed: {r.status_code} {r.text[:200]}")
        return None

    data = r.json()
    # AuthResponseDto returns field "Token" (capital T)
    token = data.get("Token") or data.get("token") or data.get("accessToken")
    if not token:
        print(f"  Token not found in response. Keys: {list(data.keys())}")
        return None

    print(f"  Logged in as {data.get('FirstName')} {data.get('LastName')} ({data.get('Role')})")
    return token


def get_queue_info(headers, queue_id):
    r = requests.get(
        f"{API_BASE}/api/Queue/{queue_id}",
        headers=headers,
        verify=False,
        timeout=10
    )
    if r.status_code == 200:
        return r.json()
    return None


def get_counters(headers, queue_id):
    # GET /api/Counter/queue/{queueId}
    r = requests.get(
        f"{API_BASE}/api/Counter/queue/{queue_id}",
        headers=headers,
        verify=False,
        timeout=10
    )
    if r.status_code == 200:
        return r.json()
    return []


def open_counter(headers, counter_id):
    # PATCH /api/Counter/{id}/open
    r = requests.patch(
        f"{API_BASE}/api/Counter/{counter_id}/open",
        headers=headers,
        verify=False,
        timeout=10
    )
    return r.status_code == 200


def take_ticket(headers, queue_id):
    # POST /api/Ticket/take
    # Body: TakeTicketDto { QueueId, UserId? }
    # Public endpoint -- no auth needed, but we pass token anyway
    r = requests.post(
        f"{API_BASE}/api/Ticket/take",
        json={"queueId": queue_id, "userId": None},
        headers=headers,
        verify=False,
        timeout=10
    )
    if r.status_code == 200:
        return r.json()
    print(f"  take_ticket failed: {r.status_code} {r.text[:150]}")
    return None


def call_ticket(headers, ticket_id, counter_id):
    # PATCH /api/Ticket/{id}/call
    # Body: UpdateTicketStatusDto { Status, CounterId? }
    # Requires Admin or Djelatnik role
    r = requests.patch(
        f"{API_BASE}/api/Ticket/{ticket_id}/call",
        json={"status": "Called", "counterId": counter_id},
        headers=headers,
        verify=False,
        timeout=10
    )
    return r.status_code == 200


def complete_ticket(headers, ticket_id):
    # PATCH /api/Ticket/{id}/complete
    # No body required
    # Requires Admin or Djelatnik role
    # This is where UpdateStatSnapshotsAsync is called internally
    r = requests.patch(
        f"{API_BASE}/api/Ticket/{ticket_id}/complete",
        headers=headers,
        verify=False,
        timeout=10
    )
    return r.status_code == 200


def get_estimate(headers, queue_id):
    # GET /api/Queue/{id}/estimate
    # Returns QueueStatusDto which includes AverageServiceMinutes
    r = requests.get(
        f"{API_BASE}/api/Queue/{queue_id}/estimate",
        headers=headers,
        verify=False,
        timeout=10
    )
    if r.status_code == 200:
        return r.json()
    return None


def tier_description(completed_count, min_tickets_for_stats=20):
    if completed_count == 0:
        return "Tier 1 -- admin default only, no real data yet"
    elif completed_count < min_tickets_for_stats:
        weight = int(completed_count / min_tickets_for_stats * 100)
        return (f"Tier 1 -- blending: {weight}% real data + "
                f"{100 - weight}% admin default ({completed_count}/{min_tickets_for_stats})")
    else:
        return f"Tier 1 -- fully real data ({completed_count} tickets completed)"


def run():
    print("=" * 62)
    print("  SmartQueue Estimation Service Simulator")
    print(f"  Queue: {QUEUE_ID}  |  Tickets: {NUM_TICKETS}")
    print("=" * 62)
    print()

    # --- Login ---
    try:
        token = login()
    except Exception as e:
        print(f"Cannot connect to API at {API_BASE}: {e}")
        print("Make sure SmartQueueAPI is running.")
        return

    if not token:
        return

    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json"
    }

    # --- Queue info ---
    print()
    queue = get_queue_info(headers, QUEUE_ID)
    if not queue:
        print(f"Queue {QUEUE_ID} not found. Check QUEUE_ID in config.")
        return

    print(f"Queue: {queue.get('name')}  |  "
          f"Status: {queue.get('status')}  |  "
          f"Default service: {queue.get('defaultServiceMinutes')} min")

    # --- Counters ---
    print()
    print("Counters:")
    counters = get_counters(headers, QUEUE_ID)
    counter_id = None

    for c in counters:
        status = c.get('status', 'Unknown')
        print(f"  Counter #{c.get('id')} -- {c.get('name')} -- {status} "
              f"-- assigned: {c.get('assignedUserName') or 'unassigned'}")

        if status in ('Open', 'Busy') and counter_id is None:
            counter_id = c.get('id')
        elif status == 'Closed' and counter_id is None:
            if open_counter(headers, c.get('id')):
                counter_id = c.get('id')
                print(f"    Opened counter #{counter_id}")

    if counter_id is None:
        print("  No open counter available. Call/complete steps will be skipped.")
        print("  Estimates will still be calculated on each take_ticket call.")
    else:
        print(f"  Using counter #{counter_id} for serving.")

    # --- Simulation ---
    print()
    print("-" * 62)
    print(f"Starting simulation: {NUM_TICKETS} tickets on queue {QUEUE_ID}")
    print("-" * 62)

    completed_count = 0
    pending = []   # list of ticket IDs taken but not yet served

    for i in range(1, NUM_TICKETS + 1):
        print()
        print(f"[Ticket {i}/{NUM_TICKETS}] Customer takes a number")

        ticket = take_ticket(headers, QUEUE_ID)
        if ticket is None:
            print("  Could not take ticket. Skipping.")
            time.sleep(TICKET_DELAY_SEC)
            continue

        t_id  = ticket.get('id')
        t_num = ticket.get('ticketNumber')
        t_pos = ticket.get('position')
        t_est = ticket.get('estimatedWaitMinutes')

        print(f"  Ticket number    : {t_num}")
        print(f"  Position         : {t_pos}")
        print(f"  Estimated wait   : {t_est} min")
        print(f"  Estimation state : {tier_description(completed_count)}")

        pending.append(t_id)

        # Every 2 tickets taken, serve 1
        if len(pending) >= 2 and counter_id is not None:
            serve_id = pending.pop(0)
            svc_secs = round(random.uniform(MIN_SERVICE_SEC, MAX_SERVICE_SEC), 1)

            print()
            print(f"  [Employee] Calling ticket ID {serve_id}...")
            if call_ticket(headers, serve_id, counter_id):
                print(f"  [Employee] Serving... ({svc_secs}s simulated)")
                time.sleep(svc_secs)

                if complete_ticket(headers, serve_id):
                    completed_count += 1
                    print(f"  [Employee] Done. ({completed_count} completed total)")

                    status = get_estimate(headers, QUEUE_ID)
                    if status:
                        avg = status.get('averageServiceMinutes', '?')
                        waiting = status.get('totalWaiting', '?')
                        print(f"  [Queue]    Avg service time: {avg} min  "
                              f"|  Waiting: {waiting}")
                else:
                    print(f"  [Employee] complete_ticket failed for ID {serve_id}")
            else:
                print(f"  [Employee] call_ticket failed for ID {serve_id}")
                pending.insert(0, serve_id)

        time.sleep(TICKET_DELAY_SEC)

    # --- Drain remaining queue ---
    if pending and counter_id is not None:
        print()
        print("-" * 62)
        print(f"All tickets taken. Serving {len(pending)} remaining...")
        print("-" * 62)

        for serve_id in pending:
            svc_secs = round(random.uniform(MIN_SERVICE_SEC, MAX_SERVICE_SEC), 1)
            if call_ticket(headers, serve_id, counter_id):
                time.sleep(svc_secs)
                if complete_ticket(headers, serve_id):
                    completed_count += 1
                    print(f"  Ticket {serve_id} done. ({completed_count} completed total)")

    # --- Final summary ---
    print()
    print("=" * 62)
    print("Simulation finished")
    print("=" * 62)
    print(f"  Tickets taken     : {NUM_TICKETS}")
    print(f"  Tickets completed : {completed_count}")

    final = get_estimate(headers, QUEUE_ID)
    if final:
        print(f"  Final avg service : {final.get('averageServiceMinutes')} min")
        print(f"  Still waiting     : {final.get('totalWaiting')}")

    print()
    print("The estimation engine has now learned from real completed tickets.")
    print("Tier 2 snapshots activate after 10+ samples per hour slot.")
    print()


if __name__ == "__main__":
    run()
