# LovelyFish

Backend API for managing products, orders, users, and email notifications.
Built with ASP.NET Core, Entity Framework, and Swagger for API documentation.

---

## Live Site / Deployment
- Deployed on Azure App Service.
- Backend API serves the frontend React app at [www.lovelyfishaquarium.co.nz](https://www.lovelyfishaquarium.co.nz)

---

## Repository
- **Frontend:** [LovelyFish Frontend](https://github.com/trinazhang2024/LovelyFish)
- **Backend:** [LovelyFish Backend](https://github.com/TrinaJinfeng/LovelyFish.git)


---

## Tech Stack
- **Backend:** ASP.NET Core  
- **Database:** SQL Server / Entity Framework Core  
- **API Documentation:** Swagger UI  
- **Email Service:** Brevo / SMTP (configured via environment variables)  
- **Deployment:** Azure App Service

---

## Installation & Running Locally

1. **Clone the repository:**
    ```bash
    git clone https://github.com/TrinaJinfeng/LovelyFish.git

2. Navigate to the project folder:
   bash
   cd LovelyFish-Backend

3. Install dependencies and restore NuGet packages:
  bash
  dotnet restore

4. Apply database migrations:
  bash
  dotnet ef database update

5. Run the backend server:
  bash
  dotnet run

6. Open Swagger UI to explore API endpoints:
  bash
  http://localhost:7148/swagger
  ⚠️ Note: Swagger URL is local; production endpoints are protected and require authentication.

---

## API Documentation

- All API endpoints are documented via Swagger UI.
- After running locally, access Swagger at:
  http://localhost:7148/swagger
- **Live Swagger (for reference only):**
  [View Swagger UI](https://lovelyfish-backend-esgtdkf7h0e2ambg.australiaeast-01.azurewebsites.net/swagger/index.html)
