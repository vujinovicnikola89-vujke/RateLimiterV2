# Rate Limiter (.NET)

## Opis

Ova biblioteka implementira **Rate Limiter middleware** za ASP.NET Core aplikacije.  
Omogućava ograničavanje broja HTTP zahteva po korisniku (npr. po IP adresi) u određenom vremenskom prozoru.  
Kada korisnik prekorači dozvoljeni broj zahteva, vraća se **HTTP 429 Too Many Requests** odgovor sa `Retry-After` header-om.

Middleware podržava konfiguraciju putem `appsettings.json` i jednostavnu integraciju kroz `IServiceCollection` ekstenziju.

---

## Instalacija

Biblioteku možete dodati:
1. Kao **projektnu referencu** (ako je lokalna biblioteka), ili  
2. Kao **NuGet paket** 


## Integracija

Nakon dodavanja NuGet paketa RateLimiter potrebno je upotrebiti funkcionalnos na nivou Program.cs na sledeci nacin :

- builder.Services.AddRequestRateLimiting();
- app.UseRequestRateLimiting();

Custom configuracija moze biti prosledjena kao parametar metode AddRequestRateLimiting(), ako nije prosledjena uzima se u obzir defalt-ono podesavanje.
Prevenstvo imaju podesavanja iz appsettings iz projketa gde je instaliran NuGet paket.

