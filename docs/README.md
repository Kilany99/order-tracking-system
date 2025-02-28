# System Architecture

This directory contains the architectural diagrams for the Order Tracking System.

## Overview
The system consists of three main services:
- Order Service: Handles order management and tracking
- Driver Service: Manages driver assignments and locations
- Notification Service: Handles system notifications

## Diagrams
- [System Overview](architecture/system-overview.puml)
- [Order Service Architecture](architecture/order-service.puml)
- [Driver Service Architecture](architecture/driver-service.puml)
- [Notification Service Architecture](architecture/notification-service.puml)
- [Monitoring Architecture](architecture/monitoring.puml)

## Technology Stack
- ASP.NET Core
- PostgreSQL
- MongoDB
- Redis
- Kafka
- Prometheus
- Grafana

## Communication
- REST APIs for service-to-service communication
- WebSocket for real-time updates
- Kafka for event-driven communication
- Redis for caching