#!/bin/bash

docker-compose -f docker-compose.yml -f docker-compose.prod.yml -p "web-ingest-prod" up -d 