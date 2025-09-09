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

status=`wget -nv -t1 --spider -S https://localhost/adminapi/health --no-check-certificate 2>&1|grep "HTTP/"|awk '{print $2}'`

if [[ $status -eq "200" ]]
then
    echo "--- Admin API application is running ---"
else
    echo "--- Admin API application is failing with status code ${status}"
    exit 2
fi