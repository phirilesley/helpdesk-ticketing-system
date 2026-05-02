#!/bin/bash

# HelpDesk System Deployment Script
# This script automates the deployment of the HelpDesk System

set -e

echo "🚀 Starting HelpDesk System Deployment..."

# Configuration
PROJECT_NAME="helpdesk-system"
DOCKER_REGISTRY="your-registry.com"
ENVIRONMENT=${ENVIRONMENT:-production}
VERSION=${VERSION:-latest}

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed. Please install Docker first."
        exit 1
    fi
    
    # Check Docker Compose
    if ! command -v docker-compose &> /dev/null; then
        log_error "Docker Compose is not installed. Please install Docker Compose first."
        exit 1
    fi
    
    # Check .NET SDK
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET SDK is not installed. Please install .NET SDK first."
        exit 1
    fi
    
    # Check Node.js
    if ! command -v node &> /dev/null; then
        log_error "Node.js is not installed. Please install Node.js first."
        exit 1
    fi
    
    log_success "All prerequisites are installed!"
}

# Build applications
build_applications() {
    log_info "Building applications..."
    
    # Build API
    log_info "Building API..."
    cd HelpDeskSystem.API
    dotnet restore
    dotnet build -c Release --no-restore
    log_success "API built successfully!"
    
    # Build Web
    log_info "Building Web..."
    cd ../HelpDeskSystem.Web
    npm install
    npm run build
    log_success "Web built successfully!"
    
    cd ..
}

# Run database migrations
run_migrations() {
    log_info "Running database migrations..."
    
    # Wait for SQL Server to be ready
    log_info "Waiting for SQL Server to be ready..."
    until docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "HelpDesk123!@#" -Q "SELECT 1" &> /dev/null; do
        log_info "SQL Server is not ready yet. Waiting..."
        sleep 5
    done
    
    # Run migrations
    cd HelpDeskSystem.API
    dotnet ef database update --connection "Server=sqlserver,1433;Database=HelpDeskSystem;User Id=sa;Password=HelpDesk123!@#;TrustServerCertificate=false;MultipleActiveResultSets=true"
    log_success "Database migrations completed!"
    
    cd ..
}

# Deploy with Docker Compose
deploy_docker() {
    log_info "Deploying with Docker Compose..."
    
    # Stop existing containers
    docker-compose down
    
    # Build and start containers
    docker-compose up --build -d
    
    # Wait for services to be ready
    log_info "Waiting for services to be ready..."
    sleep 30
    
    # Check service health
    check_service_health
    
    log_success "Deployment completed successfully!"
}

# Check service health
check_service_health() {
    log_info "Checking service health..."
    
    # Check API health
    API_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/health || echo "000")
    if [ "$API_HEALTH" = "200" ]; then
        log_success "API is healthy!"
    else
        log_warning "API health check failed (HTTP $API_HEALTH)"
    fi
    
    # Check Web health
    WEB_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:3000 || echo "000")
    if [ "$WEB_HEALTH" = "200" ]; then
        log_success "Web is healthy!"
    else
        log_warning "Web health check failed (HTTP $WEB_HEALTH)"
    fi
}

# Setup SSL certificates
setup_ssl() {
    log_info "Setting up SSL certificates..."
    
    # Create SSL directory
    mkdir -p nginx/ssl
    
    # Generate self-signed certificate for development
    if [ "$ENVIRONMENT" = "development" ]; then
        log_info "Generating self-signed SSL certificate..."
        openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
            -keyout nginx/ssl/key.pem \
            -out nginx/ssl/cert.pem \
            -subj "/C=US/ST=State/L=City/O=Organization/CN=localhost"
        log_success "Self-signed SSL certificate generated!"
    else
        log_warning "Please provide your SSL certificates in nginx/ssl/ directory"
    fi
}

# Create necessary directories
create_directories() {
    log_info "Creating necessary directories..."
    
    mkdir -p logs
    mkdir -p uploads
    mkdir -p nginx/ssl
    
    # Set permissions
    chmod 755 logs uploads nginx/ssl
    chmod +x deploy.sh
    
    log_success "Directories created successfully!"
}

# Show deployment status
show_status() {
    log_info "Deployment Status:"
    echo "===================="
    
    echo "Running containers:"
    docker-compose ps
    
    echo ""
    echo "Service URLs:"
    echo "  🌐 Frontend: http://localhost:3000"
    echo "  🔧 API: http://localhost:5000"
    echo "  📊 Hangfire Dashboard: http://localhost:5002"
    echo "  📝 Nginx Proxy: http://localhost"
    
    echo ""
    echo "Database:"
    echo "  🗄️ SQL Server: localhost:1433"
    echo "  📦 Redis: localhost:6379"
    
    echo "===================="
}

# Cleanup function
cleanup() {
    log_info "Cleaning up..."
    docker-compose down -v
    docker system prune -f
    log_success "Cleanup completed!"
}

# Show logs
show_logs() {
    log_info "Showing logs..."
    docker-compose logs -f
}

# Main deployment function
main() {
    case "${1:-deploy}" in
        "deploy")
            check_prerequisites
            create_directories
            setup_ssl
            build_applications
            deploy_docker
            show_status
            ;;
        "build")
            build_applications
            ;;
        "migrate")
            run_migrations
            ;;
        "cleanup")
            cleanup
            ;;
        "logs")
            show_logs
            ;;
        "status")
            show_status
            ;;
        "health")
            check_service_health
            ;;
        *)
            echo "Usage: $0 {deploy|build|migrate|cleanup|logs|status|health}"
            echo ""
            echo "Commands:"
            echo "  deploy    - Full deployment (default)"
            echo "  build     - Build applications only"
            echo "  migrate   - Run database migrations only"
            echo "  cleanup   - Stop and remove all containers"
            echo "  logs      - Show application logs"
            echo "  status    - Show deployment status"
            echo "  health    - Check service health"
            exit 1
            ;;
    esac
}

# Run main function with all arguments
main "$@"
