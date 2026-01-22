# Hotel Booking System - Cloud-Native Microservices Architecture

## 📋 Project Overview

This project is a comprehensive **Hotel Booking System** (similar to Hotels.com) designed with a **Microservices Architecture**. It is developed as a Cloud-Native application and deployed on **Microsoft Azure**. The system handles hotel management, intelligent price prediction using Machine Learning, high-performance hotel searching, secure booking processes, and asynchronous notifications.

### 🚀 Live Demo & Deployment Status

The application utilizes **Azure App Services**, **Azure SQL**, **Service Bus**, and **Logic Apps**.

- **Frontend Static Web App:** 
- **API Gateway:** 
- **Hotel Admin Service:** 
- **Hotel Booking Service:** 
- **Hotel Search Service:** 
- **Hotel Notification Service:** 
- **Hotel ML Service:** 
- **Video Presentation:** 
  > **⚠️ Deployment Note:** While all backend microservices (SQL, Service Bus, Logic App, ML, Admin, Booking) are successfully deployed and fully operational on Azure, I experienced intermittent high latency and HTTP 500 errors on the Frontend. I suspect this is caused by a Region Mismatch: The Frontend is hosted on Azure Static Web Apps (East US), while the entire backend ecosystem resides in Canada Central. This likely caused cross-region timeouts during API calls. Therefore, the **Demo Video** demonstrates the system running via a **Hybrid Approach** (Local Gateway connected to Backend Resources) to bypass this latency and showcase full system functionality smoothly.

## 🏗️ Architecture & Design

The system is divided into isolated microservices, communicating via REST APIs and Asynchronous Messaging (Azure Service Bus). All services are container-ready (Docker support).

### 1\. Microservices Breakdown

| Service           | Technology      | Responsibility                                        |
| :---------------- | :-------------- | :---------------------------------------------------- |
| **API Gateway**   | .NET 8 / Ocelot | Single entry point, routing, and load balancing.      |
| **Hotel Admin**   | .NET 8 Web API  | Managing hotels, rooms, and availability.             |
| **Hotel Search**  | .NET 8 Web API  | Searching hotels with **Memory Caching**.             |
| **Hotel Booking** | .NET 8 Web API  | Handling reservations and publishing events.          |
| **ML Service**    | Python / Flask  | **Random Forest** model for dynamic price prediction. |
| **Notification**  | .NET 8 Worker   | Consumes Service Bus messages and Simulates sending   |

### 2\. Cloud & DevOps Stack

- **Database:** Azure SQL Database (Relational Data).
- **Authentication:** Azure External ID (Microsoft Entra ID).
- **Messaging:** Azure Service Bus (Queue-based async communication).
- **Automation:** Azure Logic Apps (Scheduled tasks for capacity monitoring).
- **CI/CD:** GitHub Actions (Automated build & deploy pipelines).
- **Frontend:** React (Vite) hosted on Azure Static Web Apps.

## 📊 Data Models (ER Diagram Description)

The system uses a distributed database pattern. Each service manages its own data scope within Azure SQL.

### **1\. Admin & Search Context**

- **Hotels Table:** Id, Name, Address, City, Country, Description, Rating.
- **Rooms Table:** Id, HotelId (FK), RoomType, BasePrice, Capacity.
- **Availabilities Table:** Id, RoomId (FK), Date, StockCount, Price (Dynamic).

### **2\. Booking Context**

- **Reservations Table:** Id, HotelId, RoomId, UserId, CheckInDate, CheckOutDate, TotalPrice, Status.

## 🛠️ Assumptions & Implementation Details

1.  **Price Prediction:** It is assumed that prices vary based on "Seasonality" and "Room Capacity". The Python ML service predicts the price based on these features trained on a historical dataset.
2.  **Caching Strategy:** To meet the high-performance requirement for the Search Service, **In-Memory Caching** is implemented.

    - _First Request:_ Fetches from Azure SQL -> Stores in RAM.
    - _Subsequent Requests:_ Returns immediately from RAM.

3.  **Asynchronous Notification:** The booking process is decoupled. The user receives an immediate UI success response, while the email is sent asynchronously via the Notification Service worker.
4.  **Capacity Monitoring:** Azure Logic App is assumed to run nightly (triggered manually for the demo) to check for rooms with <20% capacity and alert the admin.

## 🐛 Issues Encountered & Solutions

### 1\. Azure Gateway Port Binding (503 Error)

- **Issue:** The Ocelot Gateway deployed on Azure App Service (Linux) intermittently failed to bind to the exposed port, resulting in 503 errors during the final integration test.
- **Resolution Attempt:** Configured WEBSITES_PORT, updated Dockerfiles, and switched between Kestrel/Gunicorn.
- **Final Status:** To ensure a stable demonstration of the business logic, the Gateway was run locally connecting to Microservices for the demo video.

### 2\. Python ML Dependencies

- **Issue:** The ML Service deployment took 20+ minutes due to compiling scikit-learn and pandas on the free-tier Azure App Service plan.
- **Solution:** Optimized requirements.txt and utilized pre-built wheels where possible, accepting the longer build time as a trade-off for a working cloud ML model.

## 🔄 CI/CD & Deployment Journey

The project fully implements **DevOps best practices**.

- **GitHub Actions:** Over **100+ workflow runs** were executed to refine the deployment pipeline.
- **Pipelines:** Separate YAML workflows were created for each microservice (main_hotel-admin.yml, main_hotel-ml.yml, etc.) to support independent deployments.

## 🏃 How to Run Locally

If you wish to run the project locally using the source code:

1.  **Prerequisites:** .NET 8 SDK, Python 3.11, Docker (Optional).
2.  Bash git clone \[[https://github.com/selcuksuatsayin-se/Hotel-Booking-System.git](https://github.com/selcuksuatsayin-se/Hotel-Booking-System.git)\]
3.  **Update Connection Strings:** Update appsettings.json in each service with your Azure SQL and Service Bus connection strings.
4.  Bash dotnet run --project HotelAdminService/HotelAdminService.csproj dotnet run --project HotelBookingService/HotelBookingService.csproj# ... repeat for others
5.  Bash cd HotelMLService pip install -r requirements.txtpython app.py

## © License & Copyright

This project was developed by **Selçuk Suat Sayın**.  
It is intended for educational purposes.  
Copyright © 2026. All rights reserved.
