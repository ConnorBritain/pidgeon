# Segmint HL7 Deployment Guide

Comprehensive deployment guide for Segmint HL7 platform in various environments.

## Table of Contents

- [Overview](#overview)
- [Installation Methods](#installation-methods)
- [Environment Configuration](#environment-configuration)
- [Production Deployment](#production-deployment)
- [Docker Deployment](#docker-deployment)
- [Cloud Deployment](#cloud-deployment)
- [Security Configuration](#security-configuration)
- [Monitoring and Logging](#monitoring-and-logging)
- [Maintenance](#maintenance)

## Overview

Segmint HL7 can be deployed in multiple configurations:

- **Standalone CLI**: Single-user command-line interface
- **Team Installation**: Multi-user shared configuration
- **Enterprise Deployment**: Centralized configuration management
- **Cloud Service**: Scalable cloud-based deployment
- **Container Deployment**: Docker/Kubernetes orchestration

## Installation Methods

### Method 1: Development Installation

For development and testing:

```bash
# Clone repository
git clone https://github.com/ConnorBritain/segmint.git
cd segmint

# Create virtual environment
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate

# Install in development mode
pip install -e .

# Verify installation
segmint --help
python run_tests.py
```

### Method 2: Production Installation

For production environments:

```bash
# Install from PyPI (when available)
pip install segmint-hl7

# Or install from source
pip install https://github.com/ConnorBritain/segmint/archive/main.zip

# Verify installation
segmint --version
```

### Method 3: System-wide Installation

For system-wide access:

```bash
# Install globally (requires admin privileges)
sudo pip install segmint-hl7

# Create system configuration directory
sudo mkdir -p /etc/segmint
sudo chown $USER:$USER /etc/segmint

# Initialize configuration
segmint config init --system
```

## Environment Configuration

### Environment Variables

Configure Segmint using environment variables:

```bash
# Configuration
export SEGMINT_CONFIG_PATH="/path/to/config/library"
export SEGMINT_DEFAULT_FACILITY="MAIN_HOSPITAL"
export SEGMINT_LOG_LEVEL="INFO"

# AI Integration (optional)
export OPENAI_API_KEY="your-api-key"
export LANGCHAIN_TRACING_V2="true"

# Security
export SEGMINT_ENCRYPT_CONFIGS="true"
export SEGMINT_CONFIG_PASSWORD="secure-password"

# Performance
export SEGMINT_BATCH_SIZE="100"
export SEGMINT_MAX_WORKERS="4"
```

### Configuration File

Create `/etc/segmint/config.yaml` or `~/.segmint/config.yaml`:

```yaml
# Global Segmint Configuration
segmint:
  # Configuration library settings
  config_library:
    path: "/var/lib/segmint/configs"
    encryption: true
    backup_enabled: true
    backup_path: "/var/lib/segmint/backups"
    retention_days: 30
    
  # Default facility settings
  defaults:
    facility_id: "MAIN_HOSPITAL"
    sending_application: "SEGMINT"
    receiving_application: "DEFAULT"
    
  # AI Integration
  ai:
    enabled: true
    provider: "openai"
    model: "gpt-3.5-turbo"
    max_tokens: 1000
    temperature: 0.7
    
  # Validation settings
  validation:
    default_levels: ["syntax", "semantic"]
    strict_mode: false
    clinical_validation: true
    
  # Performance settings
  performance:
    batch_size: 100
    max_workers: 4
    cache_enabled: true
    cache_ttl: 3600
    
  # Logging
  logging:
    level: "INFO"
    file: "/var/log/segmint/segmint.log"
    rotation: "daily"
    retention: 30
    
  # Security
  security:
    audit_enabled: true
    audit_file: "/var/log/segmint/audit.log"
    encryption_enabled: true
    require_user_auth: false
```

### Directory Structure

Recommended production directory structure:

```
/var/lib/segmint/
├── configs/           # Configuration library
│   ├── metadata/      # Configuration metadata
│   ├── history/       # Change history
│   └── backups/       # Configuration backups
├── templates/         # Message templates
├── examples/          # Example configurations
└── cache/            # Temporary cache files

/var/log/segmint/
├── segmint.log       # Application logs
├── audit.log         # Audit trail
└── validation.log    # Validation logs

/etc/segmint/
├── config.yaml       # Main configuration
├── certificates/     # SSL certificates
└── secrets/          # Encrypted secrets
```

## Production Deployment

### System Service Setup

Create systemd service for continuous operation:

```ini
# /etc/systemd/system/segmint.service
[Unit]
Description=Segmint HL7 Platform Service
After=network.target

[Service]
Type=simple
User=segmint
Group=segmint
WorkingDirectory=/opt/segmint
Environment=SEGMINT_CONFIG_PATH=/var/lib/segmint/configs
ExecStart=/opt/segmint/venv/bin/python -m app.api.server
ExecReload=/bin/kill -HUP $MAINPID
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

Enable and start service:

```bash
sudo systemctl enable segmint
sudo systemctl start segmint
sudo systemctl status segmint
```

### User Management

Create dedicated user for Segmint:

```bash
# Create system user
sudo useradd -r -s /bin/false -d /opt/segmint segmint

# Create directories
sudo mkdir -p /opt/segmint /var/lib/segmint /var/log/segmint
sudo chown segmint:segmint /opt/segmint /var/lib/segmint /var/log/segmint

# Set permissions
sudo chmod 750 /var/lib/segmint
sudo chmod 755 /var/log/segmint
```

### Database Setup (Optional)

For enterprise deployments with shared configuration:

```bash
# Install PostgreSQL
sudo apt-get install postgresql postgresql-contrib

# Create database
sudo -u postgres createdb segmint
sudo -u postgres createuser segmint

# Configure access
sudo -u postgres psql -c "ALTER USER segmint PASSWORD 'secure_password';"
sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE segmint TO segmint;"
```

Update configuration:

```yaml
segmint:
  database:
    enabled: true
    type: "postgresql"
    host: "localhost"
    port: 5432
    name: "segmint"
    user: "segmint"
    password: "${SEGMINT_DB_PASSWORD}"
```

## Docker Deployment

### Dockerfile

```dockerfile
# Dockerfile
FROM python:3.11-slim

# Set working directory
WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y \
    git \
    && rm -rf /var/lib/apt/lists/*

# Copy requirements
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy application
COPY . .
RUN pip install -e .

# Create non-root user
RUN useradd -r -s /bin/false segmint
RUN chown -R segmint:segmint /app

# Switch to non-root user
USER segmint

# Expose port
EXPOSE 8000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD segmint --version || exit 1

# Start command
CMD ["python", "-m", "app.api.server"]
```

### Docker Compose

```yaml
# docker-compose.yml
version: '3.8'

services:
  segmint:
    build: .
    ports:
      - "8000:8000"
    volumes:
      - segmint_configs:/var/lib/segmint/configs
      - segmint_logs:/var/log/segmint
    environment:
      - SEGMINT_CONFIG_PATH=/var/lib/segmint/configs
      - SEGMINT_LOG_LEVEL=INFO
      - OPENAI_API_KEY=${OPENAI_API_KEY}
    depends_on:
      - postgres
    restart: unless-stopped
    
  postgres:
    image: postgres:15
    environment:
      - POSTGRES_DB=segmint
      - POSTGRES_USER=segmint
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    restart: unless-stopped
    
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - segmint
    restart: unless-stopped

volumes:
  segmint_configs:
  segmint_logs:
  postgres_data:
```

### Build and Deploy

```bash
# Build image
docker build -t segmint-hl7 .

# Run with docker-compose
docker-compose up -d

# Check status
docker-compose ps
docker-compose logs segmint
```

## Cloud Deployment

### AWS Deployment

#### EC2 Instance Setup

```bash
# Launch EC2 instance (Ubuntu 22.04 LTS)
# Configure security groups for ports 22, 80, 443

# Connect and setup
ssh -i key.pem ubuntu@ec2-instance

# Install dependencies
sudo apt update
sudo apt install python3-pip python3-venv nginx

# Clone and setup Segmint
git clone https://github.com/ConnorBritain/segmint.git
cd segmint
python3 -m venv venv
source venv/bin/activate
pip install -e .
```

#### RDS Database Setup

```bash
# Create RDS PostgreSQL instance
aws rds create-db-instance \
    --db-instance-identifier segmint-db \
    --db-instance-class db.t3.micro \
    --engine postgres \
    --master-username segmint \
    --master-user-password SecurePassword123 \
    --allocated-storage 20 \
    --vpc-security-group-ids sg-12345678
```

#### S3 Configuration Storage

```bash
# Create S3 bucket for configurations
aws s3 mb s3://segmint-configs-bucket

# Configure IAM role with S3 access
aws iam create-role --role-name segmint-ec2-role \
    --assume-role-policy-document file://trust-policy.json

aws iam attach-role-policy --role-name segmint-ec2-role \
    --policy-arn arn:aws:iam::aws:policy/AmazonS3FullAccess
```

### Azure Deployment

```bash
# Create resource group
az group create --name segmint-rg --location eastus

# Create App Service plan
az appservice plan create --name segmint-plan \
    --resource-group segmint-rg --sku B1 --is-linux

# Create web app
az webapp create --resource-group segmint-rg \
    --plan segmint-plan --name segmint-app \
    --runtime "PYTHON|3.11"

# Deploy from GitHub
az webapp deployment source config --name segmint-app \
    --resource-group segmint-rg \
    --repo-url https://github.com/ConnorBritain/segmint \
    --branch main --manual-integration
```

### Google Cloud Platform

```bash
# Set project
gcloud config set project segmint-project

# Deploy to Cloud Run
gcloud run deploy segmint \
    --source . \
    --platform managed \
    --region us-central1 \
    --allow-unauthenticated
```

## Security Configuration

### SSL/TLS Setup

Configure HTTPS with Let's Encrypt:

```bash
# Install certbot
sudo apt install certbot python3-certbot-nginx

# Obtain certificate
sudo certbot --nginx -d segmint.yourdomain.com

# Configure auto-renewal
sudo systemctl enable certbot.timer
```

### Nginx Configuration

```nginx
# /etc/nginx/sites-available/segmint
server {
    listen 80;
    server_name segmint.yourdomain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name segmint.yourdomain.com;
    
    ssl_certificate /etc/letsencrypt/live/segmint.yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/segmint.yourdomain.com/privkey.pem;
    
    # Security headers
    add_header X-Frame-Options DENY;
    add_header X-Content-Type-Options nosniff;
    add_header X-XSS-Protection "1; mode=block";
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains";
    
    location / {
        proxy_pass http://127.0.0.1:8000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Firewall Configuration

```bash
# Configure UFW firewall
sudo ufw enable
sudo ufw allow ssh
sudo ufw allow 'Nginx Full'
sudo ufw status
```

### Backup Strategy

```bash
#!/bin/bash
# /usr/local/bin/segmint-backup.sh

BACKUP_DIR="/var/backups/segmint"
DATE=$(date +%Y%m%d_%H%M%S)

# Create backup directory
mkdir -p $BACKUP_DIR

# Backup configurations
tar -czf $BACKUP_DIR/configs_$DATE.tar.gz /var/lib/segmint/configs

# Backup database (if using)
pg_dump -h localhost -U segmint segmint > $BACKUP_DIR/database_$DATE.sql

# Cleanup old backups (keep 30 days)
find $BACKUP_DIR -type f -mtime +30 -delete

# Upload to S3 (optional)
aws s3 cp $BACKUP_DIR/configs_$DATE.tar.gz s3://segmint-backups/
```

Add to crontab:

```bash
# Daily backup at 2 AM
0 2 * * * /usr/local/bin/segmint-backup.sh
```

## Monitoring and Logging

### Application Monitoring

Create monitoring script:

```python
#!/usr/bin/env python3
# /usr/local/bin/segmint-monitor.py

import requests
import smtplib
import sys
from email.mime.text import MIMEText

def check_health():
    try:
        response = requests.get('http://localhost:8000/health', timeout=10)
        return response.status_code == 200
    except:
        return False

def send_alert(message):
    msg = MIMEText(message)
    msg['Subject'] = 'Segmint Alert'
    msg['From'] = 'monitor@yourdomain.com'
    msg['To'] = 'admin@yourdomain.com'
    
    smtp = smtplib.SMTP('localhost')
    smtp.send_message(msg)
    smtp.quit()

if __name__ == "__main__":
    if not check_health():
        send_alert("Segmint HL7 service is down!")
        sys.exit(1)
```

### Log Rotation

Configure logrotate:

```
# /etc/logrotate.d/segmint
/var/log/segmint/*.log {
    daily
    missingok
    rotate 30
    compress
    delaycompress
    notifempty
    create 0644 segmint segmint
    postrotate
        systemctl reload segmint
    endscript
}
```

### Metrics Collection

Set up Prometheus monitoring:

```yaml
# prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'segmint'
    static_configs:
      - targets: ['localhost:8000']
    metrics_path: '/metrics'
```

## Maintenance

### Update Procedure

```bash
#!/bin/bash
# /usr/local/bin/segmint-update.sh

# Stop service
sudo systemctl stop segmint

# Backup current installation
sudo cp -r /opt/segmint /opt/segmint.backup.$(date +%Y%m%d)

# Update code
cd /opt/segmint
sudo -u segmint git pull origin main
sudo -u segmint pip install -e .

# Run tests
sudo -u segmint python run_tests.py

# Restart service
sudo systemctl start segmint

# Verify health
sleep 10
curl -f http://localhost:8000/health || exit 1

echo "Update completed successfully"
```

### Health Checks

Create comprehensive health check:

```python
def health_check():
    checks = {
        'database': check_database_connection(),
        'config_library': check_config_library_access(),
        'ai_service': check_ai_service(),
        'disk_space': check_disk_space(),
        'memory': check_memory_usage()
    }
    
    all_healthy = all(checks.values())
    
    return {
        'status': 'healthy' if all_healthy else 'degraded',
        'checks': checks,
        'timestamp': datetime.utcnow().isoformat()
    }
```

### Performance Tuning

Monitor and optimize:

```bash
# Monitor resource usage
top -p $(pgrep -f segmint)
iostat -x 1
free -h

# Optimize Python
export PYTHONOPTIMIZE=1
export PYTHONDONTWRITEBYTECODE=1

# Configure worker processes
export SEGMINT_MAX_WORKERS=4
export SEGMINT_BATCH_SIZE=200
```

This deployment guide provides comprehensive instructions for deploying Segmint HL7 in production environments with proper security, monitoring, and maintenance procedures.