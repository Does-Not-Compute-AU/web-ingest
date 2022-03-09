#!/bin/bash
# Bash Menu Script Example
PS3='Please Choose: '
options=("Clean Project" "Start DevStack" "Recreate Databases" "Recreate Migrations" "Build Docker Image" "Quit")
select opt in "${options[@]}"
do
    case $opt in
    
        "Start DevStack")
            docker-compose -p "web-ingest" up -d 
            
            #docker exec -it web-ingest_superset_1 superset fab create-admin --username admin --firstname Superset --lastname Admin --email admin@superset.com --password admin
            #docker exec -it web-ingest_superset_1 superset db upgrade
            #docker exec -it web-ingest_superset_1 superset load_examples
            #docker exec -it web-ingest_superset_1 superset init
        ;;
    
        "Clean Project")

            echo "Removing \"bin\" folders"
            find . -iname "bin" | xargs rm -rf
            echo "Removing \"obj\" folders"
            find . -iname "obj" | xargs rm -rf
            
            ;;
          
        "Recreate Databases")
            
            #stop and remove persistence containers
            echo "Stopping Containers"
            docker stop web-ingest_redis_1
            docker stop web-ingest_pg_1
            echo "Removing Containers"
            docker rm web-ingest_redis_1
            docker rm web-ingest_pg_1
            
            docker-compose -p "web-ingest" up -d 
            
            #wait for containers init
            sleep 2s
            
            ;;
            
        "Recreate Migrations")
            
            #remove migrations folder
            rm -rf ./WebIngest.Core/Migrations/
            echo "Deleted Current Migrations Folder"
            
            #create new migration called Init as at current state
            cd ./WebIngest.WebAPI/ || exit
            echo "Recreating EF Init Migration"
            dotnet ef migrations add Init --project ../WebIngest.Core
        
            ;;
            
        "Build Docker Image")
        
            docker build --tag web-ingest:latest .
            
            ;;
            
            
        "Quit")
            break
            ;;
        *) echo "invalid option $REPLY";;
    esac
done