#!/bin/bash
echo "Starting TP-Link WebUI Backend..."
cd backend
dotnet run --urls="http://localhost:5000"