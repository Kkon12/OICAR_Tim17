[README.md](https://github.com/user-attachments/files/28454164/README.md)
# SmartQueue — Digitalni sustav upravljanja redovima čekanja

SmartQueue je full-stack sustav koji zamjenjuje fizičke redove čekanja na uslužnim šalterima. Korisnici uzimaju numerirane listiće putem mobilne aplikacije, djelatnici upravljaju redovima putem web sučelja, a administratori imaju pregled nad cijelim sustavom.

---

## Sadržaj

- [Komponente sustava](#komponente-sustava)
- [Preduvjeti](#preduvjeti)
- [Postavljanje baze podataka](#1-postavljanje-baze-podataka)
- [Pokretanje SmartQueueAPI](#2-pokretanje-smartqueueapi)
- [Pokretanje SmartQueueApp](#3-pokretanje-smartqueueapp)
- [Pokretanje SmartQueueMobile](#4-pokretanje-smartqueuemobile)
- [Zadane korisničke prijave](#zadane-korisničke-prijave)
- [Pokretanje testova](#pokretanje-testova)
- [Redoslijed pokretanja](#redoslijed-pokretanja)

---

## Komponente sustava

| Komponenta | Tehnologija | Port | Namjena |
|---|---|---|---|
| `SmartQueue.Core` | C# .NET 8 | — | Dijeljeni domenski modeli, DTOs, DbContext |
| `SmartQueueAPI` | ASP.NET Core 8 REST API | 5179 | Backend, poslovna logika, baza podataka |
| `SmartQueueApp` | ASP.NET Core 8 MVC | 5174 | Web sučelje za admina i djelatnike |
| `SmartQueueMobile` | React Native / Expo | 8081 | Mobilna aplikacija za korisnike |
| `PostgreSQL` | Docker kontejner | 5432 | Baza podataka |

---

## Preduvjeti

Prije pokretanja projekta potrebno je imati instalirano sljedeće:

### Obavezno

- [Visual Studio 2022](https://visualstudio.microsoft.com/) (s ASP.NET and web development workload)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (za PostgreSQL)
- [Node.js](https://nodejs.org/) (verzija 18 ili novija — za mobilnu aplikaciju)

### Za mobilnu aplikaciju

- [Android Studio](https://developer.android.com/studio) (za Android emulator)
- Expo Go aplikacija na fizičkom uređaju (alternativa emulatoru)

### Provjera instalacije

```bash
dotnet --version        # treba biti 8.x.x
node --version          # treba biti 18.x ili noviji
docker --version        # treba biti pokrenut
```

---

## 1. Postavljanje baze podataka

Baza podataka pokreće se kao Docker kontejner. Potrebno je pokrenuti Docker Desktop prije sljedećih koraka.

### Prvo pokretanje — kreiranje kontejnera

```bash
docker run --name smartqueue-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=SmartQueueDb \
  -p 5432:5432 \
  -d postgres:16
```

### Svako sljedeće pokretanje

```bash
docker start smartqueue-postgres
```

### Provjera da kontejner radi

```bash
docker ps
```

U listi treba biti vidljiv `smartqueue-postgres` sa statusom `Up`.

### Provjera veze s bazom (opcionalno)

```bash
docker exec -it smartqueue-postgres psql -U postgres -d SmartQueueDb
```

Za izlaz iz psql-a: `\q`

> **Napomena:** Connection string je već konfiguriran u `SmartQueueAPI/appsettings.json`. Nije potrebno ništa mijenjati za lokalni razvoj.

---

## 2. Pokretanje SmartQueueAPI

SmartQueueAPI je REST API koji služi kao jedina pristupna točka bazi podataka.

### Putem Visual Studija

1. Otvoriti `Oikar_SmartQueue.sln` u Visual Studiju 2022
2. U Solution Exploreru desni klik na `SmartQueueAPI` → **Set as Startup Project**
3. Pritisnuti **F5** ili kliknuti **Start**

API se pokreće na: `http://localhost:5179`

### Putem terminala

```bash
cd SmartQueueAPI
dotnet run
```

### Provjera da API radi

Otvoriti u pregledniku: `http://localhost:5179/swagger`

Swagger UI prikazuje sve dostupne endpointe. API automatski pri prvom pokretanju:
- Primjenjuje EF Core migracije (kreira tablice u bazi)
- Seed-a početne podatke (uloge, admin račun, primjerke redova)

---

## 3. Pokretanje SmartQueueApp

SmartQueueApp je MVC web aplikacija za administraciju i djelatnike.

### Putem Visual Studija

1. U Solution Exploreru desni klik na `SmartQueueApp` → **Set as Startup Project**
2. Pritisnuti **F5** ili kliknuti **Start**

Web aplikacija dostupna na: `http://localhost:5174`

### Putem terminala

```bash
cd SmartQueueApp
dotnet run
```

### Pokretanje API-ja i Web aplikacije istovremeno

1. Desni klik na **Solution** → **Properties**
2. Odabrati **Multiple startup projects**
3. Postaviti i `SmartQueueAPI` i `SmartQueueApp` na **Start**
4. Pritisnuti **F5**

---

## 4. Pokretanje SmartQueueMobile

SmartQueueMobile je React Native / Expo mobilna aplikacija za krajnje korisnike.

### Instalacija ovisnosti (samo prvi put)

```bash
cd SmartQueueMobile
npm install
```

### Pokretanje aplikacije

```bash
npx expo start
```

Nakon pokretanja, odabrati jednu od opcija:

| Tipka | Akcija |
|---|---|
| `w` | Otvori u web pregledniku (localhost:8081) |
| `a` | Otvori na Android emulatoru |
| `i` | Otvori na iOS simulatoru (samo macOS) |

### Pokretanje na Android emulatoru

1. Otvoriti Android Studio
2. **Device Manager** → pokrenuti **Medium Phone API 36.1** (preporučeno) ili drugi dostupni emulator
3. Pričekati dok se emulator potpuno pokrene (vidi se početni zaslon Androida)
4. U Expo terminalu pritisnuti `a`

> **Napomena za Android emulator:** Mobilna aplikacija komunicira s API-jem putem adrese `10.0.2.2:5179` (Android emulator preusmjerava ovu adresu na `localhost` host računala). Za web preglednik koristi se `localhost:5179`.

### Expo Go na fizičkom uređaju

1. Instalirati **Expo Go** s Google Play ili App Store
2. Skenirati QR kod koji se prikazuje u terminalu
3. Telefon i računalo moraju biti na istoj WiFi mreži

---

## Zadane korisničke prijave

Aplikacija automatski kreira sljedeće račune pri prvom pokretanju:

### Administrator

| Polje | Vrijednost |
|---|---|
| E-mail | `admin@smartqueue.com` |
| Lozinka | `Admin123!` |
| Uloga | Admin |
| Pristup | Web aplikacija — puna administratorska ploča |

### Djelatnik

| Polje | Vrijednost |
|---|---|
| E-mail | `ivan.horvat@smartqueue.com` |
| Lozinka | `Djelatnik123!` |
| Uloga | Djelatnik |
| Pristup | Web aplikacija — šalterska nadzorna ploča |

### Korisnik (mobilna aplikacija)

Korisnici se registriraju sami putem mobilne aplikacije. Nema zadanog korisničkog računa.

---

## Pokretanje testova

Projekt uključuje 40 automatiziranih testova u dva testna projekta.

### Putem Visual Studija

1. Izbornik **Test** → **Run All Tests** (ili `Ctrl+R, A`)
2. Rezultati su vidljivi u **Test Explorer** prozoru

### Putem terminala

```bash
# Pokreni sve testove
dotnet test

# Pokreni samo API testove
dotnet test SmartQueueAPI.Tests

# Pokreni samo App testove
dotnet test SmartQueueApp.Tests

# Pokreni s prikazom detalja
dotnet test --verbosity normal
```

### Očekivani rezultat

```
Test run passed.
Total tests: 40
     Passed: 40
      Failed: 0
     Skipped: 0
```

> **Napomena:** Testovi koriste in-memory bazu podataka i ne zahtijevaju pokrenuti Docker kontejner ni API.

---

## Redoslijed pokretanja

Za ispravno funkcioniranje cijelog sustava potrebno je poštivati sljedeći redoslijed:

```
1. Docker Desktop (pokrenut u pozadini)
        ↓
2. docker start smartqueue-postgres
        ↓
3. SmartQueueAPI (port 5179)
        ↓
4. SmartQueueApp (port 5174)
        ↓
5. Android emulator (pričekati da se potpuno pokrene)
        ↓
6. npx expo start → pritisnuti 'a' ili 'w'
```

> **Važno:** SmartQueueApp ne može se pokrenuti bez SmartQueueAPI jer sve podatke dohvaća putem REST poziva. Mobilna aplikacija može se koristiti neovisno od web aplikacije, ali API mora biti pokrenut.

---

## Struktura projekta

```
Oikar_SmartQueue/
├── SmartQueue.Core/          # Dijeljeni modeli, DTOs, DbContext, sučelja
│   ├── Models/               # Queue, Ticket, Counter, ApplicationUser...
│   ├── DTOs/                 # Data Transfer Objects po modulima
│   ├── Data/                 # AppDbContext, EF Core konfiguracija
│   └── Interfaces/           # IEstimationService
│
├── SmartQueueAPI/            # REST API (port 5179)
│   ├── Controllers/          # Auth, Queue, Ticket, Counter, Stats
│   ├── Services/             # EstimationService (Bayesov motor procjene)
│   ├── Hubs/                 # QueueHub (SignalR)
│   ├── Seeder/               # DbSeeder (početni podaci)
│   └── Program.cs            # Konfiguracija middleware pipeline-a
│
├── SmartQueueApp/            # MVC web aplikacija (port 5174)
│   ├── Controllers/          # Admin, Auth, Djelatnik, Kiosk, Home
│   ├── Services/             # IApiService, ApiService, TokenService
│   ├── Models/ViewModels/    # View modeli po kontrolerima
│   └── Views/                # Razor pogledi
│
├── SmartQueueMobile/         # React Native mobilna aplikacija
│   ├── src/api/              # api.ts, config.ts, mockApi.ts
│   ├── src/context/          # AuthContext.tsx
│   ├── src/screens/          # Zasloni aplikacije
│   └── src/types/            # TypeScript modeli
│
├── SmartQueueAPI.Tests/      # xUnit testovi za API (20 testova)
│   ├── Unit/                 # EstimationServiceTests, QueueControllerUnitTests
│   └── Integration/          # TicketControllerTests, QueueControllerTests
│
└── SmartQueueApp.Tests/      # xUnit testovi za Web app (20 testova)
    ├── Unit/                 # AdminControllerTests, AdminStaffTests, DjelatnikControllerTests
    └── Integration/          # AuthControllerTests
```

---

## Česti problemi

### API se ne može spojiti na bazu podataka

```
Provjeri: docker ps
Rješenje: docker start smartqueue-postgres
```

### Mobilna aplikacija prikazuje bijeli zaslon

```
Uzrok: expo-secure-store nije kompatibilan s Expo Go
Rješenje: Već riješeno — aplikacija koristi AsyncStorage
```

### Android emulator ne reagira na 'a'

```
Rješenje: Pričekati dok se emulator potpuno pokrene
          (mora se vidjeti početni zaslon Androida, ne samo logo)
```

### Port je već u upotrebi

```bash
# Provjeri koji proces koristi port 5179
netstat -ano | findstr :5179

# Ili promijeni port u Properties → launchSettings.json
```

### Smart App Control blokira .exe datoteke (Windows)

```
Windows Security → App & browser control → Smart App Control → Off
Napomena: Promjena je trajna i zahtijeva restart računala
```

---

## Tehnologije

| Kategorija | Tehnologije |
|---|---|
| Backend | C# .NET 8, ASP.NET Core, Entity Framework Core, PostgreSQL |
| Autentifikacija | JWT Bearer, ASP.NET Core Identity, Refresh Token rotacija |
| Real-time | SignalR (WebSocket) |
| Frontend | Razor Views, Bootstrap 5 |
| Mobilno | React Native, Expo, TypeScript, AsyncStorage |
| Testiranje | xUnit, Moq, EF Core InMemory |
| Infrastruktura | Docker, Git |

---

## Repozitorij

**GitHub:** [github.com/Kkon12/OICAR_Tim17](https://github.com/Kkon12/OICAR_Tim17)

**Kolegij:** OIKAR — Tim 17 | Akademska godina 2025./2026.
