#!/bin/bash

# SPDX-License-Identifier: Apache-2.0
# Licensed to the Ed-Fi Alliance under one or more agreements.
# The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
# See the LICENSE and NOTICES files in the project root for more information.

set -e
set +x

openssl dhparam -out dhparam.pem 4096
openssl req -x509 -newkey rsa:4096 -nodes -keyout server.key -out server.crt -days 365 -addext "subjectAltName = DNS:localhost,DNS:nginx"
