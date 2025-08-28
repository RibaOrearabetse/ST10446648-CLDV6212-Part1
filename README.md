# ST10446648-CLDV6212-Part1

ABCRetailers

ABCRetailers is an ASP.NET Core MVC web application developed as part of the CLDV6212 coursework. The system demonstrates integration of cloud services (Azure) into a retailer management platform, allowing CRUD operations on customers, products, and orders, as well as file uploads to Azure Blob Storage.

URLs:
Youtube: https://youtu.be/yB0oW75odiQ
WebApp: https://st10446648-cmfxfnbcaebgdgd3.southafricanorth-01.azurewebsites.net/ 

📌 Features

Customer Management

Add, view, update, and delete customer details.

Product Management

Manage product catalog with CRUD operations.

Order Management

Create and manage customer orders linked to products.

File Uploads

Upload and manage files with Azure Blob Storage.

Azure Integration

Uses Azure Storage services (Blobs, Tables, Queues, and File Shares) through the AzureStorageService.

🏗 Project Structure
ABCRetailers/
├── Controllers/        # MVC controllers (Customer, Product, Order, Upload, Home)
├── Models/             # Data models (CustomerDetails, Product, Order, etc.)
│   └── ViewModels/     # View-specific models
├── Services/           # AzureStorageService & IAzureStorageService
├── Views/              # Razor views for Customers, Products, Orders, Uploads, etc.
├── wwwroot/            # Static files (CSS, JS, images)
├── appsettings.json    # Application configuration
├── Program.cs          # Application entry point
└── ABCRetailers.csproj # Project configuration

⚙️ Technologies Used

Backend: ASP.NET Core MVC (.NET 9.0)

Frontend: Razor Views (HTML, CSS, Bootstrap)

Cloud Services: Microsoft Azure Storage (Blob, Table, Queue, File Share)

Database/Storage: Azure Tables and Blobs

IDE: Visual Studio 2022

🚀 Getting Started
Prerequisites

.NET 9.0 SDK

Visual Studio 2022 (with ASP.NET & web development workload)

Azure Storage account (with connection string)

Installation

Clone the repository:

git clone https://github.com/RibaOrearabetse/ST10446648-CLDV6212-Part1.git
cd ST10446648-CLDV6212-Part1/ABCRetailers


Restore dependencies:

dotnet restore


Update the appsettings.json with your Azure Storage connection string:

"ConnectionStrings": {
  "AzureStorage": "your-azure-storage-connection-string"
}

Running the Application
dotnet run


Then open your browser at:
👉 https://localhost:5001 (or the port specified in output)

📂 Usage

Navigate to /Customer to manage customers.

Navigate to /Product to manage products.

Navigate to /Order to create and view orders.

Navigate to /Upload to upload files to Azure Blob Storage.

✅ Example Workflow

Add a Customer in the Customer section.

Add a Product in the Product section.

Create a new Order by selecting a customer and product.

Upload supporting files (e.g., receipts, product images) in the Upload section.

🔮 Future Improvements

Authentication & Authorization (role-based access).

Search and filtering features for orders/products.

Enhanced error handling and logging.

Responsive UI with advanced Bootstrap components.

👨‍💻 Author

Orearabetse Riba
Student ID: ST10446648
Module: CLDV6212 – Part 1

