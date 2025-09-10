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
- **Database:** SQL Server (managed via SSMS), deployed on Azure SQL  
- **Image Storage:** Azure Blob Storage for product images  
- **API Documentation:** Swagger UI  
- **Email Service:** Brevo / SMTP (configured via environment variables)  
- **Deployment:** Azure App Service  

---

## Environment Variables
| Variable | Description |
|----------|-------------|
| `DB_CONNECTION` | Azure SQL connection string |
| `BLOB_STORAGE_URL` | Base URL for Azure Blob Storage (used to store and serve product images) |
| `BLOB_STORAGE_KEY` | Access key for Azure Blob Storage |
| `SMTP_USER` | Email service username |
| `SMTP_PASS` | Email service password |
| `FRONTEND_URL` | URL of the frontend React app |


---

## Installation & Running Locally

1. **Clone the repository:**
    ```bash
    git clone https://github.com/TrinaJinfeng/LovelyFish.git

2. **Navigate to the project folder:**
   ```bash
   cd LovelyFish-Backend

3. **Install dependencies and restore NuGet packages:**
  ```bash
  dotnet restore
```

4. **Apply database migrations:**
  ```bash
  dotnet ef database update
```

5. **Run the backend server:**
  ```bash
  dotnet run
```

6. **Open Swagger UI to explore API endpoints:**
  ```bash
  http://localhost:7148/swagger
  ⚠️ Note: Swagger URL is local; production endpoints are protected and require authentication.
```
---

## API Documentation

- All API endpoints are documented via Swagger UI.
- After running locally, access Swagger at:
  http://localhost:7148/swagger
- **Live Swagger (for reference only):**
  [View Swagger UI](https://lovelyfish-backend-esgtdkf7h0e2ambg.australiaeast-01.azurewebsites.net/swagger/index.html)
---

| Variable        | Description                   |
| --------------- | ----------------------------- |
| `DB_CONNECTION` | SQL Server connection string  |
| `SMTP_USER`     | Email service username        |
| `SMTP_PASS`     | Email service password        |
| `FRONTEND_URL`  | URL of the frontend React app |
> ⚠️ Never commit real credentials to GitHub. Use environment variables or a secret manager.
---

## 🧪 Testing & Environment Differences (Backend)

### 1. Manual API Testing
- **Swagger UI:** After implementing each API endpoint, test manually using Swagger.
  - Examples include: 
    - User registration
    - Product image upload
    - Sending email notifications
- **API Validation:** 
  - Check HTTP status codes (200, 400, 401, etc.)
  - Ensure data consistency and proper error handling

### 2. Database Connection Differences
- **Local SQL Server:** Connection string in `appsettings.Development.json`, tests work normally.
- **Azure SQL:** Connection string stored in environment variables. Sometimes, if not all required environment variables are set, backend throws database connection errors.
- **Solution:** Always configure all required environment variables for both local and cloud environments to ensure consistent connectivity.

### 3. Image Upload Differences
- **Local:** Images uploaded to local `wwwroot/images`, accessed normally.
- **Azure Blob Storage:** Directly using local paths causes 404 errors.
- **Solution:** Backend returns full Blob URLs; frontend uses these URLs directly to ensure images display correctly.

### 4. Email Service Differences
- **Local:** Used SMTP test account, emails sent successfully.
- **Production / Azure:** Initially tried using Brevo with a Zoho domain email. Even with correct DNS and verification, emails went to spam and admin inbox did not receive them.
- **Solution:** Switched to SMTP for sending and receiving emails. Credentials stored in environment variables (never hard-coded) to ensure security.

### 5. Frontend Integration / CORS
- **Local:** Frontend calls `localhost:5000` backend, works normally.
- **Production / Azure:** Backend must allow frontend URL via CORS, otherwise requests are blocked.
- **Solution:** Configure backend CORS to allow the deployed frontend URL.

> ⚠️ Note: Never commit real credentials to GitHub. Always use environment variables or a secret manager for sensitive information.

---
/Controllers   # API controllers for products, orders, users
/Models        # Database models
/Data          # DbContext and migrations
/Services      # Email service, business logic
/Properties    # Project properties

---
## Highlights

Full-stack integration with frontend React app

Proper RESTful API design with Swagger documentation

Environment variables for secure configuration

Deployed on Azure for live access

## Author

Trina Zhang
Email: zhang.trina@yahoo.co.nz
