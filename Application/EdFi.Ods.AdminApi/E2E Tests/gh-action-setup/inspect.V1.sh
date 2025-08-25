#! /bin/bash
end=$((SECONDS+ 5 * 60))

until [[ "`docker inspect -f {{.State.Health.Status}} adminapi`" == "healthy" || $SECONDS -gt $end ]]; do
    sleep 2;
done;

if [ "`docker inspect -f {{.State.Health.Status}} adminapi`" == "healthy" ]
then
    echo "--- Container is healthy ---"
else
    docker ps
    docker logs adminapi --tail 50
    echo "--- Operation timed out. Review container status ---"
    exit 1
fi

status=`curl -k --silent --output /dev/null -w '%{http_code}' https://localhost/adminapi`

if [[ $status -eq "200" ]]
then
    echo "--- Admin API application is running ---"
else
    echo "--- Admin API application is failing with status code ${status}"
    exit 2
fi