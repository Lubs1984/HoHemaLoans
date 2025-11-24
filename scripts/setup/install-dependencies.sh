#!/bin/bash

# Ho Hema Loans - Development Environment Setup
# This script installs all necessary dependencies for the development environment

set -e

echo "🏦 Ho Hema Loans - Development Environment Setup"
echo "================================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if running on macOS
if [[ "$OSTYPE" != "darwin"* ]]; then
    print_error "This script is designed for macOS. Please adapt for your OS."
    exit 1
fi

# Check if Homebrew is installed
if ! command -v brew &> /dev/null; then
    print_status "Installing Homebrew..."
    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
else
    print_success "Homebrew is already installed"
fi

# Update Homebrew
print_status "Updating Homebrew..."
brew update

# Install Node.js
if ! command -v node &> /dev/null || [[ $(node --version) != "v18"* ]]; then
    print_status "Installing Node.js 18..."
    brew install node@18
    brew link --force --overwrite node@18
else
    print_success "Node.js 18 is already installed"
fi

# Verify Node.js installation
NODE_VERSION=$(node --version)
print_success "Node.js version: $NODE_VERSION"

# Install .NET 8
if ! command -v dotnet &> /dev/null || ! dotnet --list-sdks | grep -q "8."; then
    print_status "Installing .NET 8 SDK..."
    brew install --cask dotnet-sdk
else
    print_success ".NET 8 SDK is already installed"
fi

# Verify .NET installation
DOTNET_VERSION=$(dotnet --version)
print_success ".NET version: $DOTNET_VERSION"

# Install Docker Desktop
if ! command -v docker &> /dev/null; then
    print_status "Installing Docker Desktop..."
    brew install --cask docker
    print_warning "Please start Docker Desktop manually after installation"
else
    print_success "Docker is already installed"
fi

# Install SQL Server tools (optional)
if ! command -v sqlcmd &> /dev/null; then
    print_status "Installing SQL Server command line tools..."
    brew install microsoft/mssql/mssql-tools18
else
    print_success "SQL Server tools are already installed"
fi

# Install Git (if not already installed)
if ! command -v git &> /dev/null; then
    print_status "Installing Git..."
    brew install git
else
    print_success "Git is already installed"
fi

# Install Visual Studio Code (optional)
if ! command -v code &> /dev/null; then
    print_status "Installing Visual Studio Code..."
    brew install --cask visual-studio-code
else
    print_success "Visual Studio Code is already installed"
fi

# Install useful VS Code extensions
print_status "Installing recommended VS Code extensions..."
code --install-extension ms-dotnettools.csharp
code --install-extension bradlc.vscode-tailwindcss
code --install-extension esbenp.prettier-vscode
code --install-extension ms-vscode.vscode-typescript-next
code --install-extension ms-vscode-remote.remote-containers
code --install-extension ms-mssql.mssql

print_success "VS Code extensions installed"

# Navigate to project directory
cd "$(dirname "$0")/../.."

# Install frontend dependencies
if [ -f "src/frontend/package.json" ]; then
    print_status "Installing frontend dependencies..."
    cd src/frontend
    npm install
    cd ../..
    print_success "Frontend dependencies installed"
else
    print_warning "Frontend package.json not found. Skipping frontend dependency installation."
fi

# Restore .NET packages
if [ -f "src/api/HoHema.sln" ]; then
    print_status "Restoring .NET packages..."
    cd src/api
    dotnet restore
    cd ../..
    print_success ".NET packages restored"
else
    print_warning ".NET solution file not found. Skipping .NET package restoration."
fi

# Create environment files from examples
print_status "Creating environment files..."

if [ -f "src/frontend/.env.example" ] && [ ! -f "src/frontend/.env" ]; then
    cp src/frontend/.env.example src/frontend/.env
    print_success "Frontend .env file created"
fi

if [ -f "src/api/HoHema.Api/appsettings.Development.example.json" ] && [ ! -f "src/api/HoHema.Api/appsettings.Development.json" ]; then
    cp src/api/HoHema.Api/appsettings.Development.example.json src/api/HoHema.Api/appsettings.Development.json
    print_success "API development settings created"
fi

# Create certificates directory
mkdir -p deploy/certificates
print_success "Certificates directory created"

# Generate development certificates
print_status "Generating development certificates..."
dotnet dev-certs https --trust
print_success "Development certificates generated and trusted"

echo ""
echo "🎉 Development environment setup complete!"
echo ""
echo "Next steps:"
echo "1. Start Docker Desktop"
echo "2. Run 'docker-compose up -d' to start the development environment"
echo "3. Open the project in Visual Studio Code: 'code .'"
echo ""
echo "Access URLs:"
echo "• Frontend: http://localhost:5173"
echo "• API: http://localhost:5000"
echo "• API Docs: http://localhost:5000/swagger"
echo "• Database Admin: http://localhost:8080"
echo "• Redis Admin: http://localhost:8081"
echo ""
print_success "Happy coding! 🚀"