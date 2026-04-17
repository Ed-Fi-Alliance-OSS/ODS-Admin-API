#! /bin/bash
end=$((SECONDS+ 5 * 60))

echo $1

until [[ "`docker inspect -f {{.State.Health.Status}} $1`" == "healthy" || $SECONDS -gt $end ]]; do
    sleep 2;
done;

if [ "`docker inspect -f {{.State.Health.Status}} $1`" == "healthy" ]
then
    echo "--- Container is healthy ---"
else
    docker ps
    docker logs ed-fi-ods-api --tail 50
    echo "--- Operation timed out. Review container status ---"
    exit 1
fi

status=`curl -s -o /dev/null -w "%{http_code}" -k https://localhost/api/health`
if [[ $status -eq "200" ]]
then
    echo "--- Ods API application is running ---"
else
    echo "--- Ods API application is failing with status code ${status}"
    exit 2
fi