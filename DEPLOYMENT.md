# Deployment Guide

This guide covers various deployment options for TP-Link WebUI.

## 🐳 Docker Deployment (Recommended)

### Prerequisites
- Docker and Docker Compose installed
- TP-Link switch accessible from deployment server

### Quick Start
```bash
# Clone the repository
git clone https://github.com/your-username/tplink-webui.git
cd tplink-webui

# Start with Docker Compose
docker-compose up -d

# Access the application
# Frontend: http://localhost
# Backend API: http://localhost:5000
```

### Production Configuration
1. Copy environment template:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` with production values:
   ```bash
   ASPNETCORE_ENVIRONMENT=Production
   ASPNETCORE_URLS=https://+:5001
   CORS_ORIGINS=https://your-domain.com
   ```

3. Configure SSL certificates (recommended):
   ```yaml
   # Add to docker-compose.yml
   volumes:
     - ./certs:/app/certs
   environment:
     - ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/cert.pfx
     - ASPNETCORE_Kestrel__Certificates__Default__Password=your-cert-password
   ```

## 🖥️ Manual Deployment

### Backend (.NET)
```bash
# Build and publish
cd backend
dotnet publish -c Release -o ./publish

# Run
cd publish
dotnet TPLinkWebUI.dll --urls="http://localhost:5000"
```

### Frontend (React)
```bash
# Build
cd frontend
npm install
npm run build

# Serve with nginx or any web server
# Point document root to ./dist
```

## ☁️ Cloud Deployment

### Azure App Service
1. Create App Service with .NET 9.0 runtime
2. Configure application settings:
   ```
   ASPNETCORE_ENVIRONMENT=Production
   CORS_ORIGINS=https://your-app.azurewebsites.net
   ```
3. Deploy using GitHub Actions or Azure DevOps

### AWS Elastic Beanstalk
1. Create .NET application environment
2. Package application using `dotnet publish`
3. Upload and deploy

### Google Cloud Run
```bash
# Build and push container
docker build -t gcr.io/your-project/tplink-webui-backend ./backend
docker push gcr.io/your-project/tplink-webui-backend

# Deploy
gcloud run deploy tplink-webui \
  --image gcr.io/your-project/tplink-webui-backend \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated
```

## 🔧 Configuration

### Environment Variables
```bash
# Backend
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:5001
LOG_LEVEL=Information
CORS_ORIGINS=https://your-domain.com

# Security
COOKIE_SECURE=true
COOKIE_SAME_SITE=Strict
ALLOWED_HOSTS=your-domain.com
```

### Application Settings
Create `appsettings.Production.json`:
```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "/var/log/tplink-webui/app-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  },
  "AllowedHosts": "your-domain.com"
}
```

## 🔒 Security Considerations

### HTTPS Configuration
```bash
# Generate self-signed certificate for testing
dotnet dev-certs https --export-path ./certs/cert.pfx --password your-password

# For production, use certificates from a trusted CA
```

### Firewall Rules
```bash
# Allow only necessary ports
ufw allow 80/tcp
ufw allow 443/tcp
ufw deny 5000/tcp  # Block direct backend access
```

### Reverse Proxy (Nginx)
```nginx
server {
    listen 80;
    server_name your-domain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name your-domain.com;
    
    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;
    
    # Frontend
    location / {
        root /var/www/tplink-webui;
        try_files $uri $uri/ /index.html;
    }
    
    # Backend API
    location /api/ {
        proxy_pass http://localhost:5000/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

## 📊 Monitoring

### Health Checks
```bash
# Backend health
curl http://localhost:5000/api/health

# Frontend availability
curl http://localhost/
```

### Log Monitoring
```bash
# View application logs
tail -f logs/tplink-webui-$(date +%Y-%m-%d).log

# Monitor errors
tail -f logs/errors-$(date +%Y-%m-%d).log
```

### System Monitoring
- Monitor CPU and memory usage
- Set up alerts for application errors
- Monitor switch connectivity
- Track response times

## 🔄 Updates and Maintenance

### Update Process
```bash
# Pull latest changes
git pull origin main

# Rebuild and restart
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

### Backup Strategy
```bash
# Backup configuration and logs
tar -czf backup-$(date +%Y%m%d).tar.gz \
  logs/ \
  data/ \
  .env \
  docker-compose.yml
```

### Log Rotation
```bash
# Add to crontab for log cleanup
0 2 * * * find /app/logs -name "*.log" -mtime +30 -delete
```

## 🐛 Troubleshooting

### Common Issues
1. **Connection refused**: Check firewall and port configuration
2. **CORS errors**: Verify CORS_ORIGINS setting
3. **SSL errors**: Check certificate configuration
4. **High memory usage**: Monitor for memory leaks

### Debug Mode
```bash
# Enable debug logging
export ASPNETCORE_ENVIRONMENT=Development
export LOG_LEVEL=Debug
```

### Container Debugging
```bash
# View container logs
docker logs tplink-webui-backend
docker logs tplink-webui-frontend

# Access container shell
docker exec -it tplink-webui-backend /bin/bash
```

## 📞 Support

For deployment issues:
1. Check the logs in `logs/` directory
2. Review configuration settings
3. Consult the troubleshooting section
4. Open an issue on GitHub with deployment details

---

**Note**: Always test deployments in a staging environment before production.