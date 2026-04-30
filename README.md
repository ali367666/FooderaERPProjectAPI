# 🍽️ Foodera ERP System

Foodera ERP is a full-stack enterprise resource planning system designed for restaurant and food business management.  
It handles orders, kitchen workflow, inventory, warehouse operations, and role-based access control.

---

## 🚀 Features

### 🧾 Order Management
- Create, update, and manage orders
- Order lifecycle:
  - Created → Submitted → In Preparation → Ready → Served → Paid
- Order lines tracking

### 👨‍🍳 Kitchen Module
- Kitchen screen for live order tracking
- Start preparation & mark as ready
- Only kitchen-type menu items appear

### 📦 Warehouse & Stock
- Warehouse stock management
- Stock movements tracking
- Warehouse transfer system
- Stock deduction based on recipes

### 🧪 Recipe System
- MenuItem → Recipe → Stock deduction
- Automatic inventory decrease when order is processed

### 👥 User & Role Management
- Role-based authorization (RBAC)
- Permissions system
- Admin / Waiter / Kitchen roles

### 🔔 Notifications
- System notifications
- Email integration (Warehouse transfer etc.)

### 🏢 Company Based System
- Multi-company support
- Data filtering per company

---

## 🏗️ Architecture

This project follows **Clean Architecture** principles:

- **Domain Layer** → Core business logic
- **Application Layer** → CQRS + MediatR
- **Infrastructure Layer** → Database & services
- **API Layer** → Controllers & endpoints

### ⚙️ Technologies Used

#### Backend
- ASP.NET Core Web API
- Entity Framework Core
- MediatR (CQRS pattern)
- FluentValidation
- JWT Authentication

#### Frontend
- Next.js
- TypeScript
- TailwindCSS

#### Database
- SQL Server

#### DevOps / Tools
- Docker (docker-compose)
- Git & GitHub

---

## 📂 Project Structure
