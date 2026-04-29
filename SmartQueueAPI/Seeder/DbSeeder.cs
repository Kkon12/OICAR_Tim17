using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartQueue.Core.Data;
using SmartQueue.Core.Models;

/* PRVI DEFAULT ADMIN :

  * "email": "admin@smartqueue.com", 

  "password": "Admin123!"*/

namespace SmartQueueAPI
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context)
        {
            // ── Seed Ulogee
            string[] roles = { "Admin", "Djelatnik", "Korisnik" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ── Seed Admin User
            var adminEmail = "admin@smartqueue.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    FirstName = "Super",
                    LastName = "Admin",
                    Email = adminEmail,
                    UserName = adminEmail,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, "Admin123!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ── Seed Djelatnik Users 
            var djelatnici = new[]
            {
                new { First = "Ivan",  Last = "Horvat", Email = "ivan@smartqueue.com" },
                new { First = "Maja",  Last = "Kovač",  Email = "maja@smartqueue.com" },
                new { First = "Luka",  Last = "Babić",  Email = "luka@smartqueue.com" },
            };

            foreach (var d in djelatnici)
            {
                if (await userManager.FindByEmailAsync(d.Email) == null)
                {
                    var djelatnik = new ApplicationUser
                    {
                        FirstName = d.First,
                        LastName = d.Last,
                        Email = d.Email,
                        UserName = d.Email,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(djelatnik, "Djelatnik123!");
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(djelatnik, "Djelatnik");
                }
            }

            // ── Seed Queues
            if (!await context.Queues.AnyAsync())
            {
                var queues = new List<Queue>
                {
                    new Queue
                    {
                        Name = "Opća medicina",
                        Description = "Red čekanja za opću medicinu",
                        Status = QueueStatus.Active,
                        DefaultServiceMinutes = 7,
                        MinTicketsForStats = 20
                    },
                    new Queue
                    {
                        Name = "Blagajna",
                        Description = "Red čekanja za blagajnu",
                        Status = QueueStatus.Active,
                        DefaultServiceMinutes = 3,
                        MinTicketsForStats = 20
                    },
                    new Queue
                    {
                        Name = "Info šalter",
                        Description = "Red čekanja za informacije",
                        Status = QueueStatus.Active,
                        DefaultServiceMinutes = 5,
                        MinTicketsForStats = 20
                    },
                    new Queue
                    {
                        Name = "Specijalistički pregled",
                        Description = "Red čekanja za specijaliste",
                        Status = QueueStatus.Paused,
                        DefaultServiceMinutes = 15,
                        MinTicketsForStats = 20
                    }
                };

                context.Queues.AddRange(queues);
                await context.SaveChangesAsync();

                // ── Seed Counters
                var opca = await context.Queues
                    .FirstAsync(q => q.Name == "Opća medicina");
                var blagajna = await context.Queues
                    .FirstAsync(q => q.Name == "Blagajna");
                var info = await context.Queues
                    .FirstAsync(q => q.Name == "Info šalter");

                var ivan = await userManager.FindByEmailAsync("ivan@smartqueue.com");
                var maja = await userManager.FindByEmailAsync("maja@smartqueue.com");
                var luka = await userManager.FindByEmailAsync("luka@smartqueue.com");

                var counters = new List<Counter>
                {
                    // Opća medicina counters
                    new Counter
                    {
                        Name = "Šalter 1",
                        QueueId = opca.Id,
                        Status = CounterStatus.Open,
                        AssignedUserId = ivan?.Id
                    },
                    new Counter
                    {
                        Name = "Šalter 2",
                        QueueId = opca.Id,
                        Status = CounterStatus.Open,
                        AssignedUserId = maja?.Id
                    },
                    new Counter
                    {
                        Name = "Šalter 3",
                        QueueId = opca.Id,
                        Status = CounterStatus.Closed,
                        AssignedUserId = null
                    },
                    // Blagajna counter
                    new Counter
                    {
                        Name = "Blagajna 1",
                        QueueId = blagajna.Id,
                        Status = CounterStatus.Open,
                        AssignedUserId = luka?.Id
                    },
                    // Info counter
                    new Counter
                    {
                        Name = "Info 1",
                        QueueId = info.Id,
                        Status = CounterStatus.Open,
                        AssignedUserId = null
                    }
                };

                context.Counters.AddRange(counters);
                await context.SaveChangesAsync();
            }
        }
    }
}

/*Why !await context.Queues.AnyAsync(): Checks if queues already exist before seeding
 * — prevents duplicate data on every app restart. Safe to run the seeder every time the API starts.
Why different DefaultServiceMinutes per queue: Each queue type has a realistic default 
— Blagajna (3 min) is quick, Specijalistički pregled (15 min) is long. This makes Tier 1 estimates realistic from day one.
Why seed Djelatnici and assign to Counters: Gives you realistic test data immediately
— Ivan and Maja are working at Opća medicina, Luka at Blagajna. You can test the full flow in Swagger right away.*/
