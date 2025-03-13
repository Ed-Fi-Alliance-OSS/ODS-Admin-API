// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Settings;

public static class FileHelpers
{
    public static byte[] ComputeHash(string filePath)
    {
        var runCount = 1;

        while (runCount < 4)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    using var fs = File.OpenRead(filePath);
                    return System.Security.Cryptography.SHA1
                        .Create().ComputeHash(fs);
                }
                else
                {
                    throw new FileNotFoundException();
                }
            }
            catch (IOException)
            {
                if (runCount == 3)
                {
                    throw;
                }

                Thread.Sleep(TimeSpan.FromSeconds(Math.Pow(2, runCount)));
                runCount++;
            }
        }

        return new byte[20];
    }
}
