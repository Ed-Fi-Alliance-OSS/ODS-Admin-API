#!/bin/bash
# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.
#
# 2>&1
/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P $SA_PASSWORD -Q "SELECT 1" > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "MSSQL is ready..."
    exit 0
else
    echo "MSSQL is not ready..."
    exit 1
fi
