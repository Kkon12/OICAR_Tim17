namespace SmartQueueApp.Services
{
    public static class T
    {
        // ── Navigation 
        public static string Dashboard(bool hr) => hr ? "Nadzorna ploča" : "Dashboard";
        public static string Queues(bool hr) => hr ? "Redovi" : "Queues";
        public static string Staff(bool hr) => hr ? "Osoblje" : "Staff";
        public static string Statistics(bool hr) => hr ? "Statistika" : "Statistics";
        public static string MyCounter(bool hr) => hr ? "Moj šalter" : "My Counter";
        public static string Queue(bool hr) => hr ? "Red" : "Queue";
        public static string Logout(bool hr) => hr ? "Odjava" : "Logout";

        // ── Dash
        public static string WaitingNow(bool hr) => hr ? "Čeka sada" : "Waiting Now";
        public static string ServedToday(bool hr) => hr ? "Posluženo danas" : "Served Today";
        public static string AvgWait(bool hr) => hr ? "Prosj. čekanje" : "Avg Wait Today";
        public static string OpenCounters(bool hr) => hr ? "Otvoreni šalteri" : "Open Counters";
        public static string StaffMembers(bool hr) => hr ? "Djelatnici" : "Staff Members";
        public static string TicketsToday(bool hr) => hr ? "Listići danas" : "Tickets Today";
        public static string SkippedToday(bool hr) => hr ? "Preskočeno danas" : "Skipped Today";
        public static string ActiveQueues(bool hr) => hr ? "Aktivni redovi" : "Active Queues";
        public static string QueueStatus(bool hr) => hr ? "Status redova" : "Queue Status";
        public static string ManageQueues(bool hr) => hr ? "Upravljaj redovima" : "Manage Queues";
        public static string Live(bool hr) => hr ? "Uživo" : "Live";

        // ── Queue table headers 
        public static string Status(bool hr) => hr ? "Status" : "Status";
        public static string Waiting(bool hr) => hr ? "Čeka" : "Waiting";
        public static string Completed(bool hr) => hr ? "Završeno" : "Completed";
        public static string Counters(bool hr) => hr ? "Šalteri" : "Counters";
        public static string SkipRate(bool hr) => hr ? "Preskočeno" : "Skip Rate";

        // ── Queue management 
        public static string NewQueue(bool hr) => hr ? "Novi red" : "New Queue";
        public static string QueueManagement(bool hr) => hr ? "Upravljanje redovima" : "Queue Management";
        public static string CreateAndManage(bool hr) => hr ? "Kreirajte i upravljajte redovima" : "Create and manage queues";
        public static string Detail(bool hr) => hr ? "Detalji" : "Detail";
        public static string Pause(bool hr) => hr ? "Pauziraj" : "Pause";
        public static string Activate(bool hr) => hr ? "Aktiviraj" : "Activate";
        public static string Delete(bool hr) => hr ? "Izbriši" : "Delete";
        public static string Cancel(bool hr) => hr ? "Odustani" : "Cancel";
        public static string Save(bool hr) => hr ? "Spremi" : "Save";
        public static string CreateQueue(bool hr) => hr ? "Kreiraj red" : "Create Queue";
        public static string BackToQueues(bool hr) => hr ? "Natrag na redove" : "Back to Queues";

        // ── Staff management 
        public static string StaffManagement(bool hr) => hr ? "Upravljanje osobljem" : "Staff Management";
        public static string AddStaff(bool hr) => hr ? "Dodaj djelatnika" : "Add Staff";
        public static string Deactivate(bool hr) => hr ? "Deaktiviraj" : "Deactivate";
        public static string Active(bool hr) => hr ? "Aktivan" : "Active";
        public static string Inactive(bool hr) => hr ? "Neaktivan" : "Inactive";
        public static string Protected(bool hr) => hr ? "Zaštićen" : "Protected";
        public static string Role(bool hr) => hr ? "Uloga" : "Role";
        public static string Created(bool hr) => hr ? "Kreiran" : "Created";
        public static string Name(bool hr) => hr ? "Ime" : "Name";
        public static string Email(bool hr) => hr ? "Email" : "Email";

        // ── Djelatnik
        public static string CounterDashboard(bool hr) => hr ? "Šalter nadzorna ploča" : "Counter Dashboard";
        public static string ManageCounter(bool hr) => hr ? "Upravljajte šalterom i poslužite stranke" : "Manage your counter and serve customers";
        public static string NoCounterAssigned(bool hr) => hr ? "Nema dodijeljenog šaltera" : "No counter assigned";
        public static string AskAdmin(bool hr) => hr ? "Zamolite administratora da vas dodijeli šalteru." : "Ask your Admin to assign you to a counter.";
        public static string YourCounter(bool hr) => hr ? "Vaš šalter" : "Your Counter";
        public static string NowServing(bool hr) => hr ? "Trenutno posluživanje" : "Now Serving";
        public static string ReadyToCall(bool hr) => hr ? "Spreman za poziv" : "Ready to call next";
        public static string OpenCounter(bool hr) => hr ? "Otvori šalter" : "Open Counter";
        public static string CloseCounter(bool hr) => hr ? "Zatvori šalter" : "Close Counter";
        public static string Complete(bool hr) => hr ? "Završi — Posluženo" : "Complete — Served";
        public static string Skip(bool hr) => hr ? "Preskoči — Nije prisutan" : "Skip — Not Present";
        public static string WaitingQueue(bool hr) => hr ? "Red čekanja" : "Waiting Queue";
        public static string CallNext(bool hr) => hr ? "Pozovi sljedećeg" : "Call Next";
        public static string Call(bool hr) => hr ? "Pozovi" : "Call";
        public static string Position(bool hr) => hr ? "Pozicija" : "Position";
        public static string EstWait(bool hr) => hr ? "Procj. čekanje" : "Estimated Wait";
        public static string WaitingSince(bool hr) => hr ? "Čeka od" : "Waiting Since";
        public static string NoCustomers(bool hr) => hr ? "Nema stranaka u redu" : "No customers waiting";
        public static string Actions(bool hr) => hr ? "Akcije" : "Actions";

        // ── Kiosk 
        public static string KioskTitle(bool hr) => hr ? "Uzmite listić" : "Take a Ticket";
        public static string KioskSubtitle(bool hr) => hr ? "Odaberite uslugu i uzmite listić" : "Select a service and take your ticket";
        public static string NoServices(bool hr) => hr ? "Trenutno nema dostupnih usluga" : "No services available right now";
        public static string CheckBackLater(bool hr) => hr ? "Molimo pokušajte kasnije ili zamolite osoblje za pomoć." : "Please check back later or ask staff for assistance.";
        public static string NoWaiting(bool hr) => hr ? "Nema čekanja" : "No waiting";
        public static string CountersOpen(bool hr) => hr ? "šaltera otvoreno" : "counters open";
        public static string StaffLogin(bool hr) => hr ? "Prijava osoblja" : "Staff Login";
        public static string TakeAnother(bool hr) => hr ? "Uzmi još jedan listić" : "Take Another Ticket";

        // ── Ticket confirmation 
        public static string TicketReady(bool hr) => hr ? "Vaš listić je spreman!" : "Your ticket is ready!";
        public static string TicketNumber(bool hr) => hr ? "Broj listića" : "Ticket Number";
        public static string PositionInLine(bool hr) => hr ? "Pozicija u redu" : "Position in line";
        public static string EstimatedWait(bool hr) => hr ? "Procijenjeno čekanje" : "Estimated wait";
        public static string TakenAt(bool hr) => hr ? "Uzeto u" : "Taken at";
        public static string StayNearby(bool hr) => hr ? "Ostanite u blizini i slušajte kad vaš broj bude pozvan." : "Please stay nearby and listen for your number to be called.";
        public static string Minutes(bool hr) => hr ? "minuta" : "minutes";

        // ── Login 
        public static string SignIn(bool hr) => hr ? "Prijava" : "Sign In";
        public static string SignInTo(bool hr) => hr ? "Prijavite se na vaš račun" : "Sign in to your account";
        public static string Password(bool hr) => hr ? "Lozinka" : "Password";

        // ── General 
        public static string NoData(bool hr) => hr ? "Nema podataka" : "No data";
        public static string Error(bool hr) => hr ? "Greška" : "Error";
        public static string Loading(bool hr) => hr ? "Učitavanje..." : "Loading...";
        public static string Min(bool hr) => hr ? "min" : "min";
    }
}