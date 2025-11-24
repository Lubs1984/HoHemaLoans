# Ho Hema Loans - Project Structure

This document outlines the complete project structure for the Ho Hema Loans platform.

## 📁 Root Directory Structure

```
HoHema/
├── docs/                          # Documentation
│   ├── api-specifications.md      # API documentation
│   ├── database-schema.md         # Database schema
│   ├── deployment-guide.md        # Deployment instructions
│   ├── implementation-checklist.md # Implementation checklist
│   └── requirements-analysis.md   # Requirements and architecture
│
├── src/                           # Source code
│   ├── frontend/                  # React + TypeScript frontend
│   └── api/                       # .NET Core Web API
│
├── deploy/                        # Deployment configurations
│   ├── docker/                    # Docker files
│   ├── azure/                     # Azure ARM templates
│   └── sql/                       # Database scripts
│
├── tests/                         # Test suites
│   ├── frontend/                  # Frontend tests
│   ├── api/                       # API tests
│   └── integration/               # Integration tests
│
├── scripts/                       # Utility scripts
│   ├── setup/                     # Development setup scripts
│   └── maintenance/               # Maintenance scripts
│
├── .gitignore                     # Git ignore rules
├── README.md                      # Project overview
└── docker-compose.yml             # Development environment
```

## 🎨 Frontend Structure (React + Vite + TypeScript)

```
src/frontend/
├── public/                        # Static assets
│   ├── favicon.ico
│   └── manifest.json
│
├── src/
│   ├── components/                # Reusable components
│   │   ├── common/                # Common UI components
│   │   ├── forms/                 # Form components
│   │   └── layout/                # Layout components
│   │
│   ├── pages/                     # Page components
│   │   ├── auth/                  # Authentication pages
│   │   ├── dashboard/             # Dashboard pages
│   │   ├── loans/                 # Loan application pages
│   │   └── profile/               # Profile management
│   │
│   ├── hooks/                     # Custom React hooks
│   ├── services/                  # API service layer
│   ├── store/                     # Zustand state management
│   ├── types/                     # TypeScript type definitions
│   ├── utils/                     # Utility functions
│   ├── styles/                    # Global styles and themes
│   │
│   ├── App.tsx                    # Main app component
│   ├── main.tsx                   # Application entry point
│   └── vite-env.d.ts             # Vite type definitions
│
├── package.json                   # Dependencies and scripts
├── tsconfig.json                  # TypeScript configuration
├── vite.config.ts                 # Vite configuration
├── tailwind.config.js             # Tailwind CSS configuration
└── .env.example                   # Environment variables template
```

## 🔧 Backend Structure (.NET Core Web API)

```
src/api/
├── HoHema.Api/                    # Web API project
│   ├── Controllers/               # API controllers
│   ├── Middleware/                # Custom middleware
│   ├── Configuration/             # Startup configurations
│   ├── Properties/                # Project properties
│   ├── Program.cs                 # Application entry point
│   └── appsettings.json          # Configuration settings
│
├── HoHema.Core/                   # Domain layer
│   ├── Entities/                  # Domain entities
│   ├── Interfaces/                # Service interfaces
│   ├── Services/                  # Business logic services
│   ├── DTOs/                      # Data transfer objects
│   └── Constants/                 # Application constants
│
├── HoHema.Infrastructure/         # Infrastructure layer
│   ├── Data/                      # Database context and configurations
│   ├── Repositories/              # Repository implementations
│   ├── Services/                  # External service implementations
│   └── Migrations/                # Entity Framework migrations
│
└── HoHema.sln                    # Solution file
```

## 🐳 Deployment Structure

```
deploy/
├── docker/                        # Docker configurations
│   ├── frontend/
│   │   ├── Dockerfile
│   │   └── nginx.conf
│   └── api/
│       └── Dockerfile
│
├── azure/                         # Azure deployment
│   ├── arm-templates/             # ARM templates
│   ├── bicep/                     # Bicep files
│   └── pipelines/                 # Azure DevOps pipelines
│
└── sql/                          # Database deployment
    ├── schema/                    # Schema creation scripts
    ├── data/                      # Initial data scripts
    └── migrations/                # Migration scripts
```

## 🧪 Testing Structure

```
tests/
├── frontend/                      # Frontend tests
│   ├── unit/                      # Unit tests
│   ├── integration/               # Integration tests
│   └── e2e/                       # End-to-end tests
│
├── api/                          # Backend tests
│   ├── HoHema.Api.Tests/         # API layer tests
│   ├── HoHema.Core.Tests/        # Business logic tests
│   └── HoHema.Integration.Tests/  # Integration tests
│
└── performance/                   # Performance tests
    └── k6/                        # k6 performance scripts
```

## 📜 Scripts Structure

```
scripts/
├── setup/                         # Development setup
│   ├── install-dependencies.sh    # Install all dependencies
│   ├── setup-database.sh         # Setup local database
│   └── generate-certificates.sh   # Generate dev certificates
│
└── maintenance/                   # Maintenance utilities
    ├── backup-database.sh         # Database backup
    ├── update-migrations.sh       # Update EF migrations
    └── health-check.sh            # System health check
```

This structure provides clear separation of concerns, follows industry best practices, and supports the full development lifecycle from local development to production deployment.