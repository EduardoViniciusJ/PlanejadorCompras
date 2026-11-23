<p align="center">
  <img src="src/frontend/public/icons/new-brand-mascot.png" alt="Planejador de Compras mascot" width="128" height="128" />
</p>

<h1 align="center">Planejador de Compras</h1>

<p align="center">
  <strong>Plan your purchases, compare supplier quotes and issue purchase orders — all in one place.</strong><br />
  A full-stack purchase planning system built with ASP.NET Core and Angular.
</p>

<p align="center">
  <a href="#features">Features</a> ·
  <a href="#tech-stack">Tech stack</a> ·
  <a href="#project-structure">Project structure</a> ·
  <a href="#getting-started">Getting started</a> ·
  <a href="https://github.com/EduardoViniciusJ/PlanejadorCompras/issues">Report a bug or suggest an idea</a>
</p>

> [!NOTE]
> Planejador de Compras helps you organize the whole buying flow: create shopping lists, register suppliers, collect and compare quotes on a price map, equalize offers to find the best deal, and turn the result into purchase orders — with PDF and Excel reports along the way.

> [!TIP]
> Planejador de Compras is **free and open source**. There are no paid plans: every feature, including the AI assistant, is available to everyone with the same usage limit.

## Features

- **Shopping lists and items:** create lists, add the items you need to buy and keep everything organized in one place.
- **Supplier management:** register suppliers with contacts and addresses, and link them to your shopping lists.
- **Price map:** collect quotes from different suppliers for each item and see all prices side by side.
- **Quotation requests:** create quotation requests for suppliers and track their status.
- **Quote equalization:** compare all supplier offers for a list, find the best combination and save the result as a snapshot in the equalization history.
- **Purchase orders:** turn the winning quotes into purchase orders and follow their status.
- **PDF and Excel reports:** export shopping lists, equalizations and purchase orders as ready-to-share documents.
- **Authentication:** email and password login with JWT, Google sign-in, email confirmation and password recovery.

## Tech stack

| Layer | Technologies |
|---|---|
| Backend | ASP.NET Core (.NET 10) · Entity Framework Core · PostgreSQL |
| Frontend | Angular 21 · TypeScript · ng-zorro-antd · Tabler Icons |
| Authentication | JWT Bearer · Google Sign-In · BCrypt |
| Reports | PDFsharp / MigraDoc (PDF) · ClosedXML (Excel) |
| Email | Resend |
| Tests | xUnit · Moq (backend) · Vitest (frontend) |
| Infrastructure | Docker Compose (PostgreSQL 17) · Nginx (frontend) |

## Project structure

The backend follows Clean Architecture:

```
src/
  PlanejadorCompras.Domain/          # Entities, business rules and repository interfaces
  PlanejadorCompras.Application/     # Use cases and application services
  PlanejadorCompras.Infrastructure/  # EF Core, repositories, reports and external services
  PlanejadorCompras.API/             # REST API (controllers, auth, error handling)
  frontend/                          # Angular application
tests/
  PlanejadorCompras.Application.UnitTests/
```

## Getting started

You will need the **.NET 10 SDK**, **Node.js** with npm, and **Docker** (for PostgreSQL).

**1. Clone the repository:**

```powershell
git clone https://github.com/EduardoViniciusJ/PlanejadorCompras.git
cd PlanejadorCompras
```

**2. Start the database.** Create a `.env` file in the project root with `POSTGRES_DB`, `POSTGRES_USER` and `POSTGRES_PASSWORD`, then run:

```powershell
docker compose up -d
```

PostgreSQL will be available on port `5433`.

**3. Configure the API.** Set the connection string and the JWT secret with user secrets (Google sign-in and Resend keys are optional):

```powershell
cd src/PlanejadorCompras.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=<db>;Username=<user>;Password=<password>"
dotnet user-secrets set "Authentication:Jwt:SecretKey" "<your-long-random-secret>"
```

**4. Run the API:**

```powershell
dotnet run --project src/PlanejadorCompras.API
```

**5. Run the frontend:**

```powershell
cd src/frontend
npm install
npm start
```

Open `http://localhost:4200` in your browser.

<details>
<summary>Development commands</summary>

From the project root:

```powershell
dotnet test tests/PlanejadorCompras.Application.UnitTests   # backend unit tests
dotnet build src/PlanejadorCompras.API                      # build the API and all layers
npm --prefix src/frontend run test                          # frontend tests
npm --prefix src/frontend run build                         # frontend production build
```

</details>
